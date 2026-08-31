-- ============================================================================
-- Gestión GTH · Reclutamiento — "Reemplazo": a quién se está reemplazando
--
-- El modal «Nueva solicitud de personal» ahora pregunta, cuando el tipo de
-- requerimiento es Reemplazo, a QUÉ trabajador se reemplaza. El desplegable
-- ofrece a los trabajadores del `area_scope` del solicitante y de cualquier
-- área hija (el mismo subárbol que usan los filtros de Área del resto del
-- sistema), incluido el propio solicitante: pedir el reemplazo de uno mismo es
-- un caso real (renuncia, promoción interna) y no hay motivo para bloquearlo.
--
-- Dos cambios:
--
-- 1. `gth_tipo_requerimiento.codigo` — el catálogo solo tenía `nombre`, así que
--    "¿esta vacante es un reemplazo?" solo se podía responder comparando el
--    texto "Reemplazo". Se agrega el código estable (NUEVO / REEMPLAZO), igual
--    que en `gth_estado_requerimiento`, `gth_prioridad` y `gth_tipo_proceso`:
--    backend y frontend deciden por él y renombrar el catálogo deja de ser un
--    cambio con efectos colaterales.
--
-- 2. `gth_requerimiento.reemplaza_worker_id` — FK a `workers`. Nullable a
--    propósito: solo se llena en las vacantes de tipo Reemplazo (en las Nuevo
--    queda null, y los requerimientos ya existentes quedan igual). No se guarda
--    el nombre en texto: el trabajador es una entidad viva y el nombre se lee
--    por la FK, como en `ssoma_inspeccion.inspector_worker_id`.
--
-- Idempotente: ADD COLUMN IF NOT EXISTS, índices IF NOT EXISTS y la FK solo si
-- no está.
-- ============================================================================

BEGIN;

-- ── 1. Código estable del tipo de requerimiento ─────────────────────────────

ALTER TABLE gth_tipo_requerimiento
    ADD COLUMN IF NOT EXISTS codigo text;

COMMENT ON COLUMN gth_tipo_requerimiento.codigo IS
    'Codigo estable del tipo (NUEVO / REEMPLAZO). La logica decide por aqui, nunca por nombre.';

-- Se deriva del nombre (MAYUSCULAS, sin tildes, espacios como "_"): hoy son
-- exactamente "Nuevo" y "Reemplazo", y así cualquier fila que alguien haya
-- agregado a mano también queda con un código válido en vez de null.
UPDATE gth_tipo_requerimiento
   SET codigo = upper(translate(regexp_replace(btrim(nombre), '\s+', '_', 'g'),
                                'áéíóúüÁÉÍÓÚÜñÑ', 'aeiouuAEIOUUnN'))
 WHERE codigo IS NULL;

ALTER TABLE gth_tipo_requerimiento
    ALTER COLUMN codigo SET NOT NULL;

-- Mismo criterio que `ix_gth_tipo_requerimiento_nombre_vivo`: pueden quedar
-- varias filas borradas (state = false) con el mismo código, pero solo una viva.
CREATE UNIQUE INDEX IF NOT EXISTS ix_gth_tipo_requerimiento_codigo_vivo
    ON gth_tipo_requerimiento (codigo) WHERE state = true;

-- ── 2. Trabajador al que reemplaza la vacante ───────────────────────────────

ALTER TABLE gth_requerimiento
    ADD COLUMN IF NOT EXISTS reemplaza_worker_id integer NULL;

COMMENT ON COLUMN gth_requerimiento.reemplaza_worker_id IS
    'Trabajador al que reemplaza la vacante (solo tipo REEMPLAZO). Null en las vacantes nuevas.';

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'fk_gth_req_reemplaza_worker'
          AND conrelid = 'gth_requerimiento'::regclass
    ) THEN
        ALTER TABLE gth_requerimiento
            ADD CONSTRAINT fk_gth_req_reemplaza_worker
            FOREIGN KEY (reemplaza_worker_id) REFERENCES workers(id);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_gth_requerimiento_reemplaza_worker
    ON gth_requerimiento (reemplaza_worker_id);

COMMIT;

-- ============================================================================
-- Verificación (correr después; no modifica nada)
-- ============================================================================
-- SELECT gth_tipo_requerimiento_id, nombre, codigo
-- FROM gth_tipo_requerimiento ORDER BY orden;
-- Esperado: 1 | Nuevo | NUEVO   ·   2 | Reemplazo | REEMPLAZO
--
-- SELECT column_name, is_nullable, data_type
-- FROM information_schema.columns
-- WHERE table_schema = 'public' AND table_name = 'gth_requerimiento'
--   AND column_name = 'reemplaza_worker_id';
-- Esperado: reemplaza_worker_id | YES | integer
