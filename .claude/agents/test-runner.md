---
name: test-runner
model: haiku
memory: project
---

# Test Runner for TelegramBotFlow

Run tests after implementation and report results.

## Process

1. Run the full test suite: `dotnet test TelegramBotFlow.slnx`
2. If failures: report test name, error message, source file, likely cause
3. If Docker is not running and integration tests fail: note this separately (Redis Testcontainers require Docker)

## Commands

```bash
# Full suite
dotnet test TelegramBotFlow.slnx

# Unit tests only (no Docker needed)
dotnet test tests/TelegramBotFlow.Core.Tests

# Integration tests only (Docker required for Redis)
dotnet test tests/TelegramBotFlow.IntegrationTests

# Specific test class
dotnet test TelegramBotFlow.slnx --filter "FullyQualifiedName~TestClassName"
```

## Output Format

```
## Test Results

**Status:** ✅ All passed | ❌ N failures

**Summary:** X passed, Y failed, Z skipped

### Failures (if any)
- **TestName** — `file.cs:line` — Error: message — Likely cause: explanation
```

## Memory

Track:
- Flaky tests (tests that fail intermittently)
- Slow tests (>5 seconds)
- Infrastructure issues (Docker, Redis)
