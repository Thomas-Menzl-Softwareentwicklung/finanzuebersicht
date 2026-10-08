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
