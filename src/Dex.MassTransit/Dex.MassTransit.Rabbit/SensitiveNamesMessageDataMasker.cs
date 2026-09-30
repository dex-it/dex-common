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
/// Фрагмент ищется по вхождению в имя без учёта регистра и разделителей (<c>_ - .</c>, пробел); слово — среди слов имени, разбитого
/// по разделителям, смене регистра и границе цифр, с множественным числом (<c>PinCode</c>, <c>PINs</c>): по вхождению короткие
/// <c>pin</c>, <c>otp</c> задели бы <c>Shipping</c> и <c>RootPath</c>; запись из нескольких слов совпадает с именем целиком.
/// Значение под таким именем заменяется целиком и в результат не попадает даже началом. Обычная строка, не влезшая в лимит,
/// обрезается по границе символа, если дочитана на входе, а оборванная на входе выпадает. Имя в соседнем поле (пары Key/Value) и
/// JSON внутри строки маска не видит — README модуля.
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
    private readonly HashSet<string> _nameWords;

    /// <summary>
    /// Маска по списку по умолчанию: <see cref="DefaultNameFragments"/> и <see cref="DefaultNameWords"/>.
    /// </summary>
    public SensitiveNamesMessageDataMasker()
        : this(DefaultNameFragments, DefaultNameWords)
    {
    }

    /// <remarks>
    /// Закрыт намеренно: публичный конструктор со списками контейнер выбрал бы как самый полный и подставил бы
    /// пустые <see cref="IEnumerable{T}"/> — маскировщик из контейнера не маскировал бы ничего.
    /// </remarks>
    private SensitiveNamesMessageDataMasker(IEnumerable<string> nameFragments, IEnumerable<string> nameWords)
    {
        _nameFragments = nameFragments.Select(Normalize).Where(x => x.Length > 0).Distinct().ToArray();
        _nameWords = nameWords.Select(Normalize).Where(x => x.Length > 0).ToHashSet();
    }

    /// <summary>
    /// Фрагменты имён по умолчанию.
    /// </summary>
    public static IReadOnlyList<string> DefaultNameFragments { get; } =
        ["password", "passwd", "pwd", "secret", "token", "apikey", "authorization", "credential", "privatekey", "cvv", "cvc"];

    /// <summary>
    /// Слова имён по умолчанию.
    /// </summary>
    public static IReadOnlyList<string> DefaultNameWords { get; } = ["pin", "otp"];

    /// <summary>
    /// Маска по своему списку имён вместо списка по умолчанию.
    /// </summary>
    /// <param name="nameFragments">Фрагменты, при вхождении которых в имя значение маскируется.</param>
    /// <param name="nameWords">Слова, при наличии которых среди слов имени значение маскируется; запись из нескольких слов (<c>PayerBic</c>, <c>pin_code</c>) совпадает с именем целиком.</param>
    public static SensitiveNamesMessageDataMasker Create(IEnumerable<string> nameFragments, IEnumerable<string> nameWords)
    {
        ArgumentNullException.ThrowIfNull(nameFragments);
        ArgumentNullException.ThrowIfNull(nameWords);

        return new SensitiveNamesMessageDataMasker(nameFragments, nameWords);
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
    /// <param name="length">Длина результата: вывод до последнего токена, уложившегося в лимит, или до лимита внутри обычной строки.</param>
    private bool Copy(ReadOnlySpan<byte> json, bool isComplete, int limit, ArrayBufferWriter<byte> output, out int length)
    {
        var reader = new Utf8JsonReader(json, isComplete, default);
        using var writer = new Utf8JsonWriter(output, WriterOptions);
        length = 0;

        while (reader.Read())
        {
            var sensitive = reader.TokenType == JsonTokenType.PropertyName && IsSensitive(reader.GetString());
            var whole = sensitive ? CopyMasked(ref reader, writer) : CopyToken(ref reader, writer);

            writer.Flush();

            if (output.WrittenCount > limit)
            {
                if (!sensitive && reader.TokenType == JsonTokenType.String)
                    length = WholeCharsLength(output.WrittenSpan, limit);

                return false;
            }

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

        return _nameWords.Contains(normalized)
               || _nameFragments.Any(x => normalized.Contains(x, StringComparison.Ordinal))
               || Words(name).Any(IsListedWord);
    }

    /// <summary>
    /// Слово из списка или его множественное число (<c>Pins</c>, <c>OTPs</c>).
    /// </summary>
    private bool IsListedWord(string word)
        => _nameWords.Contains(word) || (word.Length > 1 && word[^1] == 's' && _nameWords.Contains(word[..^1]));

    /// <summary>
    /// Слова имени в нижнем регистре: границы — разделители, начало заглавной после строчной или цифры, последняя заглавная
    /// аббревиатуры перед строчной (<c>PINCode</c> — pin, code) и переход между цифрой и буквой.
    /// </summary>
    private static IEnumerable<string> Words(string name)
    {
        var word = new StringBuilder();

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (IsSeparator(c))
            {
                if (word.Length > 0)
                    yield return word.ToString();

                word.Clear();
                continue;
            }

            if (word.Length > 0 && IsWordBoundary(name, i))
            {
                yield return word.ToString();
                word.Clear();
            }

            word.Append(char.ToLowerInvariant(c));
        }

        if (word.Length > 0)
            yield return word.ToString();
    }

    private static bool IsWordBoundary(string name, int i)
    {
        var c = name[i];
        var previous = name[i - 1];

        return (char.IsUpper(c) && (char.IsLower(previous) || char.IsDigit(previous)))
               || (char.IsUpper(c) && char.IsUpper(previous) && i + 1 < name.Length && char.IsLower(name[i + 1]) && !IsPluralTail(name, i + 1))
               || char.IsDigit(c) != char.IsDigit(previous);
    }

    /// <summary>
    /// Строчная <c>s</c> в конце слова после аббревиатуры: <c>PINs</c> — одно слово, а не pi, ns.
    /// </summary>
    private static bool IsPluralTail(string name, int i)
        => name[i] == 's' && (i + 1 == name.Length || !char.IsLower(name[i + 1]));

    private static bool IsSeparator(char c) => c is '_' or '-' or '.' || char.IsWhiteSpace(c);

    /// <summary>
    /// Длина начала вывода не больше <paramref name="limit"/> байт, не разрывающая символ UTF-8.
    /// </summary>
    private static int WholeCharsLength(ReadOnlySpan<byte> output, int limit)
    {
        var length = limit;

        while (length > 0 && (output[length] & 0xC0) == 0x80)
            length--;

        return length;
    }

    /// <remarks>
    /// Принимает <see langword="null"/>: запись своего списка может прийти пустой, и тогда она пропускается, а не роняет создание.
    /// </remarks>
    private static string Normalize(string? name)
    {
        if (name == null)
            return string.Empty;

        var builder = new StringBuilder(name.Length);

        foreach (var c in name)
        {
            if (!IsSeparator(c))
                builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }
}