using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dex.Cap.Inbox.Interfaces;

namespace Dex.Cap.Inbox;

internal sealed class DefaultInboxSerializer : IInboxSerializer
{
    /// <remarks>
    /// Enum пишется строкой, а не числом. Тело сообщения это контракт схемы: оно лежит в БД и читается уже
    /// после деплоя. Числовой дефолт System.Text.Json завязывает смысл на ПОРЯДОК членов enum, поэтому их
    /// перестановка (на вид безобидный рефакторинг) молча переназначила бы смысл уже сохранённых сообщений.
    /// Имя к перестановке устойчиво. Переименование члена остаётся ломающим, но это осознанное и видимое
    /// изменение, в отличие от перестановки, и при нужде закрывается атрибутом имени. Дефолт выставлен ДО
    /// первого релиза, пока сохранённых тел нет и цена смены нулевая: после релиза сменить его уже нельзя,
    /// он сам стал бы контрактом. Кому нужно иначе, подменяет весь сериализатор через IInboxSerializer.
    /// </remarks>
    private readonly JsonSerializerOptions _options = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public string Serialize(Type type, object obj)
    {
        return JsonSerializer.Serialize(obj, type, _options);
    }

    public object? Deserialize(Type type, string input)
    {
        return JsonSerializer.Deserialize(input, type, _options);
    }
}