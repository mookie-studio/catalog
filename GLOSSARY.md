# Catalog

A general-purpose, multi-tenant product catalog platform: each business manages its products from an admin area, and its visitors browse them in a public catalog. It does not sell: there are no prices, cart, orders or stock.

## Language

### Platform

**Tenant**:
An isolated business inside the platform, with its own catalog, products and admins. No data of a tenant is visible from another tenant.
_Avoid_: Customer, client, company, account

**Tenant slug**:
Short, unique name of a tenant that is part of its public catalog address (e.g. `acme-hardware`). Only a Platform Admin can change it.
_Avoid_: Code, alias, handle

**Deactivated tenant**:
A tenant suspended by a Platform Admin: its admins cannot sign in and its public catalog is not visible. Deactivation is reversible; tenants are never deleted.

**Platform Admin**:
A person who manages the platform: creates tenants and their first admin. Belongs to no tenant.
_Avoid_: Superadmin, root, global admin

**Admin**:
An authenticated person who manages the catalog and the other admins of exactly one tenant. Phase 1 has a single admin role.
_Avoid_: User, tenant user, manager

**Visitor**:
An anonymous person browsing a tenant's public catalog.
_Avoid_: Customer, client, end user

### Catalog

**Product**:
An item of the catalog with a name, description, SKU, images, status and attributes. It has no price. It may belong to several categories or none.
_Avoid_: Article, item, listing

**Product slug**:
Readable name, unique within the tenant, that is part of the product's public address (e.g. `hammer-drill-500w`). Generated from the name (disambiguated if repeated) and only changes when an admin edits it; independent from the SKU. Product names are not unique.
_Avoid_: URL, permalink, handle

**SKU**:
Code that uniquely identifies a product within its tenant; required from creation and editable. Two tenants may use the same SKU.
_Avoid_: Reference, product code

**Attribute Definition**:
An attribute declared by an admin for the whole tenant before it is used: name, type (text, number, boolean, date or option list) and whether it is filterable. Each product fills in only the ones that apply. Its type cannot change once created.
_Avoid_: Field, custom field, template

**Attribute**:
The value a product gives to an Attribute Definition (e.g. "Color: Red"). Attributes are how any business-specific detail is described, including sizes and colors. All attributes are optional.
_Avoid_: Metadata, property

**Option**:
Each allowed value of an option-list Attribute Definition. An option list is either single-select or multi-select ("Sizes: S, M, L").
_Avoid_: Enum value, choice

**Filterable attribute**:
An Attribute Definition visitors can filter the public catalog by.

**Variant**:
Not a concept in Catalog. Differences such as size or color are Attributes of the same product.

**Image**:
A picture attached to a product. A product's images are ordered; a product may have none.

**Main image**:
The first image of a product, shown in listings.
_Avoid_: Cover, thumbnail

**Category**:
Optional grouping of products for navigation, organized as a tree (Tools > Power tools > Drills). Admins choose the order of sibling categories. A category with no published products in it or its subcategories is hidden from visitors. The catalog works the same with no categories at all.
_Avoid_: Family, section

**Product status**:
Lifecycle stage of a product: Draft, Published or Archived. Only Published products are visible to visitors. Any transition is allowed; products are archived instead of deleted. A product can be edited in any status.

**Publish**:
To move a product to Published. Requires name, SKU and description; a draft only requires name and SKU.
_Avoid_: Activate, go live
