# Supabase Authentication authenticates; Catalog authorizes

For the demo, the identity of Platform Admins and Admins is managed by Supabase Authentication: sign-in, passwords and email invitations, with public sign-up disabled. The API only validates the Supabase token. Which tenant and role each person has is stored in Catalog's own database, in a membership table mapping the Supabase user id to a tenant and role. Switching identity providers then only affects token validation and the invitation flow.

## Considered Options

- **ASP.NET Core Identity**: no external dependency, rejected for the demo because of the work of building sign-in, password recovery and invitations.
- **Role and tenant as claims inside Supabase**: rejected because it ties authorization to the provider.

## Consequences

- Admins are invited by Catalog's API calling Supabase's invite endpoint with the service key, which never reaches the browser.
- Supabase RLS policies are not used: tenant isolation is guaranteed by the API (ADR 0003).
