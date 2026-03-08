# Routing Rules

## Signal → Agent Mapping

| Signal | Agent | Notes |
|--------|-------|-------|
| Architecture, design decisions, API surface design | Ethan | Lead — always for scope/structure decisions |
| Code review, PR review | Ethan | Gating authority |
| C# implementation, SDK core, serial protocol, USB, serialization | Luther | Primary backend/SDK dev |
| Bug fixes in SDK implementation | Luther | |
| Unit tests, integration tests, xUnit, test coverage | Brandt | Reviewer role — approves test quality |
| Edge cases, error handling validation | Brandt | |
| README, API docs, XML documentation, CHANGELOG | Benji | |
| Sample code, demo projects, release notes | Benji | |
| GitHub Actions workflows, CI/CD pipelines | Ilsa | |
| NuGet packaging, versioning, release process | Ilsa | |
| Scripts in `/scripts/` | Ilsa | |
| Session logs, decision merges, cross-agent context | Scribe | Silent — never user-facing |
| GitHub issues queue, backlog scanning | Ralph | Monitor — continuous when active |

## Fallback

If ambiguous, route to Ethan first. He'll delegate.
