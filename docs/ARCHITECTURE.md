# Архитектура Telegram Bot Flow

## Обзор

Telegram Bot Flow — template-проект для создания Telegram-ботов как тонких клиентов. Бот обрабатывает UI и маршрутизацию, бизнес-логика живёт на бэкенде.

## Схема обработки Update

```
┌─────────────────── Входящие обновления ───────────────────┐
│                                                           │
│  PollingService (BackgroundService)                        │
│  POST /api/bot/webhook                                    │
│                                                           │
└──────────────────────────┬────────────────────────────────┘
                           │ Update
                           ▼
┌─────────────────── Middleware Pipeline ────────────────────┐
│                                                           │
│  ErrorHandlingMiddleware                                   │
│         │                                                 │
│         ▼                                                 │
│  LoggingMiddleware                                         │
│         │                                                 │
│         ▼                                                 │
│  SessionMiddleware  ──────────►  ISessionStore             │
│         │                                                 │
│         ▼                                                 │
│  AccessPolicyMiddleware ────► IUserAccessPolicy           │
│         │                                                 │
│         ▼                                                 │
│  UpdateRouter                                              │
│                                                           │
└──────┬──────────┬──────────┬──────────┬───────────────────┘
       │          │          │          │
       ▼          ▼          ▼          ▼
  MapCommand  MapCallback  MapMessage  MapFallback
  (IBotEndpoint classes)
       │          │
       ▼          ▼
  HandlerDelegateFactory
    │
    ▼
  UpdateContext + DI services
  (IUpdateResponder, IScreenNavigator, DbContext, ...)
```

## Проекты

### TelegramBotFlow.Core

Framework-часть. При копировании в новый проект эту часть менять не нужно.

| Папка                   | Назначение                                                                                          |
| ----------------------- | --------------------------------------------------------------------------------------------------- |
| `Hosting/`              | `BotApplication`, `BotApplicationBuilder`, `PollingService`, `WebhookEndpoints`, `BotConfiguration` |
| `Pipeline/`             | `UpdateDelegate`, `IUpdateMiddleware`, `UpdatePipeline`                                             |
| `Pipeline/Middlewares/` | `ErrorHandling`, `Logging`, `Session`, `AccessPolicy` middleware                                    |
| `Routing/`              | `UpdateRouter`, `RouteEntry` — маршрутизация по типу обновления                                     |
| `Context/`              | `UpdateContext`, `IUpdateResponder`, `IUserAccessPolicy`                                            |
| `Sessions/`             | `ISessionStore`, `InMemorySessionStore`, `UserSession`                                              |
| `Screens/`              | `IScreen`, `ScreenManager`, `ScreenNavigator`, `ScreenRegistry`, `ScreenView`                       |
| `UI/`                   | `InlineKeyboard`, `ReplyKeyboard`, `MenuBuilder`                                                    |
| `Endpoints/`            | `IBotEndpoint`, `BotEndpointExtensions` — auto-discovery обработчиков                               |
| `Extensions/`           | `ServiceCollectionExtensions` — DI-регистрация                                                      |

### TelegramBotFlow.Core.Data

EF Core data layer для фреймворка. Управление пользователями бота, отдельный `BotDbContext`.

**Расширяемость:** По аналогии с ASP.NET Identity — `BotUser` можно наследовать, `BotDbContext<TUser>` поддерживает кастомные модели пользователей.

| Файл / Папка                    | Назначение                                                             |
| ------------------------------- | ---------------------------------------------------------------------- |
| `BotUser.cs`                    | Базовая сущность пользователя (`TelegramId`, `JoinedAt`, `IsBlocked`)  |
| `BotDbContext.cs`               | Generic `BotDbContext<TUser>` + default `BotDbContext`                 |
| `Configurations/`               | `BotUserConfiguration` — EF конфигурация (snake_case)                  |
| `Middleware/`                   | `UserTrackingMiddleware<TUser>` + non-generic `UserTrackingMiddleware` |
| `Infrastructure/Migrations/`    | EF Core миграция `InitUsers` (таблица `users`)                         |
| `DependencyInjectionExtensions` | `AddBotCoreData()` / `AddBotCoreData<TUser, TContext>()`               |

### TelegramBotFlow.Broadcasts

Модуль рассылок — отдельный проект со своей БД, Quartz jobs, Telegram-эндпоинтами и REST API.

**Подход к контенту:** Используется Telegram Bot API `copyMessage`. Администратор отправляет сообщение боту (любого типа: текст, фото, видео, документ с форматированием), бот сохраняет `FromChatId` + `MessageId` и при рассылке использует `copyMessage` — сообщение копируется без надписи "Переслано".

| Папка                           | Назначение                                                                 |
| ------------------------------- | -------------------------------------------------------------------------- |
| `Domain/`                       | Анемичные модели: `Broadcast`, `BroadcastSequence`, и др.                  |
| `Infrastructure/`               | `BroadcastsDbContext`, EF конфигурации (snake_case)                        |
| `Infrastructure/Configurations` | `IEntityTypeConfiguration<T>` для broadcast-сущностей                      |
| `Infrastructure/Migrations/`    | EF Core миграции + таблицы Quartz.NET                                      |
| `Features/Broadcasts/`          | `AdminBroadcastEndpoint` (IBotEndpoint) + REST API (GetAll, Send, Delete)  |
| `Features/Sequences/`           | `AdminSequenceEndpoint` (IBotEndpoint) + REST API (GetAll, Toggle, Delete) |
| `Features/Users/`               | Просмотр пользователей (данные из `BotDbContext`)                          |
| `Services/`                     | `BroadcastSender` — отправка через `copyMessage` с rate limiting           |
| `Jobs/`                         | `SequenceProcessorJob`, `BroadcastExecutionJob`                            |

**Telegram-эндпоинты (IBotEndpoint):**

- `AdminBroadcastEndpoint` — `/broadcast` команда, создание рассылок через Telegram с InlineKeyboard
- `AdminSequenceEndpoint` — `/sequence` сообщает о недоступности Telegram-мастера и направляет в REST API

**Зависимости:** Core.Data (`BotDbContext`, `BotUser`), SachkovTech.Framework (`IEndpoint`, `EndpointResult`), SachkovTech.SharedKernel (`Error`, `Envelope`), SachkovTech.Core (`CustomValidators`), CSharpFunctionalExtensions (`Result<T, Error>`), Quartz.NET (PostgreSQL persistence), EF Core + Npgsql.

### TelegramBotFlow.App

Точка кастомизации:

- `Program.cs` — конфигурация middleware, меню, auto-discovery endpoints, подключение Core.Data и Broadcasts
- `Handlers/` — обработчики команд, callback, fallback (реализуют `IBotEndpoint`)
- `Screens/` — экраны UI (реализуют `IScreen`): `MainMenuScreen`, `ProfileScreen`, `SettingsScreen`, `HelpScreen`
- `Conversations/` — папка для пошаговых сценариев (placeholder, пока пустая)
- DI-регистрация backend API клиентов
- Swagger UI для управления рассылками (`/swagger`)

## Ключевые компоненты

### UpdateContext

Контекст update-а в текущей архитектуре — data-only объект:

- `Update`, `CancellationToken`
- извлечённые факты: `ChatId`, `UserId`, `MessageId`, `MessageText`, `CallbackData`, `CommandArgument`
- runtime-состояние: `Session`, `IsAdmin`

Отправка сообщений и политика доступа вынесены в отдельные сервисы:

- `IUpdateResponder`
- `IUserAccessPolicy`

### Middleware Pipeline

Аналог ASP.NET Core middleware, но для Telegram Update:

```
ErrorHandling → Logging → Session → AccessPolicy → Router → Handler
```

Каждый middleware может:

- Выполнить логику до/после следующего
- Прервать цепочку (short-circuit)
- Модифицировать контекст

### Политика доступа (Admin)

`AccessPolicyMiddleware` вычисляет `UpdateContext.IsAdmin` на основе `IUserAccessPolicy`.

Стандартная реализация (`BotConfigurationUserAccessPolicy`) использует `Bot:AdminUserIds`.

### Routing и DI в handler-ах

Minimal API-стиль регистрации:

- `MapCommand("/start", handler)` — команды (case-insensitive, поддержка @botname)
- `MapCallback("action:*", handler)` — callback-кнопки с wildcard
- `MapAction("callbackId", handler)` — action-кнопка по строковому ID
- `MapAction<TAction>(handler)` — action-кнопка по типу (`typeof(TAction).Name`)
- `MapInput("actionId", handler)` — обработчик свободного текста при активном `PendingInputActionId`
- `MapInput<TAction>(handler)` — обработчик текста по типу (`typeof(TAction).Name`)
- `MapCallbackGroup("prefix", handler)` — callback по префиксу, action приходит в `string` параметр
- `MapMessage(predicate, handler)` — текст по предикату
- `MapUpdate(predicate, handler)` — любой тип Update

`HandlerDelegateFactory` резолвит параметры handler-а так:

- `UpdateContext` — всегда
- `CancellationToken` — из контекста
- остальные параметры — из request scope DI

Первый подходящий маршрут выигрывает.

### IEndpointResult и BotResults

Обработчики (action, input, command) возвращают `Task<IEndpointResult>` или `Task` (void):

```csharp
app.MapAction<ClearAction>(async (UpdateContext ctx, AppDb db) =>
{
    await ClearAsync(db, ctx.CancellationToken);
    return BotResults.Refresh("✅ Удалено");
});

app.MapInput<MessageInput>(async (UpdateContext ctx, AppDb db) =>
{
    await SaveAsync(db, ctx.MessageText, ctx.CancellationToken);
    return BotResults.Back("✅ Сохранено");
});
```

Доступные результаты через `BotResults`:

| Метод | Поведение |
|---|---|
| `BotResults.ShowView(view)` | Показывает произвольный `ScreenView` |
| `BotResults.Back(msg?)` | Возврат на предыдущий экран; уведомление опционально |
| `BotResults.NavigateTo<TScreen>()` | Переход к указанному экрану |
| `BotResults.Refresh(msg?)` | Перерисовка текущего экрана |
| `BotResults.Stay(msg?)` | Остаётся в режиме ожидания ввода (`KeepPending = true`) |

### Typed Actions (IBotAction)

Для типобезопасной связи кнопок с обработчиками используется маркерный интерфейс `IBotAction`:

```csharp
public struct ClearRoadmapAction : IBotAction;

// View — кнопка ссылается на тип
new ScreenView("...")
    .Button<ClearRoadmapAction>("🗑 Удалить")

// Handler — обработчик ссылается на тот же тип
app.MapAction<ClearRoadmapAction>(...);
```

Аналогично для ввода: `AwaitInput<TAction>()` в `ScreenView` + `MapInput<TAction>(handler)` в обработчике. Строковый `ACTION_ID` не нужен.

### IBotEndpoint (Auto-Discovery)

Паттерн аналогичный `IEndpoint` в ASP.NET Core Minimal API. Позволяет вынести обработчики из `Program.cs` в отдельные классы:

```csharp
public sealed class StartCommandEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
    app.MapCommand("/start", async (UpdateContext ctx, IUpdateResponder responder) =>
        {
      await responder.ReplyAsync(ctx, "Привет!");
        });
    }
}
```

Регистрация в `Program.cs`:

```csharp
BotApplicationBuilder builder = BotApplication.CreateBuilder(args);
BotApplication app = builder.Build();
app.MapBotEndpoints();
```

`builder.Build()` автоматически вызывает `AddBotEndpoints` и `AddScreens` для entry assembly.
`MapBotEndpoints` резолвит их и вызывает `MapEndpoint` для каждого.

### Screens и навигация

UI-диалоги реализованы через экраны:

1. `IScreen.RenderAsync` возвращает `ScreenView`
2. `ScreenManager` рендерит экран и обновляет навигационные поля сессии
3. `IScreenNavigator` управляет `NavigateTo`, `GoBack`, `RefreshScreen`
4. `ScreenRegistry` регистрирует экраны по конвенции snake_case из имени класса

`ScreenView` — rich builder для содержимого экрана: поддерживает InlineKeyboard, медиа (фото, видео, документ, анимация), навигационные кнопки (`NavigateButton<T>`, `BackButton`, `CloseButton`, `MenuButton`). Кнопки навигации генерируют callback `nav:{screenId}` и обрабатываются `NavigationHandler`.

### Sessions

- `ISessionStore` — абстракция хранилища
- `InMemorySessionStore` — default (ConcurrentDictionary)
- `UserSession` — key-value хранилище + состояние навигации экранов

Для production можно реализовать `RedisSessionStore` / `DbSessionStore`.

## Режимы работы

| Режим   | Компонент                            | Когда использовать             |
| ------- | ------------------------------------ | ------------------------------ |
| Polling | `PollingService` (BackgroundService) | Разработка, нет публичного URL |
| Webhook | Minimal API endpoint + `SetWebhook`  | Production, есть HTTPS URL     |

Переключение через `Bot:Mode` в `appsettings.json`.

## Практическая конфигурация App

Базовый `Program.cs` в приложении обычно собирается так:

```csharp
BotApplication app = builder.Build();

app.UseErrorHandling();
app.UseLogging();
app.UseSession();
app.UseAccessPolicy();
app.Use<UserTrackingMiddleware>();
app.UsePendingInput();

app.SetMenu(menu => menu
  .Command("start", "Главное меню"));

app.MapBotEndpoints();
await app.RunAsync();
```
