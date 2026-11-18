#!/usr/bin/env node
// Downloads the Demo dataset photos from Wikimedia Commons (public domain or CC0 only).
//
// Usage (Node.js 18 or later, no packages needed):
//
//   node download-images.mjs [--out demo-images] [--contact you@example.com]
//
// It searches Commons for each kind of product, keeps only files whose licence is public domain or CC0,
// downloads a version at most 1200 px wide, and writes <out>/<tenant-slug>/<prefix>-<n>.jpg plus
// <out>/CREDITS.md (file, source page, author, licence). Re-running skips files that already exist, so an
// interrupted run can be continued. Wikimedia asks automated clients to identify themselves: --contact adds
// your contact to the User-Agent (optional). Check every photo before using it: no brands, logos or faces.
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const API = "https://commons.wikimedia.org/w/api.php";
const LICENCE_FILTER = "haswbstatement:P275=Q6938433|P6216=Q19652"; // CC0 or public domain
const ACCEPTED = ["cc0", "public domain", "pd"];

// Tenant slug -> file prefix -> [how many, Commons search terms].
const WANTED = {
  "forja-hardware": {
    drill: [8, "cordless drill tool"],
    saw: [4, "circular saw tool"],
    sander: [4, "orbital sander"],
    hammer: [3, "claw hammer"],
    screwdriver: [3, "screwdrivers tools"],
    wrench: [3, "wrench spanner tool"],
    screws: [3, "wood screws"],
    anchors: [1, "wall plugs anchors"],
    brush: [2, "paint brush"],
    roller: [3, "paint roller"],
    tape: [1, "tape measure"],
  },
  "lino-apparel": {
    dress: [3, "linen dress"],
    tshirt: [3, "t-shirt garment"],
    trousers: [6, "trousers garment"],
    coat: [4, "wool coat garment"],
    shirt: [4, "shirt garment"],
    sweater: [4, "knitted sweater"],
    bag: [3, "tote bag"],
    scarf: [2, "wool scarf"],
    sneaker: [2, "canvas sneakers"],
    boot: [3, "leather boots"],
    beanie: [1, "knitted beanie hat"],
  },
  "folio-books": {
    spread: [5, "open book pages"],
  },
  "tallo-garden": {
    plant: [2, "potted plant"],
    pot: [1, "terracotta flower pot"],
  },
};

const args = process.argv.slice(2);
const option = (name, fallback) => (args.includes(name) ? args[args.indexOf(name) + 1] : fallback);
const out = option("--out", "demo-images");
const contact = option("--contact", "");
const userAgent = `CatalogDemoImages/1.0 (one-off download of public-domain photos${contact ? `; ${contact}` : ""})`;
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
const clean = (text) =>
  (text ?? "").replace(/<[^>]+>/g, "").replace(/&amp;/g, "&").replace(/&quot;/g, '"').replace(/&#39;/g, "'").replace(/&lt;/g, "<").replace(/&gt;/g, ">").trim();

async function getJson(params) {
  const url = `${API}?${new URLSearchParams({ ...params, format: "json", maxlag: "5" })}`;
  for (let attempt = 0; ; attempt++) {
    const response = await fetch(url, { headers: { "User-Agent": userAgent } });
    if (response.ok) return response.json();
    if ((response.status === 429 || response.status === 503) && attempt < 5) {
      const wait = 10 * (attempt + 1);
      console.log(`  Commons is busy (${response.status}); waiting ${wait} s...`);
      await sleep(wait * 1000);
      continue;
    }
    throw new Error(`Commons answered ${response.status}`);
  }
}

async function search(terms, offset) {
  const data = await getJson({
    action: "query",
    generator: "search",
    gsrsearch: `${terms} filetype:bitmap ${LICENCE_FILTER}`,
    gsrnamespace: "6",
    gsrlimit: "40",
    gsroffset: String(offset),
    prop: "imageinfo",
    iiprop: "url|extmetadata|size|mime",
    iiurlwidth: "1200",
  });
  return Object.values(data.query?.pages ?? {}).sort((a, b) => (a.index ?? 0) - (b.index ?? 0));
}

async function download(url, path) {
  const response = await fetch(url, { headers: { "User-Agent": userAgent } });
  if (!response.ok) throw new Error(`download answered ${response.status}`);
  writeFileSync(path, Buffer.from(await response.arrayBuffer()));
}

const creditsPath = join(out, "CREDITS.md");
const credits = new Map();
mkdirSync(out, { recursive: true });
if (existsSync(creditsPath)) {
  for (const line of readFileSync(creditsPath, "utf8").split("\n")) {
    if (line.startsWith("| ") && !line.startsWith("| File") && !line.startsWith("| ---")) {
      credits.set(line.split("|")[1].trim(), line);
    }
  }
}
const used = new Set([...credits.values()].map((line) => line.split("|")[2].trim()));

for (const [slug, prefixes] of Object.entries(WANTED)) {
  mkdirSync(join(out, slug), { recursive: true });
  for (const [prefix, [count, terms]] of Object.entries(prefixes)) {
    const missing = [];
    for (let n = 1; n <= count; n++) {
      if (!existsSync(join(out, slug, `${prefix}-${n}.jpg`))) missing.push(n);
    }
    if (missing.length === 0) continue;
    console.log(`${slug}: ${prefix} x${missing.length} (${terms})`);

    let candidates = [];
    let offset = 0;
    while (missing.length > 0 && offset < 200) {
      if (candidates.length === 0) {
        candidates = await search(terms, offset);
        offset += 40;
        if (candidates.length === 0) break;
      }
      const page = candidates.shift();
      const info = page.imageinfo?.[0] ?? {};
      const source = info.descriptionurl ?? "";
      const licence = clean(info.extmetadata?.LicenseShortName?.value);
      const author = clean(info.extmetadata?.Artist?.value) || "Unknown";
      if (used.has(source) || !["image/jpeg", "image/png", "image/webp"].includes(info.mime)) continue;
      if (!ACCEPTED.some((l) => licence.toLowerCase().startsWith(l)) || (info.width ?? 0) < 600) continue;

      const name = `${prefix}-${missing[0]}.jpg`;
      try {
        await download(info.thumburl ?? info.url, join(out, slug, name));
      } catch (error) {
        console.log(`  could not download ${source}: ${error.message}`); // retried on the next run
        continue;
      }
      missing.shift();
      used.add(source);
      credits.set(`${slug}/${name}`, `| ${slug}/${name} | ${source} | ${author.replaceAll("|", "/")} | ${licence} |`);
      console.log(`  ${name} <- ${source} (${licence})`);
      await sleep(1000);
    }
    if (missing.length > 0) {
      console.log(`  only part found for ${prefix}; ${missing.length} missing. Re-run later or add them by hand.`);
    }
  }
}

const rows = [...credits.keys()].sort().map((key) => credits.get(key));
writeFileSync(
  creditsPath,
  "# Image credits\n\nEvery photo is public domain or CC0, from Wikimedia Commons.\n\n" +
    "| File | Source | Author | Licence |\n| --- | --- | --- | --- |\n" +
    rows.join("\n") + "\n",
);
console.log(`\nDone. Photos in ${out}/, credits in ${creditsPath}. Review them, zip the folder and send it.`);
