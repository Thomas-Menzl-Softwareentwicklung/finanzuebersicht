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
| iOS / Mac Catalyst | Appearances: `Platforms/{iOS,MacCatalyst}/Resources/Assets.xcassets/appicon.appiconset` |
| MauiIcon | `Resources/AppIcon/appicon.png` (`#FFFFFF`) — nötig für actool/`Assets.car` + `CFBundleIconName`; Appearances kommen aus Platforms |
| Alt-SVGs | `appicon.svg` / `appiconfg.svg` behalten (später Alternate Icons) |
| Splash | `Resources/Images/brand_mark.png` auf `#F2F2F7` |
| In-App | Über-Karte: `AppIcons.BrandMark` (~64 pt) |
| Out of scope | Banner, Alternate-Icon-Wahl neu/alt, Store-Upload, UI-Farben |

## Verification

```bash
dotnet build Finanzuebersicht/Finanzuebersicht.csproj -f net10.0-maccatalyst
open ~/Applications/Finanzübersicht.app
```

Dock-/App-Icon zeigt F+€ Bandlogo auf weiß. actool darf nur **einen** `appicon`-Katalog clonen (`…/Resources/Assets.xcassets/appicon.appiconset`).
