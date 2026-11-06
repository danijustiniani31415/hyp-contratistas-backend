-- Base de la Fase C (control de costos) y de la aprobación por ítem. Esta migración faltaba: las tablas
-- lb_contrato y lb_partida solo se habían creado a mano en la base local, y el resto de los SQL del
-- 2026-09-26 (lb_partida_presupuesto_moneda, lb_pedido_item_historial) dependen de ellas.
-- Debe correrse ANTES de los demás archivos 2026-09-26_*. Es idempotente y solo agrega: el código que hoy está
-- en producción no usa nada de esto y sigue funcionando igual.

CREATE TABLE IF NOT EXISTS lb_contrato (
    id             SERIAL PRIMARY KEY,
    proyecto_id    INTEGER      NOT NULL REFERENCES lb_proyecto(id),
    codigo         VARCHAR(30)  NOT NULL UNIQUE,
    descripcion    VARCHAR(200),
    vigente_desde  DATE         NOT NULL,
    vigente_hasta  DATE,
    activo         BOOLEAN      NOT NULL DEFAULT true
);

CREATE TABLE IF NOT EXISTS lb_partida (
    id           SERIAL PRIMARY KEY,
    proyecto_id  INTEGER      NOT NULL REFERENCES lb_proyecto(id),
    codigo       VARCHAR(30)  NOT NULL,
    nombre       VARCHAR(150) NOT NULL,
    activo       BOOLEAN      NOT NULL DEFAULT true,
    UNIQUE (proyecto_id, codigo)
);

ALTER TABLE lb_pedido ADD COLUMN IF NOT EXISTS contrato_id INTEGER REFERENCES lb_contrato(id);

-- Aprobación por ítem. Los pedidos que ya existen se traducen al esquema nuevo UNA sola vez (al crear la
-- columna estado): lo que ya pasó por Visar queda VISADO, lo ya aprobado/entregado queda APROBADO, con la
-- cantidad que se había pedido, y lo rechazado queda RECHAZADO. Los PENDIENTE y CANCELADO conservan el default.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_schema = current_schema()
                     AND table_name = 'lb_pedido_item' AND column_name = 'estado') THEN
        ALTER TABLE lb_pedido_item ADD COLUMN partida_id        INTEGER REFERENCES lb_partida(id);
        ALTER TABLE lb_pedido_item ADD COLUMN cantidad_aprobada NUMERIC(12,2);
        ALTER TABLE lb_pedido_item ADD COLUMN estado            VARCHAR(20) NOT NULL DEFAULT 'PENDIENTE';
        ALTER TABLE lb_pedido_item ADD COLUMN motivo_rechazo    VARCHAR(200);

        UPDATE lb_pedido_item i
           SET estado = 'VISADO', cantidad_aprobada = i.cantidad_solicitada
          FROM lb_pedido p
         WHERE p.id = i.pedido_id AND p.estado = 'PENDIENTE_GERENTE';

        UPDATE lb_pedido_item i
           SET estado = 'APROBADO', cantidad_aprobada = i.cantidad_solicitada
          FROM lb_pedido p
         WHERE p.id = i.pedido_id AND p.estado IN ('APROBADO', 'ENTREGADO');

        UPDATE lb_pedido_item i
           SET estado = 'RECHAZADO'
          FROM lb_pedido p
         WHERE p.id = i.pedido_id AND p.estado = 'RECHAZADO';
    END IF;
END $$;
