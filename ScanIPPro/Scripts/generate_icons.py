# -*- coding: utf-8 -*-
from pathlib import Path
from PIL import Image

src = Path(r"C:\Users\yurin\.cursor\projects\d-source-YurinKonstantin-ScanIP\assets\scanip-icon-master.png")
assets = Path(__file__).resolve().parent.parent / "Assets"
assets.mkdir(parents=True, exist_ok=True)

img = Image.open(src).convert("RGBA")

sizes = {
    "StoreLogo.png": (50, 50),
    "Square44x44Logo.scale-200.png": (88, 88),
    "Square150x150Logo.scale-200.png": (300, 300),
    "Wide310x150Logo.scale-200.png": (620, 300),
    "SplashScreen.scale-200.png": (620, 300),
    "LockScreenLogo.scale-200.png": (48, 48),
    "Square44x44Logo.targetsize-24_altform-unplated.png": (24, 24),
}

for name, size in sizes.items():
    if size[0] == 620 and size[1] == 300:
        canvas = Image.new("RGBA", size, (0, 120, 212, 255))
        icon = img.copy()
        icon.thumbnail((280, 280), Image.Resampling.LANCZOS)
        ox = (size[0] - icon.width) // 2
        oy = (size[1] - icon.height) // 2
        canvas.paste(icon, (ox, oy), icon)
        canvas.save(assets / name, "PNG")
    else:
        resized = img.copy()
        resized.thumbnail(size, Image.Resampling.LANCZOS)
        canvas = Image.new("RGBA", size, (0, 0, 0, 0))
        ox = (size[0] - resized.width) // 2
        oy = (size[1] - resized.height) // 2
        canvas.paste(resized, (ox, oy), resized)
        canvas.save(assets / name, "PNG")

print("Icons generated in", assets)
