-- Toma de inventario físico: se cuenta, se compara contra el sistema y al cerrar se generan los ajustes
-- (movimientos INGRESO/SALIDA con referencia INVENTARIO) en el kardex. Idempotente.

CREATE TABLE IF NOT EXISTS lb_inventario (
    id                 bigserial PRIMARY KEY,
    almacen_id         integer      NOT NULL,                       -- lb_almacen.id (sin FK para poder aplicar con un usuario sin REFERENCES)
    fecha              date         NOT NULL,
    estado             varchar(12)  NOT NULL DEFAULT 'EN_CONTEO',   -- EN_CONTEO, CERRADO, ANULADO
    observacion        text         NULL,
    creado_por         bigint       NULL,
    creado_en          timestamptz  NOT NULL DEFAULT now(),
    cerrado_por        bigint       NULL,
    cerrado_en         timestamptz  NULL,
    cantidad_ajustes   integer      NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS lb_inventario_item (
    id                 bigserial PRIMARY KEY,
    inventario_id      bigint        NOT NULL REFERENCES lb_inventario(id) ON DELETE CASCADE,
    producto_id        bigint        NOT NULL,
    talla              varchar(10)   NOT NULL DEFAULT '',
    color              varchar(40)   NOT NULL DEFAULT '',
    cantidad_sistema   numeric(12,2) NOT NULL DEFAULT 0,            -- stock al abrir el conteo
    cantidad_contada   numeric(12,2) NULL,                          -- null = todavía no contado
    cantidad_ajuste    numeric(12,2) NULL,                          -- lo realmente ajustado al cerrar (contada - stock vigente)
    motivo             varchar(200)  NULL,
    UNIQUE (inventario_id, producto_id, talla, color)
);

CREATE INDEX IF NOT EXISTS ix_lb_inventario_almacen ON lb_inventario (almacen_id, estado);
