import importlib.util
import tempfile
import unittest
from pathlib import Path
from PIL import Image

spec = importlib.util.spec_from_file_location("renderer", Path(__file__).with_name("render-store-screenshots.py"))
renderer = importlib.util.module_from_spec(spec)
spec.loader.exec_module(renderer)


class StoreScreenshotTests(unittest.TestCase):
    def captures(self, root):
        for locale in renderer.LOCALES:
            folder = root / locale
            folder.mkdir(parents=True)
            for name in ["01-dashboard", "08-accounts", "09-import", "02-transactions", "04-recurring", "06-savings", "03-quick-expense"]:
                Image.new("RGB", (1280, 800), "#447766").save(folder / (name + ".png"))

    def test_renders_both_languages_and_keeps_raw_dimensions_and_content(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            self.captures(root / "raw")
            count = renderer.render(root / "raw", root / "store")
            self.assertEqual(count, 14)
            for locale in renderer.LOCALES:
                files = sorted((root / "store" / locale).glob("*.png"))
                self.assertEqual(len(files), 7)
                self.assertIn("accounts", files[1].name)
                self.assertIn("import", files[2].name)
                self.assertFalse(any("settings" in f.name for f in files))
                with Image.open(files[0]) as image:
                    self.assertEqual(image.size, (1280, 800))
                    self.assertEqual(image.getpixel((640, 500)), (68, 119, 102))
            with Image.open(root / "raw/de-DE/01-dashboard.png") as raw:
                self.assertEqual(raw.getpixel((0, 0)), (68, 119, 102))

    def test_transparent_window_corners_use_composition_background(self):
        with tempfile.TemporaryDirectory() as temp:
            raw = Path(temp) / "window.png"
            Image.new("RGBA", (1280, 800), (0, 0, 0, 0)).save(raw)
            image = renderer.compose(raw, {"title": "Overview", "detail": "Real app capture"}, renderer.find_font())
            self.assertEqual(image.getpixel((640, 400)), (243, 246, 244))

    def test_incomplete_capture_does_not_replace_existing_outputs(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            self.captures(root / "raw")
            (root / "raw/en-US/09-import.png").unlink()
            old = root / "store/de-DE/old.png"
            old.parent.mkdir(parents=True)
            old.write_bytes(b"previous-output")
            with self.assertRaisesRegex(ValueError, "Incomplete"):
                renderer.render(root / "raw", root / "store")
            self.assertEqual(old.read_bytes(), b"previous-output")

    def test_refuses_an_entire_device_missing_from_one_language(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            self.captures(root / "raw")
            for source in list((root / "raw/de-DE").glob("*.png")):
                (source.parent / ("iPhone-" + source.name)).write_bytes(source.read_bytes())
            with self.assertRaisesRegex(ValueError, "Device groups differ"):
                renderer.render(root / "raw", root / "store")

    def test_refuses_output_inside_raw_capture_tree(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            with self.assertRaisesRegex(ValueError, "separate"):
                renderer.render(root, root / "store")


if __name__ == "__main__":
    unittest.main()
