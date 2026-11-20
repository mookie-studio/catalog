#!/usr/bin/env python3
"""Generates the images of the Demo dataset that are not photos: one Logo per Demo tenant and the book
covers of folio-books (the titles and authors are invented, so there are no real covers to use).

Usage, from the repository root (needs Pillow: pip install Pillow):

    python3 demo/tools/generate_images.py

Output is deterministic and released as CC0 (see demo/images/CREDITS.md). It overwrites logo.png and
cover-*.jpg only; photos are never touched.
"""
import hashlib
import json
import os
import textwrap

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
FONT_DIRS = ["/usr/share/fonts/truetype/dejavu", "/usr/share/fonts/dejavu", "/Library/Fonts", "C:/Windows/Fonts"]
SERIF_BOLD = ["DejaVuSerif-Bold.ttf", "Georgia Bold.ttf", "georgiab.ttf"]
SANS_BOLD = ["DejaVuSans-Bold.ttf", "Arial Bold.ttf", "arialbd.ttf"]
SANS = ["DejaVuSans.ttf", "Arial.ttf", "arial.ttf"]

PALETTES = [
    ("#1E3A5F", "#F2E8CF", "#E07A5F"), ("#7C2D12", "#FDF6E3", "#E9C46A"), ("#264653", "#E9F5F2", "#2A9D8F"),
    ("#3D405B", "#F4F1DE", "#81B29A"), ("#5B2A86", "#F6EFFA", "#F2A541"), ("#0B3954", "#FEFFFE", "#FF5A5F"),
    ("#2B2D42", "#EDF2F4", "#EF233C"), ("#4A5D23", "#F7F3E3", "#D4A373"), ("#9A031E", "#FFF8F0", "#FB8B24"),
    ("#023047", "#F1FAEE", "#FFB703"),
]


def font(names, size):
    for directory in FONT_DIRS:
        for name in names:
            path = os.path.join(directory, name)
            if os.path.exists(path):
                return ImageFont.truetype(path, size)
    return ImageFont.load_default(size)


def seed(text):
    return int(hashlib.sha256(text.encode()).hexdigest()[:8], 16)


def centered(draw, y, text, fnt, fill, width):
    box = draw.textbbox((0, 0), text, font=fnt)
    draw.text(((width - (box[2] - box[0])) / 2, y), text, font=fnt, fill=fill)
    return y + (box[3] - box[1])


def cover(path, title, author, key):
    width, height = 800, 1200
    dark, light, accent = PALETTES[seed(key) % len(PALETTES)]
    image = Image.new("RGB", (width, height), dark)
    draw = ImageDraw.Draw(image)
    style = seed(key + "style") % 3
    if style == 0:  # concentric circles
        for i, r in enumerate(range(520, 40, -60)):
            draw.ellipse((width / 2 - r, 640 - r, width / 2 + r, 640 + r), outline=accent if i % 2 else light, width=6)
    elif style == 1:  # stripes
        for i in range(0, height, 70):
            draw.rectangle((0, i, width, i + 26), fill=accent if (i // 70) % 3 == 0 else dark)
        draw.rectangle((60, 300, width - 60, 980), fill=dark)
    else:  # mountains and a sun
        draw.ellipse((470, 380, 650, 560), fill=accent)
        draw.polygon([(0, 1000), (250, 600), (480, 1000)], fill=light)
        draw.polygon([(300, 1000), (560, 520), (800, 1000)], fill=accent)
    draw.rectangle((40, 40, width - 40, height - 40), outline=light, width=4)
    title_font = font(SERIF_BOLD, 64)
    lines = textwrap.wrap(title.upper(), 16)
    draw.rectangle((40, 80, width - 40, 140 + 82 * len(lines)), fill=dark)
    draw.rectangle((40, height - 180, width - 40, height - 80), fill=dark)
    y = 110
    for line in lines:
        y = centered(draw, y, line, title_font, light, width) + 18
    centered(draw, height - 150, author, font(SANS, 40), light, width)
    image.save(path, "JPEG", quality=82, optimize=True)


def logo(path, name, color):
    size = 512
    image = Image.new("RGB", (size, size), color)
    draw = ImageDraw.Draw(image)
    draw.ellipse((56, 56, size - 56, size - 56), outline="#FFFFFF", width=14)
    initials = "".join(word[0] for word in name.split()[:2]).upper()
    fnt = font(SANS_BOLD, 190)
    box = draw.textbbox((0, 0), initials, font=fnt)
    draw.text(((size - (box[2] - box[0])) / 2 - box[0], (size - (box[3] - box[1])) / 2 - box[1]), initials, font=fnt, fill="#FFFFFF")
    image.save(path, "PNG", optimize=True)


def main():
    tenants = json.load(open(os.path.join(ROOT, "tenants.json")))
    for tenant in tenants:
        slug = tenant["slug"]
        images = os.path.join(ROOT, "images", slug)
        os.makedirs(images, exist_ok=True)
        profile = json.load(open(os.path.join(ROOT, slug, "profile.json")))
        logo(os.path.join(images, "logo.png"), tenant["name"], profile["brandColor"])
        products_file = os.path.join(ROOT, slug, "products.json")
        for product in json.load(open(products_file)) if os.path.exists(products_file) else []:
            for file in product.get("images", []):
                if file.startswith("cover-"):
                    author = product.get("attributes", {}).get("Author", "")
                    cover(os.path.join(images, file), product["name"], author, product["sku"])
    print("Logos and covers generated.")


if __name__ == "__main__":
    main()
