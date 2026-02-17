# Testing Standards

## Pyramid

- Unit tests: бизнес-правила и edge-cases.
- Integration tests: API, БД, внешние адаптеры.
- E2E tests: только ключевые бизнес-потоки.

## Rules

- Один тест — один сценарий.
- Называть тесты по формуле `Given_When_Then`.
- Flaky tests не допускаются в main ветке.

## Coverage strategy

- Не гнаться за глобальным процентом coverage.
- Целиться в high-risk модули и критичные пути.

## CI behavior

- При падении теста PR не merge.
- Все новые багфиксы сопровождаются регрессионным тестом.
