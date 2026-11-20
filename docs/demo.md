# Demo data: load and reset

The **Demo seeder** (`api/tools/Catalog.DemoSeeder`) makes every **Demo tenant** of an environment match the **Demo dataset** committed in `demo/` ([ADR 0014](adr/0014-demo-data-seeded-through-the-api.md); terms in [GLOSSARY.md](../GLOSSARY.md)). It works the same against a local environment and production. Running it again **resets the demo**: whatever was changed during a demo is put back. What to show, and in which order, is in the [demo script](demo-script.md).

The seeder is run by a person, from a computer with the .NET 10 SDK. It is never run by a pipeline and never by the API.

## 1. What the Demo dataset contains

| Demo tenant | Business | Status | Products (Published / Featured / Draft / Archived) | Inquiries (Open / Handled) | Views per product per day |
| --- | --- | --- | --- | --- | --- |
| `forja-hardware` (Forja Hardware) | Hardware store | Active | 30 (22 / 5 / 5 / 3) | 8 (5 / 3) | 0 to 12 |
| `lino-apparel` (Lino Apparel) | Clothing | Active | 30 (22 / 5 / 5 / 3) | 8 (4 / 4) | 1 to 15 |
| `folio-books` (Folio Books) | Bookshop | Active | 30 (22 / 5 / 5 / 3) | 8 (6 / 2) | 0 to 10 |
| `tallo-garden` (Tallo Garden) | Garden centre | **Deactivated** | 3 (3 / 0 / 0 / 0) | none | 0 to 3 |

Every active Demo tenant has:

- A **Tenant profile**: a two-paragraph description, contact email `hello@<slug>.example.com`, a fictional phone and address, website `https://<slug>.example.com`, a **Logo**, a **Brand color** and Inquiries enabled.
- A two-level **Category** tree (Forja: Power tools, Hand tools, Fixings, Painting; Lino: Women, Men, Accessories, Shoes; Folio: Fiction, Non-fiction, Children).
- Six **Attribute Definitions** covering every type: Text, Number, Boolean, Date, a single-select and a multi-select option list, some of them filterable (Forja: Power, Voltage, Cordless, Weight, Material, In stock since; Lino: Sizes, Color, Material, Season, Organic, Collection date; Folio: Author, Pages, Publication date, Language, Hardcover, Formats).
- At least three Published products without Images and two without Categories, so the Dashboard quality warnings have something to show, and at least one product with every Attribute filled in.
- **Related products** appear on their own, because products share Categories.
- 30 days of **Product views** (Featured products get three times more) and eight **Inquiries** from the last four weeks.

One demo **Admin** per Demo tenant, `admin@<slug>.example.com`, plus one demo **Platform Admin** whose email you choose. Their passwords are settings (below), never committed.

### Files

| Path | Content |
| --- | --- |
| `demo/tenants.json` | The Demo tenants: `slug`, `name`, `isActive`, `adminEmail` |
| `demo/<slug>/profile.json` | Tenant profile, `inquiriesEnabled` and `logo` (a file name) |
| `demo/<slug>/catalog.json` | `categories` (tree of `name` and `children`) and `attributes` (`name`, `type`, `isFilterable`, `allowsMultiple`, `options`) |
| `demo/<slug>/products.json` | Products: `sku`, `name`, `description`, `status`, `featured`, `categories` (paths like `"Power tools/Drills"`), `attributes` (by Definition name; Options by text), `images` (file names, first is the Main image) |
| `demo/<slug>/activity.json` | `inquiries` (`sku`, `name`, `email`, `message`, `status`, `daysAgo`) and `views` (`days`, `minPerDay`, `maxPerDay`) |
| `demo/images/<slug>/` | Photos, Logos and book covers (JPEG/PNG, at most 2 MB each, at most 5 per product) |
| `demo/images/CREDITS.md` | Source, author and licence of every image |
| `demo/tools/download-images.mjs` | Downloads the public-domain/CC0 photos from Wikimedia Commons, with their credits |
| `demo/tools/generate_images.py` | Generates the Logos and the book covers (invented titles), released as CC0 |

## 2. Settings

Every setting is required. The seeder checks all of them before changing anything and names each one that is missing. Set them as environment variables (`Demo__<Name>`) or as user secrets of the seeder project (`Demo:<Name>`):

```sh
cd api/tools/Catalog.DemoSeeder
dotnet user-secrets set "Demo:ApiUrl" "http://localhost:8080"
# ...one line per setting
```

| Setting | What it is | Local value | Production value |
| --- | --- | --- | --- |
| `ApiUrl` | Base URL of the Catalog API | compose: `http://localhost:8080`; Aspire: the `api` endpoint shown in the dashboard | `https://<app-name>.azurewebsites.net` |
| `SupabaseUrl` | Supabase project URL | `http://127.0.0.1:54321` | `https://<project-ref>.supabase.co` ([infra-setup 1.1](infra-setup.md#11-create-the-project)) |
| `SupabaseAnonKey` | Publishable (anon) key, used to sign in | `ANON_KEY` from `supabase status -o env` | **Project Settings** → **API Keys** → publishable key (`sb_publishable_...`) |
| `SupabaseServiceRoleKey` | Secret key, used to create the demo accounts | `SERVICE_ROLE_KEY` from `supabase status -o env` | **Project Settings** → **API Keys** → secret key (`sb_secret_...`) |
| `DatabaseConnectionString` | The catalog database (for Admin memberships and Demo activity only) | compose: `Host=localhost;Port=54322;Database=postgres;Username=postgres;Password=postgres`; Aspire: the `catalog` resource's connection string in the dashboard | the Session pooler connection string of [infra-setup 1.2](infra-setup.md#12-database-connection-string) |
| `PlatformAdminEmail` | Email of the demo Platform Admin | any, for example `demo-platform@example.com` | any address you will use to sign in during demos |
| `PlatformAdminPassword` | Its password (at least 12 characters) | any | a strong password, kept in a password manager |
| `AdminPassword` | Password of every demo Admin (at least 12 characters) | any | a strong password, kept in a password manager |

Never commit these values. Share the demo passwords only through a password manager.

## 3. First run, locally

1. Start the local environment as described in [local development](local-development.md) (Supabase CLI, then Aspire or docker compose) and apply the migrations.
2. Set the settings of section 2.
3. Run the seeder from the repository root:

   ```sh
   dotnet run --project api/tools/Catalog.DemoSeeder -- seed
   ```

4. The first run creates the demo Platform Admin in Supabase (if it does not exist) and then stops with:

   `The demo Platform Admin (user id <id>) is not a Platform Admin. Add it to Auth:PlatformAdminUserIds (Auth__PlatformAdminUserIds__<n> on App Service) and restart the API.`

   Add that id to the API's Platform Admins and restart the API:
   - **compose**: set `PLATFORM_ADMIN_USER_ID=<id>` in `.env` (or use your own Platform Admin's email and password as `PlatformAdminEmail`/`PlatformAdminPassword`, so no change is needed) and run `docker compose up -d api`.
   - **Aspire**: `dotnet user-secrets set "Parameters:platform-admin-user-id" "<id>"` in `api/src/Catalog.AppHost`, then restart the AppHost.
5. Run the seeder again. It loads everything (about 550 changes on an empty environment; uploading the Images takes a minute or two) and ends with `Total changes: N`.
6. Open the SPA: `http://localhost:5173/forja-hardware`, `/lino-apparel`, `/folio-books`; sign in at `/login` as `admin@forja-hardware.example.com` with `AdminPassword`, or as the demo Platform Admin.

## 4. First run, in production

1. Finish the [infrastructure setup](infra-setup.md) and the first deploy.
2. Set the settings of section 2 with the production values, on your computer (never in GitHub).
3. Run the seeder once: it creates the demo Platform Admin in Supabase and stops with its user id (as in step 3.4). You can also reuse your existing Platform Admin's email and password and skip the next step.
4. In the Azure portal, open the Web App → **Settings** → **Environment variables** → **App settings**, click **Add**, name `Auth__PlatformAdminUserIds__1` (use the next free number; `__0` is your own Platform Admin), value the printed id, **Apply** and confirm the restart.
5. Run the seeder again.
6. Check: `https://<spa-domain>/forja-hardware` shows the catalog with its Brand color and Logo; `https://<spa-domain>/tallo-garden` shows the "not found" page (deactivated).

Run it outside demo hours: the App Service Free plan has 60 CPU minutes a day, and the first run generates about 100 Thumbnails. Images go to Cloudflare R2 (a few MB).

## 5. Resetting after a demo

Run the same command:

```sh
dotnet run --project api/tools/Catalog.DemoSeeder -- seed
```

What it puts back, for every Demo tenant:

- The Tenant is active again for the run (so its Admin can be used), and deactivated again at the end if the dataset says so (`tallo-garden`).
- The Tenant profile, Brand color and Inquiries switch. The Logo is uploaded again only when it was removed (or with `--force-images`).
- Categories: missing ones are created, extra ones deleted, order restored. A Category renamed during a demo is seen as an extra one (deleted) plus a missing one (created).
- Attribute Definitions and Options: missing ones created, extra ones deleted (with their values), filterable flag restored; a Definition whose type or multi-select flag differs is deleted and created again.
- Products, matched by SKU: name, description, SKU, Attributes, Categories, Featured flag and Product status. Products created during a demo (an SKU not in the dataset) are moved to **Archived**; products are never deleted.
- Images: a product whose number of Images differs from the dataset gets all its Images deleted and uploaded again.
- Demo activity: all Inquiries and Product views of the Demo tenant are deleted and written again, with dates relative to today (Inquiries sent during the demo disappear).

What it does not put back:

- Admins invited during a demo stay Admins (remove them in **Team**).
- **Old slugs**: a product whose Product slug was changed keeps its new slug (the seeder does not set slugs) and the old one stays reserved (ADR 0009). If a Platform Admin changed a Demo tenant's **Tenant slug**, the seeder cannot find it and stops with `The Tenant slug '<slug>' is reserved by another Tenant (an Old slug)...`: change the slug back in the Platform Admin area (a Tenant may go back to one of its own Old slugs), then run the seeder again.
- Products duplicated during a demo end Archived, not deleted.
- Tenants created during a demo, and every Tenant that is not in the dataset, are never touched.
- The Tenant name: the API cannot rename Tenants, so a different name is only reported in the log.

## 6. Options, log and errors

```
dotnet run --project api/tools/Catalog.DemoSeeder -- seed [--dataset <dir>] [--skip-activity] [--force-images]
```

| Option | Effect |
| --- | --- |
| `--dataset <dir>` | Use another Demo dataset folder. By default, the first `demo` folder with a `tenants.json` found walking up from the current directory. |
| `--skip-activity` | Do not rewrite Inquiries and Product views. |
| `--force-images` | Upload every product Image and Logo again (for example after replacing photos with files of the same name). |

The log prints one line per change (`<slug>: product created: FH-0001`, `category deleted`, `image uploaded`...), one line per Demo tenant with the Demo activity written, a summary per Demo tenant and finally `Total changes: N`. A run on an environment that already matches the dataset prints `Total changes: 0` (Demo activity is rewritten every time and is not counted). Exit code 0 means success, 1 any failure.

### Common errors

| Message | Cause and fix |
| --- | --- |
| `The Demo seeder cannot start: Demo__X is not set.` | A setting is missing (section 2). |
| `Demo__AdminPassword must be at least 12 characters long.` | Use a longer password. |
| `The Demo dataset is not valid: ...` | A dataset file is wrong; each line names the file and the JSON path (for example `forja-hardware/products.json $[3].attributes.Voltage: expected one Option (12 V, 18 V, 230 V).`). Nothing was changed. |
| `The demo Platform Admin (user id ...) is not a Platform Admin...` | Add the id to `Auth:PlatformAdminUserIds` and restart the API (sections 3 and 4). |
| `Tenant '<slug>' exists and is not a Demo tenant; nothing was changed.` | A real Tenant uses a Demo tenant's slug (it has Admins and none of them is the dataset `adminEmail`). Change the slug in `demo/tenants.json` (and the folders), or the real Tenant's slug. |
| `The Tenant slug '<slug>' is reserved by another Tenant (an Old slug)...` | A Demo tenant's slug was changed during a demo; change it back (section 5). |
| `<email> is already an Admin of another Tenant` | A demo Admin email is an Admin elsewhere; remove it there or change `adminEmail`. |
| `Supabase could not create the user ...: 4xx ...` | Wrong `SupabaseUrl` or secret key, or Supabase rejected the password (password rules in **Authentication** → **Providers** → **Email**). |
| `Supabase could not sign in as ...` | Wrong password for an existing account, or the email provider is disabled. |
| `GET /api/... answered 503` | The API cannot reach its database (see [infra-setup, Verify](infra-setup.md#7-verify)). |
| `password authentication failed` / `connection refused` | Wrong `DatabaseConnectionString`. |

## 7. Changing the dataset

- **Add a product**: add an entry to `demo/<slug>/products.json` with a new SKU; reference existing Category paths, Definition names and Option texts; put its images in `demo/images/<slug>/` and add a line to `CREDITS.md`.
- **Add or replace a photo**: only public-domain or CC0 images (no brands, logos or faces); at most 2 MB; run with `--force-images` if you replaced a file with the same name. `node demo/tools/download-images.mjs --out demo-images` downloads a fresh set with credits. The script only searches; review every downloaded photo by hand before committing it, and discard any that shows people, a brand or logo, or is a document or artwork rather than the product. A product without a suitable photo keeps an empty `images` list (it then also feeds the "Published without Images" Dashboard warning).
- **Regenerate Logos or book covers**: `python3 demo/tools/generate_images.py` (needs Pillow).
- **Add a Demo tenant**: add it to `tenants.json` and create its folder; the slug must follow the Tenant slug rules ([Platform Admin](functional/platform-admin.md)).
- CI checks the committed dataset (`The_committed_Demo_dataset_is_valid`) and runs the seeder twice in the end-to-end job, so a broken file fails the pull request.

## 8. Optional: a separate demo environment

To keep demo data away from real Tenants entirely, create a second Supabase project and a second App Service (or run the demo locally) following the [infrastructure setup](infra-setup.md) again, and point the seeder's settings at it. Nothing in the seeder changes.
