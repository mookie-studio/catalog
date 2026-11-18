# Rules and limits

Every limit the API enforces, in one place. Going over a limit is a 400 with the field named, unless stated otherwise.

## Text lengths

| Field | Limit |
| --- | --- |
| Tenant name | 100 characters |
| Tenant slug | 3 to 50 characters; lowercase letters, digits and single hyphens; unique across the platform |
| Tenant description | 2000 |
| Contact email / Website / Address / Phone | 254 / 300 / 300 / 40 |
| Brand color | `#RRGGBB` |
| Product name | 200 |
| Product SKU | 64; unique per Tenant |
| Product description | 5000 |
| Product slug | 100 (generated slugs: 80); lowercase letters, digits and single hyphens; unique per Tenant |
| Attribute Definition name | 100; unique per Tenant |
| Option value | 100; unique per Definition |
| Text attribute value | 500 |
| Category name | 100 |
| Inquiry name / email / message | 100 / 254 / 2000 |

## Files

| What | Formats | Size | Count |
| --- | --- | --- | --- |
| Product Image | JPG, PNG, WebP (by content) | 2 MB | 5 per product |
| Tenant Logo | JPG, PNG, WebP (by content; no SVG) | 1 MB | 1 per Tenant |
| Thumbnail (generated) | WebP, quality 80 | at most 480 px wide, never enlarged | one per Image |
| CSV import | UTF-8 CSV | 1 MB | 1000 data rows |

## Counts and paging

| Rule | Value |
| --- | --- |
| Page size | default 20, maximum 50 (larger values are capped) |
| Bulk action | 1 to 100 distinct products |
| Related products | up to 4 shown on the product page |
| Featured strip | up to 8 on the catalog home |
| Statistics range | at most 366 days, default the last 30; Dashboard periods 7/30/90 days |
| Sitemap | at most 49 999 products |

## Rate limits (per client IP, fixed window)

| What | Default | Settings |
| --- | --- | --- |
| Inquiries | 5 per 600 s | `RateLimits__Inquiries__PermitLimit`, `RateLimits__Inquiries__WindowSeconds` |
| Product views | 30 per 60 s | `RateLimits__ProductViews__PermitLimit`, `RateLimits__ProductViews__WindowSeconds` |

Over the limit: **429** ProblemDetails "Too many requests" with a `Retry-After` header. The client IP is read from `X-Forwarded-For`, because the API runs behind the host's reverse proxy.

## Status codes

| Code | Meaning |
| --- | --- |
| 200 / 201 / 202 / 204 | Success (202: accepted with no body, used by Inquiries and Product views) |
| 400 | Invalid input; `errors` lists problems by field |
| 401 | Not signed in |
| 403 | Signed in but not allowed (for example Tenant deactivated) |
| 404 | Not found, **or** belongs to another Tenant, **or** not public (Draft, Archived, deactivated Tenant) |
| 409 | Uniqueness clash (SKU, slug, Attribute name, Option) or a delete that needs confirmation (`usageCount`) or is refused (Category with subcategories) |
| 429 | Rate limit |
