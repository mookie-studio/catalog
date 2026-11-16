# Three route groups, and an Admin's tenant comes from their session

The API is split into `/api/public/{tenantSlug}/...` (Visitor, anonymous), `/api/admin/...` (Admin) and `/api/platform/...` (Platform Admin). In admin routes the tenant never travels in the URL or the body: it is taken from the user-tenant membership in Catalog's database (ADR 0004) and feeds the EF Core global filter (ADR 0003). An admin therefore cannot target another tenant by tampering with a request.

## Consequences

- Only `/api/platform` may operate across tenants, and it is the only place where bypassing the global filter is allowed.
- Common conventions: errors as ProblemDetails, page/size pagination (max 50), dates in UTC.
