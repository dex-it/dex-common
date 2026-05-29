# dex-common

Монорепозиторий shared NuGet-библиотек для разработки микросервисных приложений на .NET 8.

Каждый модуль — самостоятельный пакет со своим `.sln`, `.csproj` и версией. Публикуется на внутренний NuGet при мерже в `main` (см. `.github/workflows/publish.yml`).

Полный каталог всех пакетов с описаниями: [`docs/modules-reference.md`](docs/modules-reference.md).

## Состав

### Транзакционная надёжность — `Dex.Cap`
- **Outbox** — транзакционная публикация сообщений (EF Core, Neo4j; scheduler; MassTransit-интеграция)
- **OnceExecutor** — идемпотентное выполнение операций по ключу (EF Core, Memory, ClickHouse, Neo4j)

См. [`src/Dex.Cap/README.md`](src/Dex.Cap/README.md).

### Аудит — `Dex.Audit`
Клиент-серверная система аудита. Транспорт: MassTransit (RabbitMQ) или gRPC. EF Core interceptors для автоаудита SaveChanges/транзакций, интеграция с MediatR pipeline и `ILogger`.

См. [`src/Dex.Audit/README.md`](src/Dex.Audit/README.md).

### Messaging
- **`Dex.MassTransit`** — обёртки над MassTransit (Rabbit, SQS, ActivityTrace/OpenTelemetry)
- **`Dex.Events.Distributed`** — распределённые события поверх MassTransit; интеграция с Outbox ([README](src/Dex.Events/README.md))

### Инфраструктура
- **`Dex.DistributedCache`** — кэш HTTP-ответов с ETag, зависимостями и инвалидацией ([README](src/Dex.Cache/README.md))
- **`Dex.Lock`** — распределённые блокировки
- **`Dex.SecurityTokenProvider`** + `Dex.SecurityToken.DistributedStorage` — одноразовые security-токены
- **`Dex.Specifications`** (+ `.EntityFramework`, `.Extensions`) — паттерн Specification

### Конфигурация и безопасность
- **`Dex.Configuration.ProtectedJson`** — JSON-конфиги с автоматической расшифровкой полей ([README](src/Dex.Configuration/Dex.Configuration.ProtectedJson/README.md))
- **`Dex.Configuration.DataProtection`** + `.Cli` — фабрика `IDataProtector` и CLI для шифрования секретов ([README](src/Dex.Configuration/Dex.Configuration.Dataprotection.Cli/README.md))
- **`Dex.ResponseSigning`** — подпись тела HTTP-ответа и верификация на клиенте ([README](src/Dex.Response.Signing/README.md))
- **`Dex.TransientExceptions`** — конфигурируемый определитель transient-ошибок для Polly/MassTransit retry ([README](src/Dex.TransientExceptionsHandler/Dex.TransientExceptions/README.md))

### Базовые утилиты (netstandard2.1 + net8.0)
`Dex.Types`, `Dex.Extensions`, `Dex.Entity`, `Dex.Buffer`, `Dex.Inflector`, `Dex.Neo4J`

### Утилиты (net8.0)
`Dex.Pagination`

### Специализированное
`Dex.CreditCardType.Resolver`, `Dex.PdfGenerator`, `Dex.TeamCity`

## Сборка и тесты

```bash
# Сборка
dotnet build src/Dex.sln

# Тесты (все)
dotnet test src/Dex.sln

# Тесты одного модуля
dotnet test src/Dex.Cap/Dex.Cap.sln
```

Для тестов, требующих инфраструктуру (PostgreSQL, RabbitMQ, Redis):

```bash
docker-compose -f src/docker-compose.yml up -d
```

## Target Frameworks

- Базовые утилиты — `netstandard2.1 + net8.0` (версии `1.x`)
- Модули с EF Core / ASP.NET зависимостями — `net8.0` (версии `8.x`)

Централизованные версии зависимостей — в [`src/Directory.Build.targets`](src/Directory.Build.targets).

## Лицензия

MIT — см. [LICENSE](LICENSE).
