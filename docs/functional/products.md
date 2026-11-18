# Products

A **Product** is an item of the catalog: name, description, SKU, images, status and attributes. It has no price.

## The product list

Open **Products**. The list is paged (20 per page) and sorted by most recently updated.

| Filter | Behaviour |
| --- | --- |
| Search | Matches part of the **name or SKU**, case-insensitive |
| Status | Draft, Published or Archived |
| Category | Products directly in that Category |
| Featured only | Only Featured products |
| Images | With images / Without images |
| Has categories | With categories / Without categories |

Filters combine and live in the address (for example `/admin/products?status=Published&hasImages=false`), so a link opens the list already filtered: the [Dashboard](./dashboard-inquiries-and-stats.md) uses this. Changing a filter goes back to page 1. Featured products show a star.

## Create

**New product** asks for **Name** (required, at most 200 characters) and **SKU** (required, at most 64 characters, unique within the Tenant; other Tenants may reuse it). The product starts as a **Draft**. Two products may share a name.

The **Product slug**, the readable part of the public address, is generated from the name: lowercase, accents removed, anything else turned into single hyphens, at most 80 characters (`Hammer drill 500W` → `hammer-drill-500w`). If taken, a numeric suffix is added (`hammer`, `hammer-2`).

## Edit

The product page has sections for the main fields, **Attributes**, **Categories**, **Images**, status and Featured. A product can be edited in **any** status, including Archived.

- **Name, SKU, Description** (at most 5000 characters), **Product slug**.
- The slug does **not** change when the name changes; only an explicit edit changes it. Rules: required, at most 100 characters, lowercase letters, digits and single hyphens, unique within the Tenant (409 on `slug`). Editing the SKU to one already in use is 409 on `sku`.
- When the slug changes, the previous one becomes an **Old slug** reserved for this product: old links keep working and lead to the new address, and no other product of the Tenant can take it (409). Going back to one of the product's own Old slugs is allowed.
- Attributes and Categories: see [Catalog setup](./catalog-setup.md).

## Status

```
Draft ⇄ Published ⇄ Archived      (any transition is allowed)
```

- Only **Published** products are visible to Visitors. Drafts and Archived products answer 404 on every public endpoint.
- **Publish** requires **name, SKU and description** (400 otherwise, naming the missing field). Each time a product moves to Published, its publication date is set to now (used by the public "Newest" sort).
- Products are **archived instead of deleted**; there is no delete.
- A product can be published with **no images**; a default image is shown.

## Images

Upload JPG, PNG or WebP (detected by content), at most **2 MB** each, at most **5 per product**. A wrong format, oversized, corrupt file or sixth image is a 400 on `file` with a clear message.

- The **first** image is the **Main image**, shown in listings. **Reorder** changes it; the order must list every image of the product exactly once (otherwise 400).
- **Delete** removes the file and closes the gap in the order.
- On upload the API also generates a **Thumbnail** (WebP, at most 480 px wide, never enlarged) used wherever images appear in listings; the original is kept for the product page ([ADR 0008](../adr/0008-thumbnails-generated-by-the-api.md)). Images uploaded before Thumbnails existed fall back to the original.
- Images are stored in object storage (Cloudflare R2 in production), never in the database.

## Featured

An Admin can mark a product **Featured** (checkbox on the product page, or in bulk). Featured products appear first in the public catalog and in the "Featured" strip on the catalog home. It only matters while the product is Published. A duplicate is never Featured.

## Duplicate

**Duplicate** creates a new **Draft** copy: description, Attributes, Categories and Images (the files are copied, original and Thumbnail, with new keys). The name becomes `"{name} (copy)"` (trimmed to the maximum length), the SKU `"{sku}-COPY"` (then `-COPY-2`, `-COPY-3`... until unique) and the slug is generated from the new name. The edit page of the copy opens.

## Bulk actions

In the product list, tick products (the header checkbox selects the whole page; the selection is per page and is cleared when you change page or filter). A bar shows **"N selected"** with:

| Action | Effect |
| --- | --- |
| Publish | Publishes each product that is publishable; the others are reported with the reason (for example "A Published product requires a description.") |
| Archive | Moves to Archived |
| Move to draft | Moves to Draft |
| Mark as Featured / Unmark as Featured | Sets or clears Featured |
| Add to category… / Remove from category… | Adds or removes **only that Category**, keeping the product's other ones. Hidden when the Tenant has no Categories |

- Between 1 and 100 distinct products per action (400 otherwise).
- **Each product succeeds or fails on its own**, with the same rules as the single-product change. The summary says "N updated, M failed" and names every failed product with its reasons. A product already in the target state counts as updated and is left unchanged. Ids of other Tenants are reported as "Product not found." and never modified.

## CSV export

**Export CSV** downloads **all** the Tenant's products, in any status, in the format the import reads.

- UTF-8 with a BOM, comma-separated, RFC 4180 quoting, first row is the header.
- Fixed columns: `sku`, `name`, `description`, `status`, `slug`, `featured`, `categories`; then one column per Attribute Definition, named exactly as the Definition.
- Several values in one cell (Categories, multi-select Options) are separated by `|`. A Category is written as its path joined by ` > ` (`Tools > Power tools > Drills`). Booleans are `true`/`false`, dates `yyyy-MM-dd`, numbers use `.` as decimal separator.
- Images are not exported.

## CSV import

**Import CSV** loads or updates products from a file in the same format. Images are never imported, and Categories and Options are never created by an import.

1. Choose a file (at most **1 MB** and **1000 data rows**) and run a **preview** (`dryRun=true`): nothing is saved.
2. The preview lists, per row, what would be **created**, **updated** or **skipped**, with row number, column and reason for each error.
3. Apply the import: valid rows are saved in **one transaction**; invalid rows are skipped and reported.

Matching and field rules:
- Rows are matched by **SKU**: an existing SKU updates that product; a new SKU creates a **Draft** with a generated slug. Importing the same file twice never duplicates products.
- `status` and `slug` are only for export; they are **ignored** on import. `featured` is applied.
- A **missing column** leaves that field untouched on updates; an **empty cell** clears it (except `name`, which is required).
- Each row is validated with the same rules as the product screens (lengths, types, Option membership). An unknown Category path or unknown Option is an **error** for that row, not silently created, so typos don't pollute the catalog.
- **Unknown columns** are reported as warnings and ignored.
- A file over the limits, without `sku` and `name` columns, or not readable as CSV is rejected as a whole (400 on `file`).
