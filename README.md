# Telegram Bot Flow

Template-проект для создания Telegram-ботов на .NET 10 с middleware pipeline и Minimal API-стилем регистрации обработчиков.

## Концепция

Бот выступает **тонким клиентом** — вся бизнес-логика на бэкенде, бот занимается UI, маршрутизацией команд и вызовом backend API. Проект копируется целиком в новый репозиторий и адаптируется под конкретного бота.

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
    "Token": "123456789:ABCdefGHIjklMNOpqrsTUVwxyz",
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
export Bot__Token="123456789:ABCdefGHIjklMNOpqrsTUVwxyz"
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
    Context/                     — UpdateContext (контекст запроса)
    Endpoints/                   — IBotEndpoint, auto-discovery
    Extensions/                  — DI-регистрация сервисов
    Flows/                       — FlowBuilder, FlowManager, Validators
    Hosting/                     — BotApplication, Polling, Webhook
    Pipeline/                    — Middleware pipeline
    Pipeline/Middlewares/        — ErrorHandling, Logging, Session, Flow
    Routing/                     — UpdateRouter, RouteEntry
    Sessions/                    — ISessionStore, InMemorySessionStore
    UI/                          — InlineKeyboard, ReplyKeyboard, MenuBuilder

  TelegramBotFlow.App/           — ваш бот (точка кастомизации)
    Program.cs                   — конфигурация pipeline и middleware
    Endpoints/                   — обработчики команд, callback, flows
    appsettings.json             — конфигурация (токен, режим, логирование)

tests/
  TelegramBotFlow.Core.Tests/    — unit-тесты framework
```

## Как устроен Program.cs

```csharp
var builder = BotApplication.CreateBuilder(args);

// Регистрация сервисов
builder.Services.AddBotEndpoints(Assembly.GetExecutingAssembly());

var app = BotApplication.Build(builder);

// Middleware pipeline (порядок важен!)
app.UseErrorHandling();  // 1. Ловит все ошибки
app.UseLogging();        // 2. Логирует вход/выход + время
app.UseSession();        // 3. Загружает сессию пользователя
app.UseFlows();          // 4. Перехватывает ввод в активных flow

// Меню бота (кнопка "/" в Telegram)
app.SetMenu(menu => menu
    .Command("start", "Главное меню")
    .Command("help", "Справка"));

// Auto-discovery всех IBotEndpoint из текущей сборки
app.MapBotEndpoints();

await app.RunAsync();
```

## Как создать свой endpoint

Создайте класс в `Endpoints/`, реализующий `IBotEndpoint`:

```csharp
public sealed class MyCommandEndpoint : IBotEndpoint
{
    public void MapEndpoint(BotApplication app)
    {
        app.MapCommand("/mycommand", async ctx =>
        {
            await ctx.ReplyAsync("Ответ на команду!");
        });
    }
}
```

Endpoint будет автоматически найден и зарегистрирован через `AddBotEndpoints` + `MapBotEndpoints`.

## Типы обработчиков

| Метод                    | Что обрабатывает            | Пример                                                             |
| ------------------------ | --------------------------- | ------------------------------------------------------------------ |
| `MapCommand`             | Команды (`/start`, `/help`) | `app.MapCommand("/start", handler)`                                |
| `MapCallback`            | Нажатия InlineKeyboard      | `app.MapCallback("profile", handler)`                              |
| `MapCallback` (wildcard) | Callback с префиксом        | `app.MapCallback("order:*", handler)`                              |
| `MapMessage`             | Текст по предикату          | `app.MapMessage(ctx => ctx.MessageText == "Привет", handler)`      |
| `MapUpdate`              | Любой тип Update            | `app.MapUpdate(ctx => ctx.Update.Message?.Photo != null, handler)` |
| `MapFlow`                | Пошаговый диалог            | `app.MapFlow("/register", flow => flow.Ask(...).OnComplete(...))`  |

## UI-элементы

### InlineKeyboard (кнопки под сообщением)

```csharp
var keyboard = new InlineKeyboard()
    .Button("Текст кнопки", "callback_data")  // callback-кнопка
    .Url("Ссылка", "https://example.com")      // кнопка-ссылка
    .Row()                                      // новая строка
    .Button("Ещё кнопка", "another")
    .Build();

await ctx.ReplyAsync("Сообщение:", keyboard);

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

await ctx.ReplyAsync("Выберите:", keyboard);

// Убрать ReplyKeyboard:
await ctx.ReplyAsync("Готово", ReplyKeyboard.Remove());
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

## Запуск через Docker (production)

```bash
# Создать .env с токеном
cp .env.example .env
# Вписать BOT_TOKEN в .env

# Запустить бот + Seq
docker compose up -d --build
```

## Стек

- .NET 10, ASP.NET Core
- [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) 22.9.0
- Serilog (Console + Seq)
- xUnit + NSubstitute + FluentAssertions (тесты)
- Docker Compose

## Расширение

- **Новый endpoint**: создать класс `IBotEndpoint` в `Endpoints/` — найдётся автоматически
- **Новый middleware**: реализовать `IUpdateMiddleware`, зарегистрировать через `app.Use<T>()`
- **Другое хранилище сессий**: реализовать `ISessionStore`
- **Backend API**: добавить `HttpClient` через `builder.Services.AddHttpClient<T>()`
