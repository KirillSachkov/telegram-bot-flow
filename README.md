# Telegram Bot Flow

Template-проект для создания Telegram-ботов на .NET 10 с middleware pipeline и Minimal API-стилем регистрации обработчиков.

## Концепция

Бот выступает **тонким клиентом** — вся бизнес-логика на бэкенде, бот занимается UI, маршрутизацией команд и вызовом backend API. Проект копируется целиком в новый репозиторий и адаптируется под конкретного бота.

## Документация

- **[Руководство по использованию (USAGE)](docs/USAGE.md)** — логика меню, навигация, управление UI, методы и примеры для разработки бота (экраны, кнопки, input, BotResults, сессия).
- [Архитектура](docs/ARCHITECTURE.md), [API](docs/API.md), [Changelog](docs/CHANGELOG.md) — в папке `docs/`.

## Запуск в режиме разработки

### Предварительные требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Telegram-бот, созданный через [@BotFather](https://t.me/BotFather)
- Docker (опционально — для Seq логирования)

### Шаг 1. Клонировать проект

```bash
git clone <repo-url> my-bot
cd my-bot
```

### Шаг 2. Получить токен бота

1. Откройте [@BotFather](https://t.me/BotFather) в Telegram
2. Отправьте `/newbot` и следуйте инструкциям
3. Скопируйте полученный токен (формат: `123456789:ABCdefGHIjklMNOpqrsTUVwxyz`)

### Шаг 3. Настроить токен

Откройте `src/TelegramBotFlow.App/appsettings.Development.json` и вставьте токен:

```json
{
    "Bot": {
        "Token": "YOUR_BOT_TOKEN_HERE",
        "Mode": "Polling"
    },
    "Serilog": {
        "MinimumLevel": {
            "Default": "Debug"
        }
    }
}
```

> **Важно:** Никогда не коммитьте токен в git. Файл `appsettings.Development.json` уже содержит placeholder `YOUR_BOT_TOKEN_HERE` — замените его на реальный токен только локально.

Альтернативный способ — через переменную окружения:

```bash
export Bot__Token="YOUR_BOT_TOKEN_HERE"
```

### Шаг 4. Запустить бота

```bash
dotnet run --project src/TelegramBotFlow.App
```

В консоли должно появиться:

```
[INF] Bot @your_bot_username started in polling mode
```

Теперь откройте бота в Telegram и отправьте `/start`.

### Шаг 5 (опционально). Запустить Seq для просмотра логов

Seq — веб-интерфейс для структурированных логов. Запускается через Docker:

```bash
docker compose -f docker-compose-infra.yml up -d
```

После запуска откройте http://localhost:8081 — все логи бота будут отображаться в реальном времени.

### Шаг 6. Запустить тесты

```bash
dotnet test
```

## Структура проекта

```
src/
  TelegramBotFlow.Core/          — framework (не нужно менять при копировании)
        Context/                     — UpdateContext, IUpdateResponder, access policy
    Endpoints/                   — IBotEndpoint, auto-discovery
    Extensions/                  — DI-регистрация сервисов
        Screens/                     — IScreen, ScreenManager, ScreenNavigator
    Hosting/                     — BotApplication, Polling, Webhook
    Pipeline/                    — Middleware pipeline
        Pipeline/Middlewares/        — ErrorHandling, Logging, Session, AccessPolicy
    Routing/                     — UpdateRouter, RouteEntry
    Sessions/                    — ISessionStore, InMemorySessionStore
    UI/                          — InlineKeyboard, ReplyKeyboard, MenuBuilder

  TelegramBotFlow.Core.Data/     — EF Core data layer (BotUser, BotDbContext)
    BotUser.cs                   — базовая сущность пользователя (наследуемая)
    BotDbContext.cs               — generic DbContext<TUser> + default
    Middleware/                  — UserTrackingMiddleware (generic + non-generic)
    Configurations/              — EF конфигурация BotUser
    Infrastructure/Migrations/   — миграция InitUsers

  TelegramBotFlow.Broadcasts/    — модуль рассылок (EF Core + Quartz.NET)
    Domain/                      — Broadcast, BroadcastSequence, и др.
    Infrastructure/              — BroadcastsDbContext, EF конфигурации, миграции
    Features/                    — REST API endpoints (Broadcasts, Sequences, Users)
    Services/                    — BroadcastSender (отправка с rate limiting)
    Jobs/                        — Quartz jobs (SequenceProcessor, BroadcastExecution)

  TelegramBotFlow.App/           — ваш бот (точка кастомизации)
    Program.cs                   — конфигурация pipeline и middleware
    Features/                    — фичи по папкам (IBotEndpoint + IScreen)
        Start/                      — StartHandler (/start, /help)
        MainMenu/                   — MainMenuScreen
        Profile/                    — ProfileScreen
        Settings/                   — SettingsScreen
        Help/                       — HelpScreen
        Roadmap/                    — GetRoadmapEndpoint, ClearRoadmapEndpoint, SetRoadmapInputEndpoint, экраны, маркеры действий
        Fallback/                   — FallbackEndpoints
    appsettings.json             — конфигурация (токен, режим, логирование, БД)

tests/
  TelegramBotFlow.Core.Tests/    — unit-тесты framework
```

## Как устроен Program.cs

```csharp
var builder = BotApplication.CreateBuilder(args);

builder.Services.AddBotCoreData(builder.Configuration);

var app = builder.Build();

// Middleware pipeline (порядок важен!)
app.UseErrorHandling();  // 1. Ловит все ошибки
app.UseLogging();        // 2. Логирует вход/выход + время
app.UseSession();        // 3. Загружает сессию пользователя
app.UseAccessPolicy();   // 4. Заполняет ctx.IsAdmin
app.Use<UserTrackingMiddleware>(); // 5. Обновляет users в БД
app.UsePendingInput();   // 6. Маршрутизирует ввод по PendingInputActionId

// Меню бота (кнопка "/" в Telegram)
app.SetMenu(menu => menu
    .Command("start", "Главное меню"));

// Навигация по экранам (nav:* callback встроен во фреймворк)
app.UseNavigation<MainMenuScreen>();

// Auto-discovery всех IBotEndpoint из текущей сборки
app.MapBotEndpoints();

await app.RunAsync();
```

Навигация по экранам (callback `nav:*`) встроена во фреймворк и подключается через `UseNavigation<T>()`, где `T` — экран главного меню (кнопка «Главное меню»).

## Как создать свой endpoint

Создайте класс в `Features/` (в папке своей фичи), реализующий `IBotEndpoint`:

```csharp
public sealed class MyCommandEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapCommand("/mycommand", async (UpdateContext ctx, IUpdateResponder responder) =>
        {
            await responder.ReplyAsync(ctx, "Ответ на команду!");
        });
    }
}
```

Endpoint будет автоматически найден и зарегистрирован через `AddBotEndpoints` + `MapBotEndpoints`.

## Типы обработчиков

| Метод              | Что обрабатывает                                                       | Пример                                                             |
| ------------------ | ---------------------------------------------------------------------- | ------------------------------------------------------------------ |
| `MapCommand`       | Команды (`/start`, `/help`)                                            | `app.MapCommand("/start", handler)`                                |
| `MapCallback`      | Нажатия InlineKeyboard                                                 | `app.MapCallback("profile", handler)`                              |
| `MapCallbackGroup` | Callback с префиксом                                                   | `app.MapCallbackGroup("order", handler)`                           |
| `MapAction`        | Action-кнопки с авто-ответом на callback (и поддержкой Typed Payloads) | `app.MapAction<DeleteAction, DeletePayload>(handler)`              |
| `MapInput`         | Ввод пользователя для ожидаемого action                                | `app.MapInput("roadmap_set_message", handler)`                     |
| `MapMessage`       | Текст по предикату                                                     | `app.MapMessage(ctx => ctx.MessageText == "Привет", handler)`      |
| `MapUpdate`        | Любой тип Update                                                       | `app.MapUpdate(ctx => ctx.Update.Message?.Photo != null, handler)` |
| `MapFallback`      | Fallback при отсутствии route                                          | `app.MapFallback(handler)`                                         |

## XML-документация кода

Ключевые классы и методы в `src/TelegramBotFlow.App` и `src/TelegramBotFlow.Core` покрыты XML-комментариями (`/// <summary>`, `param`, `returns`) для удобной навигации по API и генерации reference-документации.

Параметры handler-а резолвятся через DI: кроме `UpdateContext` можно принимать сервисы (`IUpdateResponder`, `IScreenNavigator`, DbContext и т.д.).

### MapAction vs MapCallback

`MapAction` — упрощённый вариант `MapCallback` для кнопок-действий:

- автоматически отвечает на callback (убирает часики с кнопки)
- если обработчик возвращает `ScreenView`, показывает его в nav-сообщении с кнопкой "← Назад"
- поддерживает передачу типизированных параметров (Payloads) для обхода ограничения Telegram в 64 байта

```csharp
app.MapAction<DeleteAction, DeletePayload>(async (DeletePayload payload) => ...);
```

## Screens (экраны)

Экраны — основной способ строить UI бота. Экран реализует `IScreen`:

```csharp
public sealed class MainMenuScreen : IScreen
{
    public Task<ScreenView> RenderAsync(UpdateContext ctx) =>
        Task.FromResult(new ScreenView("Добро пожаловать! Выберите раздел:")
            .NavigateButton<ProfileScreen>("Профиль")
            .NavigateButton<SettingsScreen>("Настройки")
            .Row()
            .NavigateButton<HelpScreen>("Помощь")
            .Row()
            .Button("🗺 Получить Roadmap", "get_roadmap"));
}
```

Идентификатор экрана вычисляется автоматически: `MainMenuScreen` → `main_menu`, `ProfileScreen` → `profile`.

### ScreenView API

| Метод                           | Описание                                                      |
| ------------------------------- | ------------------------------------------------------------- |
| `NavigateButton<TScreen>(text)` | Кнопка навигации к экрану (callback: `nav:{screenId}`)        |
| `Button(text, callbackData)`    | Произвольная callback-кнопка                                  |
| `UrlButton(text, url)`          | Кнопка-ссылка                                                 |
| `Row()`                         | Начать новую строку кнопок                                    |
| `BackButton(text?)`             | Кнопка "← Назад" (Pop из стека)                               |
| `CloseButton(text?)`            | Кнопка "← Назад" для action-результатов (без изменения стека) |
| `MenuButton(text?)`             | Кнопка "☰ Главное меню" (сброс истории навигации)            |

### Медиа в экранах

```csharp
new ScreenView("Заголовок")
    .WithPhoto(InputFile.FromUri("https://example.com/img.jpg"))
    .BackButton();

// Также: .WithVideo(), .WithAnimation(), .WithDocument()
```

### Навигация

```csharp
// Перейти к экрану
await navigator.NavigateToAsync<ProfileScreen>(ctx);

// Назад по стеку
await navigator.GoBackAsync(ctx);

// Перерисовать текущий экран
await navigator.RefreshScreenAsync(ctx);
```

## UI-элементы

### InlineKeyboard (кнопки под сообщением)

```csharp
var keyboard = new InlineKeyboard()
    .Button("Текст кнопки", "callback_data")  // callback-кнопка
    .Url("Ссылка", "https://example.com")      // кнопка-ссылка
    .Row()                                      // новая строка
    .Button("Ещё кнопка", "another")
    .Build();

await responder.ReplyAsync(ctx, "Сообщение:", keyboard);

// Shortcut для одной кнопки:
InlineKeyboard.SingleButton("Текст", "data");
InlineKeyboard.SingleUrl("Текст", "https://...");
```

### ReplyKeyboard (кнопки вместо клавиатуры)

```csharp
var keyboard = new ReplyKeyboard()
    .Button("Вариант 1")                       // текстовая кнопка
    .Button("Вариант 2")
    .Row()                                      // новая строка
    .RequestContact("Отправить телефон")        // запрос контакта
    .RequestLocation("Отправить геолокацию")    // запрос геолокации
    .OneTime()                                  // скрыть после нажатия
    .Build();

await responder.ReplyAsync(ctx, "Выберите:", keyboard);

// Убрать ReplyKeyboard:
await responder.ReplyAsync(ctx, "Готово", ReplyKeyboard.Remove());
```

## Режимы работы

| Режим       | Когда использовать             | Конфигурация        |
| ----------- | ------------------------------ | ------------------- |
| **Polling** | Разработка, нет публичного URL | `"Mode": "Polling"` |
| **Webhook** | Production, есть HTTPS URL     | `"Mode": "Webhook"` |

Настройка в `appsettings.json`:

```json
{
    "Bot": {
        "Token": "...",
        "Mode": "Polling",
        "WebhookUrl": "https://example.com",
        "WebhookPath": "/api/bot/webhook"
    }
}
```

## Доступ администраторов

Проверка прав администратора выполняется middleware `UseAccessPolicy()` и использует `Bot:AdminUserIds`.

```json
{
    "Bot": {
        "AdminUserIds": [123456789, 987654321]
    }
}
```

## Рассылки (Broadcasts)

Модуль рассылок позволяет:

- **Ручные рассылки** — отправить любое сообщение (текст, фото, видео, документ) всем пользователям бота
- **Последовательности** — автоматические цепочки сообщений после вступления пользователя

### Создание рассылок через Telegram (copyMessage)

Посты для рассылок создаются прямо в Telegram — вы отправляете сообщение боту, и он использует `copyMessage` для доставки:

1. Добавьте свой Telegram ID в `appsettings.json` → `Bot:AdminUserIds`
2. Отправьте боту `/broadcast`
3. Отправьте сообщение (текст с форматированием, фото, видео — что угодно)
4. Бот покажет кнопки **[Отправить всем]** и **[Удалить]**
5. Нажмите **Отправить всем** — рассылка запустится через Quartz job

### Создание последовательностей

Команда `/sequence` в текущей версии не запускает Telegram-мастер.
Для создания последовательностей используйте REST API в модуле рассылок.

### Конфигурация

```json
{
    "Bot": {
        "AdminUserIds": [123456789]
    }
}
```

### REST API (мониторинг и управление)

После запуска бота Swagger UI доступен по адресу: http://localhost:5000/swagger

| Метод    | URL                          | Описание                    |
| -------- | ---------------------------- | --------------------------- |
| `GET`    | `/api/users`                 | Список пользователей бота   |
| `GET`    | `/api/broadcasts`            | Список рассылок             |
| `POST`   | `/api/broadcasts/{id}/send`  | Отправить рассылку          |
| `DELETE` | `/api/broadcasts/{id}`       | Удалить рассылку            |
| `GET`    | `/api/sequences`             | Список последовательностей  |
| `PATCH`  | `/api/sequences/{id}/toggle` | Вкл/выкл последовательность |
| `DELETE` | `/api/sequences/{id}`        | Удалить последовательность  |

### Как работают последовательности

1. Пользователь пишет боту — `UserTrackingMiddleware` сохраняет его в БД
2. Вы создаёте последовательность через REST API модуля рассылок
3. `SequenceProcessorJob` (Quartz, каждую минуту) находит пользователей, для которых пора отправить следующий шаг
4. Сообщение копируется через `copyMessage`, прогресс записывается в БД

## Запуск через Docker (production)

```bash
# Создать .env с токеном и паролем БД
cp .env.example .env
# Вписать BOT_TOKEN и DB_PASSWORD в .env

# Запустить бот + PostgreSQL + Seq
docker compose up -d --build
```

### Для разработки (только инфраструктура)

```bash
docker compose -f docker-compose-infra.yml up -d
dotnet ef database update --project src/TelegramBotFlow.Core.Data --startup-project src/TelegramBotFlow.App
dotnet ef database update --project src/TelegramBotFlow.Broadcasts --startup-project src/TelegramBotFlow.App
dotnet run --project src/TelegramBotFlow.App
```

## Стек

- .NET 10, ASP.NET Core
- [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) 22.9.0
- PostgreSQL 16 + EF Core 10
- Quartz.NET (фоновые задачи, PostgreSQL persistence)
- SachkovTech shared packages (Error, Envelope, IEndpoint, EndpointResult)
- CSharpFunctionalExtensions (Result pattern)
- FluentValidation
- Serilog (Console + Seq)
- Swagger UI
- xUnit + NSubstitute + FluentAssertions (тесты)
- Docker Compose

## Расширение

- **Новый обработчик**: создать класс `IBotEndpoint` в `Features/` (в папке фичи) — найдётся автоматически
- **Новый экран**: создать класс `IScreen` в `Features/` (в папке фичи) — зарегистрировать через `AddScreens<TAssembly>()`
- **Новый middleware**: реализовать `IUpdateMiddleware`, зарегистрировать через `app.Use<T>()`
- **Другое хранилище сессий**: реализовать `ISessionStore`
- **Backend API**: добавить `HttpClient` через `builder.Services.AddHttpClient<T>()`
- **Новый REST endpoint**: реализовать `IEndpoint` (из SachkovTech.Framework) — найдётся автоматически
- **Кастомный пользователь**: наследовать `BotUser`, использовать `AddBotCoreData<TUser, TContext>()`

