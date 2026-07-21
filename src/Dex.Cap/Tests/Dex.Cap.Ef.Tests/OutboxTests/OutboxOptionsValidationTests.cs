using System;
using System.Collections.Generic;
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
    public void MaxContentLengthBytesValidator_Defaults_AreValid()
    {
        var result = new OutboxMaxContentLengthBytesValidator().Validate(null, new OutboxOptions());

        Assert.IsTrue(result.Succeeded, result.FailureMessage ?? string.Empty);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Options_NonPositiveMaxContentLengthBytes_IsRejected(int maxContentLengthBytes)
    {
        var options = new OutboxOptions { MaxContentLengthBytes = maxContentLengthBytes };

        var result = new OutboxMaxContentLengthBytesValidator().Validate(null, options);

        Assert.IsFalse(result.Succeeded);

        // Именно с типом: инбокс объявляет одноимённую опцию, а склейка отказов в
        // OptionsValidationException.Message тип не несёт, он остаётся только в OptionsType.
        Assert.IsTrue(
            result.FailureMessage!.Contains($"{nameof(OutboxOptions)}.{nameof(OutboxOptions.MaxContentLengthBytes)}", StringComparison.Ordinal),
            result.FailureMessage);
    }

    [Test]
    public void Options_SmallestPositiveMaxContentLengthBytes_IsAccepted()
    {
        var result = new OutboxMaxContentLengthBytesValidator().Validate(null, new OutboxOptions { MaxContentLengthBytes = 1 });

        Assert.IsTrue(result.Succeeded, result.FailureMessage ?? string.Empty);
    }

    /// <summary>
    /// Правило про размер тела живёт только в <see cref="OutboxMaxContentLengthBytesValidator"/>: дубль в
    /// <see cref="OutboxOptionsValidator"/> дал бы два сообщения об одной ошибке, когда подключат и его.
    /// </summary>
    /// <remarks>
    /// Тест фиксирует сегодняшнее размещение правила, а не запрет его переносить. При слиянии валидаторов
    /// в рамках issue #239 он упадёт и должен быть снят или перевёрнут осознанно, вместе с решением о
    /// слиянии. Формулировка через отсутствие подстроки, а не через <c>Succeeded</c>, выбрана только ради
    /// точности диагностики: она называет причину падения. Падают обе одинаково, потому что при таком
    /// объекте опций остальные правила проходят.
    /// </remarks>
    [TestCase(0)]
    [TestCase(-1)]
    public void OptionsValidator_DoesNotDuplicateMaxContentLengthBytesRule(int maxContentLengthBytes)
    {
        var options = new OutboxOptions { MaxContentLengthBytes = maxContentLengthBytes };

        var result = new OutboxOptionsValidator().Validate(null, options);

        var mentionsRule = result.FailureMessage?.Contains(nameof(OutboxOptions.MaxContentLengthBytes), StringComparison.Ordinal) ?? false;
        Assert.IsFalse(mentionsRule, result.FailureMessage ?? string.Empty);
    }

    /// <summary>
    /// Спящие правила обязаны существовать: их подключение это отдельная задача (issue #239), а не отказ от
    /// них. Без этого теста <see cref="OutboxOptionsValidator"/> можно выпотрошить целиком, и набор останется
    /// зелёным, хотя <c>README.md</c> продолжит обещать потребителю все пять.
    /// </summary>
    /// <remarks>
    /// Перебираются ВСЕ пять правил и обе границы там, где их две: покрытие одного правила означало бы, что
    /// остальные четыре можно удалить незаметно. Проверяется подстрока, опознающая сработавшее правило, а не
    /// сообщение целиком: тексты трёх правил расходятся со своими условиями (говорят <c>less than 100</c> при
    /// условии <c>&gt; 100</c>) и будут править в рамках issue #239.
    /// </remarks>
    [TestCaseSource(nameof(DormantRuleCases))]
    public void OptionsValidator_DormantRules_StillExist(Action<OutboxOptions> makeInvalid, string expectedInMessage)
    {
        var options = new OutboxOptions();
        makeInvalid(options);

        var result = new OutboxOptionsValidator().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.FailureMessage!.Contains(expectedInMessage, StringComparison.Ordinal), result.FailureMessage);
    }

    private static IEnumerable<TestCaseData> DormantRuleCases()
    {
        yield return new TestCaseData((Action<OutboxOptions>)(o => o.Retries = 0), "Retries should be")
            .SetName("DormantRule_Retries_NonPositive");
        yield return new TestCaseData((Action<OutboxOptions>)(o => o.Retries = 10_001), "Retries should be")
            .SetName("DormantRule_Retries_AboveUpperBound");
        yield return new TestCaseData((Action<OutboxOptions>)(o => o.MessagesToProcess = 0), "MessagesToProcess should be")
            .SetName("DormantRule_MessagesToProcess_NonPositive");
        yield return new TestCaseData((Action<OutboxOptions>)(o => o.MessagesToProcess = 101), "MessagesToProcess should be")
            .SetName("DormantRule_MessagesToProcess_AboveUpperBound");
        yield return new TestCaseData((Action<OutboxOptions>)(o => o.ConcurrencyLimit = 0), "ConcurrencyLimit should be")
            .SetName("DormantRule_ConcurrencyLimit_NonPositive");
        yield return new TestCaseData((Action<OutboxOptions>)(o => o.ConcurrencyLimit = 101), "ConcurrencyLimit should be")
            .SetName("DormantRule_ConcurrencyLimit_AboveUpperBound");
        yield return new TestCaseData(
                (Action<OutboxOptions>)(o =>
                {
                    o.MessagesToProcess = 1;
                    o.ConcurrencyLimit = 2;
                }),
                "ConcurrencyLimit can't be greater")
            .SetName("DormantRule_ConcurrencyLimit_ExceedsMessagesToProcess");
        yield return new TestCaseData(
                (Action<OutboxOptions>)(o => o.GetFreeMessagesTimeout = TimeSpan.FromMilliseconds(999)),
                "GetFreeMessagesTimeout can't be less")
            .SetName("DormantRule_GetFreeMessagesTimeout_BelowOneSecond");
    }

    [Test]
    public void DefaultMaxContentLengthBytes_Is1MiB()
    {
        Assert.AreEqual(1024 * 1024, OutboxOptions.DefaultMaxContentLengthBytes);
        Assert.AreEqual(OutboxOptions.DefaultMaxContentLengthBytes, new OutboxOptions().MaxContentLengthBytes);
    }
}