# Adoption Playbook

## Step 1 — Pilot

- Выбрать 1 backend и 1 frontend команду.
- Подключить playbook как `git submodule`.
- Включить только PR checklist без блокировок.

## Step 2 — Measure

Минимальные метрики:

- Defect escape rate
- PR review cycle time
- Rework rate после code review
- % PR, где AI output принят без ручной доработки

## Step 3 — Stabilize

- Зафиксировать спорные правила через ADR.
- Убрать дубли и неоднозначности.
- Довести cookbook до покрытия частых сценариев.

## Step 4 — Enforce selectively

- Блокировать только критичные нарушения:
  - безопасность,
  - breaking API без миграции,
  - отсутствие тестов на критичном участке.

## Step 5 — Scale

- Подключать новые проекты через bootstrap.
- Обновлять версию playbook отдельным PR по расписанию.
