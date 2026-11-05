-- Alias de producto: otras formas de escribir el nombre («ARCO SIERRA», «ARCOSIERRA»). El sistema los aprende
-- al vincular textos de un Excel a un producto y al unificar duplicados. alias_norm (sin tildes, espacios ni
-- signos) es único: una misma escritura no puede apuntar a dos productos.
CREATE TABLE IF NOT EXISTS lb_producto_alias (
    id          BIGSERIAL PRIMARY KEY,
    producto_id BIGINT       NOT NULL REFERENCES lb_producto(id),
    alias       VARCHAR(200) NOT NULL,
    alias_norm  VARCHAR(200) NOT NULL UNIQUE,
    creado_en   TIMESTAMPTZ  NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS idx_producto_alias_producto ON lb_producto_alias(producto_id);
