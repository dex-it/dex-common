# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Монорепозиторий shared .NET NuGet-пакетов (`Dex.*`) для микросервисной платформы. Каждый модуль: самостоятельная библиотека со своим .sln, .csproj и версией. Публикуется на NuGet при мерже в `main`.

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

Тесты Dex.Cap, Dex.Cache, Dex.Events требуют PostgreSQL, RabbitMQ и Redis (docker-compose), ClickHouse доступен только в CI (`publish.yml`):

```bash
docker-compose -f src/docker-compose.yml up -d
```

Credentials для тестовой инфраструктуры: см. `src/docker-compose.yml`.

## Архитектура

### Target Frameworks и версионирование

- `netstandard2.1 + net8.0` (двойной таргет, версии `1.x`): Dex.Types, Dex.Extensions, Dex.Entity, Dex.Buffer, Dex.Inflector, Dex.Neo4J, Dex.Specifications, Dex.MassTransit
- `net8.0` (версии `8.x`): Dex.Cap, Dex.Audit, Dex.Lock, Dex.DistributedCache, Dex.Pagination, Dex.Specifications.EntityFramework
- Исключение: `Dex.Configuration.ProtectedJson` таргетит `netstandard2.0;net8.0`, но использует версию `8.x`
- Версия задаётся вручную в `<PackageVersion>` каждого .csproj
- Централизованное управление версиями зависимостей: `src/Directory.Build.targets`

### Ключевые модули

`Dex.Cap` — самый крупный модуль. Два паттерна:
- Outbox (`Dex.Cap.Outbox.*`): транзакционная отправка сообщений. `EnqueueAsync()` в рамках транзакции БД, фоновый scheduler обрабатывает очередь. Провайдеры: EF Core, Neo4j.
- OnceExecutor (`Dex.Cap.OnceExecutor.*`): идемпотентное выполнение по ключу. Провайдеры: EF Core, ClickHouse, Neo4j, Memory.
- Общий код в `Dex.Cap.Common` и `Dex.Cap.Common.Ef`.

`Dex.TransientExceptionsHandler` — конфигурируемый определитель transient-ошибок для Polly/MassTransit retry. `TransientExceptionsHandler.Default` покрывает стандартные сценарии (timeout, socket, Npgsql, HTTP 5xx, gRPC). Расширяется через builder-паттерн.

`Dex.Audit` — клиент-серверная система аудита с gRPC транспортом и EF Core interceptors. См. `src/Dex.Audit/README.md`.

`Dex.MassTransit` — обёртки над MassTransit для RabbitMQ и SQS с OpenTelemetry tracing.

Полный каталог всех 55+ пакетов с описаниями: `docs/modules-reference.md`.

### Тестовые фреймворки

Используются и xUnit, и NUnit (зависит от модуля). FluentAssertions для ассертов, Moq для мокирования.

## CI/CD

GitHub Actions (`publish.yml`): build, pack, validate (Meziantou), test, deploy на NuGet (только из `main`). Каждый .sln собирается и тестируется отдельно.

## Conventions

- Каждый NuGet-пакет содержит секцию `<!--Для NuGet-->` в .csproj с метаданными для публикации
- PostgreSQL + xmin optimistic concurrency: при использовании `UseXminAsConcurrencyToken` исключать служебные типы (`OutboxEnvelope`, `LastTransaction`)
- Регистрация через `IServiceCollection` extension methods (`AddOutbox<T>()`, `AddOnceExecutor<T>()` и т.д.)
- `OnModelCreating` extension methods для конфигурации EF моделей (`OutboxModelCreating()`, `OnceExecutorModelCreating()`)
- Тестовые проекты располагаются в `Tests/Dex.<Module>.Tests` или `Tests/Dex.<Module>.<Provider>.Tests`
- Все .sln (12 штук) собираются и тестируются CI независимо друг от друга
