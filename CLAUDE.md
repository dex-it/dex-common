# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Monorepo of shared .NET NuGet-пакетов (`Dex.*`) для микросервисной платформы. Каждый модуль — самостоятельная библиотека со своим .sln, .csproj и версией. Публикуется на NuGet при мерже в `main`.

## Build & Test

```bash
# Собрать всё
dotnet build src/Dex.sln

# Собрать конкретный модуль (у крупных модулей свой .sln)
dotnet build src/Dex.Cap/Dex.Cap.sln
dotnet build src/Dex.Audit/Dex.Audit.sln

# Запуск всех тестов
dotnet test src/Dex.sln

# Запуск тестов конкретного проекта
dotnet test src/Dex.Cap/Tests/Dex.Cap.Ef.Tests/Dex.Cap.Ef.Tests.csproj

# Запуск одного теста
dotnet test src/Dex.Cap/Tests/Dex.Cap.Ef.Tests/Dex.Cap.Ef.Tests.csproj --filter "FullyQualifiedName~TestMethodName"

# Release-сборка (генерирует .nupkg)
dotnet build --configuration Release src/Dex.sln
```

### Инфраструктура для тестов

Тесты Dex.Cap, Dex.Cache, Dex.Events требуют PostgreSQL, RabbitMQ, и/или ClickHouse:

```bash
docker-compose -f src/docker-compose.yml up -d
```

Credentials: postgres/`my-pass~003`, rabbitmq guest/guest, redis без пароля.

## Архитектура

### Target Frameworks

- Базовые библиотеки (Dex.Types, Dex.Extensions, Dex.Entity) — **netstandard2.1 + net8.0** (двойной таргет, версии `1.x`)
- Модули с EF Core / ASP.NET зависимостями (Dex.Cap, Dex.Audit) — **net8.0** (версии `8.x`)

### Версионирование

Версия задаётся вручную в `<PackageVersion>` каждого .csproj. Библиотеки netstandard имеют версии `1.x`, net8.0-only — `8.x`. Централизованное управление версиями зависимостей — через `src/Directory.Build.targets`.

### Ключевые модули

**Dex.Cap** — самый крупный модуль. Два паттерна:
- **Outbox** (`Dex.Cap.Outbox.*`): транзакционная отправка сообщений. EnqueueAsync() в рамках транзакции БД, фоновый scheduler обрабатывает очередь. Провайдеры: EF Core, Neo4j.
- **OnceExecutor** (`Dex.Cap.OnceExecutor.*`): идемпотентное выполнение по ключу. Провайдеры: EF Core, ClickHouse, Neo4j, Memory.
- Общий код в `Dex.Cap.Common` и `Dex.Cap.Common.Ef`.

**Dex.TransientExceptionsHandler** — конфигурируемый определитель transient-ошибок для Polly/MassTransit retry. `TransientExceptionsHandler.Default` покрывает стандартные сценарии (timeout, socket, Npgsql, HTTP 5xx, gRPC). Расширяется через builder-паттерн.

**Dex.Audit** — клиент-серверная система аудита с gRPC транспортом и EF Core interceptors.

**Dex.MassTransit** — обёртки над MassTransit для RabbitMQ и SQS с OpenTelemetry tracing.

### Тестовые фреймворки

Используются и **xUnit**, и **NUnit** (зависит от модуля). FluentAssertions для ассертов, Moq для мокирования.

## CI/CD

GitHub Actions (`publish.yml`): build → pack → validate (Meziantou) → test → deploy на NuGet (только из `main`). Каждый .sln собирается и тестируется отдельно.

## Полный список библиотек

### Dex.Cap — Outbox + OnceExecutor (транзакционная надёжность)
| Пакет | Назначение |
|---|---|
| `Dex.Cap.Common` | Общие абстракции Cap (исключения, стратегии, helpers) |
| `Dex.Cap.Common.Ef` | Общие EF-утилиты: `ExecuteInTransaction`, savepoint-логика, retry |
| `Dex.Cap.Outbox` | Реализация паттерна Outbox (транзакционная публикация сообщений) |
| `Dex.Cap.Outbox.Ef` | EF Core-провайдер для Outbox |
| `Dex.Cap.Outbox.Neo4j` | Neo4j-провайдер для Outbox |
| `Dex.Cap.Outbox.AspNetScheduler` | Фоновый HostedService, обрабатывающий outbox-очередь |
| `Dex.Cap.Outbox.OnceExecutor.MassTransit` | Интеграция Outbox с MassTransit (публикация через шину) |
| `Dex.Cap.OnceExecutor` | Идемпотентное выполнение операций по ключу |
| `Dex.Cap.OnceExecutor.Ef` | EF Core-провайдер для OnceExecutor |
| `Dex.Cap.OnceExecutor.Memory` | In-memory провайдер (для тестов) |
| `Dex.Cap.OnceExecutor.ClickHouse` | ClickHouse-провайдер |
| `Dex.Cap.OnceExecutor.Neo4j` | Neo4j-провайдер |
| `Dex.Cap.OnceExecutor.AspNetScheduler` | HostedService для периодической очистки идемпотентных ключей |

### Dex.Audit — система аудита (клиент-сервер)
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

### Dex.MassTransit — обёртки над MassTransit
| Пакет | Назначение |
|---|---|
| `Dex.MassTransit` | Базовые абстракции и helpers |
| `Dex.MassTransit.Rabbit` | Конфигурация шины для RabbitMQ |
| `Dex.MassTransit.SQS` | Конфигурация шины для AWS SQS |
| `Dex.MassTransit.ActivityTrace` | OpenTelemetry tracing для сообщений MassTransit |

### Dex.Events — распределённые события
| Пакет | Назначение |
|---|---|
| `Dex.Events.Distributed` | Publish/Subscribe распределённых событий поверх MassTransit |
| `Dex.Events.Distributed.OutboxExtensions` | Интеграция распределённых событий с Outbox (транзакционная публикация) |

### Dex.Cache — распределённое кэширование
| Пакет | Назначение |
|---|---|
| `Dex.DistributedCache` | Кэш HTTP-ответов с поддержкой ETag, зависимостей и инвалидации поверх `IDistributedCache` |

### Dex.Configuration — защищённая конфигурация
| Пакет | Назначение |
|---|---|
| `Dex.Configuration.ProtectedJson` | `JsonConfigurationProvider` с автоматической расшифровкой полей через `IDataProtector` |
| `Dex.Configuration.DataProtection` | Фабрика `IDataProtector` (файловые ключи, ротация, AES-256-CBC) |
| `Dex.Configuration.DataProtection.Cli` | CLI для шифрования/расшифровки секретов в JSON-конфигах |

### Dex.SecurityToken — одноразовые токены
| Пакет | Назначение |
|---|---|
| `Dex.SecurityTokenProvider` | Генерация и валидация security-токенов |
| `Dex.SecurityToken.DistributedStorage` | Распределённое хранилище токенов |

### Dex.Specification — паттерн Specification
| Пакет | Назначение |
|---|---|
| `Dex.Specifications` | Базовый паттерн Specification (композиция предикатов) |
| `Dex.Specifications.Extensions` | Расширения и комбинаторы |
| `Dex.Specifications.EntityFramework` | Интеграция с EF Core (`IQueryable.Where(spec)`) |

### Dex.Lock — распределённые блокировки
| Пакет | Назначение |
|---|---|
| `Dex.Lock` | Распределённые блокировки (in-memory / БД) |

### Dex.Response.Signing — подпись HTTP-ответов
| Пакет | Назначение |
|---|---|
| `Dex.ResponseSigning` | Подпись тела ответа контроллера и верификация на клиенте (RS256 по умолчанию), защита от подмены сервиса |

### Dex.TransientExceptionsHandler
| Пакет | Назначение |
|---|---|
| `Dex.TransientExceptions` | Конфигурируемый определитель transient-ошибок для Polly/MassTransit retry (Default покрывает timeout/socket/Npgsql/HTTP 5xx/gRPC) |

### Базовые утилиты (netstandard2.1 + net8.0)
| Пакет | Назначение |
|---|---|
| `Dex.Types` | Расширенные типы (ranges и т.п.) |
| `Dex.Extensions` | Общие extension-методы |
| `Dex.Entity` | Базовые интерфейсы/классы сущностей (Id, audit fields) |
| `Dex.Buffer` | Буферы/пулы |
| `Dex.Pagination` | DTO и helpers для постраничной выдачи |
| `Dex.Inflector` | Склонение/плюрализация строк |
| `Dex.Neo4J` | Обёртки/хелперы для Neo4jClient |

### Специализированные
| Пакет | Назначение |
|---|---|
| `Dex.CreditCardType.Resolver` | Определение типа банковской карты по номеру (Visa, MC, etc.) |
| `Dex.PdfGenerator` | Генерация PDF через RazorLight + DinkToPdf |
| `Dex.TeamCity` | Интеграция с TeamCity (service messages для сборочного агента) |

## Conventions

- Каждый NuGet-пакет содержит секцию `<!--Для NuGet-->` в .csproj с метаданными для публикации
- PostgreSQL + xmin optimistic concurrency — при использовании `UseXminAsConcurrencyToken` нужно исключать служебные типы (OutboxEnvelope, LastTransaction)
- Регистрация через `IServiceCollection` extension methods (`AddOutbox<T>()`, `AddOnceExecutor<T>()` и т.д.)
- `OnModelCreating` extension methods для конфигурации EF моделей (`OutboxModelCreating()`, `OnceExecutorModelCreating()`)
