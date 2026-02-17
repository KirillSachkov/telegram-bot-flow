# Project Onboarding Guide

## 1) Add playbook as submodule

```bash
git submodule add <PLAYBOOK_REPO_URL> .playbook
git submodule update --init --recursive
```

## 2) Bootstrap project files

macOS/Linux:

```bash
bash .playbook/scripts/bootstrap.sh .
```

Windows (PowerShell):

```powershell
./.playbook/scripts/bootstrap.ps1 -TargetDir . -PlaybookDir .playbook
```

## 3) Commit setup

```bash
git add .playbook .github .cursor .cline
git commit -m "chore: connect engineering playbook"
```

## 4) Upgrade playbook version (scheduled)

```bash
cd .playbook
git fetch --tags
git checkout <VERSION_TAG>
cd ..
bash .playbook/scripts/update-playbook.sh .
```

## 5) Per-project adaptation

- В `copilot-instructions.md` добавить проектный контекст.
- В `.cursor/rules` добавить ограничения конкретного домена.
- В `.cline/rules.md` зафиксировать workflow команды.

## Examples

### education-platform

- Зафиксировать backend/frontend архитектурные ограничения.
- Добавить cookbook для feature slices, RabbitMQ, migrations.

### telegram-bot-flow

- Зафиксировать pipeline/routing/session conventions.
- Добавить cookbook для endpoint + flow transitions.
