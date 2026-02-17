# Versioning Policy

## Scheme

SemVer: `MAJOR.MINOR.PATCH`

- `MAJOR`: несовместимые изменения правил.
- `MINOR`: новые правила/гайды без breaking effect.
- `PATCH`: исправления формулировок, шаблонов и примеров.

## Release cadence

- Базово: 1 раз в 2 недели.
- Внепланово: критические исправления.

## Upgrade policy per project

- Каждый проект фиксирует версию playbook (pinned).
- Обновление playbook делается отдельным PR.
- PR должен содержать impact-анализ и checklist.

## Change control

- Любое новое правило сопровождается:
  - примером кода,
  - anti-example,
  - критериями приемки.
