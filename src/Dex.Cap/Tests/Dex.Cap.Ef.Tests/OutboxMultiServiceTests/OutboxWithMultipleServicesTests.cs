using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.OutboxMultiServiceTests.Discriminators;
using Dex.Cap.Ef.Tests.OutboxMultiServiceTests.Handlers;
using Dex.Cap.OnceExecutor.Ef.Extensions;
using Dex.Cap.Outbox;
using Dex.Cap.Outbox.Ef.Extensions;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;
using Dex.Cap.Outbox.Options;
using Dex.Outbox.Command.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OutboxMultiServiceTests;

public class OutboxWithMultipleServicesTests : BaseTest
{
    [Test]
    public async Task OutboxMessagesAreIsolatedAcrossServicesWithSharedDatabase_Success()
    {
        // Arrange
        var sp1 = CreateServiceProvider<TestExternalServiceDiscriminator, TestOutboxExternalServiceCommand,
            TestCommandExternalServiceHandler>();
        var sp2 = CreateServiceProvider<TestDiscriminator, TestOutboxCommand, TestCommandHandler>();

        var correlationId1 = Guid.NewGuid();
        var processedCommandsCount = 0;

        // Сохраняем в outbox команду из первого сервиса
        var cmd1 = new TestOutboxExternalServiceCommand { Args = "hello world" };
        await SaveCommandAsync(sp1, cmd1, correlationId1);
        var messageIds = new List<Guid> { cmd1.TestId };

        // Act
        // Инициируем работу с outbox во втором сервисе
        TestCommandHandler.OnProcess += OnTestCommandHandler2OnProcess!;

        var handler2 = sp2.GetRequiredService<IOutboxHandler>();
        await handler2.ProcessAsync(CancellationToken.None);

        TestCommandHandler.OnProcess -= OnTestCommandHandler2OnProcess!;

        // Assert
        var envelope = await GetEnvelope(sp2, correlationId1);

        Assert.IsNull(envelope.Error); // проверяем отсутствие ошибки DiscriminatorResolveTypeException
        Assert.AreEqual(0, processedCommandsCount);
        return;

        void OnTestCommandHandler2OnProcess(object _, TestOutboxCommand m)
        {
            if (!messageIds.Contains(m.TestId))
            {
                throw new InvalidOperationException("MessageId not equals");
            }

            Interlocked.Increment(ref processedCommandsCount);
        }
    }

    [Test]
    public async Task ProcessingOutboxCommandsFromDifferentServicesWithCommonDb_Success()
    {
        // Arrange
        var sp1 = CreateServiceProvider<TestExternalServiceDiscriminator, TestOutboxExternalServiceCommand,
            TestCommandExternalServiceHandler>();
        var sp2 = CreateServiceProvider<TestDiscriminator, TestOutboxCommand, TestCommandHandler>();

        var correlationId1 = Guid.NewGuid();
        var correlationId2 = Guid.NewGuid();

        var cmd1 = new TestOutboxExternalServiceCommand { Args = "hello world" };
        await SaveCommandAsync(sp1, cmd1, correlationId1);
        var cmd2 = new TestOutboxCommand { Args = "hello world2" };
        await SaveCommandAsync(sp2, cmd2, correlationId2);

        var messageIds = new List<Guid> { cmd1.TestId, cmd2.TestId };
        var processedCommandsCount = 0;

        TestCommandExternalServiceHandler.OnProcess += OnTestCommandHandler1OnProcess!;
        TestCommandHandler.OnProcess += OnTestCommandHandler2OnProcess!;

        // Act
        // Обработка сообщений из обоих сервисов
        var handler1 = sp1.GetRequiredService<IOutboxHandler>();
        var handler2 = sp2.GetRequiredService<IOutboxHandler>();
        await handler1.ProcessAsync(CancellationToken.None);
        await handler2.ProcessAsync(CancellationToken.None);

        TestCommandExternalServiceHandler.OnProcess -= OnTestCommandHandler1OnProcess!;
        TestCommandHandler.OnProcess -= OnTestCommandHandler2OnProcess!;

        var envelope1 = await GetEnvelope(sp2, correlationId1);
        var envelope2 = await GetEnvelope(sp1, correlationId2);

        // Assert
        Assert.IsNull(envelope1.Error);
        Assert.IsNull(envelope2.Error);
        Assert.AreEqual(2, processedCommandsCount);
        return;

        void OnTestCommandHandler1OnProcess(object _, TestOutboxExternalServiceCommand m)
        {
            if (!messageIds.Contains(m.TestId))
            {
                throw new InvalidOperationException("MessageId not equals");
            }

            Interlocked.Increment(ref processedCommandsCount);
        }

        void OnTestCommandHandler2OnProcess(object _, TestOutboxCommand m)
        {
            if (!messageIds.Contains(m.TestId))
            {
                throw new InvalidOperationException("MessageId not equals");
            }

            Interlocked.Increment(ref processedCommandsCount);
        }
    }

    private ServiceProvider CreateServiceProvider<TDiscriminator, TCommand, THandler>(
        Func<IServiceCollection, IServiceCollection>? configure = null)
        where TDiscriminator : BaseOutboxTypeDiscriminator
        where TCommand : class
        where THandler : class, IOutboxMessageHandler<TCommand>
    {
        var serviceCollection = new ServiceCollection();
        AddLogging(serviceCollection);

        serviceCollection
            .AddScoped(_ => new TestDbContext(DbName))
            .AddOutbox<TestDbContext, TDiscriminator>()
            .AddOnceExecutor<TestDbContext>()
            .AddOptions<OutboxOptions>()
            .Configure(options =>
            {
                options.MessagesToProcess = 10;
                options.ConcurrencyLimit = 2;
                options.Retries = 1;
            });

        serviceCollection.AddScoped<IOutboxMessageHandler<TCommand>, THandler>();
        configure?.Invoke(serviceCollection);
        return serviceCollection.BuildServiceProvider();
    }

    private static async Task SaveCommandAsync<TCommand>(ServiceProvider sp, TCommand command, Guid? correlationId = null)
        where TCommand : class
    {
        var outboxService = sp.GetRequiredService<IOutboxService>();
        await outboxService.EnqueueAsync(command, correlationId);
        await SaveChanges(sp);
    }

    private static async Task<OutboxEnvelope> GetEnvelope(ServiceProvider sp, Guid correlationId)
    {
        return await GetDb(sp).Set<OutboxEnvelope>()
            .FirstAsync(x => x.CorrelationId == correlationId);
    }
}