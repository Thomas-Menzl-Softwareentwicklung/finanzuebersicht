# Copilot Instructions – Finanzübersicht

> **Für AI-Assistenten (Copilot & Cursor):** Diese Datei ist die zentrale Projektreferenz.
> Cursor lädt zusätzlich `.cursor/rules/app-domain.mdc` (kompakter Domänenindex mit Issue-Links).
> Vor Feature-Analysen: hier lesen, nicht von Null starten.

## Project Overview

Personal finance app built with **.NET 10** and **.NET MAUI**, targeting **macOS (Mac Catalyst)** and **iOS**.
Data is persisted locally as JSON. Architecture: **Clean Architecture + MVVM**.
Languages: **German and English** (`AppResources.resx` / `AppResources.de.resx`).

Current version baseline: `version.json` → `1.19` (patch = git height via Nerdbank.GitVersioning).

## Build & Run

```bash
dotnet restore

# Tests (CI quick path — no MAUI workload required)
dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj -c Release

# Build macOS (Mac Catalyst) — build MAUI project only, not whole solution
dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-maccatalyst -c Debug

# Build for iOS
dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-ios
```

> `ValidateXcodeVersion=false` in the `.csproj` due to SDK/Xcode version tolerance.
> `TreatWarningsAsErrors=true` globally in `Directory.Build.props`.

### Running on macOS (Mac Catalyst)

`dotnet run` and `-t:Run` fail due to macOS sandboxing. Debug builds copy the `.app` to `~/Applications` automatically; start it with:

```bash
dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-maccatalyst -c Debug
open ~/Applications/Finanzübersicht.app
```

> Dev-signed builds may fail to launch from system `/Applications` on macOS 26+ (Launch Constraint). Use `~/Applications` or open directly from the build output (`maccatalyst-arm64/Finanzübersicht.app`).

Manual copy (if needed — Apple Silicon `arm64`, Intel `x64`):

```bash
cp -R Finanzuebersicht/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/Finanzübersicht.app ~/Applications/
open ~/Applications/Finanzübersicht.app
```

One-liner after build (adjust `arm64` / `x64` as needed):

```bash
dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-maccatalyst -c Debug \
  && open ~/Applications/Finanzübersicht.app
```

## Architecture

Layered clean architecture with MVVM (`CommunityToolkit.Mvvm` source generators):

| Project | Role |
|---------|------|
| `Finanzuebersicht.Core` | Models, repository interfaces, domain services (forecast, categorization, migrations) |
| `Finanzuebersicht.Application` | Use cases (orchestration, no UI/IO) |
| `Finanzuebersicht.Infrastructure` | JSON stores, `LocalDataService`, backup, settings, import parsers |
| `Finanzuebersicht.Presentation` | ViewModels, navigation abstractions (`Routes`, `INavigationService`) |
| `Finanzuebersicht` | MAUI app: Views (XAML), Converters, Charts, `MauiProgram`, platform code |
| `Finanzuebersicht.Tests` | xUnit + NSubstitute; references Core, Application, Infrastructure, Presentation |

**Data flow:** View → ViewModel → Use Case → Repository (`I*Repository`) → JSON Store

**Use-case errors (#274):** Expected domain failures return `UseCaseResult` / `UseCaseResult<T>` with `UseCaseErrorCode` (no UI strings in Application). ViewModels map via `UseCaseErrorPresenter` → `ResourceKeys`. Unexpected exceptions: log + alert with user feedback. First adopters: Save Transaction / Transfer / Account; extend when touching other use cases (esp. Import/Backup).

**Repositories:** Prefer `ICategoryRepository`, `IAccountRepository`, `ITransactionRepository`, `IRecurringTransactionRepository`, `IBudgetRepository`, `ISparZielRepository`, `ITransactionTemplateRepository`. Reporting/generation: `IReportingService`, `IRecurringGenerationService`.

**DI entry points:**
- `MauiProgram.cs` — app services, pages
- `AddInfrastructureServices()` — stores + repositories
- `AddApplicationUseCases()` — all use cases
- `AddPresentationViewModels()` — ViewModels (`Finanzuebersicht.Presentation`)

## Navigation

**5 tabs** (`AppShell.xaml`):

| Tab | Page | Purpose |
|-----|------|---------|
| Dashboard | `DashboardPage` | Month/year overview, budgets, due recurring, account filter, cashflow link |
| Transaktionen | `TransactionsPage` | Transaction list, search, templates, account filter |
| Daueraufträge | `RecurringTransactionsPage` | Recurring transactions |
| Verwaltung | `CategoriesPage` | Toggle: **Kategorien** / **Konten** (accounts with balance) |
| Sparziele | `SparZielePage` | Savings goals |

**Toolbar:** Einstellungen → `SettingsPage`

**Shell routes** (`Routes.cs`): TransactionDetail, TransferDetail, RecurringTransactionDetail, RecurringInstanceShift, CategoryDetail, AccountDetail, Settings, BackupList, ImportPreview, Cashflow

## Feature Domains (quick reference)

### Accounts & balances
- `GetAccountBalancesUseCase` — Saldo = `OpeningBalance` + Σ transactions per account (transfers affect both sides)
- Account list with balance breakdown: **Verwaltung → Konten** (`CategoriesViewModel`, `AccountListItem`)
- Dashboard **Kontenübersicht** card with total balance and per-account bars (#213)
- Opening balance per account in **Konto bearbeiten** (#212) — no fake transactions
- Manual reconciliation (#214) — ✅ done (Ist vs. berechnet ohne Bank-API)

### Dashboard
- `LoadDashboardMonthUseCase`, `LoadDashboardYearUseCase`, `LoadForecastUseCase`
- Due recurring widget with book/skip/shift actions

### Cashflow
- `LoadCashflowOutlookUseCase` — 30-day outlook per account (`CashflowPage`, linked from Dashboard)
- Forward-looking cashflow, **not** account balance

### Import
- CSV import (DKB parser), import preview, auto-categorization (`KeywordCategorizationStrategy`, `HistoricalCategorizationStrategy`)
- No Open Banking / bank API integration

### Other
- Sparziele with transaction linking and completion forecast
- Backup/restore (ZIP/JSON), configurable data path
- CloudKit sync is a backlog idea (#243); no active CloudKit implementation in tree
- Active architecture milestone: **v1.20** (`docs/ROADMAP.md`) — before Milestone 22 features and v2.0 (encryption / multi-currency). Next: Backup (#292), then VM splits.

## Data Persistence

- JSON files per entity in configurable data directory (`SettingsService` key `DataPath`)
- Default macOS path: `~/Library/Application Support/Finanzuebersicht` (`AppPaths`)
- Stores: `CategoryStore`, `AccountStore`, `TransactionStore`, `RecurringStore`, `BudgetStore`, `SparZielStore`, `TransactionTemplateStore`
- `LocalDataService` coordinates stores and implements all `I*Repository` interfaces
- Schema migrations via `IDataMigrator` (V1→V2, V2→V3)
- Recurring auto-generation on `App.OnStart()` and `Window.Resumed`

## Key Conventions

- `CommunityToolkit.Mvvm` source generators — no manual `INotifyPropertyChanged`
- ViewModels live in `Finanzuebersicht.Presentation/ViewModels/` (namespace `Finanzuebersicht.ViewModels`)
- Register new ViewModels in `PresentationServiceCollectionExtensions.cs`
- Pages in `Finanzuebersicht/Views/`, registered in `MauiProgram.cs`
- Pages receive ViewModels via constructor injection; `OnAppearing()` triggers `Load*Command`
- Navigation from ViewModels via `INavigationService` + `Routes` constants (no direct View type references)
- Monetary values: `decimal` in C#, formatted with `CultureInfo.CurrentCulture`
- Use `Border` with `StrokeShape="RoundRectangle"` instead of deprecated `Frame`
- Colors in `Resources/Styles/Colors.xaml` — use `AppThemeBinding`, no hardcoded colors in XAML
- Localization: add keys to `AppResources.resx` + `AppResources.de.resx` + `ResourceKeys.cs`

### Mac Catalyst UI (SelectionField)

Native `Picker` controls freeze on **macOS 27 Beta** when scrolling inside picker dialogs in `ScrollView` forms. Workaround: **`SelectionField`** + **`SelectionPopup`** (`Finanzuebersicht/Controls/`, `Finanzuebersicht/Views/Popups/`). Used across detail pages, filters, and import preview. `DatePicker` remains native. Revert to native `Picker` when MAUI SR6 ([#33146](https://github.com/dotnet/maui/pull/33146)) is available. Details: `.cursor/rules/maccatalyst-picker-investigation.mdc`

### Create UX (#265, v1.19)

Unified **create** flows — no navigation push for new entries from list FAB/empty state:

| Entity | Create | Edit |
|--------|--------|------|
| Konto | `CreateFormCard` inline top (`CategoriesPage`) | `AccountDetailPage` |
| Sparziel | `CreateFormCard` inline top (`SparZielePage`) | `SparZielDetailPage` |
| Kategorie | `FormSheetPopup` + `CategoryFormView` | `CategoryDetailPage` |
| Dauerauftrag | `FormSheetPopup` + `RecurringTransactionFormView` | `RecurringTransactionDetailPage` |

Components: `CreateFormCard`, `FormSheetPopup`, `CategoryCreateSheetService`, `RecurringTransactionCreateSheetService`. Full reference: `docs/CREATE_UX.md`.

### Quick Expense Capture (Pro) + iOS Widget

In-app **Schnell** sheet on Transaktionen (`CaptureQuickExpenseUseCase`, Unkategorisiert + default account, `AppFeature.QuickExpenseCapture`) ships on all targets. iOS Home Screen WidgetKit `.appex` is embedded (presets, App Group inbox, deep link). Rebuild/resign on Mac with Xcode when Swift sources change — do **not** recreate domain/UI.

- Agent rule: `.cursor/rules/ios-quick-expense-widget.mdc`
- Steps: `Finanzuebersicht/Platforms/iOS/Widgets/README.md`
- Separate from display widget #242

## Versioning

Automatic **SemVer** via **Nerdbank.GitVersioning** (`version.json`):

- Version = `<major>.<minor>.<git-height>` (e.g. `1.19.5`)
- Bump: edit `version.json` or `nbgv set-version <version>`
- Current: `nbgv get-version`
- Stable releases: `main` and `release/v*` branches

## Git Branching Strategy

**`main` is protected** — release-ready only. All development targets `develop`.

```
main              ← stable (merge from develop when milestone complete)
develop           ← integration branch
├── feature/*     ← new features
├── fix/*         ← bug fixes
├── chore/*       ← tooling, deps, config
└── release/v*    ← release preparation
```

**Workflow:**
1. Branch from `develop`: `git checkout develop && git checkout -b feature/<name>`
2. Push and open PR targeting `develop`
3. Milestone complete: PR `develop` → `main`, then tag

Branch naming: `<type>/<short-description>` in kebab-case (English)

## Project Structure

```
Finanzuebersicht.slnx
version.json
Directory.Build.props              ← TreatWarningsAsErrors, Nerdbank.GitVersioning

Finanzuebersicht.Core/             ← net10.0
├── Models/                        ← Transaction, Category, Account, SparZiel, …
├── Services/                      ← I*Repository, IClock, ForecastService, parsers, …
└── Constants/

Finanzuebersicht.Application/      ← net10.0
└── UseCases/                      ← Dashboard, Accounts, Transactions, Recurring, SparZiele, …

Finanzuebersicht.Infrastructure/     ← net10.0
├── Services/                      ← *Store, LocalDataService, BackupService, SettingsService
└── DependencyInjection/

Finanzuebersicht.Presentation/       ← net10.0
├── ViewModels/
├── Navigation/                    ← Routes.cs
└── Services/                      ← IDialogService, INavigationService, …

Finanzuebersicht/                    ← net10.0-ios, net10.0-maccatalyst
├── MauiProgram.cs
├── AppShell.xaml
├── Controls/                      ← SelectionField (Mac Catalyst picker workaround)
├── Views/                         ← XAML pages + Popups/SelectionPopup
├── Converters/
├── Charts/
├── Resources/Strings/             ← AppResources.resx, AppResources.de.resx
└── Platforms/

Finanzuebersicht.Tests/              ← net10.0
├── Application/UseCases/
├── ViewModels/
├── Infrastructure/
└── Services/
```

## Git Commit Conventions

**Format:** Gitmoji + Conventional Commits (English)

```
<emoji> <type>(<scope>): <short summary>

<body – what changed and why>

<footer>
```

**Types & Gitmoji:**

| Emoji | Type | Usage |
|-------|------|-------|
| ✨ | `feat` | New feature or functionality |
| 🐛 | `fix` | Bug fix |
| ♻️ | `refactor` | Code restructuring without behavior change |
| 💄 | `style` | UI/UX changes, styling, design |
| 🧪 | `test` | Adding or updating tests |
| 📝 | `docs` | Documentation changes |
| 🔧 | `chore` | Build config, dependencies, tooling |
| ⚡ | `perf` | Performance improvement |
| 🗑️ | `remove` | Removing code or files |
| 🚀 | `deploy` | Deployment-related changes |
| 🏗️ | `arch` | Architecture changes (project structure) |

**Scopes:** `core`, `ui`, `viewmodel`, `service`, `model`, `converter`, `test`, `config`, `shell`, `settings`, `accounts`, `dashboard`

**Rules:**
- Subject: imperative mood, max 72 chars, no period
- Body: explain *what* and *why*; list affected files/components
- Breaking changes: `BREAKING CHANGE:` in footer
- Include `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>` when pair-programming with Copilot

## Account-related issues

| Issue | Topic | Status |
|-------|-------|--------|
| [#212](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/212) | Opening balance per account | ✅ Done |
| [#213](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/213) | Dashboard account overview with total balance | ✅ Done |
| [#214](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/214) | Manual balance reconciliation (no bank API) | ✅ Done |
