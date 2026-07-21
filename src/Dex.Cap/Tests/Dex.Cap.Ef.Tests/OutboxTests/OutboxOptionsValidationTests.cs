using System;
using Dex.Cap.Outbox.Options;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests;

/// <summary>
/// Правила валидаторов опций аутбокса. Применение правила про размер тела на старте хоста, а также то, что
/// остальные правила остаются спящими, проверяет <see cref="OutboxOptionsStartupTests"/>.
/// </summary>
public class OutboxOptionsValidationTests
{
    [Test]
    public void Options_Defaults_AreValid()
    {
        var result = new OutboxOptionsValidator().Validate(null, new OutboxOptions());

        Assert.IsTrue(result.Succeeded, result.FailureMessage ?? string.Empty);
    }

    [Test]
    public void MaxContentLengthValidator_Defaults_AreValid()
    {
        var result = new OutboxMaxContentLengthValidator().Validate(null, new OutboxOptions());

        Assert.IsTrue(result.Succeeded, result.FailureMessage ?? string.Empty);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Options_NonPositiveMaxContentLength_IsRejected(int maxContentLength)
    {
        var options = new OutboxOptions { MaxContentLength = maxContentLength };

        var result = new OutboxMaxContentLengthValidator().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.FailureMessage!.Contains(nameof(OutboxOptions.MaxContentLength), StringComparison.Ordinal), result.FailureMessage);
    }

    [Test]
    public void Options_SmallestPositiveMaxContentLength_IsAccepted()
    {
        var result = new OutboxMaxContentLengthValidator().Validate(null, new OutboxOptions { MaxContentLength = 1 });

        Assert.IsTrue(result.Succeeded, result.FailureMessage ?? string.Empty);
    }

    /// <summary>
    /// Правило про размер тела живёт только в <see cref="OutboxMaxContentLengthValidator"/>: дубль в
    /// <see cref="OutboxOptionsValidator"/> дал бы два сообщения об одной ошибке, когда подключат и его.
    /// </summary>
    /// <remarks>
    /// Проверяется отсутствие ДУБЛЯ, а не успех валидации: иначе тест читался бы как запрет возвращать
    /// правило в общий валидатор и падал бы на работах по issue #239, где цель прямо противоположная.
    /// </remarks>
    [TestCase(0)]
    [TestCase(-1)]
    public void OptionsValidator_DoesNotDuplicateMaxContentLengthRule(int maxContentLength)
    {
        var options = new OutboxOptions { MaxContentLength = maxContentLength };

        var result = new OutboxOptionsValidator().Validate(null, options);

        var mentionsRule = result.FailureMessage?.Contains(nameof(OutboxOptions.MaxContentLength), StringComparison.Ordinal) ?? false;
        Assert.IsFalse(mentionsRule, result.FailureMessage ?? string.Empty);
    }

    /// <summary>
    /// Спящие правила обязаны существовать: их подключение это отдельная задача (issue #239), а не отказ от
    /// них. Без этого теста <see cref="OutboxOptionsValidator"/> можно выпотрошить целиком, и набор останется
    /// зелёным, хотя доки продолжат обещать эти правила.
    /// </summary>
    [TestCase(0, "Retries")]
    [TestCase(-1, "Retries")]
    public void OptionsValidator_DormantRules_StillExist(int retries, string expectedInMessage)
    {
        var options = new OutboxOptions { Retries = retries };

        var result = new OutboxOptionsValidator().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.FailureMessage!.Contains(expectedInMessage, StringComparison.Ordinal), result.FailureMessage);
    }

    [Test]
    public void DefaultMaxContentLength_Is1MiB()
    {
        Assert.AreEqual(1024 * 1024, OutboxOptions.DefaultMaxContentLength);
        Assert.AreEqual(OutboxOptions.DefaultMaxContentLength, new OutboxOptions().MaxContentLength);
    }
}