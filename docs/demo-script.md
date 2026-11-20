# Demo script (15 minutes)

A demo of Catalog on the Demo dataset. Load or reset it first ([demo data](demo.md)): every step below assumes the Demo tenants exactly as the dataset describes them. Reset after the demo with the same command.

Accounts (passwords from your password manager, see [demo data, settings](demo.md#2-settings)):

| Account | Used for |
| --- | --- |
| No account (Visitor) | Public catalogs |
| `admin@forja-hardware.example.com` | Admin area of Forja Hardware |
| `admin@lino-apparel.example.com`, `admin@folio-books.example.com` | Optional, to show isolation |
| The demo Platform Admin (`Demo:PlatformAdminEmail`) | Platform Admin area |

Use a private browser window for the Visitor and a normal one for the Admin, so both sessions stay open.

## 1. One platform, three very different businesses (3 min, Visitor)

1. Open `/forja-hardware`. Point out the Brand color in the header and buttons, the Logo and the description: each Tenant brands its own catalog.
2. Featured products come first. Switch between **grid** and **list** view.
3. Search for `drill`. Then filter by **Cordless = yes** and **Voltage = 18 V** (filterable Attributes, defined by the Admin, not by the platform).
4. Open **Screwdriver set, 12 pieces**: several Images and the Thumbnails. Then open **Cordless drill driver 18 V**: Attributes of every type (Power, Voltage, Cordless, Weight, Material, In stock since), and **Related products** at the bottom (products that share its Category).
5. Open `/lino-apparel`: same platform, a clothing shop. Filter by **Sizes** (multi-select) and **Color**.
6. Open `/folio-books`: a bookshop, with Author, Pages, Publication date and Language. Same product model, no "product types": everything is Attributes (ADR 0002).

## 2. A Visitor asks a question (1 min, Visitor)

1. On `/folio-books`, open **The Glass Alibi** and click **Ask about this product**.
2. Send a short question. It arrives in the Admin's Inquiry inbox; nothing is emailed (no email notifications yet).

## 3. Running a catalog (6 min, Admin of Forja Hardware)

1. Sign in at `/login` as `admin@forja-hardware.example.com`. The admin area opens on the **Dashboard**: 22 Published, 5 Draft, 3 Archived, 5 Featured, Open Inquiries, and the quality warnings "Published without Images" and "without Category".
2. Click the "without Images" warning: the product list opens filtered. Open one, upload an Image, and see the Thumbnail appear.
3. **Most viewed products** on the Dashboard and the **Stats** page: views per product over 7, 30 or 90 days, with Inquiries; nothing about Visitors is stored (ADR 0012).
4. **Inquiries**: the Open ones from the last weeks; mark one as Handled.
5. Product list: select the five Drafts, run the **Bulk action** "Publish". The summary shows which ones succeeded and which failed with the reason (a Draft without description cannot be published).
6. **Duplicate** a product: a new Draft with the same Attributes, Categories and Images.
7. **Export CSV** of the products; mention the CSV import with a dry run.
8. **Catalog setup**: the Category tree (move Categories up, down or under another one), the Attribute Definitions and their Options, and the **Tenant profile** (change the Brand color and show the public catalog change).

## 4. Isolation (1 min)

1. Sign in as `admin@lino-apparel.example.com` in another browser: only Lino Apparel's products, Inquiries and statistics; nothing of Forja Hardware is visible, even by guessing ids (404).

## 5. The platform (3 min, Platform Admin)

1. Sign in as the demo Platform Admin and open `/platform`: the list of Tenants.
2. **Tallo Garden** is deactivated: its catalog at `/tallo-garden` shows "not found" and its Admin cannot sign in. Reactivate it, open `/tallo-garden` (it works), and deactivate it again.
3. Show **New Tenant** and inviting its first Admin (do not send real invitations during the demo).
4. Mention **Change slug**: old links keep working (Old slugs, ADR 0009). If you change a Demo tenant's slug, change it back before the reset.

## 6. After the demo (1 min)

Reset with `dotnet run --project api/tools/Catalog.DemoSeeder -- seed`: the published Drafts go back to Draft, the duplicate and anything created end Archived, the profile and Inquiries return to the dataset. See [what a reset restores](demo.md#5-resetting-after-a-demo).
