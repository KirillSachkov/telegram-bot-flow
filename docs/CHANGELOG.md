# Changelog

Все значимые изменения проекта документируются в этом файле.

Формат основан на [Keep a Changelog](https://keepachangelog.com/ru/1.0.0/).

## [Unreleased]

## [0.2.0] — 2026-02-18

### Added

- Проект `TelegramBotFlow.Core.Data` — data layer фреймворка
    - `BotUser` — базовая сущность пользователя (наследуемая, по аналогии с ASP.NET Identity)
    - `BotDbContext<TUser>` — generic DbContext
    - `UserTrackingMiddleware<TUser>` — автоматическое отслеживание пользователей (generic + non-generic)
    - `AddBotCoreData()` / `AddBotCoreData<TUser, TContext>()` — DI extensions
    - EF Core миграция `InitUsers` (таблица `users`)
- Модуль рассылок (`TelegramBotFlow.Broadcasts`)
    - Domain-модели: `Broadcast`, `BroadcastSequence`, `BroadcastSequenceStep`, `UserSequenceProgress`
    - EF Core + PostgreSQL для хранения данных рассылок
    - Quartz.NET для фонового выполнения задач (PostgreSQL persistence)
    - REST API для мониторинга и управления рассылками через Swagger UI
    - `BroadcastSender` — отправка через `copyMessage` с rate limiting
    - `SequenceProcessorJob` — обработка последовательных рассылок (каждую минуту)
    - `BroadcastExecutionJob` — выполнение ручных рассылок
    - `AdminBroadcastEndpoint` — создание рассылок через Telegram (IBotEndpoint)
- `MapAction` — новый метод роутинга для action-кнопок
    - автоматически отвечает на callback (убирает часики с кнопки)
    - если обработчик возвращает `ScreenView` — показывает его в nav-сообщении
- `ScreenView` — rich builder для экранов
    - `NavigateButton<TScreen>(text)` — кнопка навигации по типу экрана
    - `BackButton()` / `CloseButton()` / `MenuButton()` — стандартные кнопки навигации
    - `UrlButton(text, url)` — кнопка-ссылка
    - Медиа-вложения: `WithPhoto()`, `WithVideo()`, `WithAnimation()`, `WithDocument()`
- `Handlers/` — папка для `IBotEndpoint` классов в App
    - `StartHandler` — `/start` (навигация на `MainMenuScreen`) и `/help`
    - `NavigationHandler` — обработчик `nav:*` callback (back, close, menu, навигация по ID)
    - `RoadmapHandler` — action-кнопка `get_roadmap` с отображением roadmap
    - `FallbackHandler` — fallback для необработанных сообщений
- `Screens/` — папка для `IScreen` реализаций в App
    - `MainMenuScreen`, `ProfileScreen`, `SettingsScreen`, `HelpScreen`
- `Conversations/` — папка для пошаговых сценариев (placeholder)
- `IUpdateResponder` / `UpdateResponder` — сервис отправки ответов пользователю
- `IUserAccessPolicy` / `BotConfigurationUserAccessPolicy` — политика доступа администратора
- `AccessPolicyMiddleware` — middleware для вычисления `UpdateContext.IsAdmin`
- `BotRuntime` — класс запуска бота (Polling / Webhook)
- `HandlerDelegateFactory` — фабрика делегатов для DI-резолюции параметров обработчиков
- PostgreSQL в docker-compose для хранения данных
- Документация: `API.md`, `ARCHITECTURE.md`, `CHANGELOG.md`, `INFRASTRUCTURE.md`

### Changed

- `IScreen` — удалено свойство `Id`; идентификатор экрана теперь вычисляется автоматически по имени класса: суффикс `Screen` обрезается, результат переводится в `snake_case` (`MainMenuScreen` → `main_menu`, `SettingsLangScreen` → `settings_lang`)
- `ScreenRegistry` — убран `Activator.CreateInstance` для чтения `Id`; добавлены статические методы `GetIdFromType(Type)` и `GetIdFor<TScreen>()`
- `AddScreens()` — устранён анти-паттерн `BuildServiceProvider()`; экраны регистрируются через factory-singleton
- `AddTelegramBotFlow()` — `ScreenRegistry` регистрируется через `TryAddSingleton`
- `ScreenManager.RenderScreenAsync` — метод стал `internal`; публичный API навигации — только `NavigateToAsync`
- `UserSession.ClearScreen()` → `ClearCurrentScreen()` — уточнено название метода
- Пользователи (`BotUser`) вынесены из Broadcasts в Core.Data; `BroadcastsDbContext` содержит только broadcast-специфичные таблицы
- Посты рассылок теперь создаются через Telegram (`copyMessage`): `Broadcast` и `BroadcastSequenceStep` хранят `FromChatId` + `MessageId` вместо `Content`; удалены `POST /api/broadcasts` и `POST /api/sequences`
- `UpdateContext` стал data-only объектом: удалены convenience-методы ответа и service locator API; отправка вынесена в `IUpdateResponder`; флаг `IsAdmin` вычисляется в `UseAccessPolicy()`
- `BotApplication` middleware pipeline: `UseAccessPolicy()` добавлен как стандартный этап
- `AdminSequenceEndpoint`: Telegram-мастер создания последовательности временно отключён; `/sequence` возвращает инструкцию использовать REST API
- Redis session store перемещён в подпапку `Sessions/Redis/`
- `Endpoints/` в App переименован в `Handlers/`; `Screens.cs` разбит на отдельные файлы в `Screens/`

### Removed

- `FlowBuilder`, `FlowDefinition`, `FlowManager`, `FlowStep`, `Validators` — модуль Flow удалён (заменяется `Conversations/` в будущем)
- `ThrottlingMiddleware` / `ThrottlingOptions` — Rate limiting удалён из фреймворка
- `RedisSessionStore` из корня `Sessions/` (перемещён в `Sessions/Redis/`)

## [0.1.0] — 2025-02-01

### Added

- Базовый фреймворк Telegram Bot Flow
    - Middleware pipeline (ErrorHandling, Logging, Throttling, Session, Flow)
    - Minimal API-стиль роутинга (`MapCommand`, `MapCallback`, `MapMessage`, `MapFlow`)
    - `IBotEndpoint` auto-discovery
    - `FlowBuilder` для пошаговых диалогов
    - `InlineKeyboard` / `ReplyKeyboard` UI-компоненты
    - `ISessionStore` (InMemory / Redis)
    - Rate limiting (Sliding Window, per-user)
    - Polling / Webhook режимы
    - Serilog + Seq для логирования
    - Docker Compose
