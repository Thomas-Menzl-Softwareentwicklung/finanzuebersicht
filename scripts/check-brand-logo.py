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
