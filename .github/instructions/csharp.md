# C# Project Guidelines
---
applyTo: "**/*.cs"
---

## Stack
- .NET + Aspire,xUnit v3, Shouldly, NSubstitute

## Code Style
- File-scoped namespaces, primary constructors, modern C# 14 features where the TFM allows.
- `internal` by default; `public` only when required by DI/hosting contracts
- Async methods end with `Async`
- One type per file; filename matches type name
- Comments explain *why*, not *what*
- No unused parameters, no unused `using` directives.

**Check actual project state**: Before implementing, verify current .NET version and package references in relevant `.csproj` or `Directory.Build.props`. Don't assume or use outdated patterns.

**Think, don't checklist**: Reason about what could fail in production or create security risks. Examples below guide thinking, not exhaustive rules.

### Error Handling
- Guard early with `ArgumentNullException.ThrowIfNull` / `ArgumentException`.
- Use precise exception types; never catch bare `Exception` except as a final fallback with `LogError`.
- No silent swallowing – always log before discarding.

## Dependency Injection
- Required dependencies: non-nullable constructor parameters, no default values
- Optional dependencies: `T? dep = null` only when genuinely optional by design
- Don't register internal collaborators in DI if they're exclusively owned by one type

## Testing
- xUnit v3 (`[Fact]`, `[Theory]`), Shouldly assertions, NSubstitute for mocking
- Every test has a `DisplayName` in Given/When/Then style
- Test names show the risk: `Given expired token When validating Then rejects`
- Mock only external/platform dependencies — never mock code in this solution
- Prefer interfaces over `protected virtual` test seams

## Architecture
- Clean Architecture + CQRS
- Guard early, fail fast
- No magic strings — use strongly typed IDs and constants

