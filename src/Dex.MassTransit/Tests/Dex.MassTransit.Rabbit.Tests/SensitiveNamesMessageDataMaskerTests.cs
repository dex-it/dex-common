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

    [TestCase("CardCvv")]
    [TestCase("card_cvv")]
    [TestCase("cardcvv")]
    [TestCase("Cvv2")]
    [TestCase("CVV2")]
    [TestCase("CvvCode")]
    [TestCase("Cvc2")]
    [TestCase("CardCvc")]
    [TestCase("PinCode")]
    [TestCase("pin_code")]
    [TestCase("PINCode")]
    [TestCase("CardPin")]
    [TestCase("NewPin")]
    [TestCase("Pin")]
    [TestCase("OtpCode")]
    [TestCase("SmsOtp")]
    [TestCase("Otp")]
    [TestCase("NewPwd")]
    [TestCase("UserPwd")]
    public void Mask_WhenNameHasCardCodeOrOneTimeCode_ReplacesValue(string name)
    {
        var result = Mask($$"""{"{{name}}":"v"}""");

        Assert.That(result, Is.EqualTo($$"""{"{{name}}":"***"}"""));
    }

    /// <remarks>
    /// <c>pin</c> и <c>otp</c> ищутся отдельным словом имени: по вхождению они задели бы обычные слова.
    /// </remarks>
    [TestCase("Shipping")]
    [TestCase("Mapping")]
    [TestCase("Spinner")]
    [TestCase("Opinion")]
    [TestCase("Pinned")]
    [TestCase("PINNED")]
    [TestCase("Pinpoint")]
    [TestCase("RootPath")]
    [TestCase("Footprint")]
    [TestCase("HotPath")]
    public void Mask_WhenShortNameIsPartOfOrdinaryWord_KeepsValue(string name)
    {
        var result = Mask($$"""{"{{name}}":"v"}""");

        Assert.That(result, Is.EqualTo($$"""{"{{name}}":"v"}"""));
    }

    /// <remarks>
    /// Без маски начало длинного значения в записи есть; с маской оно не должно пропадать.
    /// </remarks>
    [Test]
    public void Mask_WhenOrdinaryStringExceedsLimit_KeepsItsBeginning()
    {
        var json = $$"""{"OrderId":"1","Comment":"{{new string('c', 5000)}}","Status":"s"}""";

        var result = Masker.Mask(Encoding.UTF8.GetBytes(json), isComplete: true, limit: 100);

        Assert.That(result, Does.StartWith("""{"OrderId":"1","Comment":"ccc"""));
        Assert.That(result, Does.EndWith("..."));
        Assert.That(Encoding.UTF8.GetByteCount(result[..^3]), Is.EqualTo(100));
    }

    [Test]
    public void Mask_WhenOrdinaryStringIsCutInsideMultibyteChar_KeepsWholeChars()
    {
        var json = $$"""{"Name":"{{new string('я', 100)}}"}""";

        var result = Masker.Mask(Encoding.UTF8.GetBytes(json), isComplete: true, limit: 12);

        Assert.That(result, Is.EqualTo("""{"Name":"я..."""));
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
        var masker = SensitiveNamesMessageDataMasker.Create(nameFragments: ["iban"], nameWords: ["bic"]);

        var result = masker.Mask("""{"PayerIban":"DE00","PayerBic":"X","Bicycle":"b","Password":"p"}"""u8, isComplete: true, limit: NoLimit);

        Assert.That(result, Is.EqualTo("""{"PayerIban":"***","PayerBic":"***","Bicycle":"b","Password":"p"}"""));
    }

    private static string Mask(string json) => Masker.Mask(Encoding.UTF8.GetBytes(json), isComplete: true, limit: NoLimit);
}