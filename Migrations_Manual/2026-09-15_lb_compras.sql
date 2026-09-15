-- Compras (Fase 3, punto 7 de CONTEXT_LOGISTICA.md sección 3). Sin DDL en el documento — el
-- rol COMPRAS ya existía desde Fase 1 (seccion 4.3) precisamente para esto. A diferencia de
-- Pedidos, una orden de compra no tiene aprobación de Gerencia (es la función propia del rol
-- COMPRAS) y se recibe en partes: un proveedor rara vez entrega todo junto.

CREATE TABLE lb_proveedor (
    id              SERIAL PRIMARY KEY,
    razon_social    VARCHAR(200) NOT NULL,
    ruc             VARCHAR(11) UNIQUE,
    contacto        VARCHAR(100),
    telefono        VARCHAR(20),
    email           VARCHAR(150),
    activo          BOOLEAN NOT NULL DEFAULT true
);

CREATE TABLE lb_orden_compra (
    id              BIGSERIAL PRIMARY KEY,
    codigo          VARCHAR(20) NOT NULL UNIQUE, -- OC-2026-000001
    proveedor_id    INT NOT NULL REFERENCES lb_proveedor(id),
    almacen_id      INT NOT NULL REFERENCES lb_almacen(id), -- a donde llega la mercaderia
    solicitado_por_usuario_sistema_id BIGINT NOT NULL REFERENCES lb_usuario_sistema(id),
    estado          VARCHAR(20) NOT NULL DEFAULT 'PENDIENTE', -- PENDIENTE, RECIBIDA_PARCIAL, RECIBIDA, CANCELADA
    observacion     TEXT,
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_orden_compra_estado ON lb_orden_compra(estado);

CREATE TABLE lb_orden_compra_item (
    id                  BIGSERIAL PRIMARY KEY,
    orden_compra_id     BIGINT NOT NULL REFERENCES lb_orden_compra(id),
    producto_id         BIGINT NOT NULL REFERENCES lb_producto(id),
    talla               VARCHAR(10) NOT NULL DEFAULT '',
    cantidad_solicitada NUMERIC(12,2) NOT NULL,
    costo_unitario      NUMERIC(12,2) NOT NULL,
    -- se acumula con cada recepcion parcial; NULL/0 = nada recibido todavia
    cantidad_recibida   NUMERIC(12,2) NOT NULL DEFAULT 0
);
CREATE INDEX idx_orden_compra_item_orden ON lb_orden_compra_item(orden_compra_id);

-- Bitacora de cada recepcion (una orden puede recibirse en varias entregas del proveedor) —
-- sin esto no hay forma de responder "¿cuándo llegó cada parte, y quién la recibió?".
CREATE TABLE lb_orden_compra_recepcion (
    id                  BIGSERIAL PRIMARY KEY,
    orden_compra_item_id BIGINT NOT NULL REFERENCES lb_orden_compra_item(id),
    cantidad            NUMERIC(12,2) NOT NULL,
    recibido_por_usuario_sistema_id BIGINT NOT NULL REFERENCES lb_usuario_sistema(id),
    creado_en           TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_oc_recepcion_item ON lb_orden_compra_recepcion(orden_compra_item_id);

CREATE SEQUENCE lb_orden_compra_correlativo START 1;

INSERT INTO lb_permiso (codigo, descripcion) VALUES
('COMPRA_CREAR',   'Crear órdenes de compra'),
('COMPRA_RECIBIR', 'Registrar recepción de mercadería de una orden de compra (repone stock)');

INSERT INTO lb_rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id FROM lb_rol r, lb_permiso p
WHERE p.codigo IN ('COMPRA_CREAR', 'COMPRA_RECIBIR')
  AND r.codigo IN ('COMPRAS', 'ADMIN');
