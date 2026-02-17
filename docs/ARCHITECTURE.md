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
│  FlowMiddleware                                            │
│         │                                                 │
│         ▼                                                 │
│  UpdateRouter                                              │
│                                                           │
└──────┬──────────┬──────────┬──────────┬───────────────────┘
       │          │          │          │
       ▼          ▼          ▼          ▼
  MapCommand  MapCallback  MapMessage  MapFlow
  (IBotEndpoint classes)
       │          │
       ▼          ▼
   Backend API (HttpClient)
```

## Проекты

### TelegramBotFlow.Core

Framework-часть. При копировании в новый проект эту часть менять не нужно.

| Папка                   | Назначение                                                                                          |
| ----------------------- | --------------------------------------------------------------------------------------------------- |
| `Hosting/`              | `BotApplication`, `BotApplicationBuilder`, `PollingService`, `WebhookEndpoints`, `BotConfiguration` |
| `Pipeline/`             | `UpdateDelegate`, `IUpdateMiddleware`, `UpdatePipeline`                                             |
| `Pipeline/Middlewares/` | `ErrorHandling`, `Logging`, `Session`, `Flow` middleware                                            |
| `Routing/`              | `UpdateRouter`, `RouteEntry` — маршрутизация по типу обновления                                     |
| `Context/`              | `UpdateContext` — контекст запроса с convenience-методами                                           |
| `Sessions/`             | `ISessionStore`, `InMemorySessionStore`, `UserSession`                                              |
| `Flows/`                | `FlowBuilder`, `FlowDefinition`, `FlowStep`, `FlowManager`, `Validators`                            |
| `UI/`                   | `InlineKeyboard`, `ReplyKeyboard`, `MenuBuilder`                                                    |
| `Endpoints/`            | `IBotEndpoint`, `BotEndpointExtensions` — auto-discovery обработчиков                               |
| `Extensions/`           | `ServiceCollectionExtensions` — DI-регистрация                                                      |

### TelegramBotFlow.App

Точка кастомизации:

- `Program.cs` — конфигурация middleware, меню, auto-discovery endpoints
- `Endpoints/` — обработчики команд, callback, flows (каждый в отдельном классе)
- DI-регистрация backend API клиентов

## Ключевые компоненты

### UpdateContext

Центральный объект, проходящий через pipeline:

- `Update` — исходный Telegram Update
- `Bot` — `ITelegramBotClient` для отправки ответов
- `Session` — `UserSession` текущего пользователя
- `ChatId`, `UserId`, `MessageText`, `CallbackData` — извлечённые поля
- `Resolve<T>()` — получение сервиса из DI
- `ReplyAsync()`, `EditMessageAsync()`, `AnswerCallbackAsync()`, `DeleteMessageAsync()` — convenience-методы

### Middleware Pipeline

Аналог ASP.NET Core middleware, но для Telegram Update:

```
ErrorHandling → Logging → Session → Flow → Router → Handler
```

Каждый middleware может:

- Выполнить логику до/после следующего
- Прервать цепочку (short-circuit)
- Модифицировать контекст

### Routing

Minimal API-стиль регистрации:

- `MapCommand("/start", handler)` — команды (case-insensitive, поддержка @botname)
- `MapCallback("action:*", handler)` — callback-кнопки с wildcard
- `MapMessage(predicate, handler)` — текст по предикату
- `MapUpdate(predicate, handler)` — любой тип Update

Первый подходящий маршрут выигрывает.

### IBotEndpoint (Auto-Discovery)

Паттерн аналогичный `IEndpoint` в ASP.NET Core Minimal API. Позволяет вынести обработчики из `Program.cs` в отдельные классы:

```csharp
public sealed class StartCommandEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapCommand("/start", async ctx =>
        {
            await ctx.ReplyAsync("Привет!");
        });
    }
}
```

Регистрация в `Program.cs`:

```csharp
builder.Services.AddBotEndpoints(Assembly.GetExecutingAssembly());
// ...
app.MapBotEndpoints();
```

`AddBotEndpoints` сканирует assembly и регистрирует все реализации `IBotEndpoint` через DI.
`MapBotEndpoints` резолвит их и вызывает `MapEndpoint` для каждого.

### Conversation Flows

Пошаговые диалоги (анкеты, формы):

1. `MapFlow` регистрирует flow + команду для запуска
2. `FlowMiddleware` перехватывает ввод пользователя в активном flow
3. `FlowManager` валидирует ввод и переходит к следующему шагу
4. При завершении вызывается `OnComplete` с собранными ответами

Состояние flow хранится в `UserSession`.

### Sessions

- `ISessionStore` — абстракция хранилища
- `InMemorySessionStore` — default (ConcurrentDictionary)
- `UserSession` — key-value хранилище + состояние flow

Для production можно реализовать `RedisSessionStore` / `DbSessionStore`.

## Режимы работы

| Режим   | Компонент                            | Когда использовать             |
| ------- | ------------------------------------ | ------------------------------ |
| Polling | `PollingService` (BackgroundService) | Разработка, нет публичного URL |
| Webhook | Minimal API endpoint + `SetWebhook`  | Production, есть HTTPS URL     |

Переключение через `Bot:Mode` в `appsettings.json`.
