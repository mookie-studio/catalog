#!/usr/bin/env python3
"""Downloads the Demo dataset photos from Wikimedia Commons (public domain or CC0 only).

Usage (Python 3.9+, no packages needed; Pillow is used to shrink the files when it is installed):

    python3 download_images.py [--out demo-images] [--contact you@example.com]

It searches Commons for each kind of product, keeps only files whose licence is public domain or CC0,
downloads a version at most 1200 px wide, and writes <out>/<tenant-slug>/<file>.jpg plus
<out>/CREDITS.md (file, source page, author, licence). Re-running skips files that already exist, so an
interrupted run can be continued. Wikimedia asks automated clients to identify themselves: --contact adds
your contact to the User-Agent (optional). Check every photo before using it: no brands, logos or faces.
"""
import argparse
import html
import json
import os
import re
import sys
import time
import urllib.parse
import urllib.request

API = "https://commons.wikimedia.org/w/api.php"
LICENCE_FILTER = "haswbstatement:P275=Q6938433|P6216=Q19652"  # CC0 or public domain (copyright status)

# Tenant slug -> file prefix -> (how many, Commons search terms).
WANTED = {
    "forja-hardware": {
        "drill": (8, "cordless drill tool"),
        "saw": (4, "circular saw tool"),
        "sander": (4, "orbital sander"),
        "hammer": (3, "claw hammer"),
        "screwdriver": (3, "screwdrivers tools"),
        "wrench": (3, "wrench spanner tool"),
        "screws": (3, "wood screws"),
        "anchors": (1, "wall plugs anchors"),
        "brush": (2, "paint brush"),
        "roller": (3, "paint roller"),
        "tape": (1, "tape measure"),
    },
    "lino-apparel": {
        "dress": (3, "linen dress"),
        "tshirt": (3, "t-shirt garment"),
        "trousers": (6, "trousers garment"),
        "coat": (4, "wool coat garment"),
        "shirt": (4, "shirt garment"),
        "sweater": (4, "knitted sweater"),
        "bag": (3, "tote bag"),
        "scarf": (2, "wool scarf"),
        "sneaker": (2, "canvas sneakers"),
        "boot": (3, "leather boots"),
        "beanie": (1, "knitted beanie hat"),
    },
    "folio-books": {
        "spread": (5, "open book pages"),
    },
    "tallo-garden": {
        "plant": (2, "potted plant"),
        "pot": (1, "terracotta flower pot"),
    },
}

ACCEPTED_LICENCES = ("cc0", "public domain", "pd")


def get_json(params, user_agent):
    url = API + "?" + urllib.parse.urlencode({**params, "format": "json", "maxlag": "5"})
    for attempt in range(6):
        request = urllib.request.Request(url, headers={"User-Agent": user_agent})
        try:
            with urllib.request.urlopen(request, timeout=60) as response:
                return json.load(response)
        except urllib.error.HTTPError as error:
            if error.code in (429, 503) and attempt < 5:
                wait = 10 * (attempt + 1)
                print(f"  Commons is busy ({error.code}); waiting {wait} s...")
                time.sleep(wait)
                continue
            raise


def search(terms, user_agent, offset=0):
    data = get_json({
        "action": "query",
        "generator": "search",
        "gsrsearch": f"{terms} filetype:bitmap {LICENCE_FILTER}",
        "gsrnamespace": "6",
        "gsrlimit": "40",
        "gsroffset": str(offset),
        "prop": "imageinfo",
        "iiprop": "url|extmetadata|size|mime",
        "iiurlwidth": "1200",
    }, user_agent)
    pages = sorted((data.get("query") or {}).get("pages", {}).values(), key=lambda p: p.get("index", 0))
    return pages


def clean(text):
    return html.unescape(re.sub(r"<[^>]+>", "", text or "")).strip()


def licence_of(info):
    meta = info.get("extmetadata", {})
    return clean(meta.get("LicenseShortName", {}).get("value")), clean(meta.get("Artist", {}).get("value")) or "Unknown"


def download(url, path, user_agent):
    request = urllib.request.Request(url, headers={"User-Agent": user_agent})
    with urllib.request.urlopen(request, timeout=120) as response, open(path, "wb") as out:
        out.write(response.read())
    shrink(path)


def shrink(path):
    try:
        from PIL import Image
    except ImportError:
        return
    with Image.open(path) as image:
        image = image.convert("RGB")
        image.thumbnail((1200, 1200))
        for quality in (82, 75, 68, 60):
            image.save(path, "JPEG", quality=quality, optimize=True)
            if os.path.getsize(path) <= 250 * 1024:
                break


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--out", default="demo-images")
    parser.add_argument("--contact", default="", help="optional contact added to the User-Agent")
    args = parser.parse_args()
    user_agent = "CatalogDemoImages/1.0 (one-off download of public-domain photos" + (f"; {args.contact}" if args.contact else "") + ")"

    credits_path = os.path.join(args.out, "CREDITS.md")
    credits = {}
    if os.path.exists(credits_path):
        for line in open(credits_path, encoding="utf-8"):
            if line.startswith("| ") and not line.startswith("| File") and not line.startswith("| ---"):
                credits[line.split("|")[1].strip()] = line.rstrip("\n")

    used = {line.split("|")[2].strip() for line in credits.values()}
    for slug, prefixes in WANTED.items():
        os.makedirs(os.path.join(args.out, slug), exist_ok=True)
        for prefix, (count, terms) in prefixes.items():
            missing = [n for n in range(1, count + 1) if not os.path.exists(os.path.join(args.out, slug, f"{prefix}-{n}.jpg"))]
            if not missing:
                continue
            print(f"{slug}: {prefix} x{len(missing)} ({terms})")
            candidates, offset = [], 0
            while len(missing) > 0 and offset < 200:
                if not candidates:
                    candidates = search(terms, user_agent, offset)
                    offset += 40
                    if not candidates:
                        break
                page = candidates.pop(0)
                info = (page.get("imageinfo") or [{}])[0]
                source = info.get("descriptionurl", "")
                licence, author = licence_of(info)
                if source in used or info.get("mime") not in ("image/jpeg", "image/png", "image/webp"):
                    continue
                if not any(licence.lower().startswith(l) for l in ACCEPTED_LICENCES) or info.get("width", 0) < 600:
                    continue
                name = f"{prefix}-{missing.pop(0)}.jpg"
                try:
                    download(info.get("thumburl") or info["url"], os.path.join(args.out, slug, name), user_agent)
                except Exception as error:  # keep going; the file is retried on the next run
                    print(f"  could not download {source}: {error}")
                    continue
                used.add(source)
                credits[f"{slug}/{name}"] = f"| {slug}/{name} | {source} | {author.replace('|', '/')} | {licence} |"
                print(f"  {name} <- {source} ({licence})")
                time.sleep(1)
            if missing:
                print(f"  only part found for {prefix}; {len(missing)} missing. Re-run later or add them by hand.")

    with open(credits_path, "w", encoding="utf-8") as out:
        out.write("# Image credits\n\nEvery photo is public domain or CC0, from Wikimedia Commons.\n\n")
        out.write("| File | Source | Author | Licence |\n| --- | --- | --- | --- |\n")
        for key in sorted(credits):
            out.write(credits[key] + "\n")
    print(f"\nDone. Photos in {args.out}/, credits in {credits_path}. Review them, zip the folder and send it.")


if __name__ == "__main__":
    sys.exit(main())
