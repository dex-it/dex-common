using System;
using Dex.Cap.Inbox;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.InboxTests;

/// <summary>
/// Тело сообщения это контракт схемы, читаемый после деплоя, поэтому дефолтный сериализатор обязан писать
/// enum именем, а не числом: числовое представление завязано на порядок членов и перестановка молча
/// переназначила бы смысл уже сохранённых тел.
/// </summary>
public class DefaultInboxSerializerTests
{
    private enum Sample
    {
        // ReSharper disable once UnusedMember.Local
        First,
        Second,
        Third
    }

    private sealed class Holder
    {
        public Sample Value { get; init; }

        public int Number { get; init; }
    }

    [Test]
    public void Serialize_Enum_IsWrittenByNameNotByNumber()
    {
        var serializer = new DefaultInboxSerializer();

        // Second это индекс 1: числовой дефолт STJ дал бы "Value":1, что сломалось бы при перестановке членов.
        var json = serializer.Serialize(typeof(Holder), new Holder { Value = Sample.Second, Number = 7 });

        Assert.IsTrue(json.Contains("\"Second\"", StringComparison.Ordinal), json);
        Assert.IsFalse(json.Contains("\"Value\":1", StringComparison.Ordinal), json);
        // Контроль: не-enum поле по-прежнему число, конвертер не задевает лишнего.
        Assert.IsTrue(json.Contains("\"Number\":7", StringComparison.Ordinal), json);
    }

    [Test]
    public void SerializeThenDeserialize_Enum_RoundTrips()
    {
        var serializer = new DefaultInboxSerializer();

        var json = serializer.Serialize(typeof(Holder), new Holder { Value = Sample.Third });
        var restored = (Holder)serializer.Deserialize(typeof(Holder), json)!;

        Assert.AreEqual(Sample.Third, restored.Value);
    }

    /// <summary>
    /// Именно то, ради чего форсится конвертер: тело, записанное ИМЕНЕМ, читается верно даже после
    /// перестановки членов enum. Здесь это воспроизведено тем, что имя в теле не совпадает с порядком.
    /// </summary>
    [Test]
    public void Deserialize_EnumStoredByName_SurvivesMemberReordering()
    {
        var serializer = new DefaultInboxSerializer();

        // Тело, сохранённое как строка имени: числовая привязка тут отсутствует в принципе.
        var restored = (Holder)serializer.Deserialize(typeof(Holder), """{"Value":"Third","Number":0}""")!;

        Assert.AreEqual(Sample.Third, restored.Value);
    }
}