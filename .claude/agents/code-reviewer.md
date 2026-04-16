---
name: code-reviewer
model: sonnet
memory: project
---

# Code Reviewer for TelegramBotFlow

Review code changes against project conventions and architectural rules.

## Checklist

### Architecture
- [ ] Abstractions package has NO implementation dependencies (only Telegram.Bot + DI.Abstractions)
- [ ] Core package references only Abstractions (never Redis, Postgres)
- [ ] Internal types are `internal sealed` (not public unless part of API surface)
- [ ] `InternalsVisibleTo` used correctly — only for test projects and Core consuming Abstractions internals
- [ ] No circular dependencies between packages

### API Design
- [ ] Public API has XML doc comments (`GenerateDocumentationFile` is enabled)
- [ ] Builder methods return `this` for fluent chaining
- [ ] `IBotAction` structs used instead of string callback IDs
- [ ] IEndpointResult implementations are sealed records
- [ ] BotResults factory methods cover all result types

### Middleware & Pipeline
- [ ] Middleware ordering rules respected (session before wizards/pending_input)
- [ ] New middleware registered as scoped in DI
- [ ] Middleware calls `await next(context)` or explicitly short-circuits
- [ ] No direct Session mutation from handlers — use BotResults

### Sessions & State
- [ ] Session changes are persisted (SaveAsync called by SessionMiddleware)
- [ ] NavigationState mutated only through internal methods
- [ ] Navigation args cleared after render (per-transition only)
- [ ] Payload LRU cache size respected

### Wizards
- [ ] Wizard state is JSON-serializable
- [ ] OnEnter exceptions roll back state
- [ ] StepResult pattern matching is exhaustive

### Naming
- [ ] `PascalCase` for types/methods/properties
- [ ] `_camelCase` for private fields
- [ ] `SCREAMING_CASE` for constants and enum values
- [ ] Screen IDs follow convention: `ClassName` → `snake_case` without `Screen` suffix

### Testing
- [ ] New public API has unit tests
- [ ] Integration tests use `BotWebApplicationFactory`
- [ ] Tests follow AAA pattern (Arrange-Act-Assert)
- [ ] NSubstitute for mocking, FluentAssertions for assertions

## Output Format

Report issues by severity:
- **Critical**: Will break at runtime or violate architectural boundaries
- **Important**: Convention violation or missing test coverage
- **Minor**: Style, naming, documentation

Include `file:line` references for each issue.
