-- ============================================================================
-- Control de Licencias: elimina las tablas del antiguo "Control de Vencimientos"
-- Ejecutar manualmente en pgAdmin, DESPUÉS de 2026-08-20_control_licencias_schema.sql.
-- Confirmado: no hay datos valiosos que migrar.
-- ============================================================================

BEGIN;

DROP TABLE IF EXISTS vecino_licencia_email;
DROP TABLE IF EXISTS vecino_licencia;

COMMIT;
