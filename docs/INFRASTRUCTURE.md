# Инфраструктура

## Обзор компонентов

| Компонент  | Образ                  | Порт      | Назначение                                       |
| ---------- | ---------------------- | --------- | ------------------------------------------------ |
| Bot        | Собственный Dockerfile | —         | Telegram-бот (ASP.NET Core)                      |
| PostgreSQL | `postgres:16-alpine`   | 5432      | Хранение данных (рассылки, пользователи, Quartz) |
| Seq        | `datalust/seq:latest`  | 5341/8081 | Структурированные логи                           |

## Docker Compose

### Разработка (только инфраструктура)

```bash
docker compose -f docker-compose-infra.yml up -d
```

Запускает: PostgreSQL, Seq

### Production (полный стек)

```bash
cp .env.example .env
# Заполнить BOT_TOKEN и DB_PASSWORD в .env

docker compose up -d --build
```

Запускает: Bot, PostgreSQL, Seq

## Переменные окружения

| Переменная    | Описание            | Пример               |
| ------------- | ------------------- | -------------------- |
| `BOT_TOKEN`   | Токен Telegram-бота | `123456789:ABC...`   |
| `DB_PASSWORD` | Пароль PostgreSQL   | `my_secure_password` |

## PostgreSQL

### Connection string

```
Host=localhost;Database=telegram_bot_flow;Username=botflow;Password=<DB_PASSWORD>
```

Для запуска в Docker (внутри сети compose) используется host `postgres`:

```
Host=postgres;Database=telegram_bot_flow;Username=botflow;Password=<DB_PASSWORD>
```

### Базы данных

- `telegram_bot_flow` — основная БД (таблицы рассылок + Quartz)

`users` хранится в этой же БД через `TelegramBotFlow.Core.Data`.

### Таблицы (broadcasts)

| Таблица                    | Назначение                                   |
| -------------------------- | -------------------------------------------- |
| `users`                    | Отслеживание пользователей бота              |
| `broadcasts`               | Ручные рассылки                              |
| `broadcast_sequences`      | Последовательности рассылок                  |
| `broadcast_sequence_steps` | Шаги последовательностей                     |
| `user_sequence_progress`   | Прогресс пользователей в последовательностях |
| `qrtz_*`                   | Таблицы Quartz.NET Scheduler                 |

## Seq (логирование)

- UI: http://localhost:8081
- API: http://localhost:5341
- Данные сохраняются в volume `seq-data`

## Volumes

| Volume          | Назначение        |
| --------------- | ----------------- |
| `seq-data`      | Данные Seq        |
| `postgres-data` | Данные PostgreSQL |
