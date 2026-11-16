# Catalog

Plataforma generalista de catálogos de productos: cada negocio gestiona sus productos desde una parte de administración y sus clientes los consultan desde una parte pública. No vende: no hay precios, carrito, pedidos ni stock.

## Language

### Plataforma

**Tenant**:
Un negocio aislado dentro de la plataforma, con su propio catálogo, productos y administradores. Ningún dato de un tenant es visible desde otro.
_Avoid_: Cliente (choca con el visitante de la parte pública), empresa, cuenta

**Tenant desactivado**:
Tenant suspendido por el Superadministrador: sus administradores no pueden entrar y su catálogo público no se ve. Es reversible; los tenants no se borran.

**Identificador del tenant**:
Nombre corto y único de un tenant que forma parte de la dirección de su catálogo público (por ejemplo `ferreteria-pepe`).
_Avoid_: Código, alias

**Superadministrador**:
Persona que gestiona la plataforma: da de alta los tenants y su primer administrador, y es el único que puede cambiar el Identificador del tenant. No pertenece a ningún tenant.
_Avoid_: Root, admin global

**Administrador**:
Persona autenticada que gestiona el catálogo y a los demás administradores de un único tenant. En la fase 1 hay un único rol.
_Avoid_: Usuario, admin user

**Visitante**:
Persona anónima que consulta el catálogo público de un tenant.
_Avoid_: Cliente, usuario final

### Catálogo

**Producto**:
Elemento del catálogo con nombre, descripción, SKU, imágenes, estado y atributos dinámicos. No tiene precio. Puede pertenecer a varias categorías o a ninguna.
_Avoid_: Artículo, ítem, referencia

**Identificador del producto**:
Nombre legible y único dentro del tenant que forma parte de la dirección pública del producto (por ejemplo `taladro-percutor-500w`). Se genera a partir del nombre (desambiguado si se repite) y solo cambia si el administrador lo edita; es independiente del SKU. El nombre del producto no es único.
_Avoid_: Slug, URL, permalink

**Definición de atributo**:
Atributo que el administrador declara en su tenant antes de usarlo: nombre, tipo (texto, número, sí/no, fecha o lista de opciones) y si es filtrable. Vale para todo el tenant; cada producto rellena solo las que le aplican. El tipo no cambia una vez creada.
_Avoid_: Campo, plantilla

**Atributo**:
Valor que un producto da a una Definición de atributo (por ejemplo "Color: Rojo"). Es la vía para describir cualquier particularidad del negocio, incluidas tallas y colores.
_Avoid_: Metadato, metadata, propiedad, campo personalizado

**Opción**:
Cada valor permitido de una Definición de atributo de tipo lista. Una lista puede ser de selección única o múltiple ("Tallas: S, M, L").
_Avoid_: Valor posible, enumerado

**Atributo filtrable**:
Definición de atributo por la que el Visitante puede filtrar el catálogo público.

**Variante**:
No existe como concepto. Las diferencias como talla o color se expresan con Atributos del mismo Producto.

**SKU**:
Código que identifica un producto de forma única dentro de su tenant, obligatorio desde la creación y editable. Dos tenants pueden repetir el mismo SKU.
_Avoid_: Referencia, código de producto

**Imagen**:
Fotografía o ilustración asociada a un producto. Las imágenes de un producto están ordenadas; un producto puede no tener ninguna.

**Imagen principal**:
La primera imagen de un producto, la que se muestra en los listados.
_Avoid_: Portada, thumbnail

**Categoría**:
Agrupación opcional de productos para navegar, organizada en árbol (Herramientas > Eléctricas > Taladros). El administrador decide el orden entre categorías hermanas. Una categoría sin productos publicados en ella ni en sus subcategorías no se muestra al Visitante. El catálogo funciona igual sin ninguna categoría.
_Avoid_: Familia, sección

**Estado del producto**:
Fase del ciclo de vida de un producto: Borrador, Publicado o Archivado. Solo los productos Publicados son visibles para el Visitante. Se puede pasar de cualquier estado a cualquier otro; un producto se archiva en lugar de borrarse.

**Publicar**:
Pasar un producto a Publicado. Exige nombre, SKU y descripción; un borrador solo exige nombre y SKU. Un producto se puede editar en cualquier estado.
_Avoid_: Activar, dar de alta
