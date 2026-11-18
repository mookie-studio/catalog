# Product views are counted per product and per day, with no personal data

Admins want to know which products interest Visitors. A Product view is recorded when the SPA opens a Published product's public page: it calls `POST /api/public/{tenantSlug}/products/{productSlug}/views`, and the API adds one to a counter row keyed by product and UTC day (`ProductViewCount`: `TenantId`, `ProductId`, `Day`, `Count`). Nothing about the Visitor is stored: no IP, no cookie, no user agent, no session id.

The count is approximate on purpose. It is recorded by the SPA, so crawlers that do not run JavaScript are not counted, and a per-IP rate limit (in memory, like Inquiries) stops one client from inflating it. Reloading the page counts again.

## Considered Options

- **One row per view with IP or session**: rejected; it is personal data (GDPR consent and retention duties) and grows without bound on free-tier PostgreSQL (ADR 0005).
- **External analytics (Google Analytics, Plausible, Cloudflare Web Analytics)**: rejected; it needs cookies or a third party per tenant, and Admins could not see the numbers inside Catalog.
- **Counting on `GET` of the product endpoint**: rejected; GETs are also made by prefetching and caches, and must stay free of side effects.

## Consequences

- Storage is one small row per product per day with at least one view.
- Unique Visitors cannot be counted; only views.
- Counters are kept forever; deleting old rows can be added later without changing the API.
