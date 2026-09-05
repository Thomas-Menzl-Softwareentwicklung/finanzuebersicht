# Einheitliches Anlegen (Create UX)

Zielbild nach UX-Mockup (Aug 2026) und Produktklärung: **kontextsensitiver FAB** + **Bottom-Sheet / Modal** (gleicher Chrome, anderer Forminhalt). Issue-Hintergrund: **#265** (v1.19), Richtung #266 für Buchungen.

Referenz-Klickdummy (extern): `Finanzübersicht Mobile App Verfeinerung-2` — dort öffnet `+` fälschlich überall die Schnellerfassung; Soll ist tab-abhängig (siehe unten).

---

## Ist-Zustand (Stand feature/create-ux-modal-sheets)

**Anlegen** (FAB / Empty State) öffnet ein **Modal** über `CreateFormModalService` (Schnell-Muster, kein Toolkit-Popup):

| Kontext | Create |
|---------|--------|
| Verwaltung › Kategorien | `CategoryCreateSheetService` + `CategoryFormView` |
| Verwaltung › Konten | `AccountCreateSheetService` + `AccountFormView` |
| Sparziele | `SparZielCreateSheetService` + `SparZielFormView` |
| Daueraufträge | `RecurringTransactionCreateSheetService` + `RecurringTransactionFormView` |
| Dashboard Schnell-FAB | `QuickExpenseCaptureSheetService` + `QuickExpenseFormView` (kostenlos) |
| Transaktionen `+` | `TransactionCreateSheetService` + `TransactionFormView` |
| Transaktionen Umbuchen | `TransferCreateSheetService` + `TransferFormView` |
| Transaktionen Import | CSV-Flow (kein Sheet) |
| Umbuchen / volle Transaktion (Bearbeiten) | Detail-Page (`TransferDetail` nur Create-Fallback / `TransactionDetail`) |

**Bearbeiten** (Tap auf Zeile) bleibt Detail-Page.

---

## Soll-Verhalten: kontextsensitiver FAB

| Tab / Kontext | FAB öffnet |
|---------------|-----------|
| Dashboard | **Schnell** (kostenlos) |
| Transaktionen | Stack: Import · Umbuchen-Sheet · `+` Sheet |
| Verwaltung › Kategorien | Sheet **Neue Kategorie** |
| Verwaltung › Konten | Sheet **Neues Konto** |
| Sparziele | Sheet **Neues Sparziel** |
| Daueraufträge | Sheet **Neuer Dauerauftrag** |

Gemeinsam:

- Liste bleibt sichtbar / abgedunkelt („darunter“)
- Speichern → Sheet zu, Liste reload
- Abbrechen / Tap außerhalb → schließen ohne Speichern
- **Kein** Navigation-Push nur zum Anlegen

Mockup-Fehler: globaler `openSheet` → immer Schnellerfassung. Das ist **kein** Produktverhalten.

```
FAB
  ├─ Dashboard         → Schnell-Sheet (kostenlos)
  ├─ Transaktionen     → Import / Umbuchen-Sheet / + Sheet
  ├─ Verwaltung/Cats   → Neue Kategorie
  ├─ Verwaltung/Konten → Neues Konto
  ├─ Sparziele         → Neues Sparziel
  └─ Daueraufträge     → Neuer Dauerauftrag
```

---

## Sheet-Wireframes (Mockup-Lücke geschlossen)

Gleicher **Chrome** wie Schnellerfassung im Dummy: Grabber, Titel, scrollbarer Inhalt, primäre Save-Aktion unten. Felder = Create-Felder der heutigen Detail-Forms (ohne Edit-only wie Abgleich / Ausnahmen).

### Shared Chrome

```
┌─────────────────────────────────────┐
│ ░░░░░ Liste (gedimmt) ░░░░░░░░░░░░░ │
│ ░░░ ┌─────────────────────────┐ ░░░ │
│ ░░░ │  ──── grabber ────      │ ░░░ │
│ ░░░ │  Titel                  │ ░░░ │
│ ░░░ │  … Formular …           │ ░░░ │
│ ░░░ │  [Abbrechen] [Speichern]│ ░░░ │
│ ░░░ └─────────────────────────┘ ░░░ │
│                            [＋] FAB │
└─────────────────────────────────────┘
```

### Schnellerfassung (Buchungen) — im Mockup ausgearbeitet

- Segment: Ausgabe | Einnahme  
- Großer Betrag + Währungssymbol  
- Horizontale Kategorie-Chips  
- Ziffernblock (optional; App darf Entry nutzen solange UX klar bleibt)  
- CTA: „Buchung speichern“  

Technischer Anker heute: `QuickExpenseCaptureSheetService` (Modal-`ContentPage`, **kein** Toolkit-Popup).

### Neue Kategorie

- Name  
- Icon-Auswahl  
- Farbe  
- Typ (Ausgabe / Einnahme / …)  
- Budget (optional, wie Detail)  

Wiederverwenden: `CategoryFormView` + Create-Pfad von `CategoryDetailViewModel` (oder schlankes Create-VM).

### Neues Konto

- Name  
- Typ (`SelectionField`)  
- Anfangssaldo (+ Hinweis)  
- Stichtag Anfangssaldo (wenn im Create-Flow vorgesehen)  

**Nicht** im Create-Sheet: Archiviert, Ist-Saldo-Abgleich (Edit-only auf Detail).

### Neues Sparziel

- Titel  
- Icon  
- Zielbetrag  
- Aktueller Betrag (Start, oft 0)  
- Monatliche Sparrate (optional)  
- Fälligkeit (optional)  

### Neuer Dauerauftrag

- Ausgabe | Einnahme  
- Betrag, Titel  
- Kategorie, Konto  
- Startdatum, optional Enddatum, Aktiv  
- Intervall (`SelectionField`)  

**Nicht** im Create-Sheet: Ausnahme-/Shift-UI (Edit auf Detail).

Wiederverwenden: `RecurringTransactionFormView`.

---

## Technisches Leitbild (`create-pattern`)

### Do

1. **Ein stabiler Sheet-Host** nach Vorbild `QuickExpenseCaptureSheetService`:
   - `Shell.Current.Navigation.PushModalAsync` + `ContentPage` (oder gleichwertiges Modal ohne CommunityToolkit `Popup`)
   - `TaskCompletionSource` für Saved/Cancelled
   - Forminhalt austauschbar (View / BindingContext je Entity)
2. **Form-Views wiederverwenden** (`CategoryFormView`, `RecurringTransactionFormView`, …) statt paralleler XAML-Duplikate.
3. **FAB-Command tab-kontextsensitiv** in der jeweiligen List-Page / dem Coordinator — kein globaler „immer Schnell“-Handler.
4. Mac/iOS: `FormContent` / schwere Child-Trees **nicht** synchron während Parent-`InitializeComponent` setzen (Loaded/HandlerChanged-Deferral).

### Don’t

- CommunityToolkit **`Popup`** für neue Create-Flows (Crash-Historie iOS/Mac nach UIScene).
- **Inline-Panel** als Ziel-UX wiederherstellen (#265 Stufe A) — Soll ist Sheet, nicht Panel oben.
- `NavigationPage` als Sheet-Wrapper, wenn sie historisch mitgecrashst hat (Schnell-Kommentar).

### Empfohlene Baustein-Evolution (Implementierung später)

```
Finanzuebersicht/Services/
  CreateFormModalService.cs              — generischer Host (Modal ContentPage)
  *CreateSheetService.cs                 — Tab-spezifische Create-Flows
  QuickExpenseCaptureSheetService.cs

Finanzuebersicht/Controls/
  *FormView.xaml                         — Sheet + Detail (shared forms)
```

Entfernt (Aug 2026): Legacy `FormSheetPopup` (CommunityToolkit) und `CreateFormCard`.

### Reihenfolge der Umsetzung (Rest)

1. Schnell-Sheet UX an Mockup angleichen (Chrome/Keypad/Chips)  
2. #266 Rest: „Häufig verwendet“ / Template-Chips im Create-Flow  
3. Mockup-Chrome an übrigen Sheets (wo sinnvoll)  

---

## Historie #265 (Kurz)

| Phase | Commit (Juli 2026) | Inhalt |
|-------|-------------------|--------|
| 0–4 | `fa193cd` … `ef70f99` | Inline Konten/Sparziele; Toolkit-Sheet Kategorien/Daueraufträge |
| Revert | `8494ba6`, `9b2a7ee` (Aug) | Create wieder Detail-Pages (Stabilität nach UIScene/Widget) |

Doku und Rules, die Stufe A/B als **live** beschrieben haben, waren nach dem Revert **Drift** — dieser Stand ist die Korrektur.

---

## A11y

- Sheet: Dialog-Titel (`A11y_FormSheetDialog` oder Nachfolger)  
- Fokus auf erstes sinnvolles Feld beim Öffnen (`FormFocusHelper`)  
- Abbrechen/Speichern: `SemanticProperties.Hint`

## Verwandte Docs

- `docs/ROADMAP.md` — v1.20 / offene UX (#266 „Häufig verwendet“)  
- `.github/copilot-instructions.md` — Create UX Kurzreferenz  
- Mockup (lokal): Downloads `Finanzübersicht Mobile App Verfeinerung-2`
