# Demo data is seeded through the API by a converging tool

The demo (ADR 0005) must open on realistic data and be reset after every showing. The Demo seeder (`api/tools/Catalog.DemoSeeder`) is a console tool that reads the Demo dataset (`demo/`) and makes every Demo tenant match it through the HTTP API: as a Platform Admin for Tenants, and as each Tenant's demo Admin for its profile, Logo, Categories, Attribute Definitions, products and Images. Every API rule applies, Thumbnails are generated (ADR 0008), and the same command works against local and production environments. Running it again converges instead of wiping: it creates what is missing, updates what differs, deletes extra Categories, Definitions and Options, and moves extra products to Archived (products are never deleted). It never runs at API start-up and never from the API process.

Only two things are written straight to the database, and only for Demo tenants:

- **Admin memberships** of the pre-created demo accounts. The API only adds Admins by sending an invitation email, and production email (Supabase's built-in SMTP) only reaches some addresses. The seeder creates the demo users in Supabase Auth with a password and inserts their membership.
- **Demo activity**: back-dated Inquiries and 30 days of Product views. The public endpoints record only "now" (ADR 0012) and are rate limited per IP.

A Tenant whose slug is in the dataset is treated as a Demo tenant only if its Admin list is empty or contains its dataset admin email; otherwise the seeder stops before changing anything.

## Considered Options

- **CSV import**: rejected; it does not handle Images, Categories or Options, nor Tenants and accounts.
- **Wiping the database and loading again**: rejected; it would delete real Tenants in a shared environment, and Tenants are never deleted.
- **Seeding endpoints in the API**: rejected; dead, privileged code in production.
- **SQL scripts for everything**: rejected; they bypass validation, cannot generate Thumbnails or upload to image storage, and break silently when rules change.

## Consequences

- The seeder needs the database connection string and the Supabase service role key, so it is run by the operator, never by the pipeline.
- Its SQL for memberships and activity must follow the schema; its tests run it against the real API and database, so a schema change that breaks it fails CI.
- What a demo leaves behind that the API cannot undo stays: Admins invited during a demo, Old slugs reserved by renames (ADR 0009).
