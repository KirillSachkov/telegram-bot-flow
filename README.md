# TelegramBotFlow

A .NET framework for building Telegram bots with middleware pipeline, screen navigation, wizard FSM, and sessions -- inspired by ASP.NET Core Minimal APIs.

## Quick Start

```csharp
BotApplicationBuilder builder = BotApplication.CreateBuilder(args);
builder.Services.AddScreens(typeof(Program).Assembly);

BotApplication app = builder.Build();

app.UseErrorHandling();
app.UseSession();

app.MapCommand("start", (UpdateContext ctx) =>
{
    ctx.Session?.Clear();
    return Task.FromResult(BotResults.NavigateToRoot<MainMenuScreen>());
});

app.UseNavigation<MainMenuScreen>();
await app.RunAsync();
```

```csharp
public class MainMenuScreen : IScreen
{
    public ValueTask<ScreenView> RenderAsync(UpdateContext ctx) =>
        ValueTask.FromResult(
            new ScreenView("<b>Main Menu</b>\nChoose an option:")
                .Button<SettingsAction>("Settings")
                .Row()
                .Button<HelpAction>("Help"));
}
```

Add `appsettings.json`:

```json
{ "Bot": { "Token": "YOUR_BOT_TOKEN" } }
```

## Features

- **Middleware pipeline** -- composable request processing (error handling, logging, sessions, access control, custom middleware)
- **Screen navigation** -- stack-based UI with back/forward/root transitions, media support, navigation args
- **Wizard FSM** -- multi-step forms with state management, step history, validation, OnEnter hooks
- **Sessions** -- per-user session with data storage and navigation state (in-memory or Redis)
- **User storage** -- `IBotUserStore<TUser>` abstraction with EF Core/PostgreSQL implementation
- **Typed actions** -- `IBotAction` marker interface for type-safe callback routing (no magic strings)
- **Input handling** -- `AwaitInput<T>()` / `MapInput<T>()` pattern for text input flows
- **Payload encoding** -- automatic inline/stored payload strategy based on Telegram's 64-byte callback_data limit
- **Polling & Webhook** -- both modes supported, webhook includes secret token validation
- **Startup validation** -- middleware ordering verified at startup to prevent runtime errors

## Installation

```bash
# Core framework (required)
dotnet add package TelegramBotFlow.Core

# Redis sessions (optional, replaces in-memory)
dotnet add package TelegramBotFlow.Core.Redis

# PostgreSQL user storage (optional)
dotnet add package TelegramBotFlow.Data.Postgres
```

## Concepts

### Pipeline

Every Telegram update flows through a middleware pipeline before reaching the router. Middlewares are registered with `Use<T>()` and execute in registration order. Built-in middlewares:

| Middleware | Method | Purpose |
|---|---|---|
| `ErrorHandlingMiddleware` | `UseErrorHandling()` | Catches exceptions, sends error message to user |
| `LoggingMiddleware` | `UseLogging()` | Logs update processing |
| `PrivateChatOnlyMiddleware` | `UsePrivateChatOnly()` | Blocks non-private-chat updates |
| `SessionMiddleware` | `UseSession()` | Loads/saves per-user session with lock |
| `AccessPolicyMiddleware` | `UseAccessPolicy()` | Resolves admin access from config |
| `WizardMiddleware` | `UseWizards()` | Intercepts updates for active wizards |
| `UserTrackingMiddleware<T>` | `Use<UserTrackingMiddleware<T>>()` | Auto-creates user records in DB |
| `PendingInputMiddleware` | `UsePendingInput()` | Routes text messages to input handlers |

### Screens

Screens are the UI building blocks. Implement `IScreen` and return a `ScreenView` with text, buttons, and media:

```csharp
public class ProfileScreen : IScreen
{
    private readonly IUserService _users;

    public ProfileScreen(IUserService users) => _users = users;

    public async ValueTask<ScreenView> RenderAsync(UpdateContext ctx)
    {
        var user = await _users.GetAsync(ctx.UserId);
        return new ScreenView($"<b>{user.Name}</b>\nJoined: {user.JoinedAt:d}")
            .WithPhoto(user.AvatarUrl)
            .NavigateButton<EditProfileScreen>("Edit Profile")
            .Row()
            .BackButton();
    }
}
```

Navigation results: `BotResults.NavigateTo<T>()`, `BotResults.Back()`, `BotResults.NavigateToRoot<T>()`, `BotResults.Refresh()`.

Screen IDs follow a convention: `MainMenuScreen` becomes `main_menu`. Override with `[ScreenId("custom")]`.

### Wizards

Multi-step forms with typed state. Each step has a renderer (view) and a processor (logic):

```csharp
public class FeedbackWizard : BotWizard<FeedbackState>
{
    protected override void ConfigureSteps(WizardBuilder<FeedbackState> builder)
    {
        builder
            .Step("message",
                (ctx, state) => new ScreenView("Enter your feedback:"),
                (ctx, state) =>
                {
                    if (string.IsNullOrWhiteSpace(ctx.MessageText))
                        return StepResult.Stay("Please enter some text");
                    state.Message = ctx.MessageText;
                    return StepResult.Finish();
                });
    }

    public override async Task<IEndpointResult> OnFinishedAsync(
        UpdateContext context, FeedbackState state)
    {
        // Save feedback...
        return BotResults.Back("Thank you!");
    }
}
```

Step results: `StepResult.GoTo("stepId")`, `StepResult.Stay()`, `StepResult.Finish()`, `StepResult.GoBack()`.

### Sessions

Per-user session with two compartments:

- **`Data`** -- key-value store for application data (`session.Data.Set("key", value)`)
- **`Navigation`** -- framework-managed state (current screen, stack, pending input, active wizard)

Default: in-memory. For production, use Redis:

```csharp
builder.Services.AddRedisSessionStore(builder.Configuration);
```

### User Storage

`IBotUserStore<TUser>` provides persistence for bot users. The built-in `EfBotUserStore` uses EF Core:

```csharp
builder.Services.AddBotCoreData(builder.Configuration);      // default BotUser
builder.Services.AddBotCoreData<AppUser, AppDbContext>(cfg);  // custom user type
```

The `UserTrackingMiddleware<TUser>` auto-creates user records on first contact with 1-hour MemoryCache deduplication.

## Configuration

Bot settings are read from the `"Bot"` configuration section:

| Property | Type | Default | Description |
|---|---|---|---|
| `Token` | `string` | required | Bot API token |
| `Mode` | `BotMode` | `POLLING` | `POLLING` or `WEBHOOK` |
| `WebhookUrl` | `string?` | `null` | Public URL for webhook |
| `WebhookPath` | `string` | `/api/bot/webhook` | Webhook endpoint path |
| `WebhookSecretToken` | `string?` | `null` | Webhook secret validation |
| `AdminUserIds` | `long[]` | `[]` | Admin Telegram user IDs |
| `ErrorMessage` | `string` | `"An error occurred..."` | Error message for users |
| `AllowedUpdates` | `UpdateType[]` | `[Message, CallbackQuery]` | Allowed update types |

## Packages

| Package | Description | Dependencies |
|---|---|---|
| `TelegramBotFlow.Core.Abstractions` | Contracts and interfaces | Telegram.Bot |
| `TelegramBotFlow.Core` | Runtime, pipeline, routing | Core.Abstractions, ASP.NET Core |
| `TelegramBotFlow.Core.Redis` | Redis session store | Core.Abstractions, StackExchange.Redis |
| `TelegramBotFlow.Data.Postgres` | EF Core user storage | Core, Npgsql.EntityFrameworkCore.PostgreSQL |

## Running

```bash
# Development (polling mode)
dotnet run --project src/TelegramBotFlow.App

# Infrastructure (PostgreSQL + Seq)
docker compose -f docker-compose-infra.yml up -d

# Tests (Docker required for integration tests)
dotnet test TelegramBotFlow.slnx
```

## License

MIT
