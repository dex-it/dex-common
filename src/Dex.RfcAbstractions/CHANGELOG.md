# Changelog — Dex.RfcAbstractions

Формат основан на [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/).
Мажорная версия привязана к версии .NET; breaking-изменения помечаются в заголовке релиза.

## [8.1.0] — BREAKING

### Изменено
- Контракт `IRfcException` переработан: удалены `StatusCode` / `RfcType` / `RfcTypes`,
  вместо них `ErrorCategory Category` + опциональный `string? ErrorCode`. HTTP-статус
  и RFC 9457 `type` резолвит middleware по категории — домен развязан от транспорта.
- Namespace изменён: `Dex.RfcExceptions` → `Dex.RfcAbstractions`.
- `ErrorCode` и `Extensions` стали обычными членами интерфейса (не default interface
  members): прямые реализаторы обязаны объявить их явно (можно вернуть null). DIM-член,
  добавленный только в наследнике, не участвует в interface mapping и молча теряется.
- `ErrorCode` ожидается в формате lowercase-kebab (`^[a-z0-9-]+(/[a-z0-9-]+)*$`);
  значение вне формата middleware отбрасывает и подставляет type по категории.

### Совместимость
- Минимально совместимая пара `Dex.RfcExceptionsHandler` + `Dex.RfcAbstractions` = 8.1.0.
- Сборки не strong-named: при смешении 8.0.x и 8.1.0 в рантайме возможен
  `TypeLoadException`. Общие библиотеки исключений требуют одновременной пересборки.
