# Changelog

All notable changes to this project will be documented in this file.

## [1.0.1] - 2026-05-10

### Fixed
- `BackButton` на временном представлении, показанном через `BotResults.ShowView(...)`,
  теперь закрывает overlay и возвращает пользователя на `CurrentScreen`, а не делает pop
  из `NavigationStack`. Раньше callback `nav:back` на action-view ошибочно попадал в
  обычный `NavigateBackAsync` и юзер уезжал на экран *до* `CurrentScreen`, поверх
  которого overlay был показан. Введён `NavigationState.IsActionViewActive` (выставляется
  в `ShowViewAsync`, сбрасывается при любом рендере экрана), который различает два режима
  Back. Поведение `BackButton` на обычном screen-view не изменилось.

## [1.0.0] - 2026-04-15

### Removed
- Broadcasts module (application-specific, not framework concern)
- SachkovTech.* dependencies
- BotSettings/RoadmapMessageConfig (bot-specific)

### Fixed
- Session lock scope -- lock now held for entire middleware pipeline
- Wizard state deserialization crash on malformed JSON
- OnEnter partial state corruption on exception
- NavigationStack exposed as mutable List (now IReadOnlyList)
- UserTrackingMiddleware unbounded memory growth (MemoryCache with 1h TTL)
- Striped lock poor distribution for sequential user IDs
- NavigateBackAsync null session crash
- PendingInputMiddleware silent failure on missing handler (now logs warning)
- NavMessageId delete not catching 429 rate limit errors

### Added
- IBotUser / IBotUserStore<TUser> abstraction (Identity-style)
- TelegramBotFlow.Data.Postgres package (EfBotUserStore)
- Webhook secret token validation (X-Telegram-Bot-Api-Secret-Token)
- [ActionId] attribute for explicit action ID override
- [ScreenId] attribute for explicit screen ID override
- ActionIdResolver for type-safe action ID resolution
- Middleware ordering validation at startup
- Configurable session lock timeout
- Comprehensive test coverage: 309 tests (248 unit + 61 integration)
- CLAUDE.md for AI agent guidance
- README.md (English)

### Changed
- Core.Data renamed to Data.Postgres
- UserTrackingMiddleware moved from Data.Postgres to Core (uses IBotUserStore)
- NavigationStack property returns IReadOnlyList<string>
- Namespaces unified to TelegramBotFlow.Core.* (removed .Abstractions prefix)
- BotUser implements IBotUser interface
- ScreenView.Button<T>() uses ActionIdResolver
- ScreenRegistry respects [ScreenId] attribute
