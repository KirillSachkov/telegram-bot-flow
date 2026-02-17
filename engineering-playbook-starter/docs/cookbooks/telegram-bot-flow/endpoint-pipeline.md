# Cookbook: Telegram Endpoint + Pipeline

## Когда использовать

Добавление новой команды/экшена в Telegram bot flow.

## Шаги

1. Создать endpoint-обработчик для команды/callback.
2. Зарегистрировать маршрут в routing/pipeline.
3. Определить состояние сессии (если нужно).
4. Добавить unit tests на routing и flow.

## Skeleton

```csharp
public sealed class HelpCommandEndpoint : ITelegramEndpoint
{
    public bool CanHandle(Update update) =>
        update.Message?.Text?.Equals("/help", StringComparison.OrdinalIgnoreCase) == true;

    public async Task HandleAsync(TelegramContext context, CancellationToken ct)
    {
        await context.SendTextAsync("Help message", ct);
    }
}
```

## Anti-patterns

- Смешивание routing и бизнес-логики в одном классе.
- Отсутствие idempotency в обработке update.
- Неявные переходы между состояниями сессии.
