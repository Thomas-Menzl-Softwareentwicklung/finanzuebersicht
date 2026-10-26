# Finanzübersicht Banner und Logo — Design

**Date:** 2026-10-08  
**Status:** Approved  
**Reference:** `docs/brand/preview/finanzuebersicht-f-over-euro-icon.jpg`, `docs/brand/preview/finanzuebersicht-f-over-euro-banner.jpg`  
**Family:** thomasmenzl.de TM-Monogramm (gefaltete Bänder, Blau → Cyan)

## Decision

Eigenes Produktzeichen in der TM-Familie. Kein TM-Klon.

| Piece | Choice |
|---|---|
| Mark | Großes gefaltetes **F**, darunter ein kleineres gefaltetes **€** |
| Icon | Nur die Marke, weißer Grund, kein Eckrahmen |
| Banner | Dieselbe Marke in vier offenen Eckklammern, plus Wordmark und Tagline |
| Wordmark | `FINANZÜBERSICHT` |
| Tagline | `FINANZEN LOKAL. KLAR. PRIVAT.` |

## Mark

- **F** wie im freigegebenen Icon: dicker Bandverlauf, Stamm links, zwei Querbalken nach rechts, untere Spitze des Stamms läuft weiter nach unten.
- **€** etwa 36 % der F-Höhe, in der Tasche **unter dem unteren Querbalken**, linke Kante am Anfang dieses Balkens. Abstand zum Balken etwa 4–5 % der F-Höhe. Nicht daneben auf gleicher Höhe, nicht als zweites F.
- Der Stamm bleibt links neben dem €. Die beiden Zeichen überlappen sich nicht.
- Geometrie: Umriss der freigegebenen Vorschau, gefüllt mit dem Bandverlauf links nach rechts. Keine eigene Navy-Faltfläche, keine Kontur, kein Schlagschatten, kein Glow.

## Farbe

| Role | Value |
|---|---|
| Band, links / dunkel | `#0032C3` |
| Band, rechts / hell | `#0ACDDE` |
| Füllung | Verlauf `#0032C3` → `#0ACDDE` über die ganze Marke |
| Wordmark | `#152038` |
| Tagline | `#0A9FBF` |
| Banner-Grund | `#f7f8fb` |
| Eckklammern | `#0ACDDE` |
| Icon-Grund | `#FFFFFF` |

## Banner-Lockup

Links die Marke in vier kurzen offenen Eckklammern (oben links, oben rechts, unten links, unten rechts). Kein geschlossener Rahmen, kein Chevron.

Rechts, vertikal zur Marke zentriert:

1. `FINANZÜBERSICHT` — geometrische Grotesk, fett, Versalien, Navy, leicht gesperrt
2. `FINANZEN LOKAL. KLAR. PRIVAT.` — kleiner, gesperrt, Cyan

Heller Grund `#f7f8fb`. Kein weißer Kasten hinter der Marke.

## Deliverables

Unter `docs/brand/`:

| File | Use |
|---|---|
| `logo-icon.png` | Icon-Master, 4096×4096, gefaltetes Band |
| `banner.png` | Banner-Master, 3840×2160, 16:9 |

Die JPG-Entwürfe in `docs/brand/preview/` bleiben die visuelle Referenz. Sie sind nicht die Lieferdatei.

## Out of scope

- App-Icon im MAUI-Bundle (`Finanzuebersicht/Resources/AppIcon/`) und Store-Upload
- UI-Farben oder In-App-Chrome
- README-Einbindung und thomasmenzl.de
