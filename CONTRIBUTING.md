# Contributing to SOCAR Dispatch

First off, thank you for contributing to **SOCAR Dispatch**! This platform is mission-critical for industrial safety, occupational health, and rapid emergency dispatch across refinery operations. To maintain code excellence, security, and traceability, all contributors are expected to follow the guidelines outlined below.

---

## 🧭 Code of Conduct & Corporate Ethics

As an enterprise engineering project:
* Security, operational resilience, and data privacy (KVKK / GDPR) are non-negotiable priorities.
* Do **not** commit hardcoded credentials, production connection strings, or personal file paths under any circumstances.
* Keep communications professional, constructive, and respectful across all issues and pull requests.

---

## 🌿 Git Branching Strategy

We follow a structured Gitflow/Trunk-based workflow aligned with our corporate Jira project keys (**SDDC**):

### Branch Naming Format
```
<type>/SDDC-<issue-id>-<short-description>
```

| Prefix | Usage | Example |
| :--- | :--- | :--- |
| `feature/` | New operational features, endpoints, or UI screens | `feature/SDDC-71-incidents-pagination` |
| `bugfix/` | Non-urgent defect fixes scheduled in sprints | `bugfix/SDDC-83-status-action-bar-lag` |
| `hotfix/` | Production blocker fixes requiring immediate deployment | `hotfix/signalr-reconnection-leak` |
| `refactor/`| Code improvements without business logic change | `refactor/SDDC-70-cqrs-pagination-engine` |
| `chore/`   | Dependencies, Docker, or build tooling updates | `chore/upgrade-postgis-16` |
| `docs/`    | Documentation, README, or architecture diagrams | `docs/update-architecture-flow` |

---

## 📝 Conventional Commits Standard

All commit messages must follow the [Conventional Commits v1.0.0](https://www.conventionalcommits.org/) specification:

```
<type>(<scope>): <description>

[optional body]

[optional footer: Jira Reference]
```

### Commit Types
* `feat`: A new user-facing or API feature.
* `fix`: A bug fix.
* `refactor`: Code change that neither fixes a bug nor adds a feature.
* `perf`: A code change that improves execution speed or reduces memory footprint.
* `test`: Adding missing tests or correcting existing automated tests.
* `docs`: Documentation-only updates.
* `chore`: Maintenance tasks, dependencies, or build tool adjustments.
* `ci`: Changes to CI/CD workflows and automated pipelines.

### Standard Scopes
`api`, `auth`, `incidents`, `teams`, `telemetry`, `mobile`, `web`, `db`, `infra`, `ui`

### Examples
```bash
# Good commit messages
git commit -m "feat(api): implement server-side pagination for incidents query"
git commit -m "fix(mobile): prevent race condition during 1-click SOS location acquisition"
git commit -m "perf(db): add composite B-Tree indexes on incidents and teams"
git commit -m "docs(readme): add system architecture Mermaid flowchart"
```

---

## 🏗️ Architectural Guidelines

### 1. Backend (.NET 8 Core)
* Strictly observe **Clean Architecture** dependency rules:
  * `Domain` has zero external dependencies.
  * `Application` depends only on `Domain` and defines interfaces (`IApplicationDbContext`, `IMediaStorageService`).
  * `Infrastructure` implements persistence (EF Core, Redis, MinIO) and external adapters.
  * `API` acts strictly as an HTTP & WebSocket transport layer (Controllers & SignalR Hubs).
* Use the **CQRS pattern with MediatR** for all use cases.
* Validate all incoming commands using **FluentValidation**.

### 2. Mobile Client (Flutter)
* Organize code using a **Feature-First** architecture:
  * `lib/features/<feature_name>/presentation/` (Views, Widgets, Controllers/Notifiers)
  * `lib/features/<feature_name>/domain/` (Models, Entities)
  * `lib/features/<feature_name>/data/` (Repositories, Data Sources, Services)
* Keep widgets clean; isolate business logic into state notifiers or BLoCs.
* Always handle offline state and GPS location permission edge-cases gracefully.

---

## 🔄 Development & Pull Request Lifecycle

1. **Synchronize:** Pull the latest changes from the default branch:
   ```bash
   git checkout main && git pull origin main
   ```
2. **Branch Out:** Create a branch following the naming convention:
   ```bash
   git checkout -b feature/SDDC-71-incident-pagination
   ```
3. **Develop & Test Locally:**
   ```bash
   # Run quality checks before pushing
   make lint
   make test
   ```
4. **Commit:** Ensure commit messages follow Conventional Commits.
5. **Open Pull Request:**
   * Push your branch to GitHub and create a Pull Request targeting `main`.
   * Fill out all sections of [`.github/pull_request_template.md`](file:///Users/demircucu/Documents/SOCAR/socar-dispatch/.github/pull_request_template.md).
   * Link the associated Jira issue key (`SDDC-XX`).
6. **Review & Merge:**
   * All GitHub Actions CI checks (`backend-ci`, `mobile-ci`) must pass green.
   * Require at least one peer code review approval before merging.
