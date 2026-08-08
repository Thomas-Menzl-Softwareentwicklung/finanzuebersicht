# Roadmap

Übersicht über geplante Releases und Features. Die Roadmap wird fortlaufend aktualisiert.

> **Hinweis:** Die Milestone-Bezeichnungen (v1.14, v1.2, v2.0) sind thematische GitHub-Planungslabels, keine sequenziellen Release-Versionen. Tatsächliche Releases (v1.0, v1.6, v1.12 …) werden durch Git-Commit-Höhe via Nerdbank.GitVersioning bestimmt.

**Aktueller Stand:** Release **v1.19** (Latest). Als Nächstes thematisch: **[v1.20 – Architektur-Fundament](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/milestone/24)** (ein Meilenstein, kein Zwischen-Release nötig). Danach Feature-Ideen (**Milestone 22**), dann **v2.0** (Verschlüsselung, echte Mehrwährung).

---

## ✅ v1.0 — Stable Release *(abgeschlossen)*

- Transaktionen, Kategorien, Daueraufträge
- Dashboard mit Charts (Monatsübersicht, Jahresverlauf)
- Budgetverwaltung & Sparziele
- Backup / Restore mit Schema-Migrations-Framework
- Accessibility (VoiceOver / Tastaturnavigation)
- CI/CD: Build-Artifacts für macOS & Windows

---

## ✅ v1.6 — Architektur & Datenrobustheit *(abgeschlossen)*

Fokus: Layering bereinigen, Persistenz robuster machen und DI modularisieren.

| Issue | Thema | Status |
|-------|-------|--------|
| [#152](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/152)–[#168](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/168) | Persistenz, Restore, Layering, DI, UseCases | ✅ Closed |

---

## ✅ v1.9 — UX-Schnellgewinne *(abgeschlossen)*

- Import-Vorschau mit Dubletten-Erkennung ([#192](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/192))
- Transaktionsvorlagen / Schnellbuchungen ([#191](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/191))
- Budget-Hinweise mit Tagesbudget ([#193](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/193))

---

## ✅ v1.10 — Multi-Account-Grundlage *(abgeschlossen)*

- Kontenmodell, Verwaltung, Filter, Umbuchungen ([#49](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/49))

---

## ✅ v1.11 — Planung & Sparziele *(abgeschlossen)*

| Issue | Thema |
|-------|-------|
| [#194](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/194) | Daueraufträge vom Dashboard buchen / überspringen / verschieben |
| [#195](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/195) | Sparziele mit Transaktionen verknüpfen |
| [#196](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/196) | Cashflow-Kalender (30 Tage) |
| [#206](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/206)–[#208](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/208) | Kontosaldo, Konto-Filter Prognose/Budget |

---

## ✅ v1.12 — Konten & Salden *(abgeschlossen)*

| Issue | Thema |
|-------|-------|
| [#212](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/212) | Anfangssaldo pro Konto |
| [#213](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/213) | Dashboard-Kontenübersicht mit Gesamtsaldo |

Weitere Umsetzungen: Umbuchungen, Transaktions-Suche/Filter/Swipe, Cashflow-Navigation, Docs & Screenshots.

---

## ✅ v1.13 — Mac Catalyst Picker *(abgeschlossen)*

Kleines Update: Mitigation für Mac-Catalyst-Picker-Freeze (`UpdateMode=WhenFinished`, `RecurrenceIntervalOption`, Handoff-Dokumentation). Branch: `fix/recurring-interval-picker`.

---

## ✅ v1.14 — Erste Schritte & Vertrauen *(abgeschlossen)* · [Milestone](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/milestone/18)

Fokus: Onboarding, einheitliche Empty States, Aktion-Feedback, Saldo-Vertrauen.

| Issue | Thema | Status |
|-------|-------|--------|
| [#227](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/227) | Onboarding für neue Nutzer | ✅ Closed |
| [#228](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/228) | Leere Zustände vereinheitlichen | ✅ Closed |
| [#229](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/229) | Feedback nach Speichern/Löschen (optional Rückgängig) | ✅ Closed |
| [#214](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/214) | Manueller Saldo-Abgleich (Ist vs. berechnet) | ✅ Closed |

---

## ✅ v1.15 — Sparziele & Planung *(abgeschlossen)* · [Milestone](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/milestone/19)

| Issue | Thema | Status |
|-------|-------|--------|
| [#230](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/230) | Sparziele bearbeiten, sicher löschen, Beitrag buchen | ✅ Closed |
| [#231](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/231) | Cashflow besser auffindbar | ✅ Closed |
| [#232](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/232) | Fällige Daueraufträge — kompaktere Dashboard-Aktionen | ✅ Closed |
| [#233](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/233) | Dashboard Informationshierarchie entschlacken | ✅ Closed |

---

## ✅ v1.16 — UI-Konsistenz & Lokalisierung *(abgeschlossen)* · [Milestone](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/milestone/20)

| Issue | Thema | Status |
|-------|-------|--------|
| [#252](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/252) | Dashboard Runde 2 — weniger Karten, klarer erster Blick | ✅ Closed |
| [#253](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/253) | Anzeige-Währung beim Erststart (getrennt von Sprache) | ✅ Closed |
| [#254](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/254) | Hardcoded XAML-Farben → zentrale Theme-Ressourcen | ✅ Closed |
| [#234](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/234) | Seitentitel und Enum-Labels lokalisieren | ✅ Closed |
| [#236](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/236) | Verwaltung Kategorien/Konten — Segment klarer | ✅ Closed |
| [#238](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/238) | Filter und Umbuchung mit Text statt Emoji | ✅ Closed |
| [#240](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/240) | Einheitliches Icon-Set statt Emoji | ✅ Closed |

---

## ✅ v1.17 — Barrierefreiheit & Mac-Formulare *(abgeschlossen)* · [Milestone](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/milestone/21)

| Issue | Thema | Status |
|-------|-------|--------|
| [#235](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/235) | Charts mit Text-Zusammenfassung (Screenreader) | ✅ Closed |
| [#237](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/237) | Listenzeilen für VoiceOver beschriften | ✅ Closed |
| [#239](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/239) | Mac Catalyst: SelectionField in Scroll-Formularen | ✅ Closed |

Weitere Umsetzungen in v1.17: Live-Währungsrefresh (`CurrencyRefreshRegistry`), Dashboard-Kacheln (Monat/Jahr), Verwaltung Kategorien/Konten im Sparziele-Kartenstil.

---

## ✅ v1.18 — Dashboard UX *(abgeschlossen)*

- Zwei-Zonen-Layout: Hero-Saldo, KPIs, eine Analytics-Karte für Monat/Jahr
- Budget-Balken, Donut mit Kategorie-Legende (Betrag + Prozent)
- Fällige Daueraufträge als schlanker, aufklappbarer Hinweis mit Schnellaktionen
- Optionale kompakte Insight-Zeilen (umschaltbar)
- Mac Catalyst: Einstellungen-Stabilität, App-Lifecycle, Release-Icons

---

## ✅ v1.19 — Einheitliches Anlegen *(Release abgeschlossen; Rest-Issue offen)* · [Milestone](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/milestone/23)

- Inline-Anlegen für Konten und Sparziele (`CreateFormCard`, Scroll-to-top)
- Sheet-Anlegen für Kategorien und Daueraufträge (`FormSheetPopup`)
- Bearbeiten bleibt auf den jeweiligen Detailseiten
- Architektur: Recurring-Schedule konsolidiert, Repository-Reads in Use Cases (#275)
- Fixes: Systemkonto-Löschen (#277), Shift-Ausnahmen, Test-Stabilität (#282)
- Magic Strings zentralisiert (#272 / PR #285)

| Issue | Thema | Status |
|-------|-------|--------|
| [#266](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/266) | Transaktionen & Umbuchen per Sheet + „Häufig verwendet“ | Offen (UX, getrennt von Architektur) |

---

## 🏗️ v1.20 — Architektur-Fundament *(aktiv)* · [Milestone](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/milestone/24)

Ein großer thematischer Meilenstein **vor** Feature-Backlog und v2.0. Kein Zwischen-Release nötig — Reihenfolge unten abarbeiten. Architektur-Arbeit bewusst vor Mehrwährung/Verschlüsselung (v2.0).

Setup-Hilfe (Milestone/Labels): `scripts/setup-v120-architecture-milestone.sh` (einmalig mit Issue-Schreibrechten; #272 bereits geschlossen).

### Empfohlene Reihenfolge

Abgeschlossen: Welle 0 (#289–#291), Welle 1 (#268, #270), Vollscan (#273), MAUI #297–#299.

Offene Issues in Abarbeitungsreihenfolge (#274 vor Import/Backup, damit neue Use Cases die Fehlerkonvention gleich mitnehmen):

| Nr. | Issue | Fokus | Stand |
|-----|--------|--------|-------|
| 1 | [#274](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/274) | Einheitliches Fehler- & Result-Modell | ✅ |
| 2 | [#269](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/269) | Import in Application-Layer (Use Cases) | ✅ |
| 3 | [#292](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/292) | Backup in Application-Layer (Use Cases) | ✅ |
| 4 | [#293](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/293) | TransactionsViewModel aufteilen (nach #269) | ✅ |
| 5 | [#267](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/267) | DashboardViewModel aufteilen | ✅ |
| 6 | [#294](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/294) | CategoriesViewModel aufteilen | ✅ |
| 7 | [#271](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/271) | App-Static-Events → `IAppEvents` | in Arbeit (`feature/271-app-events`) |
| 8 | [#295](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/295) | Shell-Tab-Routes / ID-Navigation härten | offen |
| 9 | [#296](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/296) | Listen auf CollectionView virtualisieren | offen |
| 10 | [#300](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/300) | Sync-Persistenz-Prep (`ExternalId`/`Source`) | offen |

Parallel möglich: #267 neben #269/#292; #294 neben #271 — #293 erst nach #269.

---

## 💡 Weiterer Backlog — Milestone 22 *(nach v1.20, vor v2)* · [Milestone](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/milestone/22)

Größere Produkt-Features. Sync/Open Banking setzen idealerweise #300 (und stabile Import-Grenze #269) voraus.

| Issue | Thema | Aufwand |
|-------|-------|---------|
| [#241](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/241) | Kategorien-Hierarchie (Ober-/Unterkategorien) | L |
| [#242](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/242) | Home-Screen-Widget Anzeige (iOS / macOS) | L |
| — | Interaktives iOS-Widget + In-App Schnellerfassung Ausgaben (Pro) | ✅ (iOS-Widget + Inbox; Mac/Windows nur In-App „Schnell“) |
| [#244](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/244) | Daueraufträge mit variablem Betrag | M |
| [#243](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/243) | CloudKit-Sync zwischen Geräten | XL |
| [#245](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/245) | Open Banking / automatischer Bank-Import | XL |
| [#258](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/258) | Dashboard-Kacheln individuell anordnen (Idee) | M |

---

## 🔐 v2.0 — Sicherheit & erweiterte Finanzen *(geplant, nach v1.20)* · [Milestone](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/milestone/17)

Größere Fach-Features **nach** dem Architektur-Fundament. (Anzeige-Währung gibt es bereits; hier geht es um echte Ledger-Mehrwährung.)

| Issue | Thema | Aufwand |
|-------|-------|---------|
| [#53](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/53) | Optionale lokale Verschlüsselung (passwortbasiert) | L |
| [#197](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/197) | Mehrwährung mit historischen Wechselkursen | XL |
| [#64](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/64) | CSV/PDF-Export für Steuerzwecke | M |

---

*Versionsschema: [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) – Patch-Version = Git-Commit-Höhe*
