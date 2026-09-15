-- Almacén / Kardex multi-almacén (CONTEXT_LOGISTICA.md sección 7, Fase 1 punto 3).
-- lb_almacen ya existe (Fase 1 punto 1, modelo de Personas) — acá solo stock y movimientos.

CREATE TABLE lb_stock (
    id              BIGSERIAL PRIMARY KEY,
    almacen_id      INT NOT NULL REFERENCES lb_almacen(id),
    producto_id     BIGINT NOT NULL REFERENCES lb_producto(id),
    -- '' como sentinela (no NULL) para productos sin talla: un UNIQUE con NULL nunca detecta
    -- duplicados en Postgres (NULL != NULL) — con '' sí funciona.
    talla           VARCHAR(10) NOT NULL DEFAULT '',
    cantidad_actual NUMERIC(12,2) NOT NULL DEFAULT 0,
    stock_minimo    NUMERIC(12,2) NOT NULL DEFAULT 0,
    stock_maximo    NUMERIC(12,2),
    actualizado_en  TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (almacen_id, producto_id, talla)
);

CREATE TABLE lb_movimiento (
    id              BIGSERIAL PRIMARY KEY,
    almacen_id      INT NOT NULL REFERENCES lb_almacen(id),
    producto_id     BIGINT NOT NULL REFERENCES lb_producto(id),
    talla           VARCHAR(10) NOT NULL DEFAULT '',
    tipo_movimiento VARCHAR(20) NOT NULL, -- INGRESO, SALIDA, TRANSFERENCIA (no usado aun, pero soportado)
    cantidad        NUMERIC(12,2) NOT NULL,
    costo_unitario  NUMERIC(12,2), -- nullable: Fase 3 (Compras) y PLE-SUNAT lo necesitan despues
    referencia_tipo VARCHAR(30), -- PEDIDO, COMPRA, ENTREGA_EPP, PRESTAMO_HERRAMIENTA
    referencia_id   BIGINT,
    usuario_sistema_id BIGINT REFERENCES lb_usuario_sistema(id),
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_movimiento_almacen_producto ON lb_movimiento(almacen_id, producto_id, creado_en);
CREATE INDEX idx_movimiento_referencia ON lb_movimiento(referencia_tipo, referencia_id);
