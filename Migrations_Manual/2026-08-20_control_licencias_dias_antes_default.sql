-- ============================================================================
-- Control de Licencias: días de antelación por defecto en el catálogo de tipos
-- (plantilla base y tipos propios de proyecto), para prellenar el recordatorio
-- al subir el documento sin tener que escribirlo cada vez.
-- Ejecutar manualmente en pgAdmin.
-- ============================================================================

ALTER TABLE vecino_licencia_control_tipo
    ADD COLUMN IF NOT EXISTS dias_antes_default integer NULL;
