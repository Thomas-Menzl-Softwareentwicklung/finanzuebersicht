# Issue-Entwurf: Interaktives iOS-Widget — Schnellerfassung Ausgaben (Pro)

> Zum Anlegen in GitHub kopieren (Titel + Body). Branch/PR darf dieses Doc behalten oder nach Issue-Erstellung durch Link ersetzen.
> Vorgeschlagenes Milestone: **Ideen & Langfrist (Backlog)** / Milestone 22 (nach v1.20).
> Labels: `enhancement`, optional `ios`, `pro`

---

## Titel

```
Idee: Interaktives iOS-Widget — Ausgaben schnell erfassen (Pro)
```

## Body

```markdown
## Kontext / Motivation

Tester-Feedback (iPhone): kleine Ausgaben schnell festhalten, ohne tief in die App zu navigieren.
Nur **Betrag + kurze Info** jetzt; **Kategorie und Konto später** in der App nachziehen.

Das ist **nicht** dasselbe wie [#242](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/242) (Anzeige-Widget: Saldo / Monatskennzahlen). #242 bleibt Lesen; dieses Issue ist **Schreiben**.

Monetarisierung ([docs/MONETIZATION.md](../docs/MONETIZATION.md)): Convenience-Widget = **Pro** (Einmalkauf, StoreKit auf `develop` bereits vorhanden).

## Produktversprechen

Vom Home Screen (iOS 17+): Betrag + Info → Speichern → fertig.  
Ergebnis: echte `Transaction` (Ausgabe), sichtbar in Saldo/Dashboard, Kategorie = System **`Unkategorisiert`**, Konto = **Default-Konto**.

Später in der App: Transaktion öffnen → Kategorie und Konto setzen (Konto-Wechsel auf der Detailseite ist **bereits möglich**).

## Abgrenzung

| | Dieses Issue | [#242](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/242) |
|--|--------------|--------|
| Ziel | Schnellerfassung | Kennzahlen auf einen Blick |
| Richtung | App Intent schreibt Buchung | Snapshot lesen |
| Plattform-Fokus | iPhone (WidgetKit + App Intents) | iOS + macOS Anzeige |
| Monetisierung | **Pro** | Pro (Anzeige; optional Free-Teaser später) |

**Keine** separate Draft-/Inbox-Entity. Keine leere `KategorieId` für Normalbuchungen — Pattern wie CSV-Import: Systemkategorie `SysCat_Unkategorisiert`.

## Domain / App (Voraussetzungen)

1. Capture-Pfad speichert normale Ausgabe:
   - `Typ = Ausgabe`
   - `KategorieId` → Systemkategorie Unkategorisiert (anlegen falls fehlend, wie Import)
   - `AccountId` → Default-Konto (`SystemAccountKeys.Default`) bzw. erstes aktives
   - `Titel` = Info, `Betrag`, `Datum` = heute (lokal)
2. Validation für diesen Pfad: Betrag + Titel Pflicht; Kategorie kommt vom System, nicht vom Nutzer
3. UI „Nachziehen“ (MVP reicht Filter/Hinweis):
   - Transaktionsliste: Filter oder Badge „Unkategorisiert“
   - Optional: Badge auf Tab Transaktionen mit Anzahl
4. Detailseite: Kategorie + Konto nachträglich setzen (Konto-UI existiert)

Optional (Phase 1b, nicht MVP-Blocker): In-App-Sheet „Schnell erfassen“ (Betrag + Info) — gleiche Use Case wie Widget, ohne Extension; nützlich für Tests und Mac.

## Widget (iOS)

- Native **WidgetKit**-Extension (Swift/SwiftUI), gebündelt in der MAUI-iOS-App
- **Interaktiv** via **App Intents** (iOS 17+): Betrag + Info, Aktion Speichern
- Datenweg:
  1. Intent schreibt Capture in **App Group** (Queue/JSON), **oder** öffnet die App mit Deep Link und übergibt Payload
  2. MAUI-App (Foreground / kurzer Wake) führt bestehenden Save-Use-Case aus und invalidiert Timelines
- Bevorzugt langfristig: Shared Container + App öffnet kurz / Background-fähig nur soweit Apple erlaubt; Saldo-Konsistenz über Use Cases in .NET, **nicht** Voll-JSON in Swift neu berechnen
- Deep Link zum Bearbeiten einer gerade erfassten Buchung (optional)

### Pro-Gate

- Neues `AppFeature` z. B. `QuickExpenseWidget` (Name final im PR)
- `ILicenseService.HasFeature` / `EnsureFeature` — analog Cashflow/CSV
- Free: Widget kann sichtbar sein, Speichern → Upsell (Einstellungen/Lizenz) oder Intent bricht mit Hinweis ab
- Direct-Builds: immer Pro (bestehende Regel)

Voraussetzung: Apple Developer Program + App Group Entitlement (Team vorhanden).

## Phasen

| Phase | Inhalt |
|-------|--------|
| **0 – Spike** | Swift-Widget + App Intent Mock; App Group Roundtrip |
| **1 – Domain** | Capture-Use-Case → Transaction + Unkategorisiert + Default-Konto; Filter/Badge |
| **2 – Widget MVP** | Interaktives Widget speichert über Bridge; Pro-Gate |
| **3 – Polish** | DE/EN Strings, Fehlerfälle (kein Konto, Offline), optional In-App-Sheet |
| **4 – Optional** | Lock-Screen / kleineres Widget; Siri Shortcut „Ausgabe erfassen“ |

## Akzeptanzkriterien

- [ ] Vom Home-Screen-Widget Betrag + Info speichern erzeugt eine **Ausgabe**-Transaktion
- [ ] Kategorie ist System **Unkategorisiert**; Konto ist Default-Konto
- [ ] Saldo / Monatsausgaben berücksichtigen die Buchung sofort
- [ ] In der App: Kategorie und Konto nachträglich änderbar (Detailseite)
- [ ] Liste/Filter macht Unkategorisiert-Buchungen auffindbar
- [ ] Speichern aus dem Widget nur mit **Pro** (Store); Direct unrestricted
- [ ] Ohne Pro: klarer Upsell-Pfad, keine stille Fehlbuchung
- [ ] #242 bleibt separat (keine Pflicht, Anzeige-Widgets mitzuliefern)
- [ ] DE/EN lokalisiert (Widget + App-Hinweise)
- [ ] Tests für Capture-Use-Case (Betrag/Titel, Unkategorisiert, Default-Konto)

## Nicht-Ziele (MVP)

- Kategorie oder Konto-Auswahl **im** Widget
- Separate Draft-Entity / „Umwandeln in Transaktion“
- Android-Widget
- Vollständige Bearbeitung / Löschen im Widget
- macOS Desktop-Widget für Capture (kann später; Fokus iPhone)

## Aufwand

**L–XL** — native Extension + App Group/Signing + Capture-Use-Case + Pro-Feature-Flag + UX Nachziehen.

Abhängigkeiten: Pro/Licensing auf `develop` ✅; App Groups + Widget-Signing (CI/Mac); ideal nach oder parallel zu #242-Spike nur wo Infrastruktur geteilt wird (App Group Container).

## Referenzen

- [#242](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/242) — Anzeige-Widget
- [docs/MONETIZATION.md](MONETIZATION.md) — Pro-Features
- `SystemCategoryKeys.Unkategorisiert`, Import-Fallback
- `SaveTransactionDetailUseCase` — Konto nachträglich speicherbar
```
