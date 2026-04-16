---
description: Testing conventions for TelegramBotFlow
globs: ["tests/**/*.cs"]
---

# Testing Patterns

## Frameworks
- xUnit for test framework
- FluentAssertions for assertions (`.Should().Be()`, not `Assert.Equal()`)
- NSubstitute for mocking (not Moq)
- Testcontainers.Redis for Redis integration tests

## Unit Tests (`TelegramBotFlow.Core.Tests`)
- Use `TestHelpers.CreateMessageContext()` / `TestHelpers.CreateCallbackContext()` for creating test contexts
- Test internal types directly (InternalsVisibleTo is configured)
- AAA pattern: Arrange-Act-Assert
- One assertion concept per test

## Integration Tests (`TelegramBotFlow.IntegrationTests`)
- Extend `BotFlowTestBase` for pipeline tests
- Use `BotWebApplicationFactory` for full app hosting
- `SendMessageAsync()` / `SendCallbackAsync()` to simulate user input
- `MockResponder` (NSubstitute) to verify Telegram API calls
- Docker must be running for Redis Testcontainers

## Naming
- Test class: `{ClassUnderTest}Tests`
- Test method: `{Method}_{Scenario}_{Expected}` or descriptive name
- Test file location mirrors source location

## What Must Be Tested
- All public API surface
- Middleware behavior (invocation, short-circuit, state mutation)
- Route matching (command, callback, message patterns)
- Wizard step transitions
- Session state persistence
- Endpoint result execution
