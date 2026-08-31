-- ─────────────────────────────────────────────────────────────────────────────
-- Actas de Reunión: ampliación de alcance de proyecto a organización completa.
--   - reunion: project_id pasa a opcional; se agrega area_scope_id (árbol
--     gerencia/área/subárea ya existente en area_scope). Ambos null = reunión
--     de toda la organización. Nunca coexisten (chk_reunion_ambito_unico).
--   - reunion_participante: se agrega worker_id para poder filtrar/notificar
--     por área/puesto y para firma/asistencia futuras.
--   - reunion_acuerdo: se agregan requiere_aceptacion, requiere_evidencia,
--     evidencia_url.
--   - reunion_acuerdo_responsable: deja de depender exclusivamente de
--     reunion_participante_id (ahora nullable) y pasa a worker_id (cualquier
--     trabajador de la organización, haya asistido o no), con su propio
--     estado de aceptación.
-- ─────────────────────────────────────────────────────────────────────────────

-- 1) Ámbito: proyecto pasa a opcional, se agrega area_scope_id.
ALTER TABLE reunion ALTER COLUMN project_id DROP NOT NULL;
ALTER TABLE reunion ADD COLUMN IF NOT EXISTS area_scope_id INT REFERENCES area_scope(area_scope_id);
ALTER TABLE reunion ADD CONSTRAINT chk_reunion_ambito_unico
    CHECK (NOT (project_id IS NOT NULL AND area_scope_id IS NOT NULL));
CREATE INDEX IF NOT EXISTS ix_reunion_area_scope ON reunion(area_scope_id);

-- 2) Participante: worker_id para trazabilidad (filtro por área/puesto, notificaciones).
ALTER TABLE reunion_participante ADD COLUMN IF NOT EXISTS worker_id INT REFERENCES workers(id);

-- 3) Acuerdo: aceptación + evidencia.
ALTER TABLE reunion_acuerdo ADD COLUMN IF NOT EXISTS requiere_aceptacion BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE reunion_acuerdo ADD COLUMN IF NOT EXISTS requiere_evidencia  BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE reunion_acuerdo ADD COLUMN IF NOT EXISTS evidencia_url       VARCHAR(2000);

-- 4) Responsable de acuerdo: pasa a worker_id, con aceptación individual.
--    reunion_participante_id se conserva (nullable) para no perder el historial
--    de registros creados antes de este cambio.
ALTER TABLE reunion_acuerdo_responsable ALTER COLUMN reunion_participante_id DROP NOT NULL;
ALTER TABLE reunion_acuerdo_responsable ADD COLUMN IF NOT EXISTS worker_id INT REFERENCES workers(id);
ALTER TABLE reunion_acuerdo_responsable ADD COLUMN IF NOT EXISTS estado_aceptacion VARCHAR(20) NOT NULL DEFAULT 'ACEPTADO';
ALTER TABLE reunion_acuerdo_responsable ADD COLUMN IF NOT EXISTS motivo_rechazo    TEXT;
ALTER TABLE reunion_acuerdo_responsable ADD COLUMN IF NOT EXISTS fecha_respuesta   TIMESTAMPTZ;
ALTER TABLE reunion_acuerdo_responsable ADD CONSTRAINT chk_responsable_origen
    CHECK (reunion_participante_id IS NOT NULL OR worker_id IS NOT NULL);
