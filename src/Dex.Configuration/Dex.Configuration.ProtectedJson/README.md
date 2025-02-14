# dex.configuration.protectedjson

# Документация по Dex.Configuration.ProtectedJson

## Описание
Библиотека **Dex.Configuration.ProtectedJson** предоставляет механизм для работы с конфигурацией в формате JSON, содержащей зашифрованные данные. Она расширяет стандартный **JsonConfigurationProvider**, добавляя возможность автоматической расшифровки защищённых параметров при загрузке конфигурации.

## Использование

### 1. Подключение библиотеки
Добавьте пакет **Dex.Configuration.ProtectedJson** в ваш проект.

### 2. Создание **IDataProtector**
Реализуйте механизм защиты данных, например, с использованием **DataProtectionProvider**.
```csharp
using Microsoft.AspNetCore.DataProtection;

public class DataProtection
{
    public static IDataProtector CreateProtector(DirectoryInfo keysDirectory, string purpose)
    {
        IDataProtectionProvider dataProtectionProvider = DataProtectionProvider.Create(keysDirectory);

        return dataProtectionProvider.CreateProtector(purpose);
    }
}
```

### 3. Добавление защищённого JSON-файла в конфигурацию
```csharp
 builder.Configuration.AddProtectedJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json",
            (_, keysDirectory) =>
            {
                string dataProtectionPurpose = new DataProtectionPurpose().ComputePurpose();
                return DataProtection.CreateProtector(new DirectoryInfo(keysDirectory), dataProtectionPurpose);
            },
            new[]
            {
                "ConnectionStrings:DefaultConnection",
                "ClientCredentialsOptions:ApiKey"
            },
            optional: false,
            reloadOnChange: false);

IConfiguration configuration = builder.Build();
```
### 4. Использование расшифрованных данных
```csharp
string connectionString = configuration["ConnectionStrings:Default"];
Console.WriteLine(connectionString);
```

## Пример структуры JSON-файла
```json
{
  "ConfigurationProtectionOptions": {
    "KeysDirectory": "C:\\Keys",
    "ApplicationName": "MyApp"
  },
  "DefaultConnection": {
    "Default": "ENCRYPTED_VALUE"
  },
  "ClientCredentialsOptions": {
    "ApiKey": "ENCRYPTED_VALUE"
  }
}
```