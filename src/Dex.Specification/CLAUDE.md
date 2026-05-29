# Dex.Specification

Паттерн Specification через композицию LINQ Expression. Работает и в памяти, и с EF Core.

## Создание спецификаций

```csharp
var spec = new Specification<Company>(c => c.Employees > 5);
```

EF-специфичные:
```csharp
new EfEqualSpecification<Company, Guid>(c => c.Id, companyId);
new EfLikeSpecification<Company>(c => c.Name, "%acme%");
new EfInSpecification<Company, int>(c => c.Status, new[] { 1, 2, 3 });
```

Билдер `Sp<T>`:
```csharp
var spec = Sp<Company>
    .Equal(c => c.Id, id)
    .AndEqual(c => c.Employees, 5)
    .AndLike(c => c.Name, "acme");
```

## Композиция

Операторы: `&` (AND), `|` (OR), `!` (NOT).
Extension-методы: `.And()`, `.Or()`, `.Not()` с лямбда-вариантами.

```csharp
var combined = spec1 & spec2;                          // AND
var either = spec1 | spec2;                            // OR
var negated = !spec1;                                  // NOT
var chained = spec.And(s => s.AndEqual(c => c.Active, true));  // builder
```

## Использование с IQueryable

Неявное преобразование в `Expression<Func<T, bool>>`:
```csharp
var results = dbContext.Companies.Where(specification).ToList();  // EF Core
var results = companies.Where(specification).ToList();            // in-memory
```

`DefaultSpecification<T>` всегда возвращает true (базовая для "выбрать все").

## Ограничения и gotchas

- EF-спецификации (`EfEqualSpecification` и др.) используют `EF.Property<T>()`, работают только в EF-запросах
- Компиляция предиката ленивая (один раз на экземпляр Specification): создавать один раз, переиспользовать
- Лямбда-вариант `.And(s => ...)` создаёт промежуточную `Specification(t => true)` как базу
- Expression tree должен быть транслируем в SQL для EF Core запросов
