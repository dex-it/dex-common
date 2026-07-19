using System;
using Dex.Cap.Outbox.Options;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxTests;

/// <summary>
/// Проверяет правила <see cref="OutboxOptionsValidator"/> напрямую. В отличие от инбокса валидатор аутбокса
/// на старте хоста НЕ подключён (известный техдолг), поэтому здесь тестируется сам валидатор, а не запуск.
/// </summary>
public class OutboxOptionsValidationTests
{
    [Test]
    public void Options_Defaults_AreValid()
    {
        var result = new OutboxOptionsValidator().Validate(null, new OutboxOptions());

        Assert.IsTrue(result.Succeeded, result.FailureMessage ?? string.Empty);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Options_NonPositiveMaxContentLength_IsRejected(int maxContentLength)
    {
        var options = new OutboxOptions { MaxContentLength = maxContentLength };

        var result = new OutboxOptionsValidator().Validate(null, options);

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(result.FailureMessage!.Contains(nameof(OutboxOptions.MaxContentLength), StringComparison.Ordinal), result.FailureMessage);
    }

    [Test]
    public void Options_SmallestPositiveMaxContentLength_IsAccepted()
    {
        var result = new OutboxOptionsValidator().Validate(null, new OutboxOptions { MaxContentLength = 1 });

        Assert.IsTrue(result.Succeeded, result.FailureMessage ?? string.Empty);
    }

    [Test]
    public void DefaultMaxContentLength_Is1MiB()
    {
        Assert.AreEqual(1024 * 1024, OutboxOptions.DefaultMaxContentLength);
        Assert.AreEqual(OutboxOptions.DefaultMaxContentLength, new OutboxOptions().MaxContentLength);
    }
}