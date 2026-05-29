# Dex.Specifications

Lightweight implementation of the **Specification pattern** for .NET — boolean predicates that can be combined with `&`, `|`, `!`, projected across types, and then handed to LINQ, Entity Framework Core, or used standalone for in-memory checks / validation.

| Package | Purpose |
|---|---|
| `Dex.Specifications` | Core: `Specification<T>`, composites (`AndSpecification`, `OrSpecification`, `NotSpecification`), `ValidationSpecification<T>`, fluent `FilterSpecification<T>`. |
| `Dex.Specifications.EntityFramework` | Specifications that emit EF Core shadow-property predicates: `EfEqualSpecification`, `EfInSpecification`, `EfLikeSpecification`. |
| `Dex.Specifications.Extensions` | Fluent `And` / `Or` / `Not` extension methods so you can build specifications without the `&` / `|` / `!` operators. |

---

## Core — `Dex.Specifications`

### `Specification<T>`

A reusable predicate over `T`:

```csharp
var adult        = new Specification<User>(u => u.Age >= 18);
var inMoscow     = new Specification<User>(u => u.City == "Moscow");
var adultMuscovite = adult & inMoscow;            // AndSpecification
var notMinor       = !new Specification<User>(u => u.Age < 18);
```

`Specification<T>` implicitly converts to:

* `Predicate<T>`         (`List<T>.FindAll(spec)`)
* `Func<T, bool>`        (`items.Where(spec)`)
* `Expression<Func<T, bool>>` (`dbSet.Where(spec)` — EF Core translates it)

…and exposes `IsSatisfiedBy(T item)` for explicit checks.

### Composites

```csharp
var multi = new AndSpecification<User>(adult, inMoscow, new NotSpecification<User>(banned));
multi.Add(new Specification<User>(u => u.IsActive));   // mutate after construction
```

`AndSpecification<T>`, `OrSpecification<T>`, `NotSpecification<T>` all derive from `CompositeSpecification<T>`; `DefaultSpecification<T>` / `NullSpecification<T>` are always-true placeholders for "no filter".

### `In<TO>` — project across types

Reuse a `Specification<User>` against `Order.User`:

```csharp
Specification<User>  active = new(u => u.IsActive);
Specification<Order> orderByActiveUser = active.In<Order>(o => o.User);

dbContext.Orders.Where(orderByActiveUser);            // SQL: WHERE u.IsActive
```

`In` rewrites the predicate's parameter so the same business rule can target any expression that produces a `User`.

### `ValidationSpecification<T>`

A `Specification<T>` that also produces an error message when an instance fails:

```csharp
public class EmailMustBeSet : ValidationSpecification<User>
{
    public EmailMustBeSet() { Predicate = u => !string.IsNullOrEmpty(u.Email); }
    protected override string BuildErrorMessage(User u) => $"User {u.Id} has no email";
}

var error = new EmailMustBeSet().Validate(user);      // string.Empty if satisfied
```

### `FilterSpecification<T>` — fluent filter DSL

Builds a predicate from a typed `IConditionBuilder<T>`. Useful when the rule is dynamic / data-driven:

```csharp
var spec = new FilterSpecification<User>(f => f
    .And(c => c
        .Equal(u => u.City, "Moscow")
        .And(c2 => c2.Greater(u => u.Age, 18))
        .NotIn(u => u.Status, new[] { Status.Banned, Status.Deleted })));

dbContext.Users.Where(spec);                          // translates to SQL
```

Available `IConditionBuilder<T>` operators: `Equal`, `NotEqual`, `Null`, `NotNull`, `Greater`, `GreaterOrEqual`, `Less`, `LessOrEqual`, `Contains`, `NotContains`, `StartsWith`, `EndsWith`, `In`, `NotIn`, `Between`, `NotBetween` + boolean combinators (`And`, `Or`, `NotAnd`, `NotOr`).

---

## `Dex.Specifications.EntityFramework`

Predicates expressed via EF Core shadow properties (`EF.Property<T>`), useful when the property lives on the entity model but not on the CLR type:

```csharp
var byId = new EfEqualSpecification<User, Guid>(u => u.Id, userId);
var inSet = new EfInSpecification<User, string>(u => u.Email, knownEmails);
var like  = new EfLikeSpecification<User>(u => u.Name, "иван");        // %иван%

await dbContext.Users.Where(byId & inSet).ToListAsync();
```

`EfLikeSpecification` always wraps the pattern in `%...%` — for `STARTS WITH` / `ENDS WITH` use `FilterSpecification` instead.

---

## `Dex.Specifications.Extensions`

Fluent equivalents of the operators — preferable when you build specifications conditionally:

```csharp
using Dex.Specifications.Extensions;

Specification<User> spec = new DefaultSpecification<User>();
if (city is not null) spec = spec.And(s => new Specification<User>(u => u.City == city));
if (minAge.HasValue) spec = spec.And(new Specification<User>(u => u.Age >= minAge));
if (excludeBanned)   spec = spec.Not(banned);
```

`And` / `Or` / `Not` each take either another specification or a delegate that gets an empty specification to start from.

---

## Public API surface

| Type | Purpose |
|---|---|
| `Specification<T>` | Base predicate; supports `&`, `|`, `!`, `IsSatisfiedBy`, `In<TO>`. |
| `CompositeSpecification<T>` | Abstract base with `Add` / `Remove`. |
| `AndSpecification<T>` / `OrSpecification<T>` / `NotSpecification<T>` | Boolean composites. |
| `DefaultSpecification<T>` / `NullSpecification<T>` | Always-true placeholders. |
| `ValidationSpecification<T>` | Adds `Validate` that returns an error message. |
| `FilterSpecification<T>` + `IFilterBuilder<T>` / `IConditionBuilder<T>` | Fluent / data-driven filter DSL. |
| `EfEqualSpecification<T, TProperty>` / `EfInSpecification<T, TProperty>` / `EfLikeSpecification<T>` | Shadow-property predicates for EF Core. |
| `SpecificationExtensions.And` / `Or` / `Not` | Fluent operators. |

---

## Notes

* The expression tree produced by `&` / `|` / `!` is what makes specifications compose **inside** an `Expression<Func<T, bool>>` — EF Core translates them to SQL exactly as if you had written them by hand.
* `In<TO>` is parameter-rewriting at the expression-tree level; it produces a new `Specification<TO>` and does **not** mutate the source.
* `Specification<T>` caches the compiled `Func<T, bool>` lazily — the first call to `IsSatisfiedBy` is the slow one. Re-setting `Predicate` invalidates the cache.
* `FilterSpecification<T>` rebuilds its expression tree on every read of `Predicate`. Cache the wrapping `Specification<T>` itself if it ends up on a hot path.

---

## Breaking changes

| Commit / Notes | Change |
|---|---|
| `379d6df` | Project tree refactor — namespaces split across `Dex.Specifications`, `Dex.Specifications.Extensions`, `Dex.Specifications.EntityFramework`. Update `using`s and `<PackageReference>` entries. |
| `6703860` (`#88`) | .NET 5 fix for specification expression rewriting — the source-level API is unchanged but generated expressions look different (matters only if you were inspecting `Specification.ToString()` in tests). |
