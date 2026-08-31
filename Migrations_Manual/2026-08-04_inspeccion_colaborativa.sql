-- Inspecciones colaborativas (gerenciales/cruzadas): varios coordinadores agregan
-- hallazgos por separado a un mismo registro mientras esta "Abierta".
-- Aplicar manualmente via pgAdmin/psql. No usar `dotnet ef database update`
-- (hay drift de modelo sin migrar en el repo que no corresponde a este cambio).

ALTER TABLE ssoma_inspeccion_tipo
    ADD COLUMN IF NOT EXISTS es_colaborativa boolean NOT NULL DEFAULT false;

ALTER TABLE ssoma_inspeccion
    ADD COLUMN IF NOT EXISTS es_colaborativa boolean NOT NULL DEFAULT false;

ALTER TABLE ssoma_inspeccion_hallazgo
    ADD COLUMN IF NOT EXISTS creado_por_worker_id integer NULL,
    ADD COLUMN IF NOT EXISTS creado_por_nombre text NULL;

CREATE TABLE IF NOT EXISTS ssoma_inspeccion_participante (
    id serial PRIMARY KEY,
    inspeccion_id integer NOT NULL REFERENCES ssoma_inspeccion (id) ON DELETE CASCADE,
    worker_id integer NULL,
    nombre text NOT NULL,
    cargo text NULL,
    empresa text NULL,
    fecha_union timestamp with time zone NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_ssoma_inspeccion_participante_inspeccion_id
    ON ssoma_inspeccion_participante (inspeccion_id);

-- Opcional: marcar catálogo existente de tipos como colaborativo donde aplique.
-- Revisar manualmente los nombres reales antes de correr esto:
-- UPDATE ssoma_inspeccion_tipo SET es_colaborativa = true
--   WHERE nombre ILIKE '%gerencial%' OR nombre ILIKE '%cruzada%';
