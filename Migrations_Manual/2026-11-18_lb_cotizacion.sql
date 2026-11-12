-- Cotizaciones comparativas: de lo pendiente de compra se pide precio a varios proveedores, se comparan lado a lado
-- y al adjudicar se generan las órdenes de compra (una por proveedor ganador).
CREATE SEQUENCE IF NOT EXISTS lb_cotizacion_correlativo START 1;

CREATE TABLE IF NOT EXISTS lb_cotizacion_solicitud (
    id                    bigserial PRIMARY KEY,
    codigo                varchar(30)  NOT NULL UNIQUE,
    almacen_id            integer      NOT NULL REFERENCES lb_almacen(id),
    estado                varchar(15)  NOT NULL DEFAULT 'ABIERTA',   -- ABIERTA | ADJUDICADA | CANCELADA
    observacion           text         NULL,
    motivo_adjudicacion   text         NULL,
    ordenes_generadas     text         NULL,                          -- códigos de OC separados por coma
    creado_por            bigint       NOT NULL,
    creado_en             timestamptz  NOT NULL DEFAULT now(),
    adjudicada_por        bigint       NULL,
    adjudicada_en         timestamptz  NULL
);

CREATE TABLE IF NOT EXISTS lb_cotizacion_item (
    id                          bigserial PRIMARY KEY,
    solicitud_id                bigint        NOT NULL REFERENCES lb_cotizacion_solicitud(id) ON DELETE CASCADE,
    producto_id                 bigint        NOT NULL,
    talla                       varchar(30)   NOT NULL DEFAULT '',
    color                       varchar(30)   NOT NULL DEFAULT '',
    cantidad                    numeric(12,2) NOT NULL,
    pedido_item_id              bigint        NULL,
    cotizacion_adjudicada_id    bigint        NULL
);
CREATE INDEX IF NOT EXISTS ix_lb_cotizacion_item_solicitud ON lb_cotizacion_item (solicitud_id);

CREATE TABLE IF NOT EXISTS lb_cotizacion (
    id              bigserial PRIMARY KEY,
    solicitud_id    bigint       NOT NULL REFERENCES lb_cotizacion_solicitud(id) ON DELETE CASCADE,
    proveedor_id    integer      NOT NULL,
    plazo_dias      integer      NULL,
    condicion_pago  varchar(120) NULL,
    observacion     text         NULL,
    creado_por      bigint       NOT NULL,
    creado_en       timestamptz  NOT NULL DEFAULT now(),
    UNIQUE (solicitud_id, proveedor_id)
);

CREATE TABLE IF NOT EXISTS lb_cotizacion_precio (
    id              bigserial PRIMARY KEY,
    cotizacion_id   bigint        NOT NULL REFERENCES lb_cotizacion(id) ON DELETE CASCADE,
    item_id         bigint        NOT NULL REFERENCES lb_cotizacion_item(id) ON DELETE CASCADE,
    precio_unitario numeric(14,4) NOT NULL,
    UNIQUE (cotizacion_id, item_id)
);

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'hyp_local') THEN
        GRANT ALL ON lb_cotizacion_solicitud, lb_cotizacion_item, lb_cotizacion, lb_cotizacion_precio TO hyp_local;
        GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO hyp_local;
    END IF;
END $$;
