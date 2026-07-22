# Changelog — Dex.RfcExceptionsHandler

Формат основан на [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/).
Мажорная версия привязана к версии .NET; breaking-изменения помечаются в заголовке релиза.

## [8.1.0] — BREAKING

### Изменено
- Работает с новым контрактом `Dex.RfcAbstractions` 8.1.0 (`ErrorCategory` + `ErrorCode`
  вместо `StatusCode` / `RfcType`). HTTP-статус и RFC 9457 `type` резолвит middleware.
- Новая категория `IntegrationError` мапится в HTTP 503 (серверная retriable-семантика,
  попадает в `LogError` и алерты).
- Новая категория `Timeout` даёт `type` `timeout` (совпадает с fallback-путём 408).
- Fallback-`type` для незамапленных статусов стал URI-подобным: `/problems/unknown`
  (ранее `unknown`), 418 → `/problems/im-a-teapot`.
- Доменный `ErrorCode` нормализуется и валидируется: снимаются префикс `/problems/` и
  ведущие слэши; значение проверяется на формат lowercase-kebab
  `^[a-z0-9]+(-[a-z0-9]+)*(/[a-z0-9]+(-[a-z0-9]+)*)*$`. Невалидное или вырожденное
  значение отбрасывается с откатом на код по категории.
- Кастомные `Extensions` не могут перезаписать зарезервированные ключи RFC 9457
  (`type`/`title`/`status`/`detail`/`instance`) и служебные (`exceptionType`/
  `exceptionData`/`stackTrace`/`traceId`) — такие ключи пропускаются.

### Совместимость
- Минимально совместимая пара `Dex.RfcExceptionsHandler` + `Dex.RfcAbstractions` = 8.1.0.
- Сборки не strong-named: при смешении 8.0.x и 8.1.0 в рантайме возможен
  `TypeLoadException`. Общие библиотеки исключений требуют одновременной пересборки.
