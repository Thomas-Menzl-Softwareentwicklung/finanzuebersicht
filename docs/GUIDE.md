# Entwickler-Leitfaden — Finanzübersicht

Dieser Leitfaden hilft beim lokalen Aufbau, Testen und Entwickeln der App.

## 1. Repository klonen

```bash
git clone https://github.com/tom4711/finanzuebersicht.git
cd finanzuebersicht
dotnet restore
```

## 2. Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- **macOS:** Xcode für iOS/Mac Catalyst Builds
- **Empfohlen:** `dotnet workload install maui`

Workloads installieren (macOS):

```bash
dotnet workload install maui
```

## 3. Build & Run

### Mac Catalyst (lokal starten)

`dotnet run` und `-t:Run` scheitern wegen macOS-Sandboxing — Debug-Builds landen automatisch in `~/Applications`:

```bash
dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-maccatalyst
open ~/Applications/Finanzübersicht.app
```

Manuell kopieren (Apple Silicon `arm64`, Intel `x64`):

```bash
cp -R Finanzuebersicht/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/Finanzübersicht.app ~/Applications/
open ~/Applications/Finanzübersicht.app
```

**Ein-Liner nach jedem Build (arm64):**

```bash
dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-maccatalyst \
  && open ~/Applications/Finanzübersicht.app
```

> Nur das MAUI-Projekt bauen, nicht die ganze Solution mit `-f net10.0-maccatalyst` — sonst werden auch `net10.0`-only-Projekte (Tests, Core, …) auf das Plattform-TFM gezwungen.

### iOS (Simulator)

```bash
dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-ios
```

## 4. Tests

```bash
dotnet test Finanzuebersicht.Tests
```

Aktuell ~280 Unit-Tests (Core, Application, Infrastructure, Presentation).

## 5. Wichtige Pfade & Ressourcen

| Element | Pfad |
|---------|------|
| Lokale Datenspeicherung | Einstellung `DataPath` (Settings) — Standard: `~/Library/Application Support/Finanzuebersicht` |
| Kategorisierungs-Regeln | `Finanzuebersicht.Core/Data/categorization-rules.json` → siehe [CATEGORIZATION_RULES.md](CATEGORIZATION_RULES.md) |
| UI-Texte (Englisch, Fallback) | `Finanzuebersicht/Resources/Strings/AppResources.resx` |
| UI-Texte (Deutsch) | `Finanzuebersicht/Resources/Strings/AppResources.de.resx` |
| Lokalisierungs-Schlüssel | `Finanzuebersicht.Presentation/Resources/Strings/ResourceKeys.cs` |
| Farbdefinitionen | `Finanzuebersicht/Resources/Styles/Colors.xaml` |

## 6. Projektstruktur (Clean Architecture)

```
Finanzuebersicht.slnx              ← Solution file
version.json                       ← Nerdbank.GitVersioning config
Directory.Build.props              ← Shared MSBuild properties

Finanzuebersicht/                  ← MAUI app entry point (net10.0-ios, net10.0-maccatalyst, net10.0-windows)
├── MauiProgram.cs                ← thin DI orchestrator; calls Add*() extension methods
├── App.xaml / App.xaml.cs         ← App lifecycle
├── AppShell.xaml / AppShell.xaml.cs ← Shell navigation (5 Tabs)
├── Services/                      ← MAUI-specific concrete services (LocalizationService, ShellDialogService, etc.)
├── Views/                         ← XAML pages (DashboardPage, TransactionsPage, CashflowPage, etc.)
├── Converters/                    ← Value converters (BetragDisplayConverter, etc.)
├── Resources/Strings/             ← AppResources.resx + AppResources.de.resx
├── Resources/Styles/              ← Colors.xaml, Styles.xaml
├── Charts/                        ← Custom chart implementations
└── Platforms/                     ← iOS, MacCatalyst, Windows platform code

Finanzuebersicht.Presentation/     ← Presentation Layer (net10.0) - MVVM ViewModels & UI abstractions
├── DependencyInjection/           ← AddPresentationViewModels(...)
├── ViewModels/                    ← DashboardViewModel, CashflowViewModel, TransactionsViewModel, etc.
│   └── Settings/                  ← AppearanceViewModel, BackupViewModel, StorageViewModel, AboutViewModel
├── Navigation/                    ← Routes.cs, INavigationService
├── Services/                      ← IDialogService, ILocalizationService, etc.
└── Resources/Strings/             ← ResourceKeys.cs

Finanzuebersicht.Application/      ← Application / Use Cases Layer (net10.0)
├── DependencyInjection/           ← AddApplicationUseCases()
└── UseCases/                      ← Dashboard, Accounts, Transactions, Recurring, SparZiele, Cashflow, …

Finanzuebersicht.Infrastructure/   ← Infrastructure Layer (net10.0)
├── DependencyInjection/           ← AddInfrastructureServices()
└── Services/                      ← SettingsService, BackupService, LocalDataService, *Store.cs

Finanzuebersicht.Core/             ← Domain Layer (net10.0)
├── Models/                        ← Transaction, Category, Account, SparZiel, …
├── Services/                      ← I*Repository, CategorizationService, RecurringGenerationService, …
│   └── Migrations/                ← V1ToV2Migrator, V2ToV3Migrator
└── Data/                          ← categorization-rules.json

Finanzuebersicht.Tests/            ← xUnit tests (net10.0)
└── Tests for all layers (Application, Infrastructure, Presentation, Core)
```

## 7. App-Navigation

**5 Tabs** (`AppShell.xaml`):

| Tab | Seite | Funktion |
|-----|-------|----------|
| Dashboard | `DashboardPage` | Monat/Jahr, Budgets, fällige Daueraufträge, Kontenübersicht, Cashflow-Link |
| Transaktionen | `TransactionsPage` | Liste, Suche, Filter, Vorlagen, Umbuchung |
| Daueraufträge | `RecurringTransactionsPage` | Wiederkehrende Buchungen |
| Verwaltung | `CategoriesPage` | Umschaltbar: Kategorien / Konten |
| Sparziele | `SparZielePage` | Sparziele mit Fortschritt |

**Toolbar:** Einstellungen → `SettingsPage`

**Weitere Routen** (`Routes.cs`): TransactionDetail, TransferDetail, Cashflow, ImportPreview, AccountDetail, CategoryDetail, RecurringTransactionDetail, RecurringInstanceShift, BackupList

## 8. Entwicklungs-Konventionen

### MVVM-Architektur

- **Framework:** CommunityToolkit.Mvvm mit Source Generators (kein manuelles `INotifyPropertyChanged`)
- **DI:** `MauiProgram.cs` orchestriert `AddInfrastructureServices()`, `AddApplicationUseCases()` und `AddPresentationViewModels()`
- **Neue ViewModels:** In `PresentationServiceCollectionExtensions.cs` registrieren
- **Pages:** Erhalten ViewModel via Constructor Injection
- **Data Loading:** Triggern via Command in `OnAppearing()`

### Code-Stil

- **Dezimalzahlen:** `decimal` für Geldbeträge, formatiert mit `CultureInfo.CurrentCulture`
- **UI-Elemente:** `Border` mit `StrokeShape="RoundRectangle"` statt deprecated `Frame`
- **Farben:** Über `Colors.xaml` (Apple System Colors), nutze `AppThemeBinding` für Light/Dark Mode
- **Texte:** `ILocalizationService` + `ResourceKeys` — Keys in beiden `.resx`-Dateien pflegen
- **Dialoge:** `IDialogService` für alle Benutzerdialoge

### Git-Workflow

- **Branch:** Arbeite auf `feature/*`, `fix/*` oder `chore/*` – Basis ist immer `develop`
- **Commit:** Gitmoji + Conventional Commits (siehe [.github/copilot-instructions.md](../.github/copilot-instructions.md))
- **PR:** Erstelle Pull Request gegen `develop`; `main` ist geschützt und nur für Releases

**Branch-Strategie:**

```
feature/* → develop  (PR, für laufende Entwicklung)
develop   → main     (PR, wenn ein Milestone abgeschlossen ist)
main      → v1.x-Tag (löst release.yml aus)
```

## 9. Datenspeicherung

- **Lokal:** JSON-Dateien via `LocalDataService` (Standard, keine externe Abhängigkeit)
- **Stores:** `CategoryStore`, `AccountStore`, `TransactionStore`, `RecurringStore`, `BudgetStore`, `SparZielStore`, `TransactionTemplateStore`
- **Repos:** Features arbeiten gegen spezifische `I*Repository`-Interfaces (`LocalDataService` als Composite)
- **Pfad:** Standardmäßig `~/Library/Application Support/Finanzuebersicht`, konfigurierbar über Einstellungen
- **Konten & Salden:** `GetAccountBalancesUseCase` — Saldo = Anfangssaldo + Σ Buchungen (Umbuchungen auf beiden Konten)
- **CloudKit:** nur noch als Produktidee im Backlog ([#243](https://github.com/tom4711/finanzuebersicht/issues/243)); kein aktiver CloudKit-Code im Repo. Sync-Vorbereitung: [#300](https://github.com/tom4711/finanzuebersicht/issues/300) (v1.20)
- **Daueraufträge:** Automatische Generierung auf `App.OnStart()` und `Window.Resumed`

## 10. Backup & Restore

Nutzer können in den Einstellungen:
- Backup erstellen (ZIP-Export mit allen Daten)
- Backup wiederherstellen mit automatischer Schema-Migration
- CSV-Import durchführen (mit Auto-Kategorisierung)

### Schema-Versionierung

Backups sind versioniert (`SchemaVersion` in den Metadaten). Der `DataMigrationService` migriert ältere Backups beim Restore automatisch:

| Version | Inhalt |
|---------|--------|
| v1 | categories, transactions, recurring |
| v2 | + budgets, sparziele |
| v3 | + accounts |

Neue Migratoren als `IDataMigrator`-Implementierungen in DI registrieren.

## 11. Versionierung

- **System:** Nerdbank.GitVersioning (`version.json`, aktuell Basis `1.19`)
- **Format:** `<major>.<minor>.<git-height>` (z.B. `1.19.3`)
- **MAUI-Version:** Automatisch gesetzt via `ApplicationDisplayVersion` und `ApplicationVersion` zur Buildzeit

```bash
nbgv get-version          # aktuelle Version
nbgv set-version <version> # Version bumpen
```

## 12. CI/CD

- **Quick Checks:** Unit Tests auf Ubuntu — bei jedem Push auf `develop`, `main`, `feature/*` und PRs
- **Full MAUI Build:** PRs gegen `main` und `main`-Pushes (macOS-Runner); Label `run-maui` für expliziten Build
- **Pre-Release:** Actions → "Pre-Release" → Tag z.B. `v1.2.0-beta.1`
- **Release:** Tag-Push `v*` → Artifacts (macOS + Windows) am GitHub Release

## 13. Weitere Dokumentation

| Dokument | Inhalt |
|----------|--------|
| [QUICK_START.md](QUICK_START.md) | Englische Kurzreferenz |
| [ROADMAP.md](ROADMAP.md) | Geplante Features |
| [CATEGORIZATION_RULES.md](CATEGORIZATION_RULES.md) | CSV Auto-Kategorisierung |
| [RECURRING_UI.md](RECURRING_UI.md) | Dauerauftrag Instanz verschieben |
| [ARCHITECTURE_CLEAN_CODE_REVIEW.md](ARCHITECTURE_CLEAN_CODE_REVIEW.md) | Historisches Architektur-Review |
| [copilot-instructions.md](../.github/copilot-instructions.md) | Zentrale AI-/Entwickler-Referenz |

Viel Erfolg! 🚀
