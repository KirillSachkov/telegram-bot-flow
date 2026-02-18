# API Reference

## BotApplication

Центральный класс фреймворка. Создаёт и запускает bot runtime.

### Создание и запуск

```csharp
var builder = BotApplication.CreateBuilder(args);

builder.Services.AddBotEndpoints(Assembly.GetExecutingAssembly());

var app = BotApplication.Build(builder);

app.UseErrorHandling();
app.UseLogging();
app.UseSession();
app.UseAccessPolicy();

app.MapBotEndpoints();
await app.RunAsync();
```

### Middleware

| Метод                | Описание                                              |
| -------------------- | ----------------------------------------------------- |
| `UseErrorHandling()` | Ловит исключения, логирует и отправляет error message |
| `UseLogging()`       | Логирует вход/выход и время обработки                 |
| `UseSession()`       | Загружает и сохраняет `UserSession`                   |
| `UseAccessPolicy()`  | Заполняет `UpdateContext.IsAdmin`                     |
| `Use<TMiddleware>()` | Подключает кастомный `IUpdateMiddleware`              |

### Routing

| Метод                               | Что обрабатывает                         | Пример                                                             |
| ----------------------------------- | ---------------------------------------- | ------------------------------------------------------------------ |
| `MapCommand(command, handler)`      | Команды                                  | `app.MapCommand("/start", handler)`                                |
| `MapCallback(pattern, handler)`     | Callback-кнопки                          | `app.MapCallback("profile", handler)`                              |
| `MapAction(callbackId, handler)`    | Action-кнопки (авто-ответ + ScreenView)  | `app.MapAction("get_roadmap", handler)`                            |
| `MapCallbackGroup(prefix, handler)` | Callback с префиксом                     | `app.MapCallbackGroup("broadcast", handler)`                       |
| `MapMessage(predicate, handler)`    | Сообщения по предикату                   | `app.MapMessage(ctx => ctx.MessageText == "Да", handler)`          |
| `MapUpdate(predicate, handler)`     | Любой update по предикату                | `app.MapUpdate(ctx => ctx.Update.Message?.Photo != null, handler)` |
| `MapFallback(handler)`              | Fallback, если route не найден           | `app.MapFallback(handler)`                                         |

#### MapAction

`MapAction` — специализированный `MapCallback` для кнопок-действий:
- автоматически отвечает на callback (убирает часики с кнопки)
- если обработчик возвращает `ScreenView`, показывает его в nav-сообщении с кнопкой "← Назад"

```csharp
app.MapAction("get_roadmap", () =>
    Task.FromResult(new ScreenView("Текст ответа").MenuButton()));
```

### SetMenu

```csharp
app.SetMenu(menu => menu
    .Command("start", "Главное меню")
    .Command("help", "Справка"));
```

Устанавливает список команд бота, отображаемый при нажатии `/` в Telegram.

### Внедрение зависимостей в обработчик

Параметры обработчика резолвятся автоматически:

- `UpdateContext` передаётся всегда
- `CancellationToken` берётся из контекста
- сервисы (например `IUpdateResponder`, `IScreenNavigator`, DbContext) берутся из DI scope update-а
- для `MapCallbackGroup` первый параметр `string` получает action-часть callback

```csharp
app.MapCallbackGroup("nav", async (
    UpdateContext ctx,
    string action,
    IUpdateResponder responder,
    IScreenNavigator navigator) =>
{
    await responder.AnswerCallbackAsync(ctx);
    await navigator.NavigateToAsync(ctx, action);
});
```

## UpdateContext

Контекст обновления — только данные update-а и runtime-состояние.

### Свойства

| Свойство            | Тип                 | Описание                                     |
| ------------------- | ------------------- | -------------------------------------------- |
| `Update`            | `Update`            | Исходный Telegram update                     |
| `CancellationToken` | `CancellationToken` | Токен отмены                                 |
| `Session`           | `UserSession?`      | Текущая сессия (заполняется `UseSession`)    |
| `IsAdmin`           | `bool`              | Флаг доступа (заполняется `UseAccessPolicy`) |
| `ChatId`            | `long`              | Chat ID                                      |
| `UserId`            | `long`              | User ID                                      |
| `MessageId`         | `int?`              | ID сообщения                                 |
| `CallbackData`      | `string?`           | Callback data                                |
| `MessageText`       | `string?`           | Текст сообщения                              |
| `CommandArgument`   | `string?`           | Аргумент команды после пробела               |
| `UpdateType`        | `UpdateType`        | Тип update                                   |
| `Screen`            | `string?`           | Текущий экран из сессии                      |

`UpdateContext` больше не содержит методов отправки сообщений и service locator API.

## IUpdateResponder

Сервис отправки ответов пользователю (вместо методов в `UpdateContext`).

| Метод                                                                 | Описание                        |
| --------------------------------------------------------------------- | ------------------------------- |
| `ReplyAsync(context, text, replyMarkup?, parseMode)`                  | Отправить сообщение             |
| `EditMessageAsync(context, text, replyMarkup?, parseMode)`            | Отредактировать текущее message |
| `EditMessageAsync(context, messageId, text, replyMarkup?, parseMode)` | Отредактировать message по ID   |
| `DeleteMessageAsync(context)`                                         | Удалить текущее message         |
| `DeleteMessageAsync(context, messageId)`                              | Удалить message по ID           |
| `AnswerCallbackAsync(context, text?, showAlert)`                      | Ответить на callback            |

## IUserAccessPolicy

Политика доступа администратора:

```csharp
public interface IUserAccessPolicy
{
    bool IsAdmin(UpdateContext context);
}
```

Стандартная реализация: `BotConfigurationUserAccessPolicy`, использует `Bot:AdminUserIds`.

## IBotEndpoint

Интерфейс auto-discovery для endpoint-классов:

```csharp
public interface IBotEndpoint
{
    void MapEndpoint(BotApplication app);
}
```

Регистрация:

```csharp
builder.Services.AddBotEndpoints(Assembly.GetExecutingAssembly());
app.MapBotEndpoints();
```

## IUpdateMiddleware

Контракт middleware pipeline:

```csharp
public interface IUpdateMiddleware
{
    Task InvokeAsync(UpdateContext context, UpdateDelegate next);
}
```

Подключение:

```csharp
app.Use<MyMiddleware>();
```

## Screens API

`IScreen`:

```csharp
public interface IScreen
{
    Task<ScreenView> RenderAsync(UpdateContext ctx);
}
```

`IScreenNavigator`:

```csharp
Task NavigateToAsync(UpdateContext context, string screenId);
Task NavigateToAsync<TScreen>(UpdateContext context) where TScreen : IScreen;
Task GoBackAsync(UpdateContext context);
Task RefreshScreenAsync(UpdateContext context);
```

Идентификаторы экранов вычисляются по конвенции:

- `MainMenuScreen` → `main_menu`
- `ProfileScreen` → `profile`
- `SettingsLangScreen` → `settings_lang`

### ScreenView

`ScreenView` — описание содержимого экрана: текст, медиа, кнопки.

#### Конструктор

```csharp
new ScreenView("Текст экрана")
```

#### Кнопки навигации

| Метод | Описание |
| ----- | -------- |
| `NavigateButton<TScreen>(text)` | Кнопка перехода к экрану (`nav:{screenId}`) |
| `Button(text, callbackData)` | Произвольная callback-кнопка |
| `UrlButton(text, url)` | Кнопка-ссылка |
| `Row()` | Начать новую строку кнопок |
| `BackButton(text?)` | Кнопка "← Назад" (pop стека навигации) |
| `CloseButton(text?)` | Кнопка "← Назад" без изменения стека (для action-результатов) |
| `MenuButton(text?)` | Кнопка "☰ Главное меню" (полный сброс истории навигации) |

#### Медиа

| Метод | Описание |
| ----- | -------- |
| `WithPhoto(url)` / `WithPhoto(InputFile)` | Фото |
| `WithVideo(InputFile)` | Видео |
| `WithAnimation(InputFile)` | GIF / анимация |
| `WithDocument(InputFile)` | Документ |

```csharp
new ScreenView("Описание")
    .WithPhoto(InputFile.FromUri("https://example.com/img.jpg"))
    .NavigateButton<ProfileScreen>("Перейти в профиль")
    .Row()
    .BackButton();
```

## UserSession

`UserSession` — key-value + навигационный state.

### Основные методы

| Метод                     | Описание                       |
| ------------------------- | ------------------------------ |
| `Set/GetString`           | Хранение строковых значений    |
| `GetInt/GetLong/GetBool`  | Typed чтение                   |
| `GetState<T>/SetState<T>` | Typed state через JSON         |
| `Has/Remove`              | Проверка/удаление ключа        |
| `PushScreen/PopScreen`    | Навигационный стек экранов     |
| `ClearCurrentScreen`      | Очистка только текущего экрана |
| `Clear`                   | Полный сброс сессии            |
