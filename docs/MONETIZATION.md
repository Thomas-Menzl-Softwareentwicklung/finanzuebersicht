# Monetarisierung (Entwurf)

Zielbild für den App Store: **attraktiver Free-Tier**, **Pro einmalig**, **CloudKit-Sync als günstiges Jahresabo** zur Gegenfinanzierung.

Keine StoreKit-Implementierung hier — nur Produkt-Schnitt. Preise sind Richtwerte (DE/AT/CH später lokalisiert).

## Distribution: Direct vs Store

| Build | Wie | Lokale Features | Cloud Sync |
|-------|-----|-----------------|------------|
| **Direct** (Default) | GitHub-Releases, selbst gebaut, Windows, sideload Mac | immer Pro (keine Soft-Limits) | **nie** |
| **Store** | `-p:AppDistribution=Store` für iOS / Mac App Store | Free → Pro (IAP) | Jahresabo, **ohne Pro-Pflicht** |

Compile-Flag: `APP_DISTRIBUTION_STORE` wenn `AppDistribution=Store`. CI/Release-Workflows bleiben auf **Direct**, damit bestehende Downloads unverändert voll nutzbar sind.

---

## Modell in einem Satz

> Free reicht für den Alltag auf einem Gerät. Pro schaltet Power-Features dauerhaft frei. Sync hält Daten optional über Geräte aktuell — und finanziert den laufenden Betrieb.

| Produkt | Typ | Richtpreis | Zweck |
|---------|-----|------------|--------|
| **Free** | Standard | 0 € | Einstieg, Vertrauen, Reviews |
| **Pro** | Non-Consumable IAP | **5,99 €** (Band 4,99–7,99) | Einmal-Kauf, lokale Power-Features |
| **Sync** (CloudKit) | Auto-Renewable Jahresabo | **5,99 € / Jahr** (Band 4,99–9,99) | Geräte-Sync, laufende Einnahmen |

### Product IDs (App Store Connect)

| Produkt | Product ID |
|---------|------------|
| Pro | `de.thomasmenzl.finanzuebersicht.pro` |
| Sync (jährlich) | `de.thomasmenzl.finanzuebersicht.sync.yearly` |

Konstante: `Finanzuebersicht.Core.Licensing.LicenseProductIds`.

Regeln:

- **Kein Abo für Kernfunktionen** — Free/Pro bleiben lokal nutzbar ohne Abonnement.
- **Keine Werbung, kein Tracking.**
- **Sync setzt Pro nicht voraus** — wer nur Free braucht, kann trotzdem Geräte synchronisieren. Pro bleibt reiner Feature-Kauf.
- Sync-Abo kann später „Family Sharing“ nutzen; Pro idealerweise ebenfalls shareable (Non-Consumable).

---

## Free — Ausprägung (genau)

Leitlinie: **Buchungs-Alltag unbegrenzt**, Struktur leicht begrenzt, Power-Tools = Pro.

### Enthalten (ohne Limit, wo sinnvoll)

| Bereich | Free |
|---------|------|
| Transaktionen anlegen / bearbeiten / löschen | ✅ unbegrenzt |
| Suche & Filter | ✅ |
| Umbuchen zwischen Konten | ✅ |
| Dashboard Monat / Jahr (Saldo, KPIs, Budget-Balken, Donut) | ✅ |
| Kategorien inkl. Farbe/Icon + Monatsbudget | ✅ |
| Daueraufträge (anlegen, fällige buchen/überspringen/verschieben) | ✅ mit Soft-Limit (siehe unten) |
| Manueller Saldo-Abgleich | ✅ |
| Sprache DE/EN, Dark Mode, VoiceOver | ✅ |
| **Backup & Restore** (manuell) | ✅ (Vertrauen / Datenverlust vermeiden) |
| Onboarding / Empty States | ✅ |

### Soft-Limits (Free)

| Limit | Free | Begründung |
|-------|------|------------|
| Konten | **3** | Giro + Spar + Bar/Kredit reicht den meisten; 4.+ → Pro |
| Daueraufträge | **8** | Genug für Gehalt/Miete/Abos; Power-User → Pro |
| Sparziele | **1** | Probieren möglich; mehrere Ziele → Pro |
| Transaktions-Vorlagen | **3** | Schnellbuchungen andeuten; mehr → Pro |

Anzeigen: Limit klar in UI („3 von 3 Konten · Pro für unbegrenzt“), nicht erst beim Speichern „kaputt“.

### Explizit **nicht** in Free (→ Pro)

| Feature | Warum Pro |
|---------|-----------|
| CSV-Import (inkl. Vorschau / Duplikate / Auto-Kategorie) | Power-User, hoher Mehrwert |
| 30-Tage-Cashflow | Planungs-Feature, starker Kaufanreiz |
| Unbegrenzte Konten / Daueraufträge / Sparziele / Vorlagen | Skalierung |
| Steuer-Export CSV/PDF (#64, wenn gebaut) | Klarer Pro-Nutzen |
| Home-Screen-Widget Anzeige (#242, wenn gebaut) | Convenience |
| Interaktives iOS-Widget (`AppFeature.QuickExpenseCapture`, ✅) | Starker iPhone-Kaufanreiz; In-App Schnell ist Free |
| Lokale Verschlüsselung (#53, wenn gebaut) | Security-Upsell |
| Kategorien-Hierarchie (#241, wenn gebaut) | Power-Organisation |

---

## Pro — Einmalkauf

Ein Non-Consumable: **„Finanzübersicht Pro“**.

- Hebt **alle Soft-Limits** auf.
- Schaltet die Pro-Features oben frei (sofern in der Version vorhanden).
- Gilt **dauerhaft** auf dem Apple-ID-Account (Restore Purchases).
- Unabhängig von Sync: Pro ohne Abo = volle lokale Power-App.

Positionierung im Store-Text: *„Einmal kaufen, lokal behalten — kein Abo für die Kern-App.“*

---

## Sync — günstiges Jahresabo (CloudKit, #243)

Produkt: **„Finanzübersicht Sync“** (Auto-Renewable, 1 Jahr).

| Aspekt | Vorschlag |
|--------|-----------|
| Preis | **5,99 € / Jahr** (bewusst unter „latente Abo-Abneigung“) |
| Leistung | iCloud/CloudKit-Sync zwischen iPhone / iPad / Mac |
| Ohne Abo | App bleibt voll lokal (Free oder Pro) |
| Kündigung | Daten bleiben auf Geräten; Sync stoppt, kein Lock-out der lokalen App |
| Pro-Bezug | **Unabhängig von Pro** — Free+Sync ist ein bewusst erlaubter Pfad; optional später Bundle „Pro + 1 Jahr Sync“ |

Das Abo gegenfinanziert laufende Kosten (Developer Program, Support, Weiterentwicklung). CloudKit-Infrastruktur selbst ist günstig — der Preis ist vor allem **Wert für Mehrgeräte**, nicht Server-Marge.

---

## Launch-Phasen

| Phase | Was im Store |
|-------|----------------|
| **1 — Launch** | Free + Pro (IAP, StoreKit). Sync noch nicht verkaufen (Feature fehlt). Soft-Limits + Pro-Gates aktiv. |
| **2 — Sync** | Jahresabo freischalten, sobald CloudKit (#243) + Persistenz-Prep (#300) stehen. Privacy/Support-Seiten um Sync ergänzen. |
| **3 — Feinschliff** | Intro-Offer (z. B. 1. Jahr Sync 2,99 €), optional Tip-Jar nur wenn gewünscht. |

StoreKit-Client (Pro kaufen / Restore) ist im Store-Build verdrahtet; siehe `docs/APP_STORE.md`.

Open Banking (#245) **nicht** in dieses Modell mischen — später eigenes Premium/Partner-Thema.

---

## Was Free *nicht* sein soll

- Keine Transaktions-Obergrenze (zerstört Gewohnheit und Reviews).
- Kein zeitlich begrenzter „Trial, dann tot“.
- Backup nicht hinter Paywall (Datenverlust = 1★).
- Kein Abo nur zum Entsperren von Buchungen.

---

## Offene Produktentscheidungen (kurz)

1. Daueraufträge **8 oder 10**? → Default **8**.
2. Mac Catalyst: gleiches IAP-Produkt (Universal Purchase) — ja, anstreben.

Festgelegt: Konten-Free-Limit **3**; Sync **ohne** Pro-Pflicht.

Wenn dieses Schema passt, als Nächstes: StoreKit-Produkte benennen + Feature-Flags/`ILicenseService` skizzieren (Implementierung separat).
