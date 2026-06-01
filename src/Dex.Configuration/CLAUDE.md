# Dex.Configuration

Защищённая конфигурация: автоматическая расшифровка полей JSON-конфигов и CLI для управления секретами.

## Dex.Configuration.ProtectedJson

Расширяет `JsonConfigurationProvider`: поля, помеченные как защищённые, автоматически расшифровываются при загрузке конфига.

```csharp
builder.Configuration.AddProtectedJsonFile("appsettings.protected.json");
```

Обязательные секции в конфиге:
- `ConfigurationProtectionOptions:KeysDirectory`: путь к ключам шифрования
- `ConfigurationProtectionOptions:ApplicationName`: имя приложения (для вычисления Purpose)

Защищённые ключи указываются как colon-separated пути: `"ConnectionStrings:DefaultConnection"`.

## Dex.Configuration.DataProtection

Фабрика `IDataProtector` на базе ASP.NET Core DataProtection.
- Алгоритм: AES-256-CBC (через DPAPI)
- Хранение ключей: файловое (создаётся автоматически)
- Ротация: встроенная (90 дней через IDataProtector)
- Purpose: SHA256(applicationName + projectName + salt)

## Dex.Configuration.DataProtection.Cli

Команды:
- `protect` — зашифровать plain text
- `unprotect` — расшифровать (только DEBUG)
- `unprotect-file` — расшифровать JSON-конфиг (несколько ключей)
- `protect-encrypted` — перешифровать данные с embedded-сертификата
- `encrypt` — зашифровать через embedded-сертификат (только DEBUG)

## Ограничения и gotchas

- Конфигурация загружается при старте приложения, без runtime DI
- Ключи шифрования не рекомендуется использовать для долгоживущих данных (риск компрометации)
- `unprotect` и `encrypt` доступны только в DEBUG-сборке
