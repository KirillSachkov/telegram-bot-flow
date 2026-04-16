---
description: Enforce middleware pipeline ordering rules
globs: ["src/**/Program.cs", "src/**/BotApplicationBuilder.cs", "src/**/Hosting/*.cs"]
---

# Middleware Ordering Rules

## Required Order

```
1. UseErrorHandling()      — must be FIRST (catches all exceptions)
2. UseLogging()            — after error handling
3. UsePrivateChatOnly()    — before session (don't create sessions for groups)
4. UseSession()            — REQUIRED before wizards and pending input
5. UseAccessPolicy()       — after session (reads session for admin check)
6. UseWizards()            — after session (reads/writes wizard state in session)
7. Use<UserTrackingMiddleware<T>>()  — after session (needs session for user lookup)
8. UsePendingInput()       — after session (reads pending action from session)
9. Router (terminal)       — implicit, added by framework
```

## Validated at Startup

`MiddlewareOrderValidator` throws `InvalidOperationException` if:
- `UseWizards()` is registered before `UseSession()`
- `UsePendingInput()` is registered before `UseSession()`

## Middleware Registration

- Middleware is registered on `BotApplicationBuilder` (NOT on `BotApplication`)
- Routes are registered on `BotApplication`
- `UseNavigation<T>()` is on `BotApplication` (it registers routes, not middleware)
