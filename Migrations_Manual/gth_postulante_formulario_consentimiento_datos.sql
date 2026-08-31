-- ═══════════════════════════════════════════════════════════════════════════
-- Formulario del postulante: consentimiento de protección de datos (paso 0)
-- ═══════════════════════════════════════════════════════════════════════════
--
-- Nueva primera página del formulario público: el postulante autoriza el
-- tratamiento de sus datos personales (Ley N.° 29733) antes de poder llenar
-- nada. Es obligatorio: el frontend no deja avanzar y el backend rechaza el
-- envío si no viene en true.
--
-- Se guarda NULLABLE porque las filas anteriores a este cambio (formularios ya
-- enviados o completados) no tienen esa respuesta y no se puede inventar: quedan
-- en NULL y el modal de GTH las muestra como "—". El "obligatorio" se aplica en
-- el envío, no en el esquema.
--
-- El momento del consentimiento es el del envío del formulario, que ya se
-- registra en completado_date_time; no se agrega una segunda fecha.
--
-- Idempotente: se puede correr más de una vez sin error.
-- ═══════════════════════════════════════════════════════════════════════════

BEGIN;

ALTER TABLE gth_postulante_formulario
    ADD COLUMN IF NOT EXISTS consentimiento_datos_personales boolean NULL;

COMMENT ON COLUMN gth_postulante_formulario.consentimiento_datos_personales IS
    'Paso 0: el postulante autoriza el tratamiento de sus datos personales (Ley N. 29733). NULL en los formularios anteriores a este campo.';

COMMIT;

-- Verificación (debe devolver 1 fila):
-- SELECT column_name, data_type, is_nullable FROM information_schema.columns
--  WHERE table_name = 'gth_postulante_formulario'
--    AND column_name = 'consentimiento_datos_personales';
