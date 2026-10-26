#!/usr/bin/env python3
"""Compose localized App Store images from real, unmodified app captures.

Raw captures remain untouched. Upload only the separate generated output directory.
Requires Pillow; install scripts/requirements-screenshots.txt in a Python venv.
"""
import argparse
import html
from urllib.parse import quote
import json
import shutil
import tempfile
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont, ImageOps

ROOT = Path(__file__).resolve().parents[1]
LOCALES = ("de-DE", "en-US")


def find_font(explicit=None):
    candidates = [explicit] if explicit else []
    candidates += [
        "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
        "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
    ]
    for candidate in candidates:
        if candidate and Path(candidate).is_file():
            return str(candidate)
    raise ValueError("No font found. Pass --font /path/to/bold-font.ttf")


def fit_font(draw, text, font, maximum, width):
    for size in range(maximum, 11, -1):
        face = ImageFont.truetype(font, size)
        if all(draw.textlength(line, font=face) <= width for line in text.splitlines()):
            return face
    raise ValueError(f"Caption cannot fit: {text}")


def compose(source, caption, font):
    with Image.open(source) as raw:
        # Window captures have transparent rounded corners; flatten against the
        # composition background instead of turning those pixels black.
        rgba = raw.convert("RGBA")
        raw = Image.new("RGB", rgba.size, "#f3f6f4")
        raw.paste(rgba, mask=rgba.getchannel("A"))
        w, h = raw.size
        # Preserve the exact ASC dimensions. Never crop or stretch the real app UI.
        landscape = w > h
        header = round(h * (0.23 if landscape else 0.18))
        margin = round(w * 0.035)
        canvas = Image.new("RGB", (w, h), "#f3f6f4")
        draw = ImageDraw.Draw(canvas)
        title_font = fit_font(draw, caption["title"], font, round(w * (0.033 if landscape else 0.064)), w - 2 * margin)
        top = round(h * 0.018)
        draw.multiline_text((margin, top), caption["title"], font=title_font, fill="#183c30", spacing=round(h * 0.004))
        title_box = draw.multiline_textbbox((margin, top), caption["title"], font=title_font, spacing=round(h * 0.004))
        detail_font = fit_font(draw, caption["detail"], font, round(w * (0.014 if landscape else 0.026)), w - 2 * margin)
        detail_y = title_box[3] + round(h * 0.012)
        draw.text((margin, detail_y), caption["detail"], font=detail_font, fill="#44574e")
        if caption.get("feature"):
            feature_font = fit_font(draw, caption["feature"], font, round(w * (0.011 if landscape else 0.022)), w - 2 * margin)
            feature_y = detail_y + detail_font.size + round(h * 0.007)
            draw.text((margin, feature_y), caption["feature"], font=feature_font, fill="#183c30")
            if feature_y + feature_font.size >= header:
                raise ValueError(f"Caption overlaps app image: {source}")
        elif detail_y + detail_font.size >= header:
            raise ValueError(f"Caption overlaps app image: {source}")
        available = (w - 2 * margin, h - header - round(h * 0.012))
        app = ImageOps.contain(raw, available, Image.Resampling.LANCZOS)
        canvas.paste(app, ((w - app.width) // 2, header))
        return canvas


def prepare_jobs(source, storyboard):
    jobs = []
    expected_groups = None
    for locale in LOCALES:
        captions = storyboard.get(locale)
        if not captions:
            raise ValueError(f"Missing storyboard locale: {locale}")
        files = sorted((source / locale).glob("*.png"))
        groups = {}
        for file in files:
            for caption in captions:
                suffix = caption["source"] + ".png"
                if file.name == suffix:
                    groups.setdefault("", {})[caption["source"]] = file
                elif file.name.endswith("-" + suffix):
                    prefix = file.name[:-len(suffix)]
                    groups.setdefault(prefix, {})[caption["source"]] = file
        if not groups:
            raise ValueError(f"No captures in {source / locale}")
        if expected_groups is None:
            expected_groups = set(groups)
        elif set(groups) != expected_groups:
            raise ValueError(f"Device groups differ between locales: {locale}; rerun captures")
        for prefix, captures in groups.items():
            missing = [c["source"] for c in captions if c["source"] not in captures]
            if missing:
                raise ValueError(f"Incomplete {locale}/{prefix}: missing {', '.join(missing)}; rerun captures")
            sizes = set()
            for order, caption in enumerate(captions, 1):
                file = captures[caption["source"]]
                with Image.open(file) as image:
                    sizes.add(image.size)
                target = f"{prefix}{order:02d}-{caption['source'].split('-', 1)[1]}.png"
                jobs.append((locale, file, target, caption))
            if len(sizes) != 1:
                raise ValueError(f"Mixed capture dimensions in {locale}/{prefix}: {sizes}")
    return jobs


def render(source, output, storyboard_path=ROOT / "fastlane/screenshot-storyboard.json", font=None):
    source, output = Path(source).resolve(), Path(output).resolve()
    if source == output or source in output.parents or output in source.parents:
        raise ValueError("Raw and store directories must be separate sibling trees")
    storyboard = json.loads(Path(storyboard_path).read_text())
    jobs = prepare_jobs(source, storyboard)
    font_path = find_font(font)
    # Render the complete set successfully before replacing any previous upload PNGs.
    with tempfile.TemporaryDirectory(prefix="finanz-store-") as temp:
        stage = Path(temp)
        for locale, raw, target, caption in jobs:
            folder = stage / locale
            folder.mkdir(exist_ok=True)
            compose(raw, caption, font_path).save(folder / target)
        for locale in LOCALES:
            folder = output / locale
            folder.mkdir(parents=True, exist_ok=True)
            for stale in folder.glob("*.png"):
                stale.unlink()
            for generated in (stage / locale).glob("*.png"):
                shutil.copy2(generated, folder / generated.name)
    galleries = []
    for locale in LOCALES:
        images = sorted((output / locale).glob("*.png"))
        pictures = "".join(
            f'<a href="{quote(locale + "/" + image.name)}"><img loading="lazy" src="{quote(locale + "/" + image.name)}" alt="{html.escape(image.stem)}"></a>'
            for image in images
        )
        galleries.append(f'<h2>{locale}</h2><div class="gallery">{pictures}</div>')
    preview = """<!doctype html><html lang="de"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Finanzübersicht – Store-Screenshots</title><style>
body{margin:24px;background:#f3f6f4;color:#183c30;font:16px/1.6 system-ui}h1{font-size:24px}h2{margin-top:32px}.gallery{display:flex;flex-wrap:wrap;gap:18px;align-items:start}.gallery a{display:block;max-width:100%;width:360px}img{display:block;width:100%;height:auto;border:1px solid #ccd8d1}
</style><h1>Store-Screenshots · DE / EN</h1><p>Lokale Vorschau. Zum Vergrößern ein Bild öffnen. Es wurde nichts hochgeladen.</p>""" + "".join(galleries) + "</html>"
    (output / "index.html").write_text(preview)
    return len(jobs)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--font")
    args = parser.parse_args()
    try:
        count = render(args.source, args.output, font=args.font)
    except (ValueError, OSError) as error:
        parser.exit(1, f"Screenshot rendering failed: {error}\n")
    print(f"Rendered {count} localized store screenshots into {args.output}")


if __name__ == "__main__":
    main()
