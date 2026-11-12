-- Recepción con control de guías de remisión (fase 1):
--  * Stock en tránsito: una guía nueva descuenta el origen al ser aceptada, pero el destino se acredita recién al recibir
--    (destino_acreditado = false). Las guías que ya existían quedan en true: su destino ya fue acreditado por completo.
--  * Recepción por viajes (lb_guia_recepcion): se puede recibir una parte y luego el resto; con fotos opcionales.
--  * Diferencias (lb_guia_diferencia): lo faltante o dañado queda abierto hasta resolverlo (reposición, reclamo, merma o "llegó").
-- Idempotente.

ALTER TABLE lb_guia_remision ADD COLUMN IF NOT EXISTS destino_acreditado     boolean     NOT NULL DEFAULT true;
ALTER TABLE lb_guia_remision ADD COLUMN IF NOT EXISTS recepcion_estado       varchar(20) NOT NULL DEFAULT 'PENDIENTE';
ALTER TABLE lb_guia_remision ADD COLUMN IF NOT EXISTS recepcion_cerrada_en   timestamptz NULL;
ALTER TABLE lb_guia_remision ADD COLUMN IF NOT EXISTS recordatorio_enviado_en timestamptz NULL;

ALTER TABLE lb_guia_remision_item ADD COLUMN IF NOT EXISTS cantidad_danada   numeric(12,2) NOT NULL DEFAULT 0;
ALTER TABLE lb_guia_remision_item ADD COLUMN IF NOT EXISTS cantidad_faltante numeric(12,2) NOT NULL DEFAULT 0;

-- Guías ya confirmadas con el flujo anterior: lo que no llegó pasa a faltante y la recepción queda cerrada.
UPDATE lb_guia_remision_item
SET cantidad_faltante = cantidad - cantidad_confirmada
WHERE confirmado_en IS NOT NULL AND cantidad_confirmada < cantidad AND cantidad_faltante = 0 AND cantidad_danada = 0;

UPDATE lb_guia_remision g
SET recepcion_estado = CASE WHEN EXISTS (SELECT 1 FROM lb_guia_remision_item i WHERE i.guia_remision_id = g.id AND i.cantidad_faltante > 0)
                            THEN 'CON_DIFERENCIAS' ELSE 'COMPLETA' END,
    recepcion_cerrada_en = COALESCE(g.recepcion_cerrada_en, now())
WHERE EXISTS (SELECT 1 FROM lb_guia_remision_item i WHERE i.guia_remision_id = g.id AND i.confirmado_en IS NOT NULL)
  AND g.recepcion_estado = 'PENDIENTE';

CREATE TABLE IF NOT EXISTS lb_guia_recepcion (
    id                bigserial PRIMARY KEY,
    guia_remision_id  bigint      NOT NULL REFERENCES lb_guia_remision(id),
    recibido_por      bigint      NOT NULL,
    recibido_en       timestamptz NOT NULL DEFAULT now(),
    cierra            boolean     NOT NULL DEFAULT false,
    observacion       text        NULL
);
CREATE INDEX IF NOT EXISTS ix_lb_guia_recepcion_guia ON lb_guia_recepcion (guia_remision_id);

CREATE TABLE IF NOT EXISTS lb_guia_recepcion_item (
    id                bigserial PRIMARY KEY,
    recepcion_id      bigint        NOT NULL REFERENCES lb_guia_recepcion(id) ON DELETE CASCADE,
    guia_item_id      bigint        NOT NULL REFERENCES lb_guia_remision_item(id),
    cantidad_ok       numeric(12,2) NOT NULL DEFAULT 0,
    cantidad_danada   numeric(12,2) NOT NULL DEFAULT 0,
    cantidad_faltante numeric(12,2) NOT NULL DEFAULT 0,
    motivo            varchar(30)   NULL,
    observacion       text          NULL
);
CREATE INDEX IF NOT EXISTS ix_lb_guia_recepcion_item_recepcion ON lb_guia_recepcion_item (recepcion_id);

CREATE TABLE IF NOT EXISTS lb_guia_recepcion_foto (
    id            bigserial PRIMARY KEY,
    recepcion_id  bigint       NOT NULL REFERENCES lb_guia_recepcion(id) ON DELETE CASCADE,
    nombre        varchar(200) NOT NULL,
    mime          varchar(100) NOT NULL,
    contenido     bytea        NOT NULL,
    creado_en     timestamptz  NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_lb_guia_recepcion_foto_recepcion ON lb_guia_recepcion_foto (recepcion_id);

CREATE TABLE IF NOT EXISTS lb_guia_diferencia (
    id               bigserial PRIMARY KEY,
    guia_remision_id bigint        NOT NULL REFERENCES lb_guia_remision(id),
    guia_item_id     bigint        NOT NULL REFERENCES lb_guia_remision_item(id),
    recepcion_id     bigint        NULL REFERENCES lb_guia_recepcion(id),
    tipo             varchar(10)   NOT NULL CHECK (tipo IN ('FALTANTE', 'DANADO')),
    cantidad         numeric(12,2) NOT NULL CHECK (cantidad > 0),
    motivo           varchar(30)   NULL,
    observacion      text          NULL,
    estado           varchar(10)   NOT NULL DEFAULT 'ABIERTA',
    resolucion       varchar(15)   NULL,
    resolucion_nota  text          NULL,
    resuelta_por     bigint        NULL,
    resuelta_en      timestamptz   NULL,
    creado_por       bigint        NOT NULL,
    creado_en        timestamptz   NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_lb_guia_diferencia_guia ON lb_guia_diferencia (guia_remision_id);
CREATE INDEX IF NOT EXISTS ix_lb_guia_diferencia_estado ON lb_guia_diferencia (estado);

-- Solo entornos locales: el usuario de la app local no es dueño de las tablas creadas con el superusuario.
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'hyp_local') THEN
        GRANT ALL ON lb_guia_recepcion, lb_guia_recepcion_item, lb_guia_recepcion_foto, lb_guia_diferencia TO hyp_local;
        GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO hyp_local;
        GRANT ALL ON lb_guia_remision, lb_guia_remision_item TO hyp_local;
    END IF;
END $$;
