# Transaktionen-Liste: Mockup-Design-Angleichung

**Datum:** 2026-08-08  
**Status:** Approved (Chat)  
**Referenz:** Klickdummy `Finanzübersicht Mobile App Verfeinerung-2` (Tab `isTx`)  
**Scope:** Optik der Transaktionsliste + Monats-Reiter; **kein** Layout-Umbau außer dem Reiter; **kein** Create-UX / #266

---

## Ziel

Die `TransactionsPage` optisch näher an den Mockup-Tab „Buchungen/Transaktionen“ bringen:

1. **Monats-Reiter** als Chip-Leiste (Layout-Ausnahme)
2. **Farbliche Absetzung** der Liste (Tageskarten, farbige Kategorie-Chips, Header-Typo)

Alles andere (Suche, Filter-Panel, „Häufig verwendet“, Unkategorisiert-Hinweis, FABs, Swipe-Aktionen) bleibt in Position und Verhalten; höchstens Token-Farben, keine Umstrukturierung.

---

## Nicht-Ziele

- Create-Sheet / #266 / FAB-Verhalten ändern
- Globale Theme-Farben neu definieren (bestehende `Colors.xaml`-Tokens nutzen; höchstens ein Primary-Tint ergänzen)
- Light-Mode strukturell anders als Dark
- Neuer Filter-Stack jenseits des bestehenden Suchpfads

---

## 1. Monats-Reiter (Chip-Leiste)

### Ist

`‹` / `MonatAnzeige` / `›` (`PreviousMonthCommand` / `NextMonthCommand` aus `MonthNavigationViewModel`).

### Soll — Chip-Reihenfolge

Mockup zeigt drei Chips ohne Vorwärts. Für Parität mit den Pfeilen:

**`[Vormonat] [Aktuell] [Nächster] [Gesamt]`**

| Chip | Label | Aktion | Aktiv-Zustand |
|------|-------|--------|---------------|
| Vormonat | Kurzname (z. B. „Juli“) | `PreviousMonth` | Card-Pill |
| Aktuell | Langform „MMMM yyyy“ | no-op | Accent-Pill im Monatsmodus |
| Nächster | Kurzname | `NextMonth` | Card-Pill |
| Gesamt | lokalisiert | Gesamt-Modus | Accent-Pill im Gesamt-Modus |

Leichte Abweichung vom Screenshot (zusätzlicher Nächster-Chip) ist beabsichtigt.

Bei wenig Breite: horizontal scrollbar. Accent = Primary-Tint-Hintergrund + Primary-Text; inaktiv = `CardBackground*` + Secondary-Text.

### Gesamt-Chip

- Tip → **Gesamt-Modus**: bestehender Such-/Listenpfad ohne Monatseinschränkung.
- Flag `IsGesamtMode` macht `IsSearchActive` auch bei leerem `SearchText` und ohne Filter wahr.
- Laden über `SearchTransactionsUseCase` mit leerer Query (unbounded Datum → alle Buchungen).
- UI: `IsMonthMode == false`, Suchergebnis-Liste sichtbar; Suchleiste bleibt wie bisher.
- Verlassen: `ClearSearch` / Filter zurücksetzen / Tip auf einen Monats-Chip → `IsGesamtMode = false`, Monatsmodus.

### A11y

Descriptions analog `A11y_VorherigerMonat` / Monatsname; neue Keys für Nächster Monat und Gesamt.

---

## 2. Liste — farbliche Absetzung

### Gruppen-Header

Uppercase, ~12px, bold, tertiary-Farbe (`TextTertiary` / Gray). Inhalt weiter `DatumFormatiert` (Uppercase via `TextTransform` oder Converter).

### Tages-Karte

Pro Tagesgruppe: Einträge in einem `Border` mit CornerRadius 16–18, `CardBackground*`, transparenter Stroke. Horizontal ~16 Padding, damit Karten nicht edge-to-edge kleben.

### Transaktionszeile

- Icon 38–40px, CornerRadius ~11; **Hintergrund = Kategorie-Farbe** (nicht Gray).
- `ColorMap` (`Dictionary<string,string>` Hex) analog `IconMap` / `CategoryNameMap` in Month- und Search-Results + VM.
- Converter `KategorieIdToColorConverter` (Id + ColorMap); Fallback `#8E8E93` bei fehlender Kategorie.
- Titel ~14.5–16, bold/semibold.
- Unterzeile einheitlich **Kategoriename** (`CategoryNameMap`); Verwendungszweck und Konto-Zeile in den Listenzeilen entfallen zugunsten der Mockup-Hierarchie.
- Betrag: bestehende `BetragDisplay` + `TypToColor`.
- Swipe Duplizieren/Löschen unverändert.

### Suche / Gesamt-Liste

Gleiche Zeilen- und Kartenoptik. Leere Treffer: bestehende Empty-/„keine Suchergebnisse“-Views.

### Layout unverändert (nur Optik der Zeilen/Karten)

Suchleiste, Filter-Button, Filter-Panel, „Häufig verwendet“, Unkategorisiert-Badge, FAB-Stack.

---

## 3. Technik

| Bereich | Änderung |
|---------|----------|
| `TransactionsPage.xaml` | Chip-Reiter; GroupHeader; Karten; Row-Templates |
| `TransactionsViewModel` | Chip-Labels/Commands; `IsGesamtMode`; `ColorMap` |
| `LoadTransactionsMonthUseCase` / `SearchTransactionsUseCase` (+ Results) | `ColorMap` aus `Category.Color` |
| Converter | `KategorieIdToColorConverter`; Kategorie-Name-Binding |
| Ressourcen DE/EN | Gesamt, A11y |
| Tests | Gesamt ↔ Monat; Chip Previous/Next; ColorMap in Use-Case-Tests falls vorhanden |

Keine Domain-Modell-Änderungen außer Nutzung von `Category.Color`. Keine Navigation-/Create-Sheet-/Widget-Änderungen.

**Tokens:** Primary-Tint als Resource (`PrimaryTint` / `PrimaryTintDark`) falls noch nicht vorhanden; sonst Cards/Betrag über bestehende Keys.

---

## 4. Tests / Abnahme

- Unit: Gesamt setzt Modus + Search; Clear/Monats-Chip verlässt Gesamt; Previous/Next über Chips ändert `AktuellerMonat`.
- Manuell Dark+Light: Chip-Zustände, Kartenabstand, farbige Icons, Swipe, Suche/Filter erreichbar.

---

## Entscheidungen (Chat)

| Thema | Wahl |
|-------|------|
| Monats-UI | A — Chip-Leiste |
| Gesamt | B — Suchpfad (alle Buchungen via Search-Use-Case) |
| Optik-Umfang | Ansatz 1 — Reiter + Listen-Absetzung |

---

## Plan-Phase (klein)

- Primary-Tint als Color-Resource anlegen vs. bestehende Nähe-Tokens wiederverwenden.
- Grouped `CollectionView`: Karte um die Gruppe vs. Karte pro Item — Prefer Header + Items in gemeinsamer Card-Optik ohne Nested-CollectionView (MAUI-Stabilität); konkrete XAML-Struktur im Implementierungsplan.
