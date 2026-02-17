# Cookbook: Backend Feature Slice (.NET)

## Когда использовать

Добавление нового backend endpoint в микросервис.

## Шаблон шагов

1. Добавить request/response DTO в Contracts.
2. Добавить validator.
3. Реализовать handler (command/query split).
4. Реализовать endpoint.
5. Зарегистрировать handler в DI.
6. Добавить интеграционный тест.

## Пример skeleton

```csharp
public sealed class CreateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/items", async (
            [FromBody] CreateRequest request,
            CreateHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(request, ct);
            return result.ToResponse();
        });
    }
}
```

## Anti-patterns

- Бизнес-логика в endpoint.
- Пропуск валидации.
- Использование исключений для бизнес-ошибок.
