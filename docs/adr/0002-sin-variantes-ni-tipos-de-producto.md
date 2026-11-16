# Sin variantes ni tipos de producto: todo particular va en Atributos del tenant

Para que Catalog sirva a cualquier negocio sin acoplarse a ninguno, el Producto tiene un núcleo fijo (nombre, descripción, SKU, imágenes, estado) y todo lo demás se expresa con Atributos. Las Definiciones de atributo son de todo el tenant, opcionales y tipadas; las listas de opciones admiten selección múltiple, que es como se representan tallas o colores. No existe la Variante (cada talla/color con su SKU) ni el Tipo de producto (plantilla de atributos por clase de producto).

## Considered Options

- **Variantes con SKU propio**: descartado; Catalog no vende ni gestiona stock, así que no necesita distinguir unidades vendibles.
- **Tipo de producto como plantilla de atributos**: descartado en fase 1; atar atributos a tipos o categorías complica el modelo y rompe el uso sin categorías.
- **Pares clave/valor libres**: descartado; sin definiciones no se puede validar ni filtrar.

## Consequences

- Sin atributos obligatorios en fase 1: un atributo de todo el tenant no puede exigirse a productos a los que no aplica.
- El tipo de una Definición de atributo no cambia tras crearla; borrar una opción o definición en uso elimina esos valores de los productos, previa confirmación.
