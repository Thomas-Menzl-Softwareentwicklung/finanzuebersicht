# Änderungsverlauf

## [1.19] - 2026-07-17

### Hinzugefügt

- Einheitliches Anlegen (#265): Inline-Panel (`CreateFormCard`) für Konten und Sparziele
- Sheet-Anlegen (`FormSheetPopup`) für Kategorien und Daueraufträge
- Doku: `docs/CREATE_UX.md`

### Geändert

- Recurring-Schedule-Logik konsolidiert; Repository-Zugriffe in Use Cases (#275)
- Microsoft.Maui.Controls 10.0.80, Nerdbank.GitVersioning, Test-SDK
- Create-UX: Anlegen nach UIScene-Stabilisierung wieder über Detail-Pages; Soll (kontextsensitiver FAB + Bottom-Sheet) in `docs/CREATE_UX.md`

### Behoben

- Systemkonten: Löschen-Aktion ausgeblendet, Zeilen weiter bearbeitbar (#277)
- Dauerauftrag-`OccursOnDate` berücksichtigt Shift-Ausnahmen korrekt
- NSubstitute-6-kompatible Test-Matcher (#282)

## [1.18] - 2026-06-29

### Hinzugefügt

- Dashboard: Zwei-Zonen-Layout mit Hero-Saldo, Monats-KPIs und einer Analytics-Karte (Monat/Jahr)
- Donut-Chart mit Kategorie-Legende (Betrag und Prozent nebeneinander)
- Optionale kompakte Insight-Zeilen (Konten, Budget-Warnung, 30-Tage-Prognose)

### Geändert

- README-Dashboard-Screenshots für das neue Layout

### Behoben

- Kategorie-Prozente in der Monatsansicht (zeigten zuvor 0 %)
- Dauerauftrag-Hinweis: Chevron bleibt zum Auf- und Zuklappen sichtbar
- Mac Catalyst: Einstellungen stabil, App bleibt nach Fenster-Schließen aktiv, Release-Icons im Build

## [1.17] - 2026-06-21

### Hinzugefügt

- Barrierefreiheit: Chart-Text-Zusammenfassungen und VoiceOver-Labels für Listenzeilen (#235, #237)
- SelectionField A11y und Scroll-Hinweise auf Detailseiten (#239)
- Verwaltung-Segment als Button-Toggle; Währungsanzeige zur Laufzeit (`CurrencyRefreshRegistry`)
- Dashboard: einheitliche Kacheln, aufklappbare Monats-/Jahres-Sektionen
- Verwaltung: Kategorien und Konten im Sparziele-Kartenstil

### Behoben

- Review-Follow-ups v1.17 (Währung, Transaktions-A11y)
- `LoadKategorien`-Command ohne bool-Parameter (CI/RelayCommand)
- Prognose-Kachel ohne leeres Aufklappen

## [1.16] - 2026-06-20

### Hinzugefügt

- Anzeige-Währung (EUR/USD/GBP/CHF) in Einstellungen und Onboarding, getrennt von UI-Sprache (#253)
- `IDisplayCurrencyService` und `App.CurrencyChanged` für sofortige Betrags-Aktualisierung
- Lokalisierung von Seitentiteln und Enum-Labels (Kontotyp, Transaktionstyp) (#234)
- Verwaltung: Dashboard-Segment Kategorien/Konten mit klarerem Tab-Titel (#236)
- Dashboard Runde 2: Summary oben, einklappbare Sekundärbereiche (#252)
- Einheitliche Tab-Icons in Empty States statt Emoji (#240)

### Geändert

- Hardcoded XAML-Farben durch zentrale `Colors.xaml`-Tokens ersetzt (#254)
- Transaktionen: beschriftete Buttons „Filter“ / „Umbuchen“ statt Emoji (#238)
- Debug-Build installiert nach `~/Applications` (macOS 26+ Dev-Signatur)

### Behoben

- Fällige Daueraufträge im Dashboard standardmäßig aufgeklappt
- Währungswechsel aktualisiert Salden (Konto-Detail) und Import-Beträge ohne Seitenwechsel

## [1.15] - 2026-06-20

### Hinzugefügt

- Sparziel-Detailseite: bearbeiten, löschen, Beitrag buchen (#230)
- Dashboard-Karte „Nächste 30 Tage“ für Cashflow-Vorschau (#231)
- Kompaktere Dauerauftrag-Aktionen auf dem Dashboard (#232)
- Dashboard Summary-Karte und einklappbare Budget-/Jahresabschnitte (#233)
- Fällige Daueraufträge mit 7-Tage-Vorschau und lokalisierten Hinweisen
- `LanguageChanged`-Event und feste EUR-Formatierung (`CurrencyCulture`) unabhängig von UI-Sprache

### Behoben

- Dauerauftrag- und Sparziel-Startdatum: `DatePicker` mit sofortiger Binding-Aktualisierung
- Kontoauswahl-Dropdown lesbar in Light Mode; „Alle Konten“ in EN lokalisiert
- Sprachwechsel aktualisiert Dauerauftrag-Hinweise und formatierte Beträge sofort
- Sparziel-Fortschritt nach „Beitrag buchen“; Summary-Karte beim Monat/Jahr-Wechsel

## [1.14] - 2026-06-18

### Hinzugefügt

- Onboarding-Wizard (5 Schritte) beim ersten Start, erneut aufrufbar über Einstellungen
- Einheitliches `EmptyStateView` auf Listen-Seiten
- Snackbar-Feedback beim Speichern; Transaktion löschen mit Rückgängig
- Manueller Saldo-Abgleich (Ist-Saldo passt Anfangssaldo an) in Konto bearbeiten (#214)

### Geändert

- MAUI 10.0.71 und Microsoft.Extensions.* 10.0.9

### Behoben

- Onboarding: Einstellungen-Schritt schließt Wizard nicht mehr vorzeitig; eigener Abschluss-Schritt mit „Zum Dashboard“
- Onboarding: UI-Polish (Button-Breite, Skip als Sekundär-Button, einheitliche Aktions-Buttons)

## [1.13] - 2026-06-13

### Hinzugefügt

- `SelectionField` + `SelectionPopup` als Ersatz für native `Picker` in Formularen und Filtern (Mac-Catalyst-Workaround für macOS 27 Beta)
- Scroll-Hinweis (▼) auf langen Formularseiten (Einstellungen, Dauerauftrag bearbeiten)

### Geändert

- Auswahlfelder in Transaktionen, Daueraufträgen, Konten, Kategorien, Umbuchungen, Import-Vorschau, Dashboard- und Cashflow-Filtern nutzen `SelectionField` statt `Picker`
- `DatePicker` bleibt nativ (kein Freeze auf getesteten Systemen)

### Behoben

- Dashboard: Kontosaldo erscheint beim ersten Wechsel von „Alle Konten“ zu einem Konto
- `SelectionPopup`: Schließen per Klick außerhalb; deaktivierte `SelectionField`-Zeilen öffnen kein Popup

### Dokumentation

- Mac-Catalyst-Picker-Untersuchung und Entwickler-Referenz aktualisiert

## [1.12] - 2026-06-10

### Hinzugefügt

- Dashboard-Kontenübersicht mit Gesamtsaldo und Kontofilter
- Anfangssaldo pro Konto in der Kontenverwaltung
- 30-Tage-Cashflow-Vorschau vom Dashboard

## [0.5] - Unveröffentlicht (archiviert)

### Hinzugefügt

- Zentrale `SystemCategoryKeys`-Konstanten eingeführt, um die Verwendung von `magic strings`
  in der Geschäftslogik zu vermeiden. (Issue #43)

### Geändert

- `SysCat_*`-`magic string`-Literale in Core, Infrastruktur und Tests durch
  `SystemCategoryKeys`-Konstanten ersetzt.

### Hinweise

- Alle Unit-Tests laufen lokal erfolgreich durch (118 Tests).
- Version auf 0.5 erhöht.
