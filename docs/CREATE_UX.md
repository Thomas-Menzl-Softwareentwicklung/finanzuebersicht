# Einheitliches Anlegen (Create UX)

Issue **#265** — Übersicht der Anlege-Patterns ab v1.19.

## Stufen

| Stufe | Pattern | Komponente | Entitäten |
|-------|---------|------------|-----------|
| A | Inline-Panel oben | `CreateFormCard` | Konten, Sparziele |
| B | Scroll-Sheet (Modal) | `FormSheetPopup` + Form-View | Kategorien, Daueraufträge |
| C | Detail-Seite | bestehende `*DetailPage` | Bearbeiten aller Entitäten |

## Verhalten

- **FAB / Empty State** auf Listen-Seiten → kein Navigation-Push zum Anlegen
- Nach **Speichern** schließt Panel/Sheet; Liste aktualisiert sich über Use-Case + Reload
- **Bearbeiten** per Tap auf Karte → Detail-Seite (inkl. erweiterter Felder, z. B. Ausnahmen bei Daueraufträgen)

## UI-Bausteine

```
Finanzuebersicht/Controls/
  CreateFormCard.xaml       — Stufe A (Primary-Rand, Abbrechen/Speichern)
  CategoryFormView.xaml     — Kategorie-Felder (Sheet + Detail)
  RecurringTransactionFormView.xaml

Finanzuebersicht/Views/Popups/
  FormSheetPopup.cs       — Stufe B (~70 % Fensterhöhe, scrollbar)

Finanzuebersicht/Services/
  CategoryCreateSheetService.cs
  RecurringTransactionCreateSheetService.cs
```

## Layout (Stufe A — Konten)

```
┌─────────────────────────────────────┐
│  Verwaltung › Konten                │
├─────────────────────────────────────┤
│ [ Kategorien ] [ Konten ]           │
├─────────────────────────────────────┤
│ ┌─ Neues Konto ─────────────────┐   │  ← CreateFormCard oben
│ │ Name, Typ, Anfangssaldo        │   │
│ │ [Abbrechen]  [Hinzufügen]      │   │
│ └───────────────────────────────┘   │
│ ┌─ Giro ─────────────── 1.234 € ┐   │
│ └─ Sparkonto ────────── 5.600 € ┘   │
│                            [＋] FAB │
└─────────────────────────────────────┘
```

## Layout (Stufe B — Kategorien / Daueraufträge)

```
┌─────────────────────────────────────┐
│ ░░░ Liste (gedimmt) ░░░░░░░░░░░░░░░ │
│ ░░░ ┌─ Neuer Eintrag ───────────┐ ░ │
│ ░░░ │ Formular (scrollbar)      │ ░ │
│ ░░░ │ [Abbrechen] [Speichern]   │ ░ │
│ ░░░ └───────────────────────────┘ ░ │
│                            [＋] FAB │
└─────────────────────────────────────┘
```

## A11y & Fokus (Phase 5)

- `A11y_CreateFormPanel` — Screenreader-Beschreibung für Inline-`CreateFormCard`
- `A11y_FormSheetDialog` — Sheet-Dialog mit Titel (`Dialog: {0}`)
- `FormFocusHelper` — setzt Fokus auf erstes Textfeld beim Öffnen (Panel + Sheet)
- Abbrechen/Speichern-Buttons: `SemanticProperties.Hint` = Button-Text

## Screenshots

Manuelle Screenshots für Release-Notizen (Mac Catalyst, kleines Fenster empfohlen):

1. Verwaltung → Konten → Inline-Panel geöffnet
2. Verwaltung → Kategorien → Sheet geöffnet
3. Daueraufträge → Sheet geöffnet
4. Sparziele → Panel oben

Speicherort-Vorschlag: `docs/images/create-ux/` (optional, nicht versioniert)
