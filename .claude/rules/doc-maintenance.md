---
description: Keep CLAUDE.md and AGENTS.md up to date when making structural changes
globs: ["src/**/Hosting/*.cs", "src/**/Pipeline/**/*.cs", "src/**/Routing/*.cs", "src/**/Screens/*.cs", "src/**/Wizards/*.cs", "src/**/Extensions/*.cs"]
---

# Documentation Maintenance

When modifying:
- Middleware pipeline, routing, or hosting → check CLAUDE.md "Architecture" and "Middleware ordering" sections
- Screen system or navigation → check CLAUDE.md "Adding a screen" and "Known Gotchas"
- Wizard system → check CLAUDE.md "Adding a wizard" section
- DI registration → check CLAUDE.md "DI registration" section
- BotConfiguration properties → check CLAUDE.md "Configuration" table
- Public API surface → check AGENTS.md extension points section

Update documentation ONLY if the change affects documented behavior. Don't update docs for internal refactors that don't change the API.
