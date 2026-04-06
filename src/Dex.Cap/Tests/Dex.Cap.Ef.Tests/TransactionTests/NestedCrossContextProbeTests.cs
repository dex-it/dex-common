using System;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef.Extensions;
using Dex.Cap.Ef.Tests.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.TransactionTests;

/// <summary>
/// Пробные тесты для проверки гипотезы о поведении ExecuteInTransactionAsync
/// при вложенных вызовах, когда внутренний вызов получает ДРУГОЙ инстанс DbContext.
/// Цель — зафиксировать фактическое поведение после миграции с TransactionScope.
/// </summary>
public class NestedCrossContextProbeTests : BaseTest
{
    [Test]
    public async Task Nested_DifferentInstance_MustThrowException()
    {
        // Сценарий:
        //   Outer ExecuteInTransactionAsync на контексте A.
        //   Внутри — новый scope, резолвим контекст B (новый инстанс того же типа).
        //   Inner ExecuteInTransactionAsync на B.
        //
        // Ожидание: ExecuteInTransactionAsync должен выбросить InvalidOperationException,
        // так как он обнаружит через AsyncLocal, что уже есть активная транзакция в другом контексте.
        var sp = InitServiceCollection().BuildServiceProvider();
        var outerCtx = sp.GetRequiredService<TestDbContext>();

        var outerId = Guid.NewGuid();
        var innerId = Guid.NewGuid();

        await outerCtx.ExecuteInTransactionAsync(async ct =>
        {
            outerCtx.Users.Add(new TestUser { Id = outerId, Name = "Outer_" + outerId, Years = 30 });
            await outerCtx.SaveChangesAsync(ct);

            using var innerScope = sp.CreateScope();
            var innerCtx = innerScope.ServiceProvider.GetRequiredService<TestDbContext>();

            TestContext.WriteLine($"Same instance? {ReferenceEquals(outerCtx, innerCtx)}");
            TestContext.WriteLine($"Inner CurrentTransaction is null? {innerCtx.Database.CurrentTransaction is null}");

            var ex = NUnit.Framework.Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await innerCtx.ExecuteInTransactionAsync(async ctInner =>
                {
                    innerCtx.Users.Add(new TestUser { Id = innerId, Name = "Inner_" + innerId, Years = 20 });
                    await innerCtx.SaveChangesAsync(ctInner);
                }, _ => Task.FromResult(false), cancellationToken: ct);
            });

            NUnit.Framework.Assert.That(ex!.Message, Does.Contain("Detected a nested call to ExecuteInTransactionAsync using a different DbContext instance"));
        }, _ => Task.FromResult(false));

        // Читаем из свежего контекста, чтобы не увидеть кэш.
        await using var verify = new TestDbContext(DbName);
        var outerExists = await verify.Users.AnyAsync(x => x.Id == outerId);
        var innerExists = await verify.Users.AnyAsync(x => x.Id == innerId);

        TestContext.WriteLine($"Outer in DB: {outerExists}, Inner in DB: {innerExists}");

        NUnit.Framework.Assert.Multiple(() =>
        {
            NUnit.Framework.Assert.That(outerExists, Is.True, "Outer должен быть успешно закомичен");
            NUnit.Framework.Assert.That(innerExists, Is.False, "Inner не должен был создаться");
        });
    }

    [Test]
    public Task Nested_DifferentInstance_Scoped_ResolvesToSameInstance_WithinRootSp()
    {
        try
        {
            // Вспомогательный тест, чтобы зафиксировать поведение DI:
            // при BuildServiceProvider() без явного CreateScope() Scoped-сервис резолвится из root-scope
            // и между двумя вызовами GetRequiredService вернётся ОДИН И ТОТ ЖЕ инстанс.
            // Это объясняет, почему существующие nested-тесты проходят.
            var sp = InitServiceCollection().BuildServiceProvider();
            var a = sp.GetRequiredService<TestDbContext>();
            var b = sp.GetRequiredService<TestDbContext>();

            NUnit.Framework.Assert.That(ReferenceEquals(a, b), Is.True, "Ожидается, что в root-scope Scoped сервис — singleton de facto");

            // А при явном новом scope — уже разные инстансы.
            using var scope = sp.CreateScope();
            var c = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            NUnit.Framework.Assert.That(ReferenceEquals(a, c), Is.False, "В новом scope должен быть новый инстанс");
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }
}