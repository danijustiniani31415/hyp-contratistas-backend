-- ═══════════════════════════════════════════════════════════════════════════
-- Formulario del postulante: se elimina el campo "Convocatoria de tu interés"
-- ═══════════════════════════════════════════════════════════════════════════
--
-- El postulante llega al formulario por un enlace con token que ya está atado a
-- un candidato de un requerimiento concreto, y el puesto de ese requerimiento se
-- muestra en el encabezado del propio formulario ("... para la posición X"). El
-- desplegable era redundante y además permitía declarar una convocatoria distinta
-- a aquella a la que se le invitó.
--
-- Cualquier lectura de "la convocatoria del postulante" sale ahora de la misma
-- fuente que el encabezado:
--     gth_candidato → gth_requerimiento.puesto_id → puesto.nombre
--
-- Se dan de baja las dos columnas: la vigente (FK a puesto) y la legacy que quedó
-- de cuando el catálogo era gth_puesto. Ningún código las lee ya. Los FK asociados
-- (gth_postulante_formulario_convocatoria_puesto_id_fkey y fk_gpf_convocatoria_puesto)
-- caen junto con sus columnas.
--
-- Idempotente: se puede correr más de una vez sin error.
-- ═══════════════════════════════════════════════════════════════════════════

BEGIN;

ALTER TABLE gth_postulante_formulario
    DROP COLUMN IF EXISTS convocatoria_puesto_id,
    DROP COLUMN IF EXISTS convocatoria_gth_puesto_id;

COMMIT;

-- Verificación (debe devolver 0 filas):
-- SELECT column_name FROM information_schema.columns
--  WHERE table_name = 'gth_postulante_formulario'
--    AND column_name LIKE '%convocatoria%';
