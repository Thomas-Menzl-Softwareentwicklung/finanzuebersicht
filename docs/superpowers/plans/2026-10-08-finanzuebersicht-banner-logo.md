# Finanzübersicht Banner und Logo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a standalone Finanzübersicht icon and 16:9 banner (SVG source plus PNG export) matching the approved F-over-€ mark.

**Architecture:** Two self-contained SVGs under `docs/brand/`. The icon is the mark on white. The banner repeats the same path data (no external `<use>`), adds corner brackets, wordmark, and tagline. A Python script checks structure and colors. PNGs are rasterized from the SVGs with `qlmanage` and normalized with `sips`.

**Tech Stack:** SVG, Python 3 stdlib, macOS `qlmanage` and `sips`

## Global Constraints

- Mark: large folded F, smaller folded € in the pocket under the lower crossbar, left edge of the € at the start of that bar, gap about 4–5 % of the F height, no overlap with the stem
- € height about 36 % of the F height
- Icon: white `#FFFFFF`, no corner brackets, no wordmark
- Banner ground `#f7f8fb`, brackets `#0ACDDE`, wordmark `FINANZÜBERSICHT` in `#152038`, tagline `FINANZEN LOKAL. KLAR. PRIVAT.` in `#0A9FBF`
- Band gradient `#0032C3` → `#0ACDDE`, inner fold `#0A0A73`
- No drop shadow, no glow, no `<image>` raster embed
- Do not edit `Finanzuebersicht/Resources/AppIcon/`, README, or thomasmenzl.de
- Do not commit `docs/brand/preview/`, `.finanz-crash.log`, `licenses/`, or the IAP control JPGs
- Spec: `docs/superpowers/specs/2026-10-08-finanzuebersicht-banner-logo-design.md`

## File map

| File | Role |
|---|---|
| `docs/brand/logo-icon.svg` | Icon source, 1024×1024 |
| `docs/brand/banner.svg` | Banner source, 1600×900 |
| `docs/brand/logo-icon.png` | Raster of the icon |
| `docs/brand/banner.png` | Raster of the banner |
| `scripts/check-brand-logo.py` | Structure and color check |

---

### Task 1: Icon SVG and checker

**Files:**
- Create: `scripts/check-brand-logo.py`
- Create: `docs/brand/logo-icon.svg`
- Commit also: `docs/superpowers/specs/2026-10-08-finanzuebersicht-banner-logo-design.md`

**Interfaces:**
- Consumes: approved spec colors and the mark geometry below
- Produces: `docs/brand/logo-icon.svg` with root `viewBox="0 0 1024 1024"` and a group `id="mark"` containing paths `f-stem`, `f-top`, `f-mid`, `f-fold`, `euro-body`, `euro-bar-top`, `euro-bar-bottom`

- [ ] **Step 1: Write the failing check**

Create `scripts/check-brand-logo.py`:

```python
#!/usr/bin/env python3
"""Check the Finanzübersicht brand icon SVG."""
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ICON = ROOT / "docs" / "brand" / "logo-icon.svg"
REQUIRED = ("#0032C3", "#0ACDDE", "#0A0A73", "#FFFFFF")


def main() -> None:
    if not ICON.is_file():
        sys.exit(f"missing {ICON}")
    raw = ICON.read_text(encoding="utf-8")
    root = ET.fromstring(raw)
    if root.get("viewBox") != "0 0 1024 1024":
        sys.exit(f"icon viewBox {root.get('viewBox')!r}")
    lowered = raw.lower()
    for color in REQUIRED:
        if color.lower() not in lowered:
            sys.exit(f"icon missing {color}")
    if "FINANZÜBERSICHT" in raw:
        sys.exit("icon must not contain the wordmark")
    if "<image" in lowered:
        sys.exit("icon must not embed a raster image")
    print("icon ok")


if __name__ == "__main__":
    main()
```

- [ ] **Step 2: Run the check and confirm it fails**

Run: `python3 scripts/check-brand-logo.py`

Expected: exit code 1, message `missing` and the path to `docs/brand/logo-icon.svg`.

- [ ] **Step 3: Write the icon SVG**

Create `docs/brand/logo-icon.svg` with this exact content:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1024 1024" width="1024" height="1024">
  <defs>
    <linearGradient id="band" x1="0" y1="0" x2="520" y2="0" gradientUnits="userSpaceOnUse">
      <stop offset="0" stop-color="#0032C3"/>
      <stop offset="1" stop-color="#0ACDDE"/>
    </linearGradient>
    <linearGradient id="stem" x1="75" y1="80" x2="75" y2="580" gradientUnits="userSpaceOnUse">
      <stop offset="0" stop-color="#0032C3"/>
      <stop offset="1" stop-color="#0ACDDE"/>
    </linearGradient>
  </defs>
  <rect width="1024" height="1024" fill="#FFFFFF"/>
  <g id="mark" transform="translate(252,222)">
    <path id="f-stem" fill="url(#stem)" d="M0 80H150V390L100 520L0 580Z"/>
    <path id="f-top" fill="url(#band)" d="M0 40L520 0L500 100L150 130Z"/>
    <path id="f-mid" fill="url(#band)" d="M150 230L430 185L412 270L150 310Z"/>
    <path id="f-fold" fill="#0A0A73" d="M150 310L196 286L168 348Z"/>
    <path id="euro-body" fill="url(#band)" d="M210 362L280 350L400 337L418 387L300 412L200 404L220 442L410 460L392 512L250 534L185 482L215 442L195 402Z"/>
    <path id="euro-bar-top" fill="url(#band)" d="M168 390L330 370L324 396L174 412Z"/>
    <path id="euro-bar-bottom" fill="url(#band)" d="M162 444L330 424L324 450L168 466Z"/>
  </g>
</svg>
```

- [ ] **Step 4: Run the check and confirm it passes**

Run: `python3 scripts/check-brand-logo.py`

Expected: exit code 0 and the line `icon ok`.

- [ ] **Step 5: Commit**

```bash
git add docs/superpowers/specs/2026-10-08-finanzuebersicht-banner-logo-design.md docs/brand/logo-icon.svg scripts/check-brand-logo.py
git commit -m "$(cat <<'EOF'
docs(brand): add Finanzübersicht logo icon

EOF
)"
```

Do not `git add` `docs/brand/preview/`.

---

### Task 2: Banner SVG and PNG exports

**Files:**
- Create: `docs/brand/banner.svg`
- Create: `docs/brand/logo-icon.png`
- Create: `docs/brand/banner.png`
- Modify: `scripts/check-brand-logo.py`

**Interfaces:**
- Consumes: the seven `path` `d` values and gradient stops from `docs/brand/logo-icon.svg` (copied into the banner, not referenced externally)
- Produces: `docs/brand/banner.svg` viewBox `0 0 1600 900` containing the strings `FINANZÜBERSICHT` and `FINANZEN LOKAL. KLAR. PRIVAT.`, plus both PNGs

- [ ] **Step 1: Extend the check so the banner and PNGs fail first**

Replace `scripts/check-brand-logo.py` with:

```python
#!/usr/bin/env python3
"""Check Finanzübersicht brand SVG and PNG exports."""
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BRAND = ROOT / "docs" / "brand"
ICON = BRAND / "logo-icon.svg"
BANNER = BRAND / "banner.svg"
ICON_PNG = BRAND / "logo-icon.png"
BANNER_PNG = BRAND / "banner.png"
PNG_SIG = b"\x89PNG\r\n\x1a\n"


def png_size(path: Path) -> tuple[int, int]:
    data = path.read_bytes()[:24]
    if data[:8] != PNG_SIG:
        sys.exit(f"{path} is not a PNG")
    width = int.from_bytes(data[16:20], "big")
    height = int.from_bytes(data[20:24], "big")
    return width, height


def require_colors(raw: str, colors: tuple[str, ...], label: str) -> None:
    lowered = raw.lower()
    for color in colors:
        if color.lower() not in lowered:
            sys.exit(f"{label} missing {color}")
    if "<image" in lowered:
        sys.exit(f"{label} must not embed a raster image")


def main() -> None:
    if not ICON.is_file():
        sys.exit(f"missing {ICON}")
    icon_raw = ICON.read_text(encoding="utf-8")
    icon = ET.fromstring(icon_raw)
    if icon.get("viewBox") != "0 0 1024 1024":
        sys.exit(f"icon viewBox {icon.get('viewBox')!r}")
    require_colors(icon_raw, ("#0032C3", "#0ACDDE", "#0A0A73", "#FFFFFF"), "icon")
    if "FINANZÜBERSICHT" in icon_raw:
        sys.exit("icon must not contain the wordmark")

    if not BANNER.is_file():
        sys.exit(f"missing {BANNER}")
    banner_raw = BANNER.read_text(encoding="utf-8")
    banner = ET.fromstring(banner_raw)
    if banner.get("viewBox") != "0 0 1600 900":
        sys.exit(f"banner viewBox {banner.get('viewBox')!r}")
    require_colors(
        banner_raw,
        ("#0032C3", "#0ACDDE", "#0A0A73", "#f7f8fb", "#152038", "#0A9FBF"),
        "banner",
    )
    if "FINANZÜBERSICHT" not in banner_raw:
        sys.exit("banner missing wordmark")
    if "FINANZEN LOKAL. KLAR. PRIVAT." not in banner_raw:
        sys.exit("banner missing tagline")

    for path, expected in ((ICON_PNG, (1024, 1024)), (BANNER_PNG, (1600, 900))):
        if not path.is_file():
            sys.exit(f"missing {path}")
        actual = png_size(path)
        if actual != expected:
            sys.exit(f"{path.name} size {actual}, expected {expected}")
    print("brand ok")


if __name__ == "__main__":
    main()
```

- [ ] **Step 2: Run the check and confirm it fails on the banner**

Run: `python3 scripts/check-brand-logo.py`

Expected: exit code 1, message `missing` and the path to `docs/brand/banner.svg`.

- [ ] **Step 3: Write the banner SVG**

Create `docs/brand/banner.svg`. The mark paths are the same `d` attributes as the icon. Brackets sit 36 px outside the scaled mark (`translate(130,200) scale(0.862)` on a 520×580 mark → box about x=130, y=200, w=448, h=500).

```xml
<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1600 900" width="1600" height="900">
  <defs>
    <linearGradient id="band" x1="0" y1="0" x2="520" y2="0" gradientUnits="userSpaceOnUse">
      <stop offset="0" stop-color="#0032C3"/>
      <stop offset="1" stop-color="#0ACDDE"/>
    </linearGradient>
    <linearGradient id="stem" x1="75" y1="80" x2="75" y2="580" gradientUnits="userSpaceOnUse">
      <stop offset="0" stop-color="#0032C3"/>
      <stop offset="1" stop-color="#0ACDDE"/>
    </linearGradient>
  </defs>
  <rect width="1600" height="900" fill="#f7f8fb"/>
  <g id="mark" transform="translate(130,200) scale(0.862)">
    <path id="f-stem" fill="url(#stem)" d="M0 80H150V390L100 520L0 580Z"/>
    <path id="f-top" fill="url(#band)" d="M0 40L520 0L500 100L150 130Z"/>
    <path id="f-mid" fill="url(#band)" d="M150 230L430 185L412 270L150 310Z"/>
    <path id="f-fold" fill="#0A0A73" d="M150 310L196 286L168 348Z"/>
    <path id="euro-body" fill="url(#band)" d="M210 362L280 350L400 337L418 387L300 412L200 404L220 442L410 460L392 512L250 534L185 482L215 442L195 402Z"/>
    <path id="euro-bar-top" fill="url(#band)" d="M168 390L330 370L324 396L174 412Z"/>
    <path id="euro-bar-bottom" fill="url(#band)" d="M162 444L330 424L324 450L168 466Z"/>
  </g>
  <g id="brackets" fill="none" stroke="#0ACDDE" stroke-width="8" stroke-linecap="square">
    <path d="M94 210V164H140"/>
    <path d="M568 164H614V210"/>
    <path d="M94 690V736H140"/>
    <path d="M568 736H614V690"/>
  </g>
  <text x="710" y="400" fill="#152038" font-family="Arial, Helvetica, sans-serif" font-size="54" font-weight="700" letter-spacing="2.5">FINANZÜBERSICHT</text>
  <text x="710" y="458" fill="#0A9FBF" font-family="Arial, Helvetica, sans-serif" font-size="24" letter-spacing="1.8">FINANZEN LOKAL. KLAR. PRIVAT.</text>
</svg>
```

- [ ] **Step 4: Rasterize both SVGs to the spec sizes**

Run:

```bash
qlmanage -t -s 1024 -o docs/brand docs/brand/logo-icon.svg
qlmanage -t -s 1600 -o docs/brand docs/brand/banner.svg
mv docs/brand/logo-icon.svg.png docs/brand/logo-icon.png
mv docs/brand/banner.svg.png docs/brand/banner.png
sips -z 1024 1024 docs/brand/logo-icon.png
sips -z 900 1600 docs/brand/banner.png
```

Expected: both `mv` commands succeed. `sips` prints the new pixel sizes `/pixelWidth: 1024` and `/pixelWidth: 1600`.

- [ ] **Step 5: Run the check and confirm it passes**

Run: `python3 scripts/check-brand-logo.py`

Expected: exit code 0 and the line `brand ok`.

- [ ] **Step 6: Commit**

```bash
git add docs/brand/banner.svg docs/brand/logo-icon.png docs/brand/banner.png scripts/check-brand-logo.py
git commit -m "$(cat <<'EOF'
docs(brand): add Finanzübersicht banner and PNG exports

EOF
)"
```

Do not `git add` `docs/brand/preview/`.

---

## Spec coverage

| Spec requirement | Task |
|---|---|
| F with € under the lower bar, ~36 % height, gap, no overlap | 1 (path geometry), repeated in 2 |
| Icon on white, no frame, no wordmark | 1 |
| Banner ground, brackets, wordmark, tagline, colors | 2 |
| `logo-icon.svg`, `logo-icon.png`, `banner.svg`, `banner.png` | 1 and 2 |
| Preview JPGs are not the deliverable | commits exclude `docs/brand/preview/` |
| No AppIcon / README / website change | no task touches those paths |
