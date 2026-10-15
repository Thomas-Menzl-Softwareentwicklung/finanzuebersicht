# App-Icon Preview (neues Brand-Logo)

**Date:** 2026-10-15  
**Status:** Approved (Scope A)  
**Source:** `docs/finanzuebersicht-brand-logo` → `docs/brand/logo-icon.png`

## Goal

Neues Markenzeichen als Standard-App-Icon einbauen und lokal auf Mac Catalyst prüfen.

## Decision

| Piece | Choice |
|---|---|
| Asset light | Marke auf `#FFFFFF` → `appicon-light.png` |
| Asset dark | Dieselbe Marke auf `#152038` → `appicon-dark.png` |
| Asset tinted | Graustufen-Marke, transparent → `appicon-tinted.png` |
| iOS / Mac Catalyst | Target `InjectAppIconAppearances` schreibt light/dark/tinted in das Resizetizer-`appiconset` vor actool |
| MauiIcon | Weiter für alle Targets (`appicon.png`, `#FFFFFF`) — liefert Light-Basis |
| Alt-SVGs | `appicon.svg` / `appiconfg.svg` behalten (später Alternate Icons) |
| Splash | Marke zentriert auf `#F2F2F7` (`Resources/Splash/splash.png`) |
| In-App | Über-Karte: `brand_mark.png` (~64 pt) |
| Out of scope | Banner, Alternate-Icon-Wahl neu/alt, Store-Upload, UI-Farben |

## Verification

```bash
dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-maccatalyst
open ~/Applications/Finanzübersicht.app
```

Dock-/App-Icon zeigt F+€ Bandlogo auf weiß.
