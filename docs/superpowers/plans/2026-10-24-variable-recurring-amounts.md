# Variable Daueraufträge Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Variable Daueraufträge beim Buchen eine Betragsbestätigung verlangen lassen und den letzten bestätigten Betrag transparent als Cashflow-Schätzung verwenden.

**Architecture:** Das Modell erhält ein rückwärtskompatibles Variabel-Flag und einen separaten letzten Buchungsbetrag. Der bestehende `amountOverride`-Parameter wird wiederverwendet. Nur die 30-Tage-Cashflow-Vorschau nutzt den letzten bestätigten Betrag für variable Einträge und kennzeichnet ihn; die historische Monatsprognose bleibt unverändert.

**Tech Stack:** .NET 10, C#, .NET MAUI, CommunityToolkit.Mvvm, xUnit.

**Spec:** `docs/superpowers/specs/2026-10-24-import-categorization-and-variable-recurring-design.md` (Abschnitte variable Daueraufträge)

## Global Constraints

- Bestehende Daueraufträge bleiben bei fehlendem Feld fest/verhalten sich wie bisher.
- Jeder variable Betrag wird vor dem Buchen bestätigt oder geändert; Schätzwerte erzeugen nie selbst Buchungen.
- Der initial eingetragene `Betrag` bleibt erhalten; der zuletzt bestätigte Wert wird separat als `LastBookedAmount` geführt.
- Die 30-Tage-Cashflow-Vorschau kennzeichnet variable Beträge als Schätzung; die historische Monatsprognose bleibt unverändert.
- Überspringen, Verschieben, Ausnahmen und Duplikatvermeidung behalten ihr bisheriges Verhalten.
- UI-Texte Deutsch/Englisch; Änderungen müssen mit Sync der Dauerauftragsentität kompatibel sein.

## Review Focus

- Legacy-Datensatz ohne `IsVariableAmount`/`LastBookedAmount`: bleibt fester Betrag. Test beim Laden/Speichern vorhandener Daueraufträge.
- Variable ohne vorherige Buchung: initialer Betrag wird als Schätzwert angezeigt und vorausgefüllt. Test für `LastBookedAmount == null`.
- Abbrechen der Betragseingabe: keine Transaktion und keine Aktualisierung des letzten Betrags. Test im Dashboard-Coordinator.
- Ungültige/neue Betragseingabe: kein Commit, verständlicher Validierungsfehler. Test für flexible Parsing-Regeln.
- Variable mit verändertem Override: Transaktion hat bestätigten Betrag, `LastBookedAmount` wird aktualisiert, `Betrag` bleibt Initialwert. Test im Buchungs-Use-Case.

---

### Task 1: Rückwärtskompatibles Dauerauftragsmodell und Speichern

**Files:**
- Modify: `Finanzuebersicht.Core/Models/RecurringTransaction.cs`
- Modify: `Finanzuebersicht.Application/UseCases/RecurringTransactions/SaveRecurringTransactionDetailUseCase.cs`
- Modify: `Finanzuebersicht.Presentation/ViewModels/RecurringTransactionDetailViewModel.cs`
- Modify: `Finanzuebersicht/Views/RecurringTransactionDetailPage.xaml`
- Test: `Finanzuebersicht.Tests/Application/UseCases/SaveRecurringTransactionDetailUseCaseTests.cs`
- Test: `Finanzuebersicht.Tests/ViewModels/RecurringTransactionDetailViewModelTests.cs`

**Interfaces:**
- Produces: `RecurringTransaction.IsVariableAmount : bool` (default `false`) und `RecurringTransaction.LastBookedAmount : decimal?`.
- Produces: gebundenes `RecurringTransactionDetailViewModel.IsVariableAmount : bool`.
- Der vorhandene `Betrag` ist Ausgangsschätzwert und bleibt unabhängig von `LastBookedAmount`.

- [ ] **Step 1: Failing Tests für Legacy-Default, Edit und Speichern schreiben**

Teste fehlendes Variabel-Flag als `false`, Lade-/Speicherrundlauf des Flags und dass `Betrag`/`LastBookedAmount` nicht verwechselt werden.

- [ ] **Step 2: Save/ViewModel-Tests ausführen und erwartete Fehlschläge prüfen**

Run: `dotnet test Finanzuebersicht.Tests --filter FullyQualifiedName~SaveRecurringTransactionDetailUseCaseTests`
Run: `dotnet test Finanzuebersicht.Tests --filter FullyQualifiedName~RecurringTransactionDetailViewModelTests`
Expected: neue Assertions schlagen fehl.

- [ ] **Step 3: Modellfeld und Formulartoggle ergänzen**

Ergänze optionale Felder mit `IsVariableAmount=false` und `LastBookedAmount=null` für alte JSON-Objekte. Lade/speichere den Toggle im ViewModel und Detail-Use-Case; zeige ihn in der bestehenden Detailseite.

- [ ] **Step 4: UseCase- und ViewModel-Tests erneut ausführen**

Run: `dotnet test Finanzuebersicht.Tests --filter FullyQualifiedName~SaveRecurringTransactionDetailUseCaseTests`
Run: `dotnet test Finanzuebersicht.Tests --filter FullyQualifiedName~RecurringTransactionDetailViewModelTests`
Expected: PASS.

- [ ] **Step 5: Task 1 committen**

```bash
git add Finanzuebersicht.Core/Models/RecurringTransaction.cs Finanzuebersicht.Application/UseCases/RecurringTransactions/SaveRecurringTransactionDetailUseCase.cs Finanzuebersicht.Presentation/ViewModels/RecurringTransactionDetailViewModel.cs Finanzuebersicht/Views/RecurringTransactionDetailPage.xaml Finanzuebersicht.Tests/Application/UseCases/SaveRecurringTransactionDetailUseCaseTests.cs Finanzuebersicht.Tests/ViewModels/RecurringTransactionDetailViewModelTests.cs
git commit -m "feat: mark recurring transactions as variable"
```

### Task 2: Bestätigten Override beim Buchen speichern

**Files:**
- Modify: `Finanzuebersicht.Application/UseCases/RecurringTransactions/BookDueRecurringInstanceUseCase.cs`
- Test: `Finanzuebersicht.Tests/Application/UseCases/RecurringTransactions/BookDueRecurringInstanceUseCaseTests.cs`

**Interfaces:**
- Consumes: `RecurringTransaction.IsVariableAmount`, `LastBookedAmount`, bestehender `ExecuteAsync(string recurringTransactionId, DateTime instanceDate, decimal? amountOverride = null, CancellationToken cancellationToken = default)`.
- Für feste Daueraufträge ignoriert der Use-Case `LastBookedAmount`; `amountOverride` bleibt kompatibel zu bestehenden Aufrufern.

- [ ] **Step 1: Failing UseCase-Tests für Variable und Festbetrag ergänzen**

Teste Override-Betrag auf Transaktion, Speicherung von `LastBookedAmount`, unveränderten Initial-`Betrag`, und dass feste Daueraufträge weiterhin mit ihrem `Betrag` buchen.

- [ ] **Step 2: UseCase-Test ausführen und erwartete Fehlschläge prüfen**

Run: `dotnet test Finanzuebersicht.Tests --filter FullyQualifiedName~BookDueRecurringInstanceUseCaseTests`
Expected: neue Assertions schlagen fehl.

- [ ] **Step 3: LastBookedAmount nach erfolgreicher Buchung setzen**

Setze `LastBookedAmount` nur bei variablen Daueraufträgen und nur nach erfolgreichem Speichern der Transaktion; aktualisiere anschließend den Dauerauftrag einschließlich bestehender Sync-Stempel/Sync-Notify.

- [ ] **Step 4: Buchungstests erneut ausführen**

Run: `dotnet test Finanzuebersicht.Tests --filter FullyQualifiedName~BookDueRecurringInstanceUseCaseTests`
Expected: PASS.

- [ ] **Step 5: Task 2 committen**

```bash
git add Finanzuebersicht.Application/UseCases/RecurringTransactions/BookDueRecurringInstanceUseCase.cs Finanzuebersicht.Tests/Application/UseCases/RecurringTransactions/BookDueRecurringInstanceUseCaseTests.cs
git commit -m "feat: remember variable recurring amounts"
```

### Task 3: Dashboard-Betragseingabe für variable Fälligkeiten

**Files:**
- Modify: `Finanzuebersicht.Presentation/Services/IDialogService.cs`
- Modify: `Finanzuebersicht/Services/Dialogs/ShellDialogService.cs`
- Modify: `Finanzuebersicht.Presentation/ViewModels/DashboardDueRecurringCoordinator.cs`
- Modify: `Finanzuebersicht/Resources/Strings/AppResources.resx`
- Modify: `Finanzuebersicht/Resources/Strings/AppResources.de.resx`
- Modify: `Finanzuebersicht.Presentation/Resources/Strings/ResourceKeys.cs`
- Create: `Finanzuebersicht.Tests/ViewModels/DashboardDueRecurringCoordinatorTests.cs`

**Interfaces:**
- Produces: `IDialogService.ShowPromptAsync(string title, string message, string accept, string cancel, string initialValue)` returning `Task<string?>`; `ShellDialogService` chooses MAUI's numeric keyboard internally.
- `DashboardDueRecurringCoordinator.TryBookAsync` fragt bei variablen Items einen Betrag ab, validiert über `FlexibleAmountParser` und übergibt `amountOverride`; bei Abbruch kehrt es ohne Use-Case-Aufruf zurück.

- [ ] **Step 1: Failing Tests für Variabel-Prompt und Cancel ergänzen**

Teste dass variabel den letzten bestätigten Betrag oder initialen `Betrag` vorausfüllt, bestätigte Eingabe an den Buchungs-Use-Case geht und Abbruch keinen Aufruf auslöst; teste dass fest weiterhin nur die bisherige Bestätigung anzeigt.

- [ ] **Step 2: Dashboard-Tests ausführen und erwartete Fehlschläge prüfen**

Run: `dotnet test Finanzuebersicht.Tests --filter FullyQualifiedName~DashboardDueRecurringCoordinatorTests`
Expected: neue Assertions schlagen fehl.

- [ ] **Step 3: Prompt-Service und Coordinator-Verhalten implementieren**

Ergänze in `ShellDialogService` den MAUI Prompt mit Dezimal-Tastatur. Parse Betrag per `FlexibleAmountParser`; bei ungültiger oder nichtpositiver Eingabe lokalisierte Fehlermeldung zeigen und keinen Buchungs-Use-Case aufrufen. Verwende nach erfolgreicher Validierung den vorhandenen Use-Case mit `amountOverride`.

- [ ] **Step 4: Deutsche und englische Texte ergänzen**

Ergänze lokalisierte Prompt-, Betragslabel- und Validierungstexte in den App-Ressourcen und ResourceKeys.

- [ ] **Step 5: Dashboard-Tests erneut ausführen**

Run: `dotnet test Finanzuebersicht.Tests --filter FullyQualifiedName~DashboardDueRecurringCoordinatorTests`
Expected: PASS.

- [ ] **Step 6: Task 3 committen**

```bash
git add Finanzuebersicht.Presentation/Services/IDialogService.cs Finanzuebersicht/Services/Dialogs/ShellDialogService.cs Finanzuebersicht.Presentation/ViewModels/DashboardDueRecurringCoordinator.cs Finanzuebersicht/Resources/Strings/AppResources.resx Finanzuebersicht/Resources/Strings/AppResources.de.resx Finanzuebersicht.Presentation/Resources/Strings/ResourceKeys.cs Finanzuebersicht.Tests/ViewModels/DashboardDueRecurringCoordinatorTests.cs
git commit -m "feat: confirm variable recurring amounts"
```

### Task 4: Schätzung in der 30-Tage-Cashflow-Vorschau kennzeichnen

**Files:**
- Modify: `Finanzuebersicht.Application/UseCases/Dashboard/CashflowModels.cs`
- Modify: `Finanzuebersicht.Application/UseCases/Dashboard/LoadCashflowOutlookUseCase.cs`
- Modify: `Finanzuebersicht/Views/CashflowPage.xaml`
- Modify: `Finanzuebersicht/Views/DashboardPage.xaml`
- Modify: `Finanzuebersicht/Resources/Strings/AppResources.resx`
- Modify: `Finanzuebersicht/Resources/Strings/AppResources.de.resx`
- Modify: `Finanzuebersicht.Presentation/Resources/Strings/ResourceKeys.cs`
- Test: `Finanzuebersicht.Tests/Application/UseCases/LoadCashflowOutlookUseCaseTests.cs`

**Interfaces:**
- Produces: `CashflowEntry.IsEstimatedAmount : bool` (default `false`).
- Projektionen variabler Daueraufträge verwenden `recurring.LastBookedAmount ?? recurring.Betrag`; feste Einträge und bereits gebuchte Transaktionen haben `IsEstimatedAmount=false`.

- [ ] **Step 1: Failing Cashflow-Tests für initiale und letzte Schätzung schreiben**

Teste Variable ohne `LastBookedAmount` projiziert `Betrag` mit Kennzeichen; Variable mit `LastBookedAmount` projiziert den letzten Wert; feste Daueraufträge und tatsächliche Transaktionen bleiben unmarkiert.

- [ ] **Step 2: Cashflow-Test ausführen und erwartete Fehlschläge prüfen**

Run: `dotnet test Finanzuebersicht.Tests --filter FullyQualifiedName~LoadCashflowOutlookUseCaseTests`
Expected: neue Assertions schlagen fehl.

- [ ] **Step 3: Cashflow-Projektion und Kennzeichnung ergänzen**

Ergänze `IsEstimatedAmount`, wähle den effektiven Betrag nur für variable geplante Instanzen und kennzeichne ihn zugänglich in Cashflow-Seite sowie Dashboard-Vorschau. Der historische Monats-Forecast bleibt unverändert.

- [ ] **Step 4: Cashflow-Tests und Projektbuild ausführen**

Run: `dotnet test Finanzuebersicht.Tests --filter FullyQualifiedName~LoadCashflowOutlookUseCaseTests`
Run: `dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-maccatalyst`
Expected: beide erfolgreich; XAML-Bindings und lokalisierte Ressourcen kompilieren.

- [ ] **Step 5: Task 4 committen**

```bash
git add Finanzuebersicht.Application/UseCases/Dashboard/CashflowModels.cs Finanzuebersicht.Application/UseCases/Dashboard/LoadCashflowOutlookUseCase.cs Finanzuebersicht/Views/CashflowPage.xaml Finanzuebersicht/Views/DashboardPage.xaml Finanzuebersicht/Resources/Strings/AppResources.resx Finanzuebersicht/Resources/Strings/AppResources.de.resx Finanzuebersicht.Presentation/Resources/Strings/ResourceKeys.cs Finanzuebersicht.Tests/Application/UseCases/LoadCashflowOutlookUseCaseTests.cs
git commit -m "feat: mark variable recurring cashflow estimates"
```

### Task 5: Regression prüfen

**Files:**
- Test: `Finanzuebersicht.Tests/Application/UseCases/RecurringTransactions/BookDueRecurringInstanceUseCaseTests.cs`
- Test: `Finanzuebersicht.Tests/Application/UseCases/SaveRecurringTransactionDetailUseCaseTests.cs`
- Test: `Finanzuebersicht.Tests/ViewModels/RecurringTransactionDetailViewModelTests.cs`
- Test: `Finanzuebersicht.Tests/ViewModels/DashboardDueRecurringCoordinatorTests.cs`

- [ ] **Step 1: Bestehende Dauerauftrags- und Cashflowtests ausführen**

Run: `dotnet test Finanzuebersicht.Tests --filter FullyQualifiedName~Recurring`
Run: `dotnet test Finanzuebersicht.Tests --filter FullyQualifiedName~Cashflow`
Expected: PASS.

- [ ] **Step 2: Gesamte Test-Suite ausführen**

Run: `dotnet test Finanzuebersicht.Tests`
Expected: PASS.

