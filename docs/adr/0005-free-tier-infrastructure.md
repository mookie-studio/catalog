# Free-tier infrastructure for the demo: Supabase, Cloudflare R2, Azure Container Apps and Cloudflare Pages

To keep the demo at zero cost, PostgreSQL lives in the Supabase project (the same one used for authentication), images in Cloudflare R2 served from a public Cloudflare domain, the .NET API in Azure Container Apps with scale to zero, and the SPA in Cloudflare Pages. Each piece was chosen for its free tier and for avoiding lock-in: the API reaches PostgreSQL through EF Core (not the Supabase client) and R2 through its S3-compatible API, behind Catalog's own storage interface.

## Considered Options

- **Supabase Storage or Azure Blob for images**: rejected for a smaller (1 GB) or temporary (12 months) free tier.
- **Azure App Service F1 for the API**: rejected for its daily CPU limits.

## Consequences

- With scale to zero, the first request after an idle period takes a few seconds.
- Images are uploaded through the API (not signed URLs): JPG, PNG or WebP, at most 2 MB per image and 5 images per product, no thumbnails in phase 1.
- Locally everything runs in Docker (docker compose): Supabase PostgreSQL and authentication, the API, the SPA and an S3-compatible store instead of R2, with no dependency on cloud services.
- CI with GitHub Actions on every pull request; deployment on merge to `main`, with EF Core migrations applied by the pipeline and never at API start-up.
