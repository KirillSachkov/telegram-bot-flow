# Cookbook: Frontend Feature (FSD)

## Когда использовать

Добавление новой пользовательской возможности в Next.js frontend.

## Шаги

1. Создать слайс в `features/<feature-name>/`.
2. Разделить на `api/`, `model/`, `ui/`.
3. Экспортировать через `index.ts`.
4. Подключить feature в `widgets`/`app`.
5. Добавить тест сценария.

## Skeleton

```ts
// features/example-feature/model/use-example.ts
export function useExample() {
  // business logic
}
```

## Anti-patterns

- Импорт из `app` в `features`.
- Логика API прямо в компонентах UI.
- Дублирование server-state в локальный store без причины.
