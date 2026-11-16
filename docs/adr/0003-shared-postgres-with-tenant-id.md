# A single shared PostgreSQL database, isolated by TenantId

All tenants share one PostgreSQL database. Every business table carries a TenantId, and EF Core applies a global query filter per tenant so no query can skip it by accident. Chosen for cost and operational simplicity: one migration, one connection, nothing to provision per tenant.

## Considered Options

- **One database per tenant**: stronger isolation, rejected for the cost of operating and migrating N databases.
- **One schema per tenant**: a middle ground, rejected for the same migration complexity.

## Consequences

- A cross-tenant leak can only come from bypassing the global filter (`IgnoreQueryFilters`), so its use must be exceptional and reviewed.
- Per-tenant uniqueness (SKU, Product slug) is enforced with composite unique indexes (TenantId, field).
