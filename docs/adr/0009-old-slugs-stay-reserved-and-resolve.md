# Old slugs stay reserved and resolve to the current address

When a Tenant slug or Product slug changes, the previous value is recorded as an Old slug. Public endpoints accept an Old slug and answer as if the current slug had been used; their response always carries the current slug, and the SPA replaces the address in the browser with it. Because the public catalog is a SPA served by Cloudflare Pages, the API returns data rather than HTTP redirects.

An Old slug stays reserved for its owner: another tenant cannot take an old Tenant slug, and another product of the same tenant cannot take an old Product slug (generation skips it, an explicit edit gets 409). The owner can go back to one of its own Old slugs, which then stops being an Old slug.

## Considered Options

- **HTTP 301 from the API**: rejected; the browser calls the API with fetch, so a redirect would not change the page address.
- **Letting anyone reuse an Old slug**: rejected; old public links would silently show someone else's catalog or product.

## Consequences

- Old Tenant slugs are global (unique across tenants); Old Product slugs are unique per tenant.
- Old slugs are never deleted automatically.
- Deactivated tenants and unpublished products stay 404 through an Old slug as well.
