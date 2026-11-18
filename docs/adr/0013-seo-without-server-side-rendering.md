# SEO without server-side rendering

The public catalog is a SPA on Cloudflare Pages (ADR 0005), and server-side rendering stays out of scope. To still be findable, Catalog does three cheap things:

1. **Page metadata in the SPA.** Each public page renders its `<title>`, `<meta name="description">` and Open Graph tags (`og:title`, `og:description`, `og:image`) with React 19's built-in document metadata. Google runs JavaScript and reads them.
2. **A Sitemap per tenant, served by the API.** `GET /api/public/{tenantSlug}/sitemap.xml` lists the catalog home and every Published product with its `lastmod`. Addresses point to the SPA, using the configured `PublicSite:BaseUrl`. A sitemap index at `GET /api/public-sitemap.xml` lists the Sitemaps of every active tenant.
3. **`robots.txt` in the SPA**, generated at build time, allows everything except `/admin`, `/platform`, `/login` and `/accept-invite`, and points to the sitemap index on the API host. Search engines accept a sitemap on another host when `robots.txt` points to it.

## Considered Options

- **Server-side rendering or prerendering**: rejected for now; it needs a server or build-time knowledge of every tenant's products, which a free static host does not give.
- **Cloudflare Pages Functions to inject tags**: postponed; it adds a second runtime to maintain for little gain while Google renders JavaScript.

## Consequences

- Link previews in apps that do not run JavaScript (WhatsApp, Slack, X) show the generic SPA title, not the product. Accepted for now.
- The API needs `PublicSite:BaseUrl` in every environment that serves sitemaps.
