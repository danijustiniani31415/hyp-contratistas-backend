-- Comprobantes de compra leídos con IA (factura, boleta, nota de crédito…) y sus líneas.
-- Un mismo (proveedor, tipo, serie, número) no se puede cargar dos veces.

CREATE TABLE IF NOT EXISTS lb_comprobante_compra (
    id                              bigserial PRIMARY KEY,
    proveedor_id                    integer       NOT NULL REFERENCES lb_proveedor(id),
    almacen_id                      integer       NULL     REFERENCES lb_almacen(id),
    tipo                            varchar(30)   NOT NULL,
    serie                           varchar(20)   NOT NULL,
    numero                          varchar(30)   NOT NULL,
    fecha_emision                   date          NOT NULL,
    moneda                          varchar(3)    NOT NULL DEFAULT 'PEN',
    tipo_cambio                     numeric(10,4) NULL,
    subtotal                        numeric(14,2) NOT NULL DEFAULT 0,
    igv                             numeric(14,2) NOT NULL DEFAULT 0,
    total                           numeric(14,2) NOT NULL DEFAULT 0,
    forma_pago                      varchar(60)   NULL,
    detraccion_monto                numeric(14,2) NULL,
    orden_compra_ref                varchar(60)   NULL,
    guia_ref                        varchar(60)   NULL,
    archivo_url                     text          NULL,
    observacion                     text          NULL,
    modelo_ia                       varchar(60)   NULL,
    estado                          varchar(20)   NOT NULL DEFAULT 'REGISTRADO',
    creado_por_usuario_sistema_id   bigint        NOT NULL,
    creado_en                       timestamptz   NOT NULL DEFAULT now(),
    CONSTRAINT uq_lb_comprobante_compra UNIQUE (proveedor_id, tipo, serie, numero)
);

CREATE TABLE IF NOT EXISTS lb_comprobante_compra_item (
    id                      bigserial PRIMARY KEY,
    comprobante_compra_id   bigint        NOT NULL REFERENCES lb_comprobante_compra(id) ON DELETE CASCADE,
    linea                   integer       NOT NULL,
    descripcion             text          NOT NULL,
    cantidad                numeric(14,3) NOT NULL,
    unidad                  varchar(20)   NULL,
    precio_unitario         numeric(14,4) NOT NULL DEFAULT 0,
    total                   numeric(14,2) NOT NULL DEFAULT 0,
    destino                 varchar(20)   NOT NULL DEFAULT 'GASTO',
    producto_id             bigint        NULL,
    talla                   varchar(20)   NOT NULL DEFAULT '',
    color                   varchar(40)   NOT NULL DEFAULT '',
    categoria_gasto         varchar(60)   NULL,
    movimiento_registrado   boolean       NOT NULL DEFAULT false
);

CREATE INDEX IF NOT EXISTS ix_lb_comprobante_compra_item_comprobante ON lb_comprobante_compra_item (comprobante_compra_id);
CREATE INDEX IF NOT EXISTS ix_lb_comprobante_compra_fecha ON lb_comprobante_compra (fecha_emision DESC);
