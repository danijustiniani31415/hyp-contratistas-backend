-- ============================================================================
-- Descanso médico: caso clínico, catálogo CIE-10 y seguimiento reforzado
-- ----------------------------------------------------------------------------
-- Problema que resuelve: hoy cada ss_descanso_medico es un registro plano sin
-- agrupador — reconstruir el historial de un problema de salud exige caminar
-- la cadena prorroga_del_id a mano, el alta vive en el descanso individual (no
-- en un caso), el diagnóstico CIE-10 es texto libre disperso en dos tablas, y
-- el "tipo" de seguimiento es una lista hardcodeada en el frontend.
--
-- Qué agrega:
--   1) cie10_catalogo        — catálogo oficial (se puebla aparte, ver nota abajo).
--   2) ss_descanso_caso      — agrupador: descanso original + "más descanso" +
--                              seguimientos + alta, hasta que se cierra.
--   3) ss_seguimiento_tipo   — catálogo que reemplaza la lista hardcodeada del
--                              frontend ('Médico','Asistenta Social','Seguimiento','Alta').
--   4) Columnas nuevas en ss_descanso_medico y ss_descanso_seguimiento.
--   5) Backfill: cada descanso existente sin prorroga_del_id crea su propio
--      caso; los que sí lo tienen heredan el caso del descanso raíz de su
--      cadena; los seguimientos heredan el caso de su descanso.
--
-- Las columnas legacy (ss_descanso_medico.diagnostico_cie10 texto libre,
-- fecha_alta/alta_por_id/alta_observaciones, ss_descanso_seguimiento.tipo texto)
-- NO se dropean: quedan congeladas para auditoría, el código deja de escribirlas.
--
-- IMPORTANTE: cie10_catalogo se crea vacía acá. La carga de códigos oficiales
-- (MINSA/OPS) se hace por separado con un COPY/INSERT del archivo real — no se
-- inventan códigos médicos en esta migración.
--
-- Ejecutar completo. Es idempotente salvo el backfill (solo toca filas con
-- caso_id NULL).
-- ============================================================================

BEGIN;

-- ── 1. Catálogo CIE-10 (vacío — ver nota de arriba) ─────────────────────────
CREATE TABLE IF NOT EXISTS cie10_catalogo (
  codigo      varchar(10) PRIMARY KEY,
  descripcion text NOT NULL,
  activo      boolean NOT NULL DEFAULT true
);

-- ── 2. Caso clínico ──────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS ss_descanso_caso (
  id                 serial PRIMARY KEY,
  worker_id          integer NOT NULL REFERENCES workers(id),
  fecha_apertura     date NOT NULL,
  estado             varchar(20) NOT NULL DEFAULT 'Abierto',
  fecha_cierre       date,
  alta_por_id        integer,
  alta_observaciones text,
  fecha_reapertura   date,
  created_at         timestamptz NOT NULL DEFAULT now(),
  updated_at         timestamptz NOT NULL DEFAULT now(),
  state              boolean NOT NULL DEFAULT true
);

CREATE INDEX IF NOT EXISTS ix_ss_descanso_caso_worker_id ON ss_descanso_caso(worker_id);

-- ── 3. Catálogo de tipo de seguimiento ───────────────────────────────────────
CREATE TABLE IF NOT EXISTS ss_seguimiento_tipo (
  id     serial PRIMARY KEY,
  nombre varchar(60) NOT NULL,
  orden  integer NOT NULL DEFAULT 0,
  active boolean NOT NULL DEFAULT true
);

INSERT INTO ss_seguimiento_tipo (nombre, orden, active)
VALUES
  ('Médico',            1, true),
  ('Asistenta Social',  2, true),
  ('Seguimiento',       3, true),
  ('Alta',              4, true)
ON CONFLICT DO NOTHING;

-- ── 4. Columnas nuevas ───────────────────────────────────────────────────────
ALTER TABLE ss_descanso_medico
  ADD COLUMN IF NOT EXISTS caso_id                 integer REFERENCES ss_descanso_caso(id),
  ADD COLUMN IF NOT EXISTS diagnostico_cie10_codigo varchar(10) REFERENCES cie10_catalogo(codigo);

ALTER TABLE ss_descanso_seguimiento
  ADD COLUMN IF NOT EXISTS caso_id                  integer REFERENCES ss_descanso_caso(id),
  ADD COLUMN IF NOT EXISTS tipo_id                  integer REFERENCES ss_seguimiento_tipo(id),
  ADD COLUMN IF NOT EXISTS diagnostico_cie10_codigo  varchar(10) REFERENCES cie10_catalogo(codigo),
  ADD COLUMN IF NOT EXISTS puesto_trabajo_snapshot   varchar(200),
  ADD COLUMN IF NOT EXISTS confidencial              boolean NOT NULL DEFAULT true;

-- ── 5. Backfill de caso_id ───────────────────────────────────────────────────
-- 5a) Un caso nuevo por cada descanso RAÍZ (sin prorroga_del_id).
INSERT INTO ss_descanso_caso (worker_id, fecha_apertura, estado, fecha_cierre, alta_por_id, alta_observaciones, created_at)
SELECT d.worker_id, d.fecha_inicio,
       CASE WHEN d.fecha_alta IS NOT NULL THEN 'Cerrado' ELSE 'Abierto' END,
       d.fecha_alta, d.alta_por_id, d.alta_observaciones, d.created_at
  FROM ss_descanso_medico d
 WHERE d.prorroga_del_id IS NULL
   AND d.caso_id IS NULL;

-- 5b) El descanso raíz apunta al caso recién creado (match por worker+fecha_inicio+created_at,
--     que es único para cada descanso raíz procesado arriba).
UPDATE ss_descanso_medico d
   SET caso_id = c.id
  FROM ss_descanso_caso c
 WHERE d.prorroga_del_id IS NULL
   AND d.caso_id IS NULL
   AND c.worker_id = d.worker_id
   AND c.fecha_apertura = d.fecha_inicio
   AND c.created_at = d.created_at;

-- 5c) Los descansos que SÍ tienen prorroga_del_id heredan el caso_id caminando
--     la cadena hasta la raíz (recursivo, hasta 20 saltos — más que suficiente).
WITH RECURSIVE cadena AS (
  SELECT id, prorroga_del_id, caso_id, 0 AS profundidad
    FROM ss_descanso_medico
   WHERE caso_id IS NULL AND prorroga_del_id IS NOT NULL
  UNION ALL
  SELECT hijo.id, padre.prorroga_del_id, padre.caso_id, hijo.profundidad + 1
    FROM cadena hijo
    JOIN ss_descanso_medico padre ON padre.id = hijo.prorroga_del_id
   WHERE hijo.caso_id IS NULL AND hijo.profundidad < 20
)
UPDATE ss_descanso_medico d
   SET caso_id = raiz.caso_id
  FROM (
    SELECT DISTINCT ON (id) id, caso_id
      FROM cadena
     WHERE caso_id IS NOT NULL
     ORDER BY id, profundidad DESC
  ) raiz
 WHERE d.id = raiz.id
   AND d.caso_id IS NULL;

-- 5d) Seguimientos heredan el caso_id de su descanso.
UPDATE ss_descanso_seguimiento s
   SET caso_id = d.caso_id
  FROM ss_descanso_medico d
 WHERE s.descanso_id = d.id
   AND s.caso_id IS NULL;

-- ── 6. caso_id pasa a obligatorio en ss_descanso_medico ─────────────────────
ALTER TABLE ss_descanso_medico ALTER COLUMN caso_id SET NOT NULL;

COMMIT;

-- ── Verificación ────────────────────────────────────────────────────────────
-- SELECT count(*) FROM ss_descanso_medico WHERE caso_id IS NULL;               -- debe ser 0
-- SELECT count(*) FROM ss_descanso_seguimiento WHERE caso_id IS NULL;          -- debe ser 0
-- SELECT c.id, c.estado, count(d.id) FROM ss_descanso_caso c
--   LEFT JOIN ss_descanso_medico d ON d.caso_id = c.id GROUP BY 1,2 ORDER BY 1;
