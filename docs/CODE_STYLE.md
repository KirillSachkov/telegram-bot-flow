# Стиль кода / Code Style

## Именование констант и enum / Constant & Enum Naming

В этом проекте для **констант** и **значений enum** используется стиль `SCREAMING_CASE` (верхний регистр с подчёркиванием).

In this project, **constants** and **enum values** use `SCREAMING_CASE` (uppercase with underscores).

```csharp
// Константы / Constants
public const string SECTION_NAME = "Bot";
public const int SINGLETON_ID = 1;
public const int BATCH_SIZE = 25;
public const int MAX_NAVIGATION_DEPTH = 20;

// Enum
public enum BotMode
{
    POLLING,
    WEBHOOK
}
```

> Это сознательное отступление от стандартного .NET PascalCase для констант и enum.
> Причина: визуальная отличимость констант от обычных свойств/методов.

> This is a deliberate deviation from the standard .NET PascalCase for constants and enums.
> Reason: visual distinction of constants from regular properties/methods.

## Прочие правила / Other Rules

| Правило / Rule | Стиль / Style |
|---|---|
| Классы, методы, свойства / Classes, methods, properties | `PascalCase` |
| Локальные переменные, параметры / Local variables, parameters | `camelCase` |
| Приватные поля / Private fields | `_camelCase` |
| Константы / Constants | `SCREAMING_CASE` |
| Enum значения / Enum values | `SCREAMING_CASE` |
| Интерфейсы / Interfaces | `IPascalCase` |

## XML-документация / XML Documentation

- Весь код документируется на **русском языке** с `/// <summary>` тегами.
- All code is documented in **Russian** using `/// <summary>` tags.

## Структура App / App Structure

Фичи организованы в `Features/{FeatureName}/` — хэндлер, экран и action-маркеры рядом.

Features are organized in `Features/{FeatureName}/` — handler, screen, and action markers together.

```
Features/
  MainMenu/
    MainMenuScreen.cs
  Profile/
    ProfileScreen.cs
  Roadmap/
    RoadmapHandler.cs
    AdminRoadmapHandler.cs
    AdminRoadmapScreen.cs
    SetRoadmapInputScreen.cs
    ClearRoadmapAction.cs
    SetRoadmapInput.cs
```
