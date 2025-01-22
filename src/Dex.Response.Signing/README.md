# Сервис подписи и верификации ответов сервиса.

## Общее

- Реализована логика подписи ответа (только самого тела запроса) и его верификации на стороне, с которой отправлялся запрос.
- Функционал разбит отдельно на логику подписи и логику верификации.
- Если ответ неуспешен - он не шифруется и не расшифровывается
- Успешным считается ответ со статусом 200 (Ok)
- Так как шифруется только тело запроса - запрос должен его содержать.
- Для механизма шифрования и расшифровки используется сертификат, вшитый в сборку

## Назначение

- Исключить вероятность подмены сервиса, реализовав функционал шифрования и расшифровывания ответа 

## Подключение в проекте и использование

Необходимо добавить секцию в конфигурацию (поле Algorithm опционально, по дефолту используется RS256)
```
"ResponseSigningOptions": {
  "DefaultPassword": "wNx!a2*ThM",
  "Algorithm": "RS256"
}
```

### Подпись ответа

Подключение зависимостей
```
builder.Services.AddResponseSigning(builder.Configuration);
```

Для шифрования ответа по определенному пути необходимо добавить фильтр на контроллер/метод
```
[HttpGet]
[SignResponseFilter]
public ActionResult<object> Get()
{
    return Ok(new
    {
        TestNum = 180,
        TestString = "Test",
        TestDate = DateTime.Now
    });
}
```

### Верификация ответа

Подключение зависимостей
```
builder.Services.AddResponseVerifying(builder.Configuration);
```

Добавляем обработчики для http-запросов (в примере нативный и рефит)
```
builder.Services.AddHttpClient(
    "TestClient",
    client =>
    {
        client.BaseAddress = new Uri("http://localhost:5678");
    })
    .AddResponseVerifyingHandler();

builder.Services
    .AddRefitClient<IRespondentApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5678"))
    .AddResponseVerifyingHandler();
```