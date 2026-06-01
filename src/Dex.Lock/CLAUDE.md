# Dex.Lock

Распределённые асинхронные блокировки с гарантией FIFO-порядка.

## Интерфейсы

- `IAsyncLock<T>`: одна блокировка, `LockAsync()` возвращает `ValueTask<T>`
- `IAsyncLockProvider<T, TR>`: создаёт/управляет блокировками по ключу (ConcurrentDictionary)
- `IAsyncLockProviderFactory<TR>`: фабрика провайдеров с уникальным scope (instanceId)

## Использование

```csharp
var provider = factory.Create<string>();
var locker = provider.GetLocker("myKey");

// Вариант 1: ручное освобождение
using (await locker.LockAsync())
{
    // критическая секция
}

// Вариант 2: через делегат
await asyncLock.LockAsync(async () => { /* работа */ });
```

## Реализация (AsyncLock)

- Быстрый атомарный путь для неконкурентных блокировок
- FIFO-очередь для ожидающих задач при конкуренции
- LockReleaser (struct, IDisposable): short token предотвращает двойное освобождение
- Thread-safe: object sync для очереди, атомарные операции для флага блокировки

## Ограничения и gotchas

- Блокировка per-key: одинаковый ключ = одна блокировка, разные ключи = разные блокировки
- `AsyncLockProvider<T>` растёт неограниченно (нет eviction неиспользуемых блокировок)
- `RemoveLocker()` доступен только на провайдере, не в публичном интерфейсе
