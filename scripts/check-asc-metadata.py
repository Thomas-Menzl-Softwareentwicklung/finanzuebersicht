#!/usr/bin/env python3
"""Fail if App Store metadata exceeds ASC field limits or name.txt exists."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1] / "fastlane" / "metadata"
LIMITS = {
    "subtitle.txt": 30,
    "promotional_text.txt": 170,
    "keywords.txt": 100,
    "description.txt": 4000,
    "release_notes.txt": 4000,
}

def main() -> int:
    errors: list[str] = []
    name = ROOT / "name.txt"
    if name.exists():
        errors.append(f"do not ship {name} (would overwrite ASC display name)")
    for locale in ("de-DE", "en-US"):
        folder = ROOT / locale
        if not folder.is_dir():
            errors.append(f"missing {folder}")
            continue
        for filename, limit in LIMITS.items():
            path = folder / filename
            if not path.is_file():
                errors.append(f"missing {path}")
                continue
            text = path.read_text(encoding="utf-8").strip()
            if len(text) > limit:
                errors.append(f"{path}: {len(text)} chars > {limit}")
    if errors:
        print("\n".join(errors), file=sys.stderr)
        return 1
    print("ASC metadata length checks passed")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
