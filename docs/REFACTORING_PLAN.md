# План рефакторинга TelegramBotFlow

## ✅ Выполнено

### 1. Убрать токен из appsettings.Development.json
- Заменён на `YOUR_BOT_TOKEN_HERE`
- Файл уже в `.gitignore` — новые клоны не получат реальный токен
- **Действие:** отозвать старый токен через @BotFather, создать новый, использовать через `dotnet user-secrets` или env-переменные

### 2. Пересоздать миграции
- Удалены все миграции Core.Data (включая `Cock`) и Broadcasts
- Создана единая миграция `Init` для Core.Data (users + bot_settings)
- Broadcasts миграции не пересозданы — проект не подключён к App

---

## 🔲 Структура папок App (Гибрид)

### Текущая проблема
Плоские `Handlers/` и `Screens/` — при росте бота превращается в кашу. `IBotAction` маркеры определены внутри Screen-файлов, что затрудняет поиск.

### Целевая структура
```
src/TelegramBotFlow.App/
  Program.cs
  Screens/                          ← общие экраны (без бизнес-логики)
    MainMenuScreen.cs
    HelpScreen.cs
    ProfileScreen.cs
    SettingsScreen.cs
  Features/
    Start/
      StartHandler.cs               ← /start, /help
    Navigation/
      NavigationHandler.cs           ← nav:* callbacks
      FallbackHandler.cs             ← fallback
    Roadmap/
      RoadmapHandler.cs              ← get_roadmap callback
      AdminRoadmapHandler.cs         ← admin actions
      AdminRoadmapScreen.cs          ← admin экран
      SetRoadmapInputScreen.cs       ← ввод roadmap
      ClearRoadmapAction.cs          ← IBotAction marker
      SetRoadmapInput.cs             ← IBotAction marker
  Data/
    BotSettings.cs                   ← перенести из Core.Data
    RoadmapMessageConfig.cs          ← owned type
    BotSettingsConfiguration.cs      ← EF config
```

### Принцип
- **Screens/** — общие UI-экраны без бизнес-логики (MainMenu, Help, Profile, Settings)
- **Features/{Name}/** — хэндлер + экран + actions рядом. Открываешь папку фичи — видишь всё
- **IBotAction маркеры** — отдельные файлы рядом с хэндлерами, а не внутри Screen-классов

---

## 🔲 Перенести BotSettings из Core.Data в App

### Проблема
`BotSettings` с `RoadmapMessageConfig` — app-специфичная сущность, но живёт в фреймворке (`Core.Data`). Нарушает принцип "Core не содержит бизнес-логику".

### Действие
- Перенести `BotSettings`, `RoadmapMessageConfig`, `BotSettingsConfiguration` в `App/Data/`
- Убрать `DbSet<BotSettings>` из `BotDbContext` (оставить только `Users`)
- Создать `AppDbContext : BotDbContext<BotUser>` с `DbSet<BotSettings>` в App

---

## 🔲 Качество кода

### 3. UserTrackingMiddleware — кэширование
- Сейчас `AnyAsync` на каждый update → лишний SQL-запрос
- Добавить in-memory `HashSet<long>` с user IDs или проверку через Session

### 4. HandlerDelegateFactory — compiled delegates
- `MethodInfo.Invoke` на каждый update — медленно
- Заменить на `Expression.Compile()` — ~10x ускорение hot path

### 5. Убрать пустые catch-блоки
- `ScreenMessageRenderer.cs` и `ErrorHandlingMiddleware.cs` глотают исключения
- Минимум: `logger.LogWarning`

### 6. StayResult.ExecuteAsync — guard для callback
- Вызывает `AnswerCallbackAsync` даже при текстовом вводе
- Добавить `if (context.CallbackQuery is not null)`

### 7. Согласовать ParseMode
- `ScreenMessageRenderer` хардкодит `Html`, `IUpdateResponder` — `default` (None)
- Сделать `ParseMode.Html` дефолтом или вынести в `BotConfiguration`

---

## 🔲 Стилистика

### 8. Константы → PascalCase
- `SECTION_NAME` → `SectionName`, `SINGLETON_ID` → `SingletonId`, `BATCH_SIZE` → `BatchSize`
- Enum: `POLLING`/`WEBHOOK`/`COMMAND` → `Polling`/`Webhook`/`Command`

### 9. Язык документации
- Русский для App (бизнес), английский для Core (фреймворк)

### 10. Удалить пустую папку Conversations/

---

## 🔲 Инфраструктура

### 11. Исправить порты
- `appsettings.json` → `Port=5434`, docker-compose → `5432:5432` — не совпадают

### 12. Исправить Dockerfile
- Добавить `COPY` для `Core.Data` и `Broadcasts` csproj — без них `dotnet restore` упадёт

### 13. Добавить auth на REST API
- Все `/api/*` эндпоинты открыты — нужна авторизация для production
