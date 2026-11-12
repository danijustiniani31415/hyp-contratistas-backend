-- Fase 2 · Recepción con control en compras: por cada entrega se declara qué llegó bien y qué llegó dañado; al cerrar el
-- saldo lo que no llegó queda como faltante. Dañado/faltante/sobrante generan una diferencia que logística resuelve.
ALTER TABLE lb_orden_compra_recepcion ADD COLUMN IF NOT EXISTS cantidad_danada numeric(12,2) NOT NULL DEFAULT 0;
ALTER TABLE lb_orden_compra_recepcion ADD COLUMN IF NOT EXISTS motivo varchar(30) NULL;
ALTER TABLE lb_orden_compra_recepcion ADD COLUMN IF NOT EXISTS observacion text NULL;
ALTER TABLE lb_orden_compra_item ADD COLUMN IF NOT EXISTS cantidad_faltante numeric(12,2) NOT NULL DEFAULT 0;

CREATE TABLE IF NOT EXISTS lb_compra_diferencia (
    id                    bigserial PRIMARY KEY,
    orden_compra_id       bigint        NOT NULL REFERENCES lb_orden_compra(id),
    orden_compra_item_id  bigint        NOT NULL REFERENCES lb_orden_compra_item(id),
    recepcion_id          bigint        NULL REFERENCES lb_orden_compra_recepcion(id),
    tipo                  varchar(10)   NOT NULL,              -- FALTANTE | DANADO | SOBRANTE
    cantidad              numeric(12,2) NOT NULL,
    motivo                varchar(30)   NULL,
    observacion           text          NULL,
    estado                varchar(10)   NOT NULL DEFAULT 'ABIERTA', -- ABIERTA | RESUELTA
    resolucion            varchar(20)   NULL,                  -- REPOSICION | RECLAMO | NOTA_CREDITO | ACEPTADO
    resolucion_nota       text          NULL,
    resuelta_por          bigint        NULL,
    resuelta_en           timestamptz   NULL,
    creado_por            bigint        NOT NULL,
    creado_en             timestamptz   NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_lb_compra_diferencia_orden ON lb_compra_diferencia (orden_compra_id);
CREATE INDEX IF NOT EXISTS ix_lb_compra_diferencia_estado ON lb_compra_diferencia (estado);

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'hyp_local') THEN
        GRANT ALL ON lb_compra_diferencia, lb_orden_compra_recepcion, lb_orden_compra_item TO hyp_local;
        GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO hyp_local;
    END IF;
END $$;
