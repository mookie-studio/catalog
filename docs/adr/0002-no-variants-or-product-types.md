# No variants or product types: everything business-specific is a tenant Attribute

To serve any business without coupling to one, a Product has a fixed core (name, description, SKU, images, status) and everything else is expressed with Attributes. Attribute Definitions belong to the whole tenant, are optional and typed; option lists can be multi-select, which is how sizes or colors are represented. There is no Variant (each size/color with its own SKU) and no Product Type (an attribute template per class of product).

## Considered Options

- **Variants with their own SKU**: rejected; Catalog does not sell or track stock, so it does not need to distinguish sellable units.
- **Product type as an attribute template**: rejected for phase 1; tying attributes to types or categories complicates the model and breaks catalogs without categories.
- **Free key/value pairs**: rejected; without definitions nothing can be validated or filtered.

## Consequences

- No required attributes in phase 1: a tenant-wide attribute cannot be required of products it does not apply to.
- An Attribute Definition's type cannot change after creation; deleting an option or definition in use removes those values from products, after confirmation.
