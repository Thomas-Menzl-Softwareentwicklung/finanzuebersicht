# Import-Kategorisierung Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** CSV-Importvorschläge nachvollziehbar anzeigen und Nutzer offene oder falsche Kategorien gruppiert und in einem Schritt korrigieren lassen.

**Architecture:** Die bestehende `CsvImportOrchestrator`-Erkennung bleibt lokal und wird um Herkunftsmetadaten erweitert. Die Importvorschau erhält Mehrfachauswahl, Gruppenauswahl nach exaktem normalisiertem Zahlungspartner und eine Sammelaktion zur Kategoriezuweisung; der vorhandene Commit-Pfad bleibt bestehen.

**Tech Stack:** .NET 10, C#, .NET MAUI, CommunityToolkit.Mvvm, xUnit.

**Spec:** `docs/superpowers/specs/2026-10-24-import-categorization-and-variable-recurring-design.md` (Abschnitte Import-Kategorisierung)

## Global Constraints

- Die Erkennung bleibt lokal; keine Bankanbindung, Cloud-KI oder Übertragung von Buchungsdaten.
- Gruppierung erfolgt konservativ über denselben exakten normalisierten Zahlungspartner, den die Import-Erkennung verwendet.
- Duplikate und ungültige Zeilen dürfen nicht in Kategorie-Sammelaktionen gelangen.
- Import-Mapping, Duplikaterkennung, Einzelkorrektur und Commit-Verhalten bleiben erhalten.
- UI-Texte müssen Deutsch und Englisch unterstützen; Aktionen müssen zugänglich benannt sein.

## Review Focus

- Unbekannter Zahlungspartner: bleibt unkategorisiert und kann gruppiert korrigiert werden. Test im Orchestrator-/Importtest.
- Uneinheitliche Historie: historische Zuordnung wird nur angezeigt, wenn die bestehende 50-%-Schwelle erfüllt ist. Test für Treffer und Unterschreiten der Schwelle.
- Gleicher Händler mit unterschiedlichen Kategorien: genaue Händlergruppe umfasst alle Zeilen mit demselben normalisierten Händler; Sammeländerung aktualisiert jede ausgewählte Zeile. Test in `ImportPreviewViewModelTests`.
- Duplikat/ungültige Zeile in Auswahl: kann weder Kategorie noch Commit-Zustand durch Bulk-Aktion ändern. Test in `ImportPreviewViewModelTests`.
- Benutzer nimmt Zeile nach Kategorieänderung aus: geänderte Kategorie bleibt im Preview-Modell, `IsIncluded` steuert weiterhin den Commit. Test in `ImportPreviewViewModelTests`.

---

### Task 1: Vorschlagsquelle und Zahlungspartner-Schlüssel im Importmodell

**Files:**
- Modify: `Finanzuebersicht.Core/Models/ImportPreviewRow.cs`
- Create: `Finanzuebersicht.Core/Models/ImportCategoryMatchSource.cs`
- Modify: `Finanzuebersicht.Application/UseCases/Import/CsvImportOrchestrator.cs`
- Test: `Finanzuebersicht.Tests/Application/UseCases/Import/CsvImportUseCaseTests.cs`

**Interfaces:**
- Produces: `ImportPreviewRow.CategoryMatchSource` mit `None`, `KeywordRule`, `History` sowie `ImportPreviewRow.NormalizedPayeeKey`.
- Für Duplikate und ungültige Zeilen: `CategoryMatchSource.None`; für erkannte Regel-/Historientreffer: jeweilige Quelle.

- [ ] **Step 1: Failing tests für Herkunft und Gruppenschlüssel ergänzen**

Teste Regelmatch → `KeywordRule`, historischer Match → `History`, kein Match → `None`; teste außerdem, dass zwei Titel mit gleicher bestehender Normalisierung denselben `NormalizedPayeeKey` erhalten.

- [ ] **Step 2: Importtests ausführen und erwartete Fehlschläge prüfen**

Run: `dotnet test Finanzuebersicht.Tests --filter FullyQualifiedName~CsvImportUseCaseTests`
Expected: neue Assertions schlagen fehl, bestehende Tests bleiben unverändert.

- [ ] **Step 3: Matchresultat im Orchestrator ausgeben**

Passe die private Kategorisierung in `CsvImportOrchestrator` so an, dass Kategorie-ID und Quelle gemeinsam zurückgegeben werden. Setze den normalisierten Zahlungspartner-Schlüssel auf importierbare Zeilen. Bestehende Priorität (Keyword zuerst, danach Historie) und 50-%-Historiengrenze unverändert lassen.

- [ ] **Step 4: Importtests erneut ausführen**

Run: `dotnet test Finanzuebersicht.Tests --filter FullyQualifiedName~CsvImportUseCaseTests`
Expected: PASS.

- [ ] **Step 5: Task 1 committen**

```bash
git add Finanzuebersicht.Core/Models/ImportPreviewRow.cs Finanzuebersicht.Core/Models/ImportCategoryMatchSource.cs Finanzuebersicht.Application/UseCases/Import/CsvImportOrchestrator.cs Finanzuebersicht.Tests/Application/UseCases/Import/CsvImportUseCaseTests.cs
git commit -m "feat: expose import category match sources"
```

### Task 2: Mehrfachauswahl und Sammelzuweisung im ViewModel

**Files:**
- Modify: `Finanzuebersicht.Presentation/ViewModels/ImportPreviewViewModel.cs`
- Test: `Finanzuebersicht.Tests/ViewModels/ImportPreviewViewModelTests.cs`

**Interfaces:**
- Produces: `ImportPreviewRowItemViewModel.IsCategorizationSelected`, `ImportPreviewViewModel.SelectedBulkCategory`, `SelectSamePayeeCommand`, `ApplyCategoryToSelectionCommand`, `ClearCategorizationSelectionCommand`.
- `SelectSamePayeeCommand` nimmt alle committierbaren Zeilen mit demselben nichtleeren `NormalizedPayeeKey` auf; Duplikate und ungültige Zeilen werden ausgeschlossen.

- [ ] **Step 1: Failing ViewModel-Tests für Händlergruppe und Bulk-Zuweisung schreiben**

Teste Händlergruppe markiert gleiche Schlüssel, lässt andere Schlüssel/Duplikate/ungültige Zeilen unmarkiert; Sammelzuweisung ändert die Kategorie und den Status jeder ausgewählten committierbaren Zeile; Abwählen entfernt nur die Auswahl.

- [ ] **Step 2: ViewModel-Tests ausführen und erwartete Fehlschläge prüfen**

Run: `dotnet test Finanzuebersicht.Tests --filter FullyQualifiedName~ImportPreviewViewModelTests`
Expected: neue Assertions schlagen fehl.

- [ ] **Step 3: Auswahl- und Bulk-Commands implementieren**

Implementiere explizite Auswahlzustände auf Zeilen. Beim Category-Apply aktualisiere `ImportPreviewRow.Transaction.KategorieId` und Status mit demselben Verhalten wie die existierende Einzelauswahl; rufe danach den vorhandenen Refresh-Pfad auf. Keine Änderungen an Import-Commit-Semantik.

- [ ] **Step 4: ViewModel-Tests erneut ausführen**

Run: `dotnet test Finanzuebersicht.Tests --filter FullyQualifiedName~ImportPreviewViewModelTests`
Expected: PASS.

- [ ] **Step 5: Task 2 committen**

```bash
git add Finanzuebersicht.Presentation/ViewModels/ImportPreviewViewModel.cs Finanzuebersicht.Tests/ViewModels/ImportPreviewViewModelTests.cs
git commit -m "feat: bulk edit import categories"
```

### Task 3: Vorschlagsquelle und Sammelaktionen in der Importvorschau

**Files:**
- Modify: `Finanzuebersicht/Views/ImportPreviewPage.xaml`
- Modify: `Finanzuebersicht.Presentation/ViewModels/ImportPreviewViewModel.cs`
- Modify: `Finanzuebersicht.Presentation/Resources/Strings/ResourceKeys.cs`
- Modify: `Finanzuebersicht/Resources/Strings/AppResources.resx`
- Modify: `Finanzuebersicht/Resources/Strings/AppResources.de.resx`
- Test: `Finanzuebersicht.Tests/ViewModels/ImportPreviewViewModelTests.cs`

**Interfaces:**
- Consumes: Task 1 match metadata and Task 2 selection commands.
- Produces: lokalisierte Anzeige der Vorschlagsquelle und UI für Auswahl, Zahlungspartnergruppe, Zielkategorie und Sammelzuweisung.

- [ ] **Step 1: Localization/ViewModel-Tests für Vorschlagstexte und gruppierte Auswahl ergänzen**

Teste, dass Keyword-, Historien- und fehlende Vorschläge unterschiedliche lokalisierbare Quellen bereitstellen und Sammelaktionen nach Kategorieänderung die Summary/Filter neu berechnen.

- [ ] **Step 2: Tests ausführen und erwartete Fehlschläge prüfen**

Run: `dotnet test Finanzuebersicht.Tests --filter FullyQualifiedName~ImportPreviewViewModelTests`
Expected: neue Assertions schlagen fehl.

- [ ] **Step 3: Vorschauoberfläche und deutsche/englische Texte ergänzen**

Füge eine kompakte Auswahlsteuerung in die vorhandene Importvorschau ein, kennzeichne die Vorschlagsquelle an der jeweiligen Zeile und zeige die Bulk-Kategorieaktion nur bei aktiver Auswahl. Die bestehende Einzel-Kategorieauswahl bleibt verfügbar.

- [ ] **Step 4: ViewModel-Tests und Projektbuild ausführen**

Run: `dotnet test Finanzuebersicht.Tests --filter FullyQualifiedName~ImportPreviewViewModelTests`
Run: `dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-maccatalyst`
Expected: beide erfolgreich; XAML-Bindings/Resource-Keys kompilieren.

- [ ] **Step 5: Task 3 committen**

```bash
git add Finanzuebersicht/Views/ImportPreviewPage.xaml Finanzuebersicht.Presentation/ViewModels/ImportPreviewViewModel.cs Finanzuebersicht.Presentation/Resources/Strings/ResourceKeys.cs Finanzuebersicht/Resources/Strings/AppResources.resx Finanzuebersicht/Resources/Strings/AppResources.de.resx Finanzuebersicht.Tests/ViewModels/ImportPreviewViewModelTests.cs
git commit -m "feat: explain import category suggestions"
```

### Task 4: Importpfad integrieren

**Files:**
- Test: `Finanzuebersicht.Tests/Application/UseCases/Import/CsvImportUseCaseTests.cs`
- Test: `Finanzuebersicht.Tests/ViewModels/ImportPreviewViewModelTests.cs`

- [ ] **Step 1: Integrationstest für Kategorisieren und Commit ergänzen**

Teste, dass eine in der Sammelaktion zugewiesene Kategorie im Commit in der gespeicherten Transaktion landet und eine nicht ausgewählte Zeile nicht wegen der Kategorieaktion mitimportiert wird.

- [ ] **Step 2: Betroffene Importtests ausführen**

Run: `dotnet test Finanzuebersicht.Tests --filter FullyQualifiedName~Import`
Expected: PASS.

- [ ] **Step 3: Gesamte Test-Suite ausführen**

Run: `dotnet test Finanzuebersicht.Tests`
Expected: PASS.

- [ ] **Step 4: Task 4 committen**

```bash
git add Finanzuebersicht.Tests/Application/UseCases/Import/CsvImportUseCaseTests.cs Finanzuebersicht.Tests/ViewModels/ImportPreviewViewModelTests.cs
git commit -m "test: cover categorized import commit flow"
```

