# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Монорепозиторий shared .NET NuGet-пакетов (`Dex.*`) для микросервисной платформы. Каждый модуль: самостоятельная библиотека со своим .sln, .csproj и версией. Публикуется на NuGet при мерже в `main`.

Детали каждого модуля (архитектура, DI, gotchas) описаны в `CLAUDE.md` внутри его каталога.

## Build & Test

```bash
dotnet build src/Dex.sln                    # собрать всё
dotnet test src/Dex.sln                     # все тесты
dotnet build src/Dex.Cap/Dex.Cap.sln        # крупный модуль, свой .sln
dotnet test <path>.csproj --filter "FullyQualifiedName~TestMethodName"
dotnet build --configuration Release src/Dex.sln   # release, генерирует .nupkg
```

Тестовая инфраструктура (PostgreSQL, RabbitMQ, Redis):
```bash
docker-compose -f src/docker-compose.yml up -d
```
ClickHouse доступен только в CI (`publish.yml`). Credentials: см. `src/docker-compose.yml`.

## Target Frameworks и версионирование

- `netstandard2.1 + net8.0`, версии `1.x`: Types, Extensions, Entity, Buffer, Inflector, Neo4J, Specifications, MassTransit
- `net8.0`, версии `8.x`: Cap, Audit, Lock, DistributedCache, Pagination, Specifications.EntityFramework
- Исключение: `Dex.Configuration.ProtectedJson` таргетит `netstandard2.0;net8.0`, но версия `8.x`
- Версия задаётся вручную в `<PackageVersion>` каждого .csproj
- Централизованные зависимости: `src/Directory.Build.targets`

## Каталог модулей

### Транзакционная надёжность и messaging

| Модуль | Назначение | Детали |
|---|---|---|
| Dex.Cap | Outbox (транзакционная публикация) + OnceExecutor (идемпотентность) | `src/Dex.Cap/CLAUDE.md` |
| Dex.MassTransit | Обёртки над MassTransit: RabbitMQ, SQS, OpenTelemetry tracing | `src/Dex.MassTransit/CLAUDE.md` |
| Dex.Events | Распределённые события поверх MassTransit, Outbox-интеграция | `src/Dex.Events/CLAUDE.md` |

### Аудит и безопасность

| Модуль | Назначение | Детали |
|---|---|---|
| Dex.Audit | Клиент-серверная система аудита: gRPC, MassTransit, EF interceptors | `src/Dex.Audit/CLAUDE.md` |
| Dex.Configuration | Автодешифровка JSON-конфигов, DataProtection, CLI для секретов | `src/Dex.Configuration/CLAUDE.md` |
| Dex.SecurityToken | Одноразовые security-токены с DataProtection | `src/Dex.SecurityToken/CLAUDE.md` |
| Dex.Response.Signing | Подпись HTTP-ответов (JWS/RS256) и верификация на клиенте | `src/Dex.Response.Signing/CLAUDE.md` |

### Инфраструктура

| Модуль | Назначение | Детали |
|---|---|---|
| Dex.Cache | Кэш HTTP-ответов с ETag, зависимостями и инвалидацией | `src/Dex.Cache/CLAUDE.md` |
| Dex.Lock | Распределённые async-блокировки с FIFO-очередью | `src/Dex.Lock/CLAUDE.md` |
| Dex.Specification | Паттерн Specification (Expression composition, EF Core) | `src/Dex.Specification/CLAUDE.md` |
| Dex.TransientExceptionsHandler | Определитель transient-ошибок для Polly/MassTransit retry | `src/Dex.TransientExceptionsHandler/CLAUDE.md` |

### Базовые утилиты (без отдельного CLAUDE.md)

`Dex.Types` (расширенные типы), `Dex.Extensions` (extension-методы), `Dex.Entity` (базовые сущности), `Dex.Buffer` (буферы), `Dex.Inflector` (плюрализация), `Dex.Neo4J` (обёртки Neo4jClient), `Dex.Pagination` (постраничная выдача, net8.0).

### Специализированные (без отдельного CLAUDE.md)

`Dex.CreditCardType.Resolver` (тип карты по номеру), `Dex.PdfGenerator` (PDF через RazorLight+DinkToPdf), `Dex.TeamCity` (service messages).

Полный каталог всех 55+ пакетов с описаниями: `docs/modules-reference.md`.

## CI/CD

GitHub Actions (`publish.yml`): build, pack, validate (Meziantou), test, deploy на NuGet (только из `main`). Каждый .sln (12 штук) собирается и тестируется отдельно.

## Conventions

- NuGet-метаданные: секция `<!--Для NuGet-->` в каждом .csproj
- PostgreSQL + xmin concurrency: исключать `OutboxEnvelope`, `LastTransaction` из `UseXminAsConcurrencyToken`
- DI-регистрация через `IServiceCollection` extensions (`AddOutbox<T>()`, `AddOnceExecutor<T>()` и т.д.)
- EF-конфигурация через `OnModelCreating` extensions (`OutboxModelCreating()`, `OnceExecutorModelCreating()`)
- Тесты: xUnit и NUnit (зависит от модуля), FluentAssertions для ассертов, Moq для мокирования
- Тестовые проекты: `Tests/Dex.<Module>.Tests` или `Tests/Dex.<Module>.<Provider>.Tests`
