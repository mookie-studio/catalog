# Dashboard, Inquiries and statistics

## Dashboard

The **Dashboard** (`/admin/dashboard`) is the admin area's start page: where an Admin lands after signing in and where `/admin` leads.

### Counts
Each card links to the product list (or the inbox) already filtered:

| Card | Counts | Opens |
| --- | --- | --- |
| Draft | Products in Draft | Products, status Draft |
| Published | Products in Published | Products, status Published |
| Archived | Products in Archived | Products, status Archived |
| Featured | Products that are **both Published and Featured** | Products, Published and Featured |
| Open inquiries | Inquiries with status Open | Inquiries, filtered by Open |

### Needs attention
Quality warnings about **Published** products, each linking to the filtered product list so the Admin can fix them:
- "N published products have no images" → products with status Published and no Images.
- "N published products have no categories" → Published and no Category. **Hidden when the Tenant has no Categories at all**, so Admins are not nagged about a feature they don't use.
- A warning whose count is 0 is hidden; with none left the page says "Everything looks good."

### Most viewed products
A table of products ranked by Product views over a period: **Last 7 days**, **Last 30 days** (default) or **Last 90 days**, counted in UTC days and ending today. Columns: product (links to its edit page), status, **Views** and **Inquiries** received in the same period. Only products with at least one view or Inquiry appear, ordered by views, then Inquiries, then name; 10 per page. An empty period says "No views yet in this period." The period and page live in the address (`?period=7&page=2`).

The note under the title states the privacy rule: **views are counted per day without storing anything about visitors.**

## Inquiries

An **Inquiry** is a message a Visitor sends to a Tenant about one **Published** product, with their name, email and message. Catalog **does not send email** about Inquiries (see the backlog issue about email notifications); Admins read them in the inbox.

### Turning Inquiries on
**Profile → Inquiries → Accept Inquiries** (off by default). While off, the product pages show no form and the API answers 404 to Inquiry requests.

### What a Visitor sends
On a product page, **Ask about this product** opens a form with **Your name** (required, at most 100 characters), **Your email** (required, valid, at most 254) and **Message** (required, at most 2000). Errors appear next to the field. On success: "Thank you, your message was sent."

### Spam protection
- A hidden **honeypot** field (`website`) that people never fill: a request with it filled is answered like a success and silently discarded.
- A **rate limit per client IP**: by default 5 Inquiries per 10 minutes (`RateLimits__Inquiries__PermitLimit`, `RateLimits__Inquiries__WindowSeconds`). Beyond it the API answers 429 with `Retry-After` and the form says "Too many messages, try again later."

### The inbox
Open **Inquiries** (a badge in the navigation shows how many are Open). Newest first, paged, with a **Show** filter: Open or Handled (or all). Each Inquiry shows the visitor's name, email, message, date and the **product** it is about.
- **Mark handled** / **Reopen**: the status can go both ways.
- **Delete**: removes the Inquiry for good, so personal data is not kept longer than needed.

## Product views

A **Product view** is one opening of a Published product's public page. It is recorded by the SPA, once per product page opened, with one request that increases a counter for the product and the UTC day ([ADR 0012](../adr/0012-product-views-counted-per-day.md)).

- **Nothing about the Visitor is stored**: no IP, no cookie, no browser identifier. Nothing is kept in the Visitor's browser either.
- The numbers are **approximate by design**: reloading counts again; crawlers that don't run JavaScript aren't counted; one IP is limited (default 30 views per minute, `RateLimits__ProductViews__PermitLimit` / `__WindowSeconds`); a failed request (including a 429) is ignored silently.
- Views of an **Old slug** count for the current product. Views of Draft or Archived products are not recorded (404).
- **Unique visitors cannot be counted**; only views.
- The numbers are shown in the Dashboard's most-viewed table; the same data is available from `GET /api/admin/stats/products?from=yyyy-MM-dd&to=yyyy-MM-dd` (dates inclusive, at most 366 days, default the last 30; invalid ranges are 400).
