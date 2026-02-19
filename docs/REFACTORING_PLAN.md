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

### 3. Реструктуризация App по фичам

- Все хэндлеры и экраны перемещены в `Features/{FeatureName}/`
- `IBotAction` маркеры вынесены в отдельные файлы рядом с хэндлерами
- Удалены пустые папки `Handlers/`, `Screens/`, `Conversations/`

### 4. Кэширование UserTrackingMiddleware

- Добавлен `static ConcurrentDictionary<long, byte>` для кэша user ID
- Запрос `AnyAsync` выполняется только один раз на нового пользователя

### 5. Expression.Compile в HandlerDelegateFactory

- `MethodInfo.Invoke` заменён на скомпилированный делегат через `Expression.Lambda().Compile()`
- ~20-50x ускорение hot path вызова обработчиков

### 6. Убраны пустые catch-блоки

- `ScreenMessageRenderer.TryDeleteAsync` — добавлен `LogDebug`
- `ErrorHandlingMiddleware.TryNotifyUser` — добавлен `LogWarning`

### 7. Согласован ParseMode

- `IUpdateResponder.ReplyAsync` и `EditMessageAsync` теперь по умолчанию `ParseMode.Html`
- Согласовано с `ScreenMessageRenderer` который тоже использует `Html`

### 8. StayResult guard для callback

- `StayResult.ExecuteAsync` теперь проверяет `ctx.Update.CallbackQuery is not null` перед `AnswerCallbackAsync`

### 9. Исправлены порты

- `appsettings.json` → `Port=5432` (было 5434), совпадает с docker-compose

### 10. Исправлен Dockerfile

- Добавлена строка `COPY` для `TelegramBotFlow.Core.Data.csproj`

### 11. Документация стиля кода

- Создан `docs/CODE_STYLE.md` — `SCREAMING_CASE` для констант и enum зафиксирован как стандарт проекта
- Все документы `ARCHITECTURE.md`, `API.md`, `INFRASTRUCTURE.md`, `CHANGELOG.md` — билингвальные (RU/EN)

---

## 🔲 Осталось сделать

