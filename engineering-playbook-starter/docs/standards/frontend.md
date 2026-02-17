# Frontend Standards (Next.js/React)

## Layering

- Следовать FSD: `app > widgets > features > entities > shared`.
- Запрещены импорты из верхних слоев в нижние.

## State management

- Server state: TanStack Query.
- Client/UI state: Zustand или локальный state.
- Не дублировать одно и то же состояние в нескольких сторах.

## Forms

- React Hook Form + Zod schema.
- Валидация должна быть одинаковой на UI и API уровне.

## UI

- Использовать только принятый design system.
- Не добавлять произвольные токены цветов/теней вне темы.

## Quality

- Тесты на критические пользовательские сценарии.
- Ошибки API отображать предсказуемо и единообразно.
