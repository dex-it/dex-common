using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.InboxTests.Messages;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.Inbox.Interfaces;

namespace Dex.Cap.Ef.Tests.InboxTests.Handlers;

/// <summary>
/// Пишет бизнес-эффект через тот же DbContext, что и инбокс, но НЕ коммитит:
/// коммит делает транзакция обработки, поэтому эффект и статус атомарны.
/// </summary>
public class TestInboxUserCommandHandler(TestDbContext dbContext) : IInboxMessageHandler<TestInboxUserCommand>
{
    public async Task Process(TestInboxUserCommand message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        await dbContext.Users.AddAsync(new TestUser { Name = message.UserName }, cancellationToken).ConfigureAwait(false);

        if (message.ThrowAfterEffect)
        {
            throw new InvalidOperationException("Handler failed after the business effect");
        }
    }
}
