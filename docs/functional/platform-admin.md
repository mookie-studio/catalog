# Platform Admin

The Platform Admin runs the installation: it creates the businesses (Tenants) and invites their first Admin. It belongs to no Tenant and cannot see or edit a Tenant's products, Categories or Inquiries. The only place where data of all Tenants is read is the `/platform` area.

## Who is a Platform Admin

A Platform Admin is a Supabase user whose id is listed in the API setting `Auth__PlatformAdminUserIds__0` (add `__1`, `__2`... for more). It is configuration, not data: nothing in the interface makes someone a Platform Admin. Creating the first one is covered step by step in [infra-setup.md §1.7](../infra-setup.md) (production) and [local-development.md](../local-development.md) (local).

Sign in at `/login`; a Platform Admin lands on `/platform/tenants`.

## Tenants

A Tenant is an isolated business with its own catalog, products and Admins.

### Create a Tenant
1. Open **Tenants**, fill the **New Tenant** form: **Name** (1 to 100 characters) and **Tenant slug**.
2. The **Tenant slug** is part of the public address (`/{tenantSlug}`). Rules: 3 to 50 characters, lowercase letters, digits and single hyphens between them (`acme-hardware`), unique across the whole platform. A slug that is in use, or that is an Old slug reserved for another Tenant, is refused with 409 on `slug`.
3. Click **Create tenant**. The Tenant starts **active** and empty.

### Invite the first Admin
1. In the Tenant's row click **Manage**, open **Admins**, type the **Admin email** and click **Invite**.
2. The API asks the identity provider (Supabase) to send an invitation email with a link to `/accept-invite`. The person chooses a password there and lands in the admin area. See [Access and team](./access-and-team.md).
3. More Admins can be invited the same way, or by the Tenant's own Admins.
4. The list shows each Admin's email (paged).

### List Tenants
The list is paged and shows name, Tenant slug, state and creation date.

### Deactivate and reactivate
- **Deactivate** suspends a Tenant: its Admins can no longer use the admin area (admin requests answer 403 "Tenant deactivated"), and its public catalog, product pages, Sitemap and all public endpoints answer 404.
- **Reactivate** reverses it completely. Nothing is lost: deactivation never deletes data.
- Tenants are **never deleted**.

### Change a Tenant slug
Only a Platform Admin can change it (Admins cannot rename their public address).
- The new slug follows the same rules as on creation, and must not belong to another Tenant or be reserved as another Tenant's Old slug (409).
- The previous slug becomes an **Old slug** reserved for this Tenant: public links that use it keep working and lead to the current address ([ADR 0009](../adr/0009-old-slugs-stay-reserved-and-resolve.md)). The browser address is replaced with the current slug, keeping the rest of the path and the query string.
- The Tenant can go back to one of its own Old slugs, which then stops being an Old slug.

## What a Platform Admin cannot do
Read or edit a Tenant's products, Attribute Definitions, Categories, Inquiries or statistics; delete a Tenant; see Visitors' data (there is none: Product views store nothing about the Visitor).
