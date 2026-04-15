# Repository Guidelines

## Project Structure & Module Organization

- `src/` contains production code split by project.
- `src/TelegramBotFlow.App` is the bot application entry point and feature code (`Features/{FeatureName}/`).
- `src/TelegramBotFlow.Core*` and `src/TelegramBotFlow.Core.Abstractions` contain framework/runtime abstractions, middleware, routing, screens, sessions, and optional Redis/data integrations.
- `src/TelegramBotFlow.Broadcasts` contains broadcast domain, jobs, and API features.
- `tests/TelegramBotFlow.Core.Tests` holds unit tests; `tests/TelegramBotFlow.IntegrationTests` holds integration tests (including Redis/Testcontainers).
- `docs/` contains architecture, API, usage, infrastructure, and style references.

## Build, Test, and Development Commands

- `dotnet restore` restores NuGet packages for all projects.
- `dotnet build TelegramBotFlow.slnx` builds the full solution (`TreatWarningsAsErrors=true`).
- `dotnet run --project src/TelegramBotFlow.App` runs the bot locally.
- `dotnet test` runs all unit and integration tests.
- `dotnet test --filter "FullyQualifiedName~RedisSessionStoreIntegrationTests"` runs Redis-focused integration tests.
- `docker compose -f docker-compose-infra.yml up -d` starts local PostgreSQL + Seq.

## Coding Style & Naming Conventions

- Follow `.editorconfig`: C# uses 4 spaces, nullable enabled, and warnings are treated as errors.
- Use naming rules from `docs/CODE_STYLE.md`.
- `PascalCase` for types/methods/properties, `camelCase` for locals/params, `_camelCase` for private fields.
- Constants and enum values use `SCREAMING_CASE` (intentional deviation from default .NET style).
- Keep feature files co-located under `Features/{FeatureName}/` and prefer typed bot actions over string callback IDs.

## Testing Guidelines

- Frameworks: xUnit + FluentAssertions + NSubstitute; coverage via `coverlet.collector`.
- Name test files as `*Tests.cs`; keep tests grouped by domain folder (`Routing/`, `Pipeline/`, `Sessions/`, etc.).
- For integration tests using Redis/Testcontainers, ensure Docker is running.
- Run coverage with `dotnet test --collect:"XPlat Code Coverage"` before major PRs.

## Commit & Pull Request Guidelines

- Recent history mostly follows Conventional Commits (`feat:`, `refactor:`, `chore:`); keep this format and use imperative summaries.
- Keep commits focused and avoid mixing refactors with behavior changes unless necessary.
- PRs should include clear scope and motivation.
- PRs should link the issue/task when available.
- PRs should include test evidence (`dotnet test` output or a coverage note).
- PRs should call out config/migration changes (EF or infrastructure).
- PRs should include screenshots or log snippets for user-facing bot-flow changes.

## Security & Configuration Tips

- Never commit bot tokens or real secrets; use `.env`/local settings and keep `.env.example` sanitized.
- Validate DB and bot settings before running production compose (`docker compose up -d --build`).
