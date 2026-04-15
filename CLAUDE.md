# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

TelegramBotFlow is a .NET 10 framework for building Telegram bots with a middleware pipeline, screen-based navigation, wizard FSM, sessions, and ASP.NET Core Minimal API-style route registration.

## Project Structure

| Package | Responsibility |
|---|---|
| `TelegramBotFlow.Core.Abstractions` | Contracts layer: interfaces (`IScreen`, `IBotAction`, `IBotUser`, `IUpdateMiddleware`, `IEndpointResult`), `UpdateContext`, `ScreenView` builder, `StepResult`, session/navigation state, UI builders. No implementation dependencies. |
| `TelegramBotFlow.Core` | Runtime: middleware pipeline, routing, `BotApplication` host, `ScreenManager`, `WizardMiddleware`, `SessionMiddleware`, in-memory stores, update processing (polling/webhook). |
| `TelegramBotFlow.Core.Redis` | Optional Redis session store (`RedisSessionStore` replaces `InMemorySessionStore`). |
| `TelegramBotFlow.Data.Postgres` | Optional EF Core/PostgreSQL data layer: `BotUser` entity, `BotDbContext<TUser>`, `EfBotUserStore<TUser>`. RootNamespace: `TelegramBotFlow.Core.Data`. |
| `TelegramBotFlow.App` | Example bot application using the framework. |

Tests:
- `tests/TelegramBotFlow.Core.Tests` -- unit tests
- `tests/TelegramBotFlow.IntegrationTests` -- integration tests (Redis via Testcontainers)

## Build & Test Commands

```bash
dotnet build TelegramBotFlow.slnx
dotnet test TelegramBotFlow.slnx
dotnet test TelegramBotFlow.slnx --filter "FullyQualifiedName~TestClassName"

# Run the example app
dotnet run --project src/TelegramBotFlow.App

# EF Core migration (Data.Postgres)
dotnet ef migrations add {Name} \
  --project src/TelegramBotFlow.Data.Postgres \
  --startup-project src/TelegramBotFlow.App
```

## Architecture

```
Telegram Update
  -> Channel<Update> (bounded, 1000)
  -> UpdateProcessingWorker (scoped DI per update)
  -> Pipeline: Middleware chain -> Router -> Handler -> IEndpointResult
  -> IEndpointResult.ExecuteAsync(BotExecutionContext)
  -> ScreenManager / NavigationService / IUpdateResponder
```

**Two runtime modes:**
- **Polling** -- `PollingService` (HostedService) polls `getUpdates` and writes to the channel
- **Webhook** -- ASP.NET Core POST endpoint receives updates, validates `X-Telegram-Bot-Api-Secret-Token`

**Pipeline flow:** Each `UpdateContext` passes through the middleware chain (error handling -> logging -> private chat filter -> session -> access policy -> wizards -> user tracking -> pending input) and terminates at the `UpdateRouter`, which matches the update to a registered handler. Handlers return `IEndpointResult` (analogous to ASP.NET Core `IResult`), which executes itself via `BotExecutionContext`.

## Key Patterns

### Adding a command

Implement `IBotEndpoint` and register with `MapCommand()`:

```csharp
public class StartEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapCommand("start", (UpdateContext ctx) =>
        {
            ctx.Session?.Clear();
            return Task.FromResult(BotResults.NavigateToRoot<MainMenuScreen>());
        });
    }
}
```

### Adding a screen

Implement `IScreen`, return `ScreenView`:

```csharp
public class SettingsScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx) =>
        ValueTask.FromResult(
            new ScreenView("Settings")
                .Button<ChangeLanguageAction>("Language")
                .Row()
                .BackButton());
}
```

### Adding a callback action

Create a struct implementing `IBotAction`, register with `MapAction<T>()`:

```csharp
public struct ChangeLanguageAction : IBotAction;

// In endpoint:
app.MapAction<ChangeLanguageAction>((UpdateContext ctx) =>
    Task.FromResult(BotResults.NavigateTo<LanguageScreen>()));

// With payload:
app.MapAction<SelectItemAction, Guid>((UpdateContext ctx, Guid itemId) =>
    Task.FromResult(BotResults.NavigateTo<ItemScreen>()
        .WithArg("itemId", itemId)));
```

### Adding input handling

Use `AwaitInput<T>()` on ScreenView and `MapInput<T>()` for the handler:

```csharp
// In screen:
new ScreenView("Enter your name:")
    .AwaitInput<NameInputAction>()
    .BackButton();

// In endpoint:
app.MapInput<NameInputAction>((UpdateContext ctx) =>
{
    string name = ctx.MessageText!;
    // process name...
    return Task.FromResult(BotResults.Back("Saved!"));
});
```

### Adding a wizard

Extend `BotWizard<TState>`, implement `ConfigureSteps()` and `OnFinishedAsync()`:

```csharp
public class CreateItemWizard : BotWizard<CreateItemState>
{
    protected override void ConfigureSteps(WizardBuilder<CreateItemState> builder)
    {
        builder
            .Step("name",
                (ctx, state) => new ScreenView("Enter item name:"),
                (ctx, state) =>
                {
                    state.Name = ctx.MessageText!;
                    return StepResult.GoTo("confirm");
                })
            .Step("confirm",
                (ctx, state) => new ScreenView($"Create '{state.Name}'?")
                    .Button("Yes", "wizard:confirm")
                    .Button("No", "wizard:cancel"),
                (ctx, state) => ctx.CallbackData switch
                {
                    "wizard:confirm" => StepResult.Finish(),
                    _ => StepResult.GoBack()
                });
    }

    public override async Task<IEndpointResult> OnFinishedAsync(
        UpdateContext context, CreateItemState state)
    {
        // persist state.Name...
        return BotResults.NavigateToRoot<MainMenuScreen>();
    }
}
```

Launch: `BotResults.StartWizard<CreateItemWizard>()`

### Adding custom middleware

Implement `IUpdateMiddleware`, register with `Use<T>()`:

```csharp
public class RateLimitMiddleware : IUpdateMiddleware
{
    public async Task InvokeAsync(UpdateContext context, UpdateDelegate next)
    {
        // pre-processing
        await next(context);
        // post-processing
    }
}

// Registration:
app.Use<RateLimitMiddleware>();
```

### DI registration

```csharp
// Required: core framework
builder.Services.AddTelegramBotFlow(builder.Configuration);

// Optional: screens from assembly
builder.Services.AddScreens(typeof(Program).Assembly);

// Optional: wizards from assembly
builder.Services.AddWizards(typeof(Program).Assembly);

// Optional: IBotEndpoint implementations
builder.Services.AddBotEndpoints(typeof(Program).Assembly);

// Optional: PostgreSQL user storage
builder.Services.AddBotCoreData(builder.Configuration);

// Optional: Redis sessions (replaces InMemorySessionStore)
builder.Services.AddRedisSessionStore(builder.Configuration);
```

## Configuration

`BotConfiguration` is bound from the `"Bot"` section of `appsettings.json`:

| Property | Type | Default | Description |
|---|---|---|---|
| `Token` | `string` | required | Telegram Bot API token |
| `Mode` | `BotMode` | `POLLING` | `POLLING` or `WEBHOOK` |
| `WebhookUrl` | `string?` | `null` | Public URL for webhook mode |
| `WebhookPath` | `string` | `/api/bot/webhook` | Webhook endpoint path |
| `WebhookSecretToken` | `string?` | `null` | Secret for `X-Telegram-Bot-Api-Secret-Token` validation |
| `AdminUserIds` | `long[]` | `[]` | Telegram user IDs with admin access |
| `ErrorMessage` | `string` | `"An error occurred..."` | Message sent on unhandled errors |
| `StorageChannelId` | `long` | `0` | Channel ID for file storage |
| `AllowedUpdates` | `UpdateType[]` | `[Message, CallbackQuery]` | Update types to receive |

Redis session config is in the `"Redis"` section:

| Property | Type | Default | Description |
|---|---|---|---|
| `ConnectionString` | `string` | `localhost:6379` | Redis connection string |
| `SessionTtlMinutes` | `int?` | `null` | Session TTL in minutes (null = no expiry) |

## Known Gotchas

- **Middleware ordering** -- `UseWizards()` and `UsePendingInput()` must be registered after `UseSession()`. This is validated at startup via `MiddlewareOrderValidator`; incorrect order throws `InvalidOperationException`. The `UseErrorHandling()` middleware should be first.

- **Payload encoding** -- Button payloads < 64 bytes UTF-8 are inlined in callback_data (`action:j:{json}`). Payloads >= 64 bytes are stored in session LRU cache and referenced by shortId (`action:s:{shortId}`). The LRU cache holds 500 entries. Expired payloads throw `PayloadExpiredException`.

- **Telegram callback_data 64-byte limit** -- Total callback_data must fit in 64 bytes. Action IDs + payload prefix consume part of this budget. Keep action IDs short.

- **Session lock scope** -- `SessionMiddleware` acquires a per-user lock for the entire pipeline execution (try-finally pattern). This prevents concurrent updates from corrupting session state.

- **Screen ID convention** -- `ClassName` is converted to `snake_case` with `Screen` suffix stripped: `MainMenuScreen` -> `main_menu`. Override with `[ScreenId("custom_id")]` attribute.

- **Action ID** -- Defaults to the type name (`struct ChangeLanguage : IBotAction` -> `"ChangeLanguage"`). Override with `[ActionId("custom_id")]` attribute.

- **NavigationStack is read-only** -- The `NavigationStack` property returns `IReadOnlyList<string>`. Mutate only through framework methods (`BotResults.Back()`, `BotResults.NavigateTo<T>()`).

- **UserTrackingMiddleware memory** -- Uses `MemoryCache` with 1-hour sliding expiration to avoid DB lookups on every update. Not unbounded.

- **Data.Postgres RootNamespace** -- The `TelegramBotFlow.Data.Postgres` project uses `RootNamespace = TelegramBotFlow.Core.Data`. Import `TelegramBotFlow.Core.Data`, not `TelegramBotFlow.Data.Postgres`.

- **InternalsVisibleTo** -- `Core.Abstractions` exposes internals to `Core`, `Core.Redis`, and test projects. `Core` exposes internals to test projects. This enables testing of internal pipeline components.

- **Wizard state deserialization** -- If `PayloadJson` in `WizardStorageState` is malformed, the wizard catches `JsonException` and returns `BotResults.Back()` (exits the wizard gracefully).

- **OnEnter exception safety** -- If a wizard step's `OnEnter` throws, the state is rolled back to pre-`OnEnter` snapshot and the user stays on the current step.

- **PendingInputMiddleware fallthrough** -- If `PendingInputActionId` is set but no handler is registered for that action ID, the middleware logs a warning and passes the update to the router (does not silently swallow it).

- **Commands reset pending input** -- If a user sends a `/command` while input is pending, the pending state is cleared and the command is routed normally.

## Dependency Graph

```
Core.Abstractions  (contracts only, Telegram.Bot + DI.Abstractions)
     |
     +----> Core  (runtime, ASP.NET Core, Telegram.Bot)
     |       |
     |       +----> Data.Postgres  (EF Core, Npgsql)
     |       |
     |       +----> App  (example bot)
     |
     +----> Core.Redis  (StackExchange.Redis)
```

## Post-Change Checklist

- `dotnet build TelegramBotFlow.slnx` passes (warnings are errors)
- `dotnet test TelegramBotFlow.slnx` passes (Docker must be running for integration tests)
- If adding public API: add XML doc comments (`GenerateDocumentationFile` is enabled)
- If changing middleware: verify ordering rules in `MiddlewareOrderValidator`
- If changing session/navigation state: check both `InMemorySessionStore` and `RedisSessionStore`
- If changing `IBotUserStore`: update both `EfBotUserStore` and `UserTrackingMiddleware`
- If modifying `Core.Abstractions` contracts: check `InternalsVisibleTo` consumers (Core, Core.Redis, tests)
- Never delete or modify existing EF migrations in `Data.Postgres`

## Coding Style

- `.editorconfig`: 4 spaces, nullable enabled, `TreatWarningsAsErrors=true`
- `PascalCase` for types/methods/properties, `camelCase` for locals/params, `_camelCase` for private fields
- Constants and enum values: `SCREAMING_CASE` (intentional)
- Prefer typed bot actions (`IBotAction` structs) over string callback IDs
- Feature code co-located under `Features/{FeatureName}/` in the App project
