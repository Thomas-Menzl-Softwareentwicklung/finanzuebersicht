<div align="center">

# Finanzübersicht

**Persönliche Finanzverwaltung für iOS und macOS (Mac Catalyst) und Windows**

[![CI](https://github.com/tom4711/finanzuebersicht/actions/workflows/ci.yml/badge.svg)](https://github.com/tom4711/finanzuebersicht/actions/workflows/ci.yml)
[![Pre-Release](https://github.com/tom4711/finanzuebersicht/actions/workflows/prerelease.yml/badge.svg)](https://github.com/tom4711/finanzuebersicht/actions/workflows/prerelease.yml)
[![Release](https://github.com/tom4711/finanzuebersicht/actions/workflows/release.yml/badge.svg)](https://github.com/tom4711/finanzuebersicht/actions/workflows/release.yml)
[![Downloads](https://img.shields.io/github/downloads/tom4711/finanzuebersicht/total)](https://github.com/tom4711/finanzuebersicht/releases)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10-blueviolet)](https://dotnet.microsoft.com/download)

</div>

Finanzübersicht ist eine Open‑Source App zur lokalen Verwaltung von Einnahmen, Ausgaben und wiederkehrenden Buchungen. Die App speichert Daten lokal als JSON. Cloud-Sync (z. B. CloudKit) ist eine Backlog-Idee ([#243](https://github.com/tom4711/finanzuebersicht/issues/243)), derzeit nicht im Code enthalten.

Kurz: .NET 10 + MAUI, Multi-Language UI (Deutsch & Englisch), MVVM-Architektur.

## Kern-Features

- Dashboard mit Monats- und Jahresübersicht, Budget-Hinweisen, Trend-Indikator und **Kontenübersicht** (Gesamtsaldo)
- **30-Tage-Cashflow-Vorschau** inkl. geplanter Daueraufträge
- **Mehrere Konten** mit Anfangssaldo, Saldo pro Konto und **Umbuchungen** zwischen Konten
- Transaktionen anlegen, editieren, suchen, filtern, duplizieren und als Vorlage speichern
- CSV-Import (DKB-Format) mit Vorschau, Auto-Kategorisierung und Duplikat-Erkennung
- Wiederkehrende Buchungen (Daueraufträge) mit Instanz-Verschieben und Ausnahmen
- Kategorien mit Icon, Farbe und monatlichem Budget
- **Sparziele** mit Fortschrittsbalken und Prognose
- **Backup & Restore** mit automatischer Schema-Migration (v1 → v3)
- Accessibility / VoiceOver-Unterstützung (iOS & macOS)
- Dark Mode Unterstützung
- Multi-Language Support (Deutsch & Englisch)

Hinweis: Die Benutzeroberfläche unterstützt Deutsch und Englisch; weitere Sprachen sind möglich, werden aber nicht aktiv gepflegt.

## Screenshots

*Dark Mode auf macOS (Mac Catalyst), v1.19*

### Dashboard

Hero-Saldo, Monats-KPIs und eine Analytics-Karte mit Budget-Balken, Donut-Chart und Kategorieliste. Monat/Jahr-Umschalter in der Karte; fällige Daueraufträge als Hinweis mit Schnellaktionen.

| Monatsansicht | Jahresansicht |
|:---:|:---:|
| ![Dashboard Monatsansicht](docs/screenshots/dashboard-monat.png) | ![Dashboard Jahresansicht](docs/screenshots/dashboard-jahr.png) |

Fällige Daueraufträge aufklappen und direkt buchen, überspringen oder verschieben:

![Dauerauftrag auf dem Dashboard](docs/screenshots/dashboard-dauerauftrag.png)

### Transaktionen & Import

Liste mit Vorlagen, Filter, Swipe-Aktionen, Umbuchung zwischen Konten und CSV-Import mit Duplikat-Erkennung.

| Transaktionsliste | Filter & Suche | Swipe-Aktionen |
|:---:|:---:|:---:|
| ![Transaktionsliste](docs/screenshots/transaktionen.png) | ![Filter](docs/screenshots/transaktionen-filter.png) | ![Swipe](docs/screenshots/transaktionen-swipe.png) |

| Neue Transaktion | Umbuchung | Import-Vorschau |
|:---:|:---:|:---:|
| ![Transaktion erfassen](docs/screenshots/transaktion-detail.png) | ![Umbuchung](docs/screenshots/umbuchung.png) | ![Import-Vorschau](docs/screenshots/import-vorschau.png) |

### Daueraufträge

Übersicht wiederkehrender Buchungen und Detailansicht mit Intervall und Erinnerung.

| Übersicht | Detail |
|:---:|:---:|
| ![Daueraufträge](docs/screenshots/dauerauftraege.png) | ![Dauerauftrag bearbeiten](docs/screenshots/dauerauftrag-detail.png) |

### Verwaltung

Kategorien und Konten im einheitlichen Kartenlayout — inkl. Gesamtsaldo, Typ-Icons und Archivieren.

| Kategorien | Konten |
|:---:|:---:|
| ![Kategorien](docs/screenshots/verwaltung-kategorien.png) | ![Konten](docs/screenshots/verwaltung-konten.png) |

Konto bearbeiten (Anfangssaldo):

![Anfangssaldo](docs/screenshots/konto-bearbeiten.png)

### Sparziele

Fortschritt zu Sparzielen mit Prognose.

![Sparziele](docs/screenshots/sparziele.png)

Neues Sparziel anlegen:

![Neues Sparziel](docs/screenshots/sparziel-neu.png)

### Einstellungen

Darstellung, Sprache, Anzeige-Währung, Speicherort, Backup & Restore und App-Info.

| Allgemein | Über & Bibliotheken |
|:---:|:---:|
| ![Einstellungen](docs/screenshots/einstellungen.png) | ![Einstellungen Über](docs/screenshots/einstellungen-ueber.png) |

## Plattformen

| Plattform | Status |
|-----------|--------|
| macOS (Mac Catalyst) | ✅ Unterstützt |
| iOS | ✅ Unterstützt / kein Build wegen fehlendem Dev Account |
| Windows | ✅ Unterstützt / eingeschränkt getestet |

## Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- macOS (Xcode) für iOS / Mac Catalyst Builds
- Optional: Windows + Visual Studio oder Visual Studio Code für  Windows-Builds

Installiere benötigte Workloads:

```bash
dotnet workload install maui
```

## Schnellstart

### Für Deutsch-sprechende Entwickler

Siehe [Entwickler-Leitfaden (docs/GUIDE.md)](docs/GUIDE.md) für detaillierte Setup- und Entwicklungsanweisungen.

```bash
git clone https://github.com/tom4711/finanzuebersicht.git
cd finanzuebersicht
dotnet restore
```

Build & Start (Mac Catalyst):

```bash
dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-maccatalyst
# Debug-Build kopiert automatisch nach ~/Applications (nicht /Applications — macOS 26+ Dev-Signatur)
open ~/Applications/Finanzübersicht.app

# Tests
dotnet test Finanzuebersicht.Tests
```

### For English Speakers

See [Quick Start (docs/QUICK_START.md)](docs/QUICK_START.md) for a concise guide.

## Projektstruktur (Clean Architecture)

**6 Schichten:**

```
Finanzuebersicht/                  ← MAUI App Entry Point (iOS, macOS, Windows)
├── App.xaml, AppShell.xaml, MauiProgram.cs
├── Controls/                      ← SelectionField (Picker-Workaround Mac Catalyst)

Finanzuebersicht.Presentation/     ← Presentation Layer (MVVM)
├── DependencyInjection/           ← AddPresentationViewModels(...)
├── ViewModels/                    ← DashboardVM, TransactionsVM, etc.
├── Navigation/                    ← Shell Navigation
├── Services/                      ← namespace Finanzuebersicht.Presentation.Services
└── Resources/                     ← Presentation resource helpers (e.g. string keys)

Finanzuebersicht.Application/      ← Use Cases / Application Layer (net10.0)
├── DependencyInjection/           ← AddApplicationUseCases()
└── UseCases/                      ← namespace Finanzuebersicht.Application.UseCases.*

Finanzuebersicht.Infrastructure/   ← DI-Registrierung, Persistenz & Infrastrukturdienste (net10.0)
├── DependencyInjection/           ← AddInfrastructureServices()
└── Services/                      ← SettingsService, BackupService, LocalDataService, *Store.cs

Finanzuebersicht.Core/             ← Domain Layer (Modelle, Geschäftslogik, Services)
├── Models/                        ← namespace Finanzuebersicht.Models
└── Services/                      ← namespace Finanzuebersicht.Core.Services (Interfaces, Domain Services, AppPaths, Migrations)

Finanzuebersicht.Tests/            ← xUnit Tests (net10.0)
└── Testet alle Layer (Application, Infrastructure, Presentation, Core)
```

**Notizen:**
- Views bleiben in `Finanzuebersicht/Views/` (MAUI App)
- Converters bleiben in `Finanzuebersicht/Converters/` (MAUI App)
- ViewModels sind in `Finanzuebersicht.Presentation/ViewModels/`
- `FileLogger` wurde entfernt; Logging läuft über `ILogger<T>`
- `IDataService` ist nur noch Legacy-Kompatibilität; neue Features nutzen spezifische Repository-Interfaces
- `MauiProgram.cs` ist ein schlanker Orchestrator für die layer-spezifischen DI-Extensions

## Lokalisierung & Ressourcen

- UI-Texte: `Finanzuebersicht/Resources/Strings/AppResources.resx` (Englisch, Fallback) und `AppResources.de.resx` (Deutsch)
- Schlüssel-Konstanten: `Finanzuebersicht.Presentation/Resources/Strings/ResourceKeys.cs`
- Sprache: Systemsprache oder manuell in den Einstellungen (Deutsch / Englisch)

## Dokumentation

| Dokument | Inhalt |
|----------|--------|
| [CHANGELOG.md](CHANGELOG.md) | Versionshistorie |
| [docs/GUIDE.md](docs/GUIDE.md) | Entwickler-Leitfaden (Deutsch): Setup, Architektur, Konventionen |
| [docs/QUICK_START.md](docs/QUICK_START.md) | Quick Start (Englisch) |
| [docs/ROADMAP.md](docs/ROADMAP.md) | Geplante Features und Milestones |
| [docs/CATEGORIZATION_RULES.md](docs/CATEGORIZATION_RULES.md) | Auto-Kategorisierung beim CSV-Import |
| [docs/RECURRING_UI.md](docs/RECURRING_UI.md) | Dauerauftrag: Instanz verschieben |
| [docs/ARCHITECTURE_CLEAN_CODE_REVIEW.md](docs/ARCHITECTURE_CLEAN_CODE_REVIEW.md) | Historisches Architektur-Review (Mai 2026) |
| [.github/copilot-instructions.md](.github/copilot-instructions.md) | Zentrale AI-/Entwickler-Referenz |

## Versionierung & CI

- Nerdbank.GitVersioning (`version.json`) steuert Versionsnummern (aktuell Basis `1.19`)
- CI / Pre-Release / Release Workflows in `.github/workflows/`

### Full MAUI build (macCatalyst)

- Quick checks (unit tests, linters) werden bei Push auf Branches ausgeführt (ubuntu runner) — das spart macOS-Runner-Minuten.
- Der vollständige MAUI macCatalyst-Build läuft für Pull Requests gegen `main` und für `main`-Pushes. Dadurch werden macOS-Ressourcen nur bei echten Integrationsprüfungen verwendet.
- **Pre-Release:** Manuell via Actions → "Pre-Release" → Run workflow mit gewünschtem Tag (z.B. `v1.2.0-beta.1`).
- **Release:** Bei Tag-Push (`v*`) oder manuellem Trigger werden Build-Artifacts (macOS + Windows) an das GitHub Release gehängt.
- Um einen vollständigen MAUI-Build für einen PR explizit anzustoßen, füge das Label `run-maui` zur PR hinzu oder starte den Workflow manuell über Actions → "CI - Split quick and full" → Run workflow.

## Mitmachen

Arbeite auf Feature-Branches und öffne Pull Requests gegen **`develop`**:

```bash
git checkout -b feature/mein-feature
```

Wenn ein Milestone fertig ist, wird `develop` per PR in `main` gemerged. `main` ist geschützt und immer release-ready.

## Lizenz

GPL v3 — siehe LICENSE
