-- ============================================================================
-- Salidas: motivo adicional obligatorio por motivo del catalogo
-- ============================================================================
-- Hay motivos del catalogo que por si solos no dicen nada: "Visita a obra" o
-- "Visita a salas de venta" no explican a que se va. Ahora cada motivo puede
-- marcarse con `requiere_motivo_adicional` desde Configuracion -> Motivos; al
-- elegirlo en Solicitud de Salidas el formulario pide un texto obligatorio que
-- se guarda en `ga_solicitud_trayecto.motivo_adicional`.
--
-- Ojo: NO se confunde con `motivo_libre`, que es la via "Otro motivo" (el
-- trabajador escribe un motivo que no esta en el catalogo, y entonces
-- motivo_id queda nulo). Aca el motivo_id existe: motivo_adicional es el
-- detalle que lo acompana.
--
-- Idempotente: se puede correr mas de una vez.
-- ============================================================================

BEGIN;

-- ── 1. Flag de configuracion del motivo ─────────────────────────────────────

ALTER TABLE ga_motivo_salida
    ADD COLUMN IF NOT EXISTS requiere_motivo_adicional boolean NOT NULL DEFAULT false;

COMMENT ON COLUMN ga_motivo_salida.requiere_motivo_adicional IS
  'Si true, al elegir este motivo en Solicitud de Salidas se exige escribir un motivo adicional (detalle) que se guarda en ga_solicitud_trayecto.motivo_adicional.';

-- ── 2. Texto del motivo adicional en el trayecto ────────────────────────────
-- Nullable a proposito: los trayectos historicos y los de motivos que no lo
-- exigen no lo tienen. La obligatoriedad la impone el motivo, no la columna.

ALTER TABLE ga_solicitud_trayecto
    ADD COLUMN IF NOT EXISTS motivo_adicional text NULL;

COMMENT ON COLUMN ga_solicitud_trayecto.motivo_adicional IS
  'Detalle obligatorio que acompana al motivo cuando ga_motivo_salida.requiere_motivo_adicional = true. Distinto de motivo_libre (via "Otro motivo", con motivo_id nulo).';

-- ── 3. Motivos que arrancan con el flag prendido ────────────────────────────
-- Por descripcion y no por id: los ids no son los mismos en dev y en prod.

UPDATE ga_motivo_salida
   SET requiere_motivo_adicional = true
 WHERE lower(descripcion) IN ('visita a obra', 'visita a salas de venta')
   AND requiere_motivo_adicional = false;

COMMIT;

-- ── Verificacion ────────────────────────────────────────────────────────────
-- SELECT id, descripcion, requiere_adjunto, es_hora_estimada, requiere_motivo_adicional
--   FROM ga_motivo_salida ORDER BY descripcion;
