using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Dex.MassTransit.Rabbit;

/// <summary>
/// Маскирует значения полей тела по именам: <see cref="IMessageDataMasker"/> по умолчанию, пакетом не регистрируется.
/// </summary>
/// <remarks>
/// Имя сравнивается без учёта регистра и разделителей <c>_</c>, <c>-</c>, <c>.</c>: фрагмент — по вхождению, точное имя —
/// целиком (короткие имена по вхождению задели бы <c>Shipping</c> и <c>Mapping</c>). Значение под таким именем, включая
/// объект и массив, заменяется целиком. Копирование идёт по токенам, поэтому ни оборванный на входе секрет, ни значение,
/// не влезшее в лимит, в результат не попадают даже началом.
/// </remarks>
public sealed class SensitiveNamesMessageDataMasker : IMessageDataMasker
{
    /// <summary>
    /// Значение на месте замаскированного.
    /// </summary>
    public const string MaskedValue = "***";

    private const string TruncationMark = "...";

    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        SkipValidation = true
    };

    private readonly string[] _nameFragments;
    private readonly HashSet<string> _exactNames;

    /// <summary>
    /// Маска по списку по умолчанию: <see cref="DefaultNameFragments"/> и <see cref="DefaultExactNames"/>.
    /// </summary>
    public SensitiveNamesMessageDataMasker()
        : this(DefaultNameFragments, DefaultExactNames)
    {
    }

    /// <remarks>
    /// Закрыт намеренно: публичный конструктор со списками контейнер выбрал бы как самый полный и подставил бы
    /// пустые <see cref="IEnumerable{T}"/> — маскировщик из контейнера не маскировал бы ничего.
    /// </remarks>
    private SensitiveNamesMessageDataMasker(IEnumerable<string> nameFragments, IEnumerable<string> exactNames)
    {
        _nameFragments = nameFragments.Select(Normalize).Where(x => x.Length > 0).Distinct().ToArray();
        _exactNames = exactNames.Select(Normalize).Where(x => x.Length > 0).ToHashSet();
    }

    /// <summary>
    /// Фрагменты имён по умолчанию.
    /// </summary>
    public static IReadOnlyList<string> DefaultNameFragments { get; } =
        ["password", "passwd", "secret", "token", "apikey", "authorization", "credential", "privatekey"];

    /// <summary>
    /// Точные имена по умолчанию.
    /// </summary>
    public static IReadOnlyList<string> DefaultExactNames { get; } = ["pin", "pwd", "cvv", "cvc", "otp"];

    /// <summary>
    /// Маска по своему списку имён вместо списка по умолчанию.
    /// </summary>
    /// <param name="nameFragments">Фрагменты, при вхождении которых в имя значение маскируется.</param>
    /// <param name="exactNames">Имена, совпадение с которыми целиком маскирует значение.</param>
    public static SensitiveNamesMessageDataMasker Create(IEnumerable<string> nameFragments, IEnumerable<string> exactNames)
    {
        ArgumentNullException.ThrowIfNull(nameFragments);
        ArgumentNullException.ThrowIfNull(exactNames);

        return new SensitiveNamesMessageDataMasker(nameFragments, exactNames);
    }

    /// <inheritdoc />
    public string Mask(ReadOnlySpan<byte> json, bool isComplete, int limit)
    {
        var output = new ArrayBufferWriter<byte>(Math.Clamp(Math.Min(json.Length, limit), 1, 64 * 1024));
        var complete = Copy(json, isComplete, Math.Max(0, limit), output, out var length);

        return Encoding.UTF8.GetString(output.WrittenSpan[..length]) + (complete ? string.Empty : TruncationMark);
    }

    /// <summary>
    /// Копирует токены до конца входа или до лимита; истина — вход записан целиком.
    /// </summary>
    /// <param name="json">Тело или его начало.</param>
    /// <param name="isComplete">Ложь — передано только начало тела.</param>
    /// <param name="limit">Предельный размер вывода в байтах.</param>
    /// <param name="output">Куда писать.</param>
    /// <param name="length">Длина результата: вывод до последнего токена, уложившегося в лимит.</param>
    private bool Copy(ReadOnlySpan<byte> json, bool isComplete, int limit, ArrayBufferWriter<byte> output, out int length)
    {
        var reader = new Utf8JsonReader(json, isComplete, default);
        using var writer = new Utf8JsonWriter(output, WriterOptions);
        length = 0;

        while (reader.Read())
        {
            var whole = reader.TokenType == JsonTokenType.PropertyName && IsSensitive(reader.GetString())
                ? CopyMasked(ref reader, writer)
                : CopyToken(ref reader, writer);

            writer.Flush();

            if (output.WrittenCount > limit)
                return false;

            length = output.WrittenCount;

            if (!whole)
                return false;
        }

        return isComplete;
    }

    /// <summary>
    /// Пишет имя и маску вместо значения; ложь — значение оборвано на входе.
    /// </summary>
    private static bool CopyMasked(ref Utf8JsonReader reader, Utf8JsonWriter writer)
    {
        WritePropertyName(ref reader, writer);
        writer.WriteStringValue(MaskedValue);

        if (!reader.Read())
            return false;

        return reader.TokenType is not (JsonTokenType.StartObject or JsonTokenType.StartArray) || reader.TrySkip();
    }

    private static bool CopyToken(ref Utf8JsonReader reader, Utf8JsonWriter writer)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.StartObject:
                writer.WriteStartObject();
                break;
            case JsonTokenType.EndObject:
                writer.WriteEndObject();
                break;
            case JsonTokenType.StartArray:
                writer.WriteStartArray();
                break;
            case JsonTokenType.EndArray:
                writer.WriteEndArray();
                break;
            case JsonTokenType.PropertyName:
                WritePropertyName(ref reader, writer);
                break;
            case JsonTokenType.String:
                if (reader.ValueIsEscaped)
                    writer.WriteStringValue(reader.GetString());
                else
                    writer.WriteStringValue(reader.ValueSpan);
                break;
            case JsonTokenType.Number:
                writer.WriteRawValue(reader.ValueSpan, skipInputValidation: true);
                break;
            case JsonTokenType.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonTokenType.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonTokenType.Null:
                writer.WriteNullValue();
                break;
        }

        return true;
    }

    private static void WritePropertyName(ref Utf8JsonReader reader, Utf8JsonWriter writer)
    {
        if (reader.ValueIsEscaped)
            writer.WritePropertyName(reader.GetString()!);
        else
            writer.WritePropertyName(reader.ValueSpan);
    }

    private bool IsSensitive(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        var normalized = Normalize(name);

        return _exactNames.Contains(normalized) || _nameFragments.Any(x => normalized.Contains(x, StringComparison.Ordinal));
    }

    private static string Normalize(string name)
    {
        var builder = new StringBuilder(name.Length);

        foreach (var c in name)
        {
            if (c is not ('_' or '-' or '.'))
                builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }
}