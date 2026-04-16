---
description: Enforce package dependency rules between TelegramBotFlow packages
globs: ["src/**/*.cs", "src/**/*.csproj"]
---

# Package Boundary Rules

## Dependency Graph (strict, top-down only)

```
Core.Abstractions  (contracts only)
     |
     +----> Core  (runtime)
     |       |
     |       +----> Data.Postgres
     |       +----> App (example)
     |
     +----> Core.Redis
```

## Hard Rules

- **Core.Abstractions** must NOT reference Core, Core.Redis, or Data.Postgres
- **Core.Abstractions** dependencies: only `Telegram.Bot`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Configuration.Abstractions`
- **Core** references Abstractions only (not Redis, not Postgres)
- **Core.Redis** references Abstractions only (not Core)
- **Data.Postgres** references Core (for extension methods)
- Internal types must be `internal sealed` unless part of the public API surface
- `InternalsVisibleTo`: Abstractions → Core, Core.Redis, test projects; Core → test projects
