using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.InboxTests.Messages;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.InboxTests;

/// <summary>
/// Обработчик вызывается через контракт интерфейса. Поиск метода Process рефлексией ломался на трёх
/// законных формах обработчика, и все три молча уводили сообщение в DeadLettered.
/// </summary>
public class InboxHandlerInvocationTests : BaseTest
{
    [SetUp]
    public override async Task Setup()
    {
        await base.Setup();
        MultiMessageHandler.Handled.Clear();
        ExplicitHandler.Handled = false;
        SyncThrowingHandler.Fail = false;
    }

    [Test]
    public async Task Process_HandlerServingTwoMessageTypes_RoutesEachToItsOwnProcess()
    {
        // Один класс на два типа сообщений даёт два публичных метода Process. Порядок Type.GetMethods
        // не определён, поэтому поиск по имени брал произвольный и падал на приведении аргумента.
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, MultiMessageHandler>()
            .AddScoped<IInboxMessageHandler<TestErrorInboxCommand>, MultiMessageHandler>()
            .BuildServiceProvider();

        var inbox = sp.GetRequiredService<IInboxService>();
        await inbox.EnqueueAsync(new TestInboxCommand { Args = "a" }, new InboxMessageIdentity("m-1", "c-1"));
        await inbox.EnqueueAsync(new TestErrorInboxCommand { Args = "b" }, new InboxMessageIdentity("m-2", "c-1"));

        await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);

        Assert.AreEqual(2, MultiMessageHandler.Handled.Count);
        Assert.IsTrue(MultiMessageHandler.Handled.Contains(nameof(TestInboxCommand)));
        Assert.IsTrue(MultiMessageHandler.Handled.Contains(nameof(TestErrorInboxCommand)));

        foreach (var envelope in await GetDb(sp).Set<InboxEnvelope>().ToListAsync())
        {
            Assert.AreEqual(InboxMessageStatus.Succeeded, envelope.Status);
        }
    }

    [Test]
    public async Task Process_HandlerWithExplicitInterfaceImplementation_IsInvoked()
    {
        // Явная реализация компилируется в приватный метод: BindingFlags.Public её не видел,
        // и валидный обработчик молча не работал.
        var sp = InitInboxServiceCollection()
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, ExplicitHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestInboxCommand { Args = "explicit" },
            new InboxMessageIdentity("m-1", "c-1"));

        await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);

        Assert.IsTrue(ExplicitHandler.Handled);
        Assert.AreEqual(InboxMessageStatus.Succeeded, (await GetDb(sp).Set<InboxEnvelope>().SingleAsync()).Status);
    }

    [Test]
    public async Task Process_HandlerThrowingSynchronously_KeepsTheRealErrorMessage()
    {
        // Синхронный throw из не-async метода заворачивался в TargetInvocationException,
        // и в ErrorMessage попадало «Exception has been thrown by the target of an invocation».
        SyncThrowingHandler.Fail = true;

        var sp = InitInboxServiceCollection(retries: 1)
            .AddScoped<IInboxMessageHandler<TestInboxCommand>, SyncThrowingHandler>()
            .BuildServiceProvider();

        await sp.GetRequiredService<IInboxService>().EnqueueAsync(
            new TestInboxCommand { Args = "boom" },
            new InboxMessageIdentity("m-1", "c-1"));

        await sp.GetRequiredService<IInboxHandler>().ProcessAsync(CancellationToken.None);

        var envelope = await GetDb(sp).Set<InboxEnvelope>().SingleAsync();
        Assert.AreEqual(InboxMessageStatus.DeadLettered, envelope.Status);
        Assert.AreEqual("Handler threw synchronously", envelope.ErrorMessage);
    }

    private sealed class MultiMessageHandler : IInboxMessageHandler<TestInboxCommand>, IInboxMessageHandler<TestErrorInboxCommand>
    {
        public static readonly System.Collections.Concurrent.ConcurrentBag<string> Handled = [];

        public Task Process(TestInboxCommand message, CancellationToken cancellationToken)
        {
            Handled.Add(nameof(TestInboxCommand));
            return Task.CompletedTask;
        }

        public Task Process(TestErrorInboxCommand message, CancellationToken cancellationToken)
        {
            Handled.Add(nameof(TestErrorInboxCommand));
            return Task.CompletedTask;
        }
    }

    private sealed class ExplicitHandler : IInboxMessageHandler<TestInboxCommand>
    {
        public static bool Handled { get; set; }

        Task IInboxMessageHandler<TestInboxCommand>.Process(TestInboxCommand message, CancellationToken cancellationToken)
        {
            Handled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class SyncThrowingHandler : IInboxMessageHandler<TestInboxCommand>
    {
        public static bool Fail { get; set; }

        public Task Process(TestInboxCommand message, CancellationToken cancellationToken)
        {
            if (Fail)
            {
                throw new InvalidOperationException("Handler threw synchronously");
            }

            return Task.CompletedTask;
        }
    }
}