# Changelog — Dex.RfcAbstractions

Формат основан на [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/),
проект придерживается [Semantic Versioning](https://semver.org/lang/ru/).

## [8.1.0] — BREAKING

### Изменено
- Контракт `IRfcException` переработан: удалены `StatusCode` / `RfcType` / `RfcTypes`,
  вместо них `ErrorCategory Category` + опциональный `string? ErrorCode`. HTTP-статус
  и RFC 9457 `type` резолвит middleware по категории — домен развязан от транспорта.
- Namespace изменён: `Dex.RfcExceptions` → `Dex.RfcAbstractions`.
- `ErrorCode` стал обычным членом интерфейса (не default interface member): прямые
  реализаторы обязаны объявить его явно (`public string? ErrorCode => null;`).

### Совместимость
- Минимально совместимая пара `Dex.RfcExceptionsHandler` + `Dex.RfcAbstractions` = 8.1.0.
- Сборки не strong-named: при смешении 8.0.x и 8.1.0 в рантайме возможен
  `TypeLoadException`. Общие библиотеки исключений требуют одновременной пересборки.
