## Решение Dex.DataProtection

Содержит сборку `Dex.Configuration.DataProtection` по созданию `IDataProtector` служащий для защиты данных.

Данный `IDataProtector` создается с параметрами:  
- Ключи хранятся на файловой системе (параметр `keysDirectory`) - будут созданы при шифровании (если не найдены)  
- Purpose (предназначение) определяется через `applicationName` (имя приложения) и `projectName` (имя проекта)
- Алгоритм шифрования: по умолчанию (`AES-256-CBC`)  
- Время жизни ключей: по умолчанию (90 дней) - ротация обеспечивается самим `IDataProtector`  

Не рекомендуется использовать `IDataProtector` для долгоживущих шифрованных данных (в связи с возможностью потери/кражи ключей)

Данное решение также содержит CLI `Dex.Configuration.DataProtection.Cli` которое можно использовать для шифрования/расшифрования данных.

В решение добавлена возможность работы с зашифрованными данными. Данные шифруются вшитым в сборку сертификатом.
Получение зашифрованных данных возможно только в DEBUG-режиме.

Команда для генерации сертификата:
```
New-SelfSignedCertificate -Subject "CN=DataProtectionContainer" -FriendlyName DataProtection -CertStoreLocation "Cert:\CurrentUser\My" -KeyExportPolicy Exportable -KeyLength 2048 -KeyAlgorithm RSA -HashAlgorithm SHA256 -Provider "Microsoft Enhanced RSA and AES Cryptographic Provider" -NotAfter (Get-Date).AddYears(10) -Verbose
```

Команды:  
`protect` - для шифрования данных  
`unprotect` - для расшифровки данных (доступна при сборке в Debug конфигурации, убрано из сборки Release конфигурации)  
`unprotect-file` - для расшифровки данных из файла конфигураций (json)  
`protect-encrypted` - для перешифровки данных (на вход получаем зашифрованные вшитым в решение сертификатом, расшифровываем его и вновь шифруем)
`encrypt` - шифрует данные вшитым в сборку сертификатом. Доступно только в DEBUG-режиме.


Параметры команд:  
```
--keys-directory - директория нахождения ключей  
```

```
--project-name - имя проекта

доступные аргументы (расширяется при необходимости в enum ProjectName):
`Template`
```

```
--application-name - имя приложения (необязателен при расшифровки файла)

Доступные аргументы для проекта Template (расширяется при необходимости в enum TemplateApplicationName):
AuditWriter
```

```
--data - данные для шифровки/расшифровки/перешифровки  
```

```
--file - имя файла для расшифровки
```

```
--configuration-key - ключ в конфигурации который необходимо расшифровать (можно передать несколько ключей)
```

Пример вызова CLI:  
Зашифровать данные:  
```shell
.\Dex.Configuration.DataProtection.Cli.exe protect --keys-directory "./keys" --project-name "Template" --application-name "AuditWriter" --data "data_to_protect"
```

Расшифровать файл:  
```shell
.\Dex.Configuration.DataProtection.Cli.exe unprotect-file --keys-directory "./keys" --project-name "Template" --file "./configs/appsettings.Production.json" --configuration-key "AuthorizationSettings:ApiResourceSecret" --configuration-key "RabbitMqOptions:Password"
```

Перешифровать данные (заранее зашифрованные вшитым сертификатом):
```shell
.\Dex.Configuration.DataProtection.Cli.exe protect-encrypted --keys-directory "./keys" --project-name "Template" --application-name "AuditWriter" --data "encrypted_data_to_reencrypt"
```

Зашифровать данные вшитым сертификатом:  
```shell
.\Dex.Configuration.DataProtection.Cli.exe encrypt --data "data_to_encrypt"
```