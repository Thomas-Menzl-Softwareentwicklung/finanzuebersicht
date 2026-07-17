# Roadmap

Übersicht über geplante Releases und Features. Die Roadmap wird fortlaufend aktualisiert.

> **Hinweis:** Die Milestone-Bezeichnungen (v1.14, v1.2, v2.0) sind thematische GitHub-Planungslabels, keine sequenziellen Release-Versionen. Tatsächliche Releases (v1.0, v1.6, v1.12 …) werden durch Git-Commit-Höhe via Nerdbank.GitVersioning bestimmt.

**Aktueller Stand:** Release **v1.19** (Latest). Nächster thematischer Backlog: **Milestone 22** (vor v2.0).

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
| [#152](https://github.com/tom4711/finanzuebersicht/issues/152)–[#168](https://github.com/tom4711/finanzuebersicht/issues/168) | Persistenz, Restore, Layering, DI, UseCases | ✅ Closed |

---

## ✅ v1.9 — UX-Schnellgewinne *(abgeschlossen)*

- Import-Vorschau mit Dubletten-Erkennung ([#192](https://github.com/tom4711/finanzuebersicht/issues/192))
- Transaktionsvorlagen / Schnellbuchungen ([#191](https://github.com/tom4711/finanzuebersicht/issues/191))
- Budget-Hinweise mit Tagesbudget ([#193](https://github.com/tom4711/finanzuebersicht/issues/193))

---

## ✅ v1.10 — Multi-Account-Grundlage *(abgeschlossen)*

- Kontenmodell, Verwaltung, Filter, Umbuchungen ([#49](https://github.com/tom4711/finanzuebersicht/issues/49))

---

## ✅ v1.11 — Planung & Sparziele *(abgeschlossen)*

| Issue | Thema |
|-------|-------|
| [#194](https://github.com/tom4711/finanzuebersicht/issues/194) | Daueraufträge vom Dashboard buchen / überspringen / verschieben |
| [#195](https://github.com/tom4711/finanzuebersicht/issues/195) | Sparziele mit Transaktionen verknüpfen |
| [#196](https://github.com/tom4711/finanzuebersicht/issues/196) | Cashflow-Kalender (30 Tage) |
| [#206](https://github.com/tom4711/finanzuebersicht/issues/206)–[#208](https://github.com/tom4711/finanzuebersicht/issues/208) | Kontosaldo, Konto-Filter Prognose/Budget |

---

## ✅ v1.12 — Konten & Salden *(abgeschlossen)*

| Issue | Thema |
|-------|-------|
| [#212](https://github.com/tom4711/finanzuebersicht/issues/212) | Anfangssaldo pro Konto |
| [#213](https://github.com/tom4711/finanzuebersicht/issues/213) | Dashboard-Kontenübersicht mit Gesamtsaldo |

Weitere Umsetzungen: Umbuchungen, Transaktions-Suche/Filter/Swipe, Cashflow-Navigation, Docs & Screenshots.

---

## ✅ v1.13 — Mac Catalyst Picker *(abgeschlossen)*

Kleines Update: Mitigation für Mac-Catalyst-Picker-Freeze (`UpdateMode=WhenFinished`, `RecurrenceIntervalOption`, Handoff-Dokumentation). Branch: `fix/recurring-interval-picker`.

---

## ✅ v1.14 — Erste Schritte & Vertrauen *(abgeschlossen)* · [Milestone](https://github.com/tom4711/finanzuebersicht/milestone/18)

Fokus: Onboarding, einheitliche Empty States, Aktion-Feedback, Saldo-Vertrauen.

| Issue | Thema | Status |
|-------|-------|--------|
| [#227](https://github.com/tom4711/finanzuebersicht/issues/227) | Onboarding für neue Nutzer | ✅ Closed |
| [#228](https://github.com/tom4711/finanzuebersicht/issues/228) | Leere Zustände vereinheitlichen | ✅ Closed |
| [#229](https://github.com/tom4711/finanzuebersicht/issues/229) | Feedback nach Speichern/Löschen (optional Rückgängig) | ✅ Closed |
| [#214](https://github.com/tom4711/finanzuebersicht/issues/214) | Manueller Saldo-Abgleich (Ist vs. berechnet) | ✅ Closed |

---

## ✅ v1.15 — Sparziele & Planung *(abgeschlossen)* · [Milestone](https://github.com/tom4711/finanzuebersicht/milestone/19)

| Issue | Thema | Status |
|-------|-------|--------|
| [#230](https://github.com/tom4711/finanzuebersicht/issues/230) | Sparziele bearbeiten, sicher löschen, Beitrag buchen | ✅ Closed |
| [#231](https://github.com/tom4711/finanzuebersicht/issues/231) | Cashflow besser auffindbar | ✅ Closed |
| [#232](https://github.com/tom4711/finanzuebersicht/issues/232) | Fällige Daueraufträge — kompaktere Dashboard-Aktionen | ✅ Closed |
| [#233](https://github.com/tom4711/finanzuebersicht/issues/233) | Dashboard Informationshierarchie entschlacken | ✅ Closed |

---

## ✅ v1.16 — UI-Konsistenz & Lokalisierung *(abgeschlossen)* · [Milestone](https://github.com/tom4711/finanzuebersicht/milestone/20)

| Issue | Thema | Status |
|-------|-------|--------|
| [#252](https://github.com/tom4711/finanzuebersicht/issues/252) | Dashboard Runde 2 — weniger Karten, klarer erster Blick | ✅ Closed |
| [#253](https://github.com/tom4711/finanzuebersicht/issues/253) | Anzeige-Währung beim Erststart (getrennt von Sprache) | ✅ Closed |
| [#254](https://github.com/tom4711/finanzuebersicht/issues/254) | Hardcoded XAML-Farben → zentrale Theme-Ressourcen | ✅ Closed |
| [#234](https://github.com/tom4711/finanzuebersicht/issues/234) | Seitentitel und Enum-Labels lokalisieren | ✅ Closed |
| [#236](https://github.com/tom4711/finanzuebersicht/issues/236) | Verwaltung Kategorien/Konten — Segment klarer | ✅ Closed |
| [#238](https://github.com/tom4711/finanzuebersicht/issues/238) | Filter und Umbuchung mit Text statt Emoji | ✅ Closed |
| [#240](https://github.com/tom4711/finanzuebersicht/issues/240) | Einheitliches Icon-Set statt Emoji | ✅ Closed |

---

## ✅ v1.17 — Barrierefreiheit & Mac-Formulare *(abgeschlossen)* · [Milestone](https://github.com/tom4711/finanzuebersicht/milestone/21)

| Issue | Thema | Status |
|-------|-------|--------|
| [#235](https://github.com/tom4711/finanzuebersicht/issues/235) | Charts mit Text-Zusammenfassung (Screenreader) | ✅ Closed |
| [#237](https://github.com/tom4711/finanzuebersicht/issues/237) | Listenzeilen für VoiceOver beschriften | ✅ Closed |
| [#239](https://github.com/tom4711/finanzuebersicht/issues/239) | Mac Catalyst: SelectionField in Scroll-Formularen | ✅ Closed |

Weitere Umsetzungen in v1.17: Live-Währungsrefresh (`CurrencyRefreshRegistry`), Dashboard-Kacheln (Monat/Jahr), Verwaltung Kategorien/Konten im Sparziele-Kartenstil.

---

## ✅ v1.18 — Dashboard UX *(abgeschlossen)*

- Zwei-Zonen-Layout: Hero-Saldo, KPIs, eine Analytics-Karte für Monat/Jahr
- Budget-Balken, Donut mit Kategorie-Legende (Betrag + Prozent)
- Fällige Daueraufträge als schlanker, aufklappbarer Hinweis mit Schnellaktionen
- Optionale kompakte Insight-Zeilen (umschaltbar)
- Mac Catalyst: Einstellungen-Stabilität, App-Lifecycle, Release-Icons

---

## ✅ v1.19 — Einheitliches Anlegen *(abgeschlossen)*

- Inline-Anlegen für Konten und Sparziele (`CreateFormCard`, Scroll-to-top)
- Sheet-Anlegen für Kategorien und Daueraufträge (`FormSheetPopup`)
- Bearbeiten bleibt auf den jeweiligen Detailseiten
- Architektur: Recurring-Schedule konsolidiert, Repository-Reads in Use Cases (#275)
- Fixes: Systemkonto-Löschen (#277), Shift-Ausnahmen, Test-Stabilität (#282)

---

## 💡 Weiterer Backlog — Milestone 22 *(vor v2)* · [Milestone](https://github.com/tom4711/finanzuebersicht/milestone/22)

Größere Features — Priorisierung nach v1.19.

| Issue | Thema | Aufwand |
|-------|-------|---------|
| [#241](https://github.com/tom4711/finanzuebersicht/issues/241) | Kategorien-Hierarchie (Ober-/Unterkategorien) | L |
| [#242](https://github.com/tom4711/finanzuebersicht/issues/242) | Home-Screen-Widget (iOS / macOS) | L |
| [#244](https://github.com/tom4711/finanzuebersicht/issues/244) | Daueraufträge mit variablem Betrag | M |
| [#243](https://github.com/tom4711/finanzuebersicht/issues/243) | CloudKit-Sync zwischen Geräten | XL |
| [#245](https://github.com/tom4711/finanzuebersicht/issues/245) | Open Banking / automatischer Bank-Import | XL |
| [#258](https://github.com/tom4711/finanzuebersicht/issues/258) | Dashboard-Kacheln individuell anordnen (Idee) | M |

---

## 🔧 v1.1 — Qualität & Performance *(Backlog)* · [Milestone](https://github.com/tom4711/finanzuebersicht/milestone/5)

Technische Schulden — parallel zu UX-Releases möglich.

| Issue | Thema | Aufwand |
|-------|-------|---------|
| [#101](https://github.com/tom4711/finanzuebersicht/issues/101) | Store-Basisklasse (`JsonStore<T>`) | M |
| [#102](https://github.com/tom4711/finanzuebersicht/issues/102) | `YearOverviewViewModel`: ObservableCollection | S |
| [#103](https://github.com/tom4711/finanzuebersicht/issues/103) | Namespace-Bereinigung | M |
| [#100](https://github.com/tom4711/finanzuebersicht/issues/100) | Bulk-Replace API für Restore | M |

---

## 🔐 v2.0 — Sicherheit & erweiterte Finanzen *(geplant)* · [Milestone](https://github.com/tom4711/finanzuebersicht/milestone/17)

Größere Architekturänderungen — nach v1.14–v1.19.

| Issue | Thema | Aufwand |
|-------|-------|---------|
| [#53](https://github.com/tom4711/finanzuebersicht/issues/53) | Optionale lokale Verschlüsselung (passwortbasiert) | L |
| [#197](https://github.com/tom4711/finanzuebersicht/issues/197) | Mehrwährung mit historischen Wechselkursen | XL |
| [#64](https://github.com/tom4711/finanzuebersicht/issues/64) | CSV/PDF-Export für Steuerzwecke | M |

---

*Versionsschema: [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning) – Patch-Version = Git-Commit-Höhe*
