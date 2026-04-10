# Dex.Response.Signing

Подпись тела HTTP-ответа (RS256/SHA256) и верификация на клиенте. Формат: JWS (header.payload.signature, Base64URL).

## Серверная сторона (подпись)

```csharp
services.AddResponseSigning(configuration);
// Регистрирует: IPrivateKeyExtractor (Singleton), ISignDataService, IJwsSignatureService (Transient)
```

На контроллере:
```csharp
[SignResponseFilter]
public ActionResult Get() { ... }
```

Подписываются только HTTP 200 ответы с ObjectResult. Ошибки проходят без подписи.

## Клиентская сторона (верификация)

```csharp
services.AddResponseVerifying(configuration);
services.AddHttpClient("Name", ...).AddResponseVerifyingHandler();
// Регистрирует: IPublicKeyExtractor (Singleton), IVerifySignService, IJwsParsingService, SignatureVerificationHandler
```

Верифицируются только 2xx ответы. JWS парсится, подпись проверяется, возвращается оригинальный payload.

## Конфигурация

```json
{
  "ResponseSigningOptions": {
    "DefaultPassword": "...",
    "Algorithm": "RS256"
  }
}
```

## Ограничения и gotchas

- Пустые ответы (null payload) не подписываются и не верифицируются
- Требуется embedded X.509 сертификат с RSA ключевой парой
- Алгоритм в JWS header должен совпадать с алгоритмом подписи
- Нет проверки expiration/audience: только чистая верификация подписи
