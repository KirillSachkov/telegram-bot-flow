---
description: EF Core migration safety rules
globs: ["src/**/Migrations/*.cs"]
---

# Migration Rules

- **NEVER** delete or modify existing migration files — they are immutable once committed
- Fix migration errors by creating a NEW corrective migration
- Migration command: `dotnet ef migrations add {Name} --project src/TelegramBotFlow.Data.Postgres --startup-project src/TelegramBotFlow.App`
- The pre-tool hook blocks editing migration files automatically
