using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Dex.MassTransit.Rabbit.Tests;

/// <summary>
/// Маска тела по именам полей в <see cref="SensitiveNamesMessageDataMasker"/>.
/// </summary>
/// <remarks>
/// Вход — начало сериализованного тела, поэтому проверяется и оборванный вход: секрет, разрезанный
/// лимитом, не должен попасть в запись даже началом.
/// </remarks>
[TestFixture]
public class SensitiveNamesMessageDataMaskerTests
{
    private const int NoLimit = 100_000;

    private static readonly SensitiveNamesMessageDataMasker Masker = new();

    [Test]
    public void Mask_WhenNameIsSensitive_ReplacesValue()
    {
        var result = Mask("""{"Login":"user","Password":"p@ss"}""");

        Assert.That(result, Is.EqualTo("""{"Login":"user","Password":"***"}"""));
    }

    [TestCase("refresh_token")]
    [TestCase("X-Api-Key")]
    [TestCase("ClientSecret")]
    [TestCase("authorization")]
    public void Mask_WhenNameContainsFragmentInAnyCaseOrSeparators_ReplacesValue(string name)
    {
        var result = Mask($$"""{"{{name}}":"value"}""");

        Assert.That(result, Is.EqualTo($$"""{"{{name}}":"***"}"""));
    }

    [Test]
    public void Mask_ShortNamesMatchOnlyWholeName()
    {
        var result = Mask("""{"Pin":"1234","Shipping":"courier","Mapping":"a"}""");

        Assert.That(result, Is.EqualTo("""{"Pin":"***","Shipping":"courier","Mapping":"a"}"""));
    }

    [Test]
    public void Mask_WhenSensitiveValueIsNotString_ReplacesWholeValue()
    {
        var result = Mask("""{"Pin":1234,"Credentials":{"User":"a","Pass":"b"},"Tokens":["t1","t2"],"Id":7}""");

        Assert.That(result, Is.EqualTo("""{"Pin":"***","Credentials":"***","Tokens":"***","Id":7}"""));
    }

    [Test]
    public void Mask_KeepsOtherValuesAsTheyWere()
    {
        const string json = """{"Card":{"Cvv":"123","Holder":"Иван \"И\"\n"},"Sum":10.50,"Ok":true,"Note":null,"Ids":[1,2]}""";

        var result = Mask(json);

        Assert.That(result, Is.EqualTo("""{"Card":{"Cvv":"***","Holder":"Иван \"И\"\n"},"Sum":10.50,"Ok":true,"Note":null,"Ids":[1,2]}"""));
    }

    [Test]
    public void Mask_WhenResultExceedsLimit_CutsBeforeLimitAndMarksIt()
    {
        var json = "{\"Items\":[" + string.Join(",", Enumerable.Repeat("\"abcdef\"", 50)) + "]}";

        var result = Masker.Mask(Encoding.UTF8.GetBytes(json), isComplete: true, limit: 40);

        Assert.That(result, Does.EndWith("..."));
        Assert.That(Encoding.UTF8.GetByteCount(result[..^3]), Is.LessThanOrEqualTo(40));
        Assert.That(json, Does.StartWith(result[..^3]));
    }

    /// <remarks>
    /// Лимит режет вывод, а не вход: замаскированный длинный токен освобождает место под следующие поля.
    /// </remarks>
    [Test]
    public void Mask_WhenMaskFreesSpace_FollowingFieldsFit()
    {
        var json = $$"""{"Token":"{{new string('x', 3000)}}","Tail":"end"}""";

        var result = Masker.Mask(Encoding.UTF8.GetBytes(json), isComplete: true, limit: 100);

        Assert.That(result, Is.EqualTo("""{"Token":"***","Tail":"end"}"""));
    }

    [Test]
    public void Mask_WhenInputIsCutInsideSecret_WritesNoPartOfIt()
    {
        var result = Masker.Mask("""{"Name":"a","Password":"supersecr"""u8, isComplete: false, limit: NoLimit);

        Assert.That(result, Is.EqualTo("""{"Name":"a","Password":"***"..."""));
    }

    [Test]
    public void Mask_WhenInputIsCutInsideOrdinaryValue_DropsTheTokenAndMarksIt()
    {
        var result = Masker.Mask("""{"Name":"a","Note":"unfinish"""u8, isComplete: false, limit: NoLimit);

        Assert.That(result, Is.EqualTo("""{"Name":"a","Note":..."""));
    }

    [Test]
    public void Mask_WhenInputIsCutInsideSensitiveObject_WritesMaskAndMarksIt()
    {
        var result = Masker.Mask("""{"Credentials":{"User":"a","Pa"""u8, isComplete: false, limit: NoLimit);

        Assert.That(result, Is.EqualTo("""{"Credentials":"***"..."""));
    }

    [Test]
    public void Mask_WithCustomNames_UsesOnlyThem()
    {
        var masker = SensitiveNamesMessageDataMasker.Create(nameFragments: ["iban"], exactNames: ["bic"]);

        var result = masker.Mask("""{"PayerIban":"DE00","Bic":"X","Password":"p"}"""u8, isComplete: true, limit: NoLimit);

        Assert.That(result, Is.EqualTo("""{"PayerIban":"***","Bic":"***","Password":"p"}"""));
    }

    private static string Mask(string json) => Masker.Mask(Encoding.UTF8.GetBytes(json), isComplete: true, limit: NoLimit);
}