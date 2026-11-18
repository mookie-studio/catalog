# Catalog setup

What an Admin prepares before (and while) loading products: Attribute Definitions, Categories and the Tenant profile. All three are optional; a catalog with only products works.

## Attribute Definitions

Attributes are how **any business-specific detail** is described, including sizes and colors ([ADR 0002](../adr/0002-no-variants-or-product-types.md)). An **Attribute Definition** is declared once for the whole Tenant; each product then fills in only the definitions that apply to it. Every attribute value is optional.

Open **Attributes** in the admin area (list paged, 20 per page).

### Create
Fields: **Name** (required, at most 100 characters, unique within the Tenant: a duplicate is 409 on `name`), **Type**, **Filterable** and, for option lists, **Allows multiple**.

| Type | Value a product gives | Example |
| --- | --- | --- |
| Text | Free text, at most 500 characters | Material: "Oak" |
| Number | A number (`.` as decimal separator) | Power (W): 500 |
| Boolean | Yes / No | Cordless: Yes |
| Date | A calendar date (`yyyy-MM-dd`) | Release date |
| OptionList | One or several of the Definition's Options | Sizes: S, M, L |

- The **type cannot change** once created, so existing values never become invalid.
- **Allows multiple** exists only for OptionList (a single-select list stores one Option, a multi-select list several). It cannot be set on other types (400).
- **Filterable** makes the Definition a filter in the public catalog (see [Public catalog](./public-catalog.md)). It can be switched on and off later, together with the name, with **Update**.

### Options (option lists)
- **Add option**: value required, at most 100 characters, unique within the Definition (409 on `value`). Options keep the order in which they were added. Options can only be added to OptionList definitions (400).
- **Delete option**: if no product uses it, it is deleted. If products use it, the API answers 409 with `usageCount`; the interface shows "N product(s) use it" and asks to confirm. On confirmation (`confirm=true`) the Option is removed from those products' values and deleted.

### Delete a Definition
Same flow: unused → deleted; used → 409 with `usageCount`, then a confirmed delete removes the Definition **and its values from every product** that had one.

### On a product
Values are set from the product's edit page, section **Attributes**, and validated against the Definition: a number must be numeric, a date a valid date, an option must belong to the Definition and a single-select list takes at most one. An unknown Definition id or invalid value is a 400 naming the attribute. Setting an empty value clears it.

## Categories

Optional grouping of products for navigation, organized as a **tree** (Tools > Power tools > Drills). A product may belong to several Categories or none; the catalog works exactly the same with no Categories at all.

Open **Categories** in the admin area. The tree is returned whole (it is not paginated).

- **Create**: **New top-level Category**, or **Add** under an existing one. Name required, at most 100 characters. Names need not be unique. A missing parent is 400 on `parentId`.
- **Rename**.
- **Move** to another parent (or to the top level) at a given position among its siblings (`position` 0 or greater). A Category cannot be moved under itself or one of its own descendants (400 on `parentId`), so the tree never has cycles.
- **Order**: the Admin chooses the order of siblings; navigation shows them in that order.
- **Delete**: allowed only for a Category without subcategories (otherwise 409 "A Category with subcategories cannot be deleted."). Its products simply leave it; they are not deleted.
- **Visitors** only see a Category that has at least one **Published** product in it or in its subcategories; empty branches are hidden.
- Assigning products to Categories: from the product page, section **Categories**, or in bulk from the product list ([Products](./products.md)).

## Tenant profile

What a Tenant shows about itself in its public catalog. Open **Profile**. All fields are optional; empty strings are stored as nothing.

| Field | Rule |
| --- | --- |
| Description | At most 2000 characters. Shown in the catalog's about/contact section and used as page description |
| Contact email | Valid email, at most 254 characters |
| Phone | At most 40 characters |
| Address | At most 300 characters |
| Website | Absolute `http` or `https` URL, at most 300 characters |
| Brand color | `#RRGGBB`. Applied to the public header and buttons; text on it is automatically black or white, whichever is more readable (WCAG relative luminance). Without it the default theme is used |
| Logo | See below |
| Accept Inquiries | Switch, **off by default**. Visitors can only send Inquiries while it is on |

Invalid values are a 400 naming the field (`contactEmail`, `websiteUrl`, `brandColor`), shown next to it in the form. **Save profile** shows "Saved."

The Tenant's **name** and **Tenant slug** are not part of the profile; only a Platform Admin changes them.

### Logo
JPG, PNG or WebP only (detected by content, not by file name or declared type), at most **1 MB**. SVG is not accepted (script risk). Uploading a new Logo replaces and deletes the previous file; **Remove** deletes it. The Logo is shown in the public header next to the name.
