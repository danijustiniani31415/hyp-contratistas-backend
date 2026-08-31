-- ============================================================================
-- Descansos médicos: un solo clasificador "tipo" (se elimina "motivo")
-- ----------------------------------------------------------------------------
-- Antes existían DOS ejes para clasificar un descanso:
--   * ss_descanso_medico.tipo / tipo_id  → Particular | Ocupacional
--   * ss_descanso_medico.motivo (texto libre) / motivo_id → Accidente | Enfermedad
-- Ahora queda UNO solo, ss_descanso_tipo, con los 4 valores que son el cruce de
-- ambos ejes. Mi Salud (el trabajador) solo puede elegir los 2 "común", y los ve
-- con su nombre corto ("Accidente" / "Enfermedad"), pero se guardan con el
-- nombre largo. Salud Ocupacional (SSOMA) puede elegir los 4.
--
-- Las columnas legacy (tipo texto, motivo, motivo_id) NO se dropean: quedan
-- congeladas con su valor histórico para auditoría y el código deja de leerlas
-- y escribirlas. Al final del archivo están, comentadas, las sentencias de
-- limpieza definitiva por si más adelante se decide eliminarlas.
--
-- Ejecutar completo. Es idempotente salvo el backfill, que solo toca filas con
-- tipo_id apuntando al catálogo viejo o en NULL.
-- ============================================================================

BEGIN;

-- ── 1. Catálogo: columnas nuevas ────────────────────────────────────────────
-- nombre_corto        → etiqueta que ve el trabajador en Mi Salud.
-- disponible_mi_salud → true solo en los tipos que el trabajador puede elegir.
-- orden               → orden fijo del desplegable (no alfabético).
ALTER TABLE ss_descanso_tipo
  ADD COLUMN IF NOT EXISTS nombre_corto        varchar(60),
  ADD COLUMN IF NOT EXISTS disponible_mi_salud boolean NOT NULL DEFAULT false,
  ADD COLUMN IF NOT EXISTS orden               integer NOT NULL DEFAULT 0;

-- ── 2. Baja lógica del catálogo viejo ───────────────────────────────────────
-- Nunca se borran filas (auditoría). El índice único ux_ss_descanso_tipo_nombre_state
-- es parcial (WHERE state), así que al pasarlos a state=false los nombres quedan libres.
UPDATE ss_descanso_tipo
   SET state = false, active = false, updated_at = now()
 WHERE lower(nombre) IN ('particular', 'ocupacional')
   AND state;

-- ── 3. Los 4 tipos nuevos ───────────────────────────────────────────────────
INSERT INTO ss_descanso_tipo (nombre, nombre_corto, disponible_mi_salud, orden, active, state)
VALUES
  ('Accidente común',        'Accidente',              true,  1, true, true),
  ('Enfermedad común',       'Enfermedad',             true,  2, true, true),
  ('Accidente ocupacional',  'Accidente ocupacional',  false, 3, true, true),
  ('Enfermedad ocupacional', 'Enfermedad ocupacional', false, 4, true, true)
ON CONFLICT DO NOTHING;

-- ── 4. Backfill de los descansos existentes ─────────────────────────────────
-- Regla acordada (se respeta el `tipo` tal como fue registrado, sin reinterpretar):
--   eje común/ocupacional  ← tipo = 'Ocupacional' ? ocupacional : común
--   eje accidente/enfermedad ← motivo_id/motivo dice "enfermedad" ? Enfermedad : Accidente
-- (a hoy solo el descanso con motivo "Enfermedad" cae en la rama Enfermedad;
--  el resto del histórico son cuadros traumáticos → Accidente).
UPDATE ss_descanso_medico d
   SET tipo_id    = t.id,
       updated_at = now()
  FROM ss_descanso_tipo t
 WHERE t.state
   AND t.nombre = (
         CASE WHEN lower(coalesce(
                     (SELECT m.nombre FROM ss_descanso_motivo m WHERE m.id = d.motivo_id),
                     d.motivo, '')) LIKE '%enfermedad%'
              THEN 'Enfermedad ' ELSE 'Accidente ' END
      || CASE WHEN lower(coalesce(d.tipo, '')) = 'ocupacional'
              THEN 'ocupacional' ELSE 'común' END
       )
   AND (d.tipo_id IS NULL
        OR d.tipo_id IN (SELECT id FROM ss_descanso_tipo WHERE NOT state));

-- ── 5. tipo_id pasa a ser obligatorio y `tipo` deja de serlo ────────────────
-- A partir de aquí el clasificador vive solo en tipo_id; la columna `tipo` ya no
-- se escribe (se deja nullable para que los INSERT del backend no la necesiten).
ALTER TABLE ss_descanso_medico ALTER COLUMN tipo_id SET NOT NULL;
ALTER TABLE ss_descanso_medico ALTER COLUMN tipo    DROP NOT NULL;

COMMIT;

-- ── Verificación ────────────────────────────────────────────────────────────
-- SELECT t.nombre, count(*)
--   FROM ss_descanso_medico d JOIN ss_descanso_tipo t ON t.id = d.tipo_id
--  GROUP BY t.nombre ORDER BY 1;

-- ── Limpieza definitiva (OPCIONAL, no ejecutar junto con lo de arriba) ──────
-- Solo cuando ya no se necesite el histórico de los campos viejos:
-- ALTER TABLE ss_descanso_medico DROP CONSTRAINT ss_descanso_medico_motivo_id_fkey;
-- ALTER TABLE ss_descanso_medico DROP COLUMN motivo_id;
-- ALTER TABLE ss_descanso_medico DROP COLUMN motivo;
-- ALTER TABLE ss_descanso_medico DROP COLUMN tipo;
-- DROP TABLE ss_descanso_motivo;
