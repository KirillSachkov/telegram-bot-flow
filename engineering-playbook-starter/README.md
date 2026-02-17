# Engineering Playbook Starter

Единый репозиторий правил разработки и AI-инструкций для нескольких проектов (.NET/Next.js и др.).

## Цели

- Один source of truth для стандартов кода, архитектуры и AI-правил.
- Переиспользование между проектами и устройствами.
- Управляемые обновления через версионирование.

## Быстрый старт

1. Создай новый репозиторий, например `engineering-playbook`.
2. Скопируй содержимое этого starter-kit в новый repo.
3. Создай первый релиз, например `v1.0.0`.
4. Подключай playbook к проектам через `git submodule`.

## Подключение к проекту (рекомендуется)

```bash
git submodule add <PLAYBOOK_REPO_URL> .playbook
git submodule update --init --recursive
bash .playbook/scripts/bootstrap.sh
```

## Обновление версии playbook в проекте

```bash
cd .playbook
git fetch --tags
git checkout v1.0.0
cd ..
bash .playbook/scripts/update-playbook.sh
```

## Где что лежит

- `docs/principles/` — архитектурные инварианты.
- `docs/standards/` — стандарты кодирования.
- `docs/ai/` — инструкции для AI-инструментов.
- `docs/cookbooks/` — практические рецепты по проектам.
- `docs/templates/` — шаблоны документов.
- `integration/` — примеры интеграции в проекты.
- `scripts/` — bootstrap/update скрипты.

## Правила управления

- Все изменения в playbook идут через PR.
- Любое изменение правила сопровождается примером в cookbook.
- Breaking changes помечаются в `docs/governance/versioning.md`.
