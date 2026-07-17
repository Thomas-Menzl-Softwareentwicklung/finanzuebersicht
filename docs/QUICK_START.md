# Quick Start — Finanzübersicht

A quick reference guide for getting started with Finanzübersicht development.

## Clone & Setup

```bash
git clone https://github.com/tom4711/finanzuebersicht.git
cd finanzuebersicht
dotnet restore
dotnet workload install maui
```

## Build & Run

### Mac Catalyst

`dotnet run` fails due to macOS sandboxing — Debug builds install to `~/Applications`:

```bash
dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-maccatalyst
open ~/Applications/Finanzübersicht.app
```

Manual copy (Apple Silicon `arm64`, Intel `x64`):

```bash
cp -R Finanzuebersicht/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/Finanzübersicht.app ~/Applications/
open ~/Applications/Finanzübersicht.app
```

One-liner (arm64):

```bash
dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-maccatalyst \
  && open ~/Applications/Finanzübersicht.app
```

> Build the MAUI project only, not the whole solution with `-f net10.0-maccatalyst`.

### iOS

```bash
dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-ios
```

## Run Tests

```bash
dotnet test Finanzuebersicht.Tests
```

~280 unit tests across Core, Application, Infrastructure, and Presentation.

## Project Structure

| Location | Purpose |
|----------|---------|
| `Finanzuebersicht/` | MAUI app entry point (App.xaml, MauiProgram.cs, Views, Converters, Resources) |
| `Finanzuebersicht.Presentation/` | Presentation layer (ViewModels, Navigation, `AddPresentationViewModels`) |
| `Finanzuebersicht.Application/` | Application / Use Cases layer (`UseCases/*`, `AddApplicationUseCases`) |
| `Finanzuebersicht.Infrastructure/` | Infrastructure layer (`*Store.cs`, `LocalDataService`, `BackupService`, `AddInfrastructureServices`) |
| `Finanzuebersicht.Core/` | Domain layer: Models + `Core.Services` (interfaces, domain services, migrations) |
| `Finanzuebersicht.Tests/` | xUnit tests (net10.0) |

## Key Resources

- **Localization:** `AppResources.resx` (English fallback), `AppResources.de.resx` (German), `ResourceKeys.cs`
- **Colors:** `Finanzuebersicht/Resources/Styles/Colors.xaml`
- **ViewModels:** `Finanzuebersicht.Presentation/ViewModels/` — register new ones in `PresentationServiceCollectionExtensions.cs`
- **Architecture:** MVVM with CommunityToolkit.Mvvm
- **Data:** JSON via `LocalDataService`; new code uses `I*Repository` interfaces

## Architecture Overview

- **MVVM** using CommunityToolkit.Mvvm source generators
- **DI:** `MauiProgram.cs` calls `AddInfrastructureServices()`, `AddApplicationUseCases()`, `AddPresentationViewModels()`
- **Localization:** German + English (device language or Settings)
- **Data Persistence:** JSON files locally; schema migrations v1 → v3 on restore
- **Logging:** `ILogger<T>` (FileLogger removed)

## Features

- Dashboard: month/year overview, budgets, due recurring, account overview card, cashflow link
- 30-day cashflow outlook (planned recurring included)
- Multi-account with opening balance, transfers between accounts
- Transactions: search, filter, templates, swipe delete/duplicate
- CSV import (DKB) with preview and auto-categorization
- Recurring transactions with instance shift/skip/exceptions
- Categories with monthly budgets; savings goals with progress
- Backup & Restore with automatic schema migration
- Accessibility / VoiceOver, Dark Mode, German & English UI

## Development Conventions

- **Branch:** `feature/*`, `fix/*`, `chore/*` from `develop` (not `main`)
- **Commits:** Gitmoji + Conventional Commits (see [.github/copilot-instructions.md](../.github/copilot-instructions.md))
- **Code:** `ILocalizationService` + `ResourceKeys` for text, `IDialogService` for dialogs
- **Monetary values:** Always use `decimal`

## Versioning

Automatic SemVer via **Nerdbank.GitVersioning** (`version.json`, base `1.19`):

```bash
nbgv get-version
```

## Documentation

| Document | Content |
|----------|---------|
| [GUIDE.md](GUIDE.md) | Detailed German developer guide |
| [ROADMAP.md](ROADMAP.md) | Planned features |
| [CATEGORIZATION_RULES.md](CATEGORIZATION_RULES.md) | CSV auto-categorization |
| [RECURRING_UI.md](RECURRING_UI.md) | Recurring instance shift UI |
| [copilot-instructions.md](../.github/copilot-instructions.md) | Central AI/developer reference |

## Contributing

1. Create feature branch from `develop`: `git checkout develop && git checkout -b feature/<name>`
2. Make changes, commit using Conventional Commits + Gitmoji
3. Push and create a Pull Request targeting `develop`
4. When a milestone is complete, PR `develop` → `main`

See [README.md](../README.md#mitmachen) for full branching strategy.
