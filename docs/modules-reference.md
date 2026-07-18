# Полный каталог пакетов dex-common

## Dex.Cap — Outbox + Inbox + OnceExecutor (транзакционная надёжность)

| Пакет | Назначение |
|---|---|
| `Dex.Cap.Common` | Общие абстракции Cap (исключения, стратегии, helpers) |
| `Dex.Cap.Common.Ef` | Общие EF-утилиты: `ExecuteInTransaction`, savepoint-логика, retry |
| `Dex.Cap.Outbox` | Реализация паттерна Outbox (транзакционная публикация сообщений) |
| `Dex.Cap.Outbox.Ef` | EF Core-провайдер для Outbox |
| `Dex.Cap.Outbox.Neo4j` | Neo4j-провайдер для Outbox |
| `Dex.Cap.Outbox.AspNetScheduler` | Фоновый HostedService, обрабатывающий outbox-очередь |
| `Dex.Cap.Outbox.OnceExecutor.MassTransit` | Интеграция Outbox с MassTransit (публикация через шину) |
| `Dex.Cap.Inbox` | Реализация паттерна Inbox: дедупликация входящих сообщений и фоновая обработка |
| `Dex.Cap.Inbox.Ef` | EF Core-провайдер для Inbox (PostgreSQL) |
| `Dex.Cap.Inbox.AspNetScheduler` | Фоновые HostedService: обработка inbox-очереди и её чистка |
| `Dex.Cap.OnceExecutor` | Идемпотентное выполнение операций по ключу |
| `Dex.Cap.OnceExecutor.Ef` | EF Core-провайдер для OnceExecutor |
| `Dex.Cap.OnceExecutor.Memory` | In-memory провайдер (для тестов) |
| `Dex.Cap.OnceExecutor.ClickHouse` | ClickHouse-провайдер |
| `Dex.Cap.OnceExecutor.Neo4j` | Neo4j-провайдер |
| `Dex.Cap.OnceExecutor.AspNetScheduler` | HostedService для периодической очистки идемпотентных ключей |

Подробности: [`src/Dex.Cap/README.md`](../src/Dex.Cap/README.md)

## Dex.Audit — система аудита (клиент-сервер)

| Пакет | Назначение |
|---|---|
| `Dex.Audit.Domain` | Доменные модели аудита (события, настройки) |
| `Dex.Audit.Client.Abstractions` | Интерфейсы клиента аудита |
| `Dex.Audit.Client` | Базовая реализация клиента: enrichment событий и отправка на сервер |
| `Dex.Audit.Client.Implementations` | Готовая реализация клиента поверх MassTransit/RabbitMQ + Memory Cache |
| `Dex.Audit.Client.Implementations.Grpc` | Готовая реализация клиента через gRPC |
| `Dex.Audit.Server.Abstractions` | Интерфейсы сервера аудита |
| `Dex.Audit.Server` | Базовая реализация сервера: приём событий, управление настройками |
| `Dex.Audit.Server.Implementations` | Готовая реализация сервера (EF Core + MassTransit) |
| `Dex.Audit.Server.Implementations.Grpc` | Готовая gRPC-реализация сервера |
| `Dex.Audit.Implementations.Common` | Общий код для Client/Server implementations |
| `Dex.Audit.EF.Interceptors.Abstractions` | Интерфейс `IAuditEntity` для автоаудита |
| `Dex.Audit.EF.Interceptors` | EF Core interceptors: автоаудит SaveChanges и транзакций |
| `Dex.Audit.Logger` | `ILogger`-адаптер: `logger.LogAudit(...)` пишет аудит-события |
| `Dex.Audit.MediatR` | MediatR pipeline behavior для аудита команд и ответов |

Подробности: [`src/Dex.Audit/README.md`](../src/Dex.Audit/README.md)

## Dex.MassTransit — обёртки над MassTransit

| Пакет | Назначение |
|---|---|
| `Dex.MassTransit` | Базовые абстракции и helpers |
| `Dex.MassTransit.Rabbit` | Конфигурация шины для RabbitMQ |
| `Dex.MassTransit.SQS` | Конфигурация шины для AWS SQS |
| `Dex.MassTransit.ActivityTrace` | OpenTelemetry tracing для сообщений MassTransit |

## Dex.Events — распределённые события

| Пакет | Назначение |
|---|---|
| `Dex.Events.Distributed` | Publish/Subscribe распределённых событий поверх MassTransit |
| `Dex.Events.Distributed.OutboxExtensions` | Интеграция распределённых событий с Outbox (транзакционная публикация) |

Подробности: [`src/Dex.Events/README.md`](../src/Dex.Events/README.md)

## Dex.Cache — распределённое кэширование

| Пакет | Назначение |
|---|---|
| `Dex.DistributedCache` | Кэш HTTP-ответов с поддержкой ETag, зависимостей и инвалидации поверх `IDistributedCache` |

Подробности: [`src/Dex.Cache/README.md`](../src/Dex.Cache/README.md)

## Dex.Configuration — защищённая конфигурация

| Пакет | Назначение |
|---|---|
| `Dex.Configuration.ProtectedJson` | `JsonConfigurationProvider` с автоматической расшифровкой полей через `IDataProtector` |
| `Dex.Configuration.DataProtection` | Фабрика `IDataProtector` (файловые ключи, ротация, AES-256-CBC) |
| `Dex.Configuration.DataProtection.Cli` | CLI для шифрования/расшифровки секретов в JSON-конфигах |

Подробности: [`src/Dex.Configuration/Dex.Configuration.ProtectedJson/README.md`](../src/Dex.Configuration/Dex.Configuration.ProtectedJson/README.md), [`CLI README`](../src/Dex.Configuration/Dex.Configuration.Dataprotection.Cli/README.md)

## Dex.SecurityToken — одноразовые токены

| Пакет | Назначение |
|---|---|
| `Dex.SecurityTokenProvider` | Генерация и валидация security-токенов |
| `Dex.SecurityToken.DistributedStorage` | Распределённое хранилище токенов |

## Dex.Specification — паттерн Specification

| Пакет | Назначение |
|---|---|
| `Dex.Specifications` | Базовый паттерн Specification (композиция предикатов) |
| `Dex.Specifications.Extensions` | Расширения и комбинаторы |
| `Dex.Specifications.EntityFramework` | Интеграция с EF Core (`IQueryable.Where(spec)`) |

## Dex.Lock — распределённые блокировки

| Пакет | Назначение |
|---|---|
| `Dex.Lock` | Распределённые блокировки (in-memory / БД) |

## Dex.Response.Signing — подпись HTTP-ответов

| Пакет | Назначение |
|---|---|
| `Dex.ResponseSigning` | Подпись тела ответа контроллера и верификация на клиенте (RS256 по умолчанию), защита от подмены сервиса |

Подробности: [`src/Dex.Response.Signing/README.md`](../src/Dex.Response.Signing/README.md)

## Dex.TransientExceptionsHandler

| Пакет | Назначение |
|---|---|
| `Dex.TransientExceptions` | Конфигурируемый определитель transient-ошибок для Polly/MassTransit retry (Default покрывает timeout/socket/Npgsql/HTTP 5xx/gRPC) |

Подробности: [`src/Dex.TransientExceptionsHandler/Dex.TransientExceptions/README.md`](../src/Dex.TransientExceptionsHandler/Dex.TransientExceptions/README.md)

## Базовые утилиты (netstandard2.1 + net8.0)

| Пакет | Назначение |
|---|---|
| `Dex.Types` | Расширенные типы (ranges и т.п.) |
| `Dex.Extensions` | Общие extension-методы |
| `Dex.Entity` | Базовые интерфейсы/классы сущностей (Id, audit fields) |
| `Dex.Buffer` | Буферы/пулы |
| `Dex.Inflector` | Склонение/плюрализация строк |
| `Dex.Neo4J` | Обёртки/хелперы для Neo4jClient |

## net8.0-only утилиты

| Пакет | Назначение |
|---|---|
| `Dex.Pagination` | DTO и helpers для постраничной выдачи |

## Специализированные

| Пакет | Назначение |
|---|---|
| `Dex.CreditCardType.Resolver` | Определение типа банковской карты по номеру (Visa, MC, etc.) |
| `Dex.PdfGenerator` | Генерация PDF через RazorLight + DinkToPdf |
| `Dex.TeamCity` | Интеграция с TeamCity (service messages для сборочного агента) |
