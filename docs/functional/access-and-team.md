# Access and team

## Invitations and sign-in

Public sign-up is disabled. The only way into the admin area is an **invitation** by email, sent by a Platform Admin (first Admin of a Tenant) or by an Admin of the same Tenant.

### Accepting an invitation
1. The invited person receives an email from the identity provider (Supabase) with a link.
2. The link opens `/accept-invite`, titled **Choose your password**. They type the password twice and click **Save password**.
3. They are signed in and land on the **Dashboard** (`/admin/dashboard`).
4. An invitation link works for a limited time, set by Supabase. An expired link needs a new invitation.

### Signing in and out
- `/login` asks for **Email** and **Password**. A Platform Admin lands on `/platform/tenants`, an Admin on `/admin/dashboard`. If the person was sent to the sign-in page from a protected address, they return to it after signing in.
- **Sign out** is in the header of the admin and platform areas.
- A signed-out person who opens `/admin` or `/platform` is sent to `/login`.
- A signed-in person who is neither an Admin nor a Platform Admin sees the **No access** page ("Your account does not have access to this area."). If their Tenant is deactivated, the API answers 403 with the title "Tenant deactivated" and the admin area is unusable until a Platform Admin reactivates it.

## How the Tenant is decided

The Admin's Tenant comes from their **session** (the membership stored by Catalog for their user id), never from the address or the request body. An Admin therefore cannot name another Tenant, and a request that tries to touch another Tenant's resource simply gets 404.

Authentication belongs to Supabase; **authorization belongs to Catalog** ([ADR 0004](../adr/0004-supabase-authenticates-catalog-authorizes.md)): the list of who administers which Tenant is in Catalog's own database.

## Team (the Admins of a Tenant)

Open **Team** in the admin area. There is a single Admin role: every Admin can do everything in their Tenant.

- **List**: the Admins of the Tenant, paged, with their email.
- **Invite**: type an email and click **Invite**. The person receives the invitation described above. Inviting an email that is already an Admin of any Tenant is refused with 409 on `email` ("This email is already an Admin of a Tenant."): a person administers exactly one Tenant. A malformed email is a 400.
- **Remove**: removes the person's access to the Tenant. An Admin **cannot remove themselves** (400), so a Tenant is never left without a way in by accident.
