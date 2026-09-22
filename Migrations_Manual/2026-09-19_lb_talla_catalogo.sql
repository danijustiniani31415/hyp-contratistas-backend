-- Catálogo fijo de tallas por tipo (CONTEXT_LOGISTICA.md sección 6, ampliación de Pedidos).
-- Mismo patrón de "atributo de variante" que Odoo/SAP: el producto sigue siendo un solo maestro
-- en lb_producto (Producto.TipoTalla dice qué catálogo de tallas aplica), y la talla real se
-- registra en lb_stock/lb_movimiento/lb_pedido_item — no se duplica el producto por talla.
-- Correr DESPUÉS de esta migración y ANTES de seed_lb_producto_materiales.sql.

ALTER TABLE lb_producto ADD COLUMN IF NOT EXISTS tipo_talla VARCHAR(20);

CREATE TABLE IF NOT EXISTS lb_talla (
    id          SERIAL PRIMARY KEY,
    tipo_talla  VARCHAR(20) NOT NULL,
    valor       VARCHAR(10) NOT NULL,
    orden       INT NOT NULL,
    UNIQUE (tipo_talla, valor)
);

INSERT INTO lb_talla (tipo_talla, valor, orden) VALUES
    ('ROPA', 'XS', 1),
    ('ROPA', 'S', 2),
    ('ROPA', 'M', 3),
    ('ROPA', 'L', 4),
    ('ROPA', 'XL', 5),
    ('ROPA', 'XXL', 6),
    ('CALZADO', '35', 1),
    ('CALZADO', '36', 2),
    ('CALZADO', '37', 3),
    ('CALZADO', '38', 4),
    ('CALZADO', '39', 5),
    ('CALZADO', '40', 6),
    ('CALZADO', '41', 7),
    ('CALZADO', '42', 8),
    ('CALZADO', '43', 9),
    ('CALZADO', '44', 10),
    ('GUANTES', '6', 1),
    ('GUANTES', '7', 2),
    ('GUANTES', '8', 3),
    ('GUANTES', '9', 4),
    ('GUANTES', '10', 5),
    ('GUANTES', '11', 6)
ON CONFLICT (tipo_talla, valor) DO NOTHING;
