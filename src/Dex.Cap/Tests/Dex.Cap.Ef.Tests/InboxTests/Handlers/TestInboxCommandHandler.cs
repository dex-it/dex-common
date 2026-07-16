using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.InboxTests.Messages;
using Dex.Cap.Inbox.Interfaces;

namespace Dex.Cap.Ef.Tests.InboxTests.Handlers;

public class TestInboxCommandHandler : IInboxMessageHandler<TestInboxCommand>
{
    public static event EventHandler<TestInboxCommand>? OnProcess;

    /// <summary>
    /// Асинхронная точка вмешательства: позволяет задержать обработчик, чтобы воспроизвести
    /// дренаж партии и истечение аренды в очереди ожидания.
    /// </summary>
    public static Func<TestInboxCommand, Task>? OnProcessAsync { get; set; }

    /// <summary>
    /// То же, но с токеном обработки: нужен, чтобы отличить обработчик, уважающий отмену, от игнорирующего её.
    /// </summary>
    public static Func<TestInboxCommand, CancellationToken, Task>? OnProcessWithTokenAsync { get; set; }

    public async Task Process(TestInboxCommand message, CancellationToken cancellationToken)
    {
        OnProcess?.Invoke(this, message);

        var hook = OnProcessAsync;

        if (hook is not null)
        {
            await hook(message).ConfigureAwait(false);
        }

        var tokenHook = OnProcessWithTokenAsync;

        if (tokenHook is not null)
        {
            await tokenHook(message, cancellationToken).ConfigureAwait(false);
        }
    }
}