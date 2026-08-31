-- ═══════════════════════════════════════════════════════════════════════════
-- Formulario del postulante: CV documentado subido por el propio postulante
-- ═══════════════════════════════════════════════════════════════════════════
--
-- Hasta ahora el único CV del proceso era el que GTH cargaba en la long list
-- (gth_candidato.cv_*). GTH pidió obtener también el CV documentado de manos
-- del postulante, desde el formulario público, para poder comparar los dos: el
-- que consiguió el reclutador y el que el postulante declara como suyo.
--
-- Se guarda igual que el de la long list —nombre + url + item/drive de
-- SharePoint— y en la misma carpeta del requerimiento ("Long list {codigo}").
-- Nada de bytes en la base.
--
-- Todas NULLABLE: los formularios anteriores a este cambio (enviados,
-- completados, aprobados o rechazados) no tienen CV y no se puede inventar. La
-- obligatoriedad se aplica en el envío del formulario, no en el esquema — el
-- mismo criterio que consentimiento_datos_personales.
--
-- Idempotente: se puede correr más de una vez sin error.
-- ═══════════════════════════════════════════════════════════════════════════

BEGIN;

ALTER TABLE gth_postulante_formulario
    ADD COLUMN IF NOT EXISTS cv_nombre          text NULL,
    ADD COLUMN IF NOT EXISTS cv_nombre_original text NULL,
    ADD COLUMN IF NOT EXISTS cv_url             text NULL,
    ADD COLUMN IF NOT EXISTS cv_item_id         text NULL,
    ADD COLUMN IF NOT EXISTS cv_drive_id        text NULL;

COMMENT ON COLUMN gth_postulante_formulario.cv_nombre IS
    'Nombre del CV documentado tal como quedó en SharePoint. NULL en los formularios anteriores a este campo.';
COMMENT ON COLUMN gth_postulante_formulario.cv_nombre_original IS
    'Nombre con el que el postulante subió su CV: es el que se le muestra a él y a GTH (el de SharePoint lleva el codigo del requerimiento y un timestamp).';
COMMENT ON COLUMN gth_postulante_formulario.cv_url IS
    'Link al CV documentado en SharePoint (el que abren GTH y el solicitante).';
COMMENT ON COLUMN gth_postulante_formulario.cv_item_id IS
    'Item id del CV documentado en SharePoint/OneDrive.';
COMMENT ON COLUMN gth_postulante_formulario.cv_drive_id IS
    'Drive id de la carpeta de SharePoint donde quedó el CV documentado.';

COMMIT;

-- Verificación (debe devolver 5 filas):
-- SELECT column_name, data_type, is_nullable FROM information_schema.columns
--  WHERE table_name = 'gth_postulante_formulario'
--    AND column_name LIKE 'cv_%'
--  ORDER BY column_name;
