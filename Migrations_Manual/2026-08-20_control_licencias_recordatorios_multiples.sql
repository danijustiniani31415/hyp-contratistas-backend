-- ============================================================================
-- Control de Licencias: recordatorios múltiples por licencia
-- Antes: un solo recordatorio (fecha_recordatorio/dias_antes) por
-- vecino_licencia_control. Ahora: una lista en vecino_licencia_control_recordatorio,
-- para poder tener varios activos a la vez (ej. 30, 15, 7 y 2 días antes).
-- Ejecutar manualmente en pgAdmin.
-- ============================================================================

BEGIN;

CREATE TABLE vecino_licencia_control_recordatorio (
    vecino_licencia_control_recordatorio_id SERIAL PRIMARY KEY,
    vecino_licencia_control_id              INT NOT NULL REFERENCES vecino_licencia_control(vecino_licencia_control_id),
    dias_antes                               INT NOT NULL,
    fecha_recordatorio                       DATE NOT NULL,
    enviado_date_time                        TIMESTAMPTZ NULL,
    created_date_time                        TIMESTAMPTZ NOT NULL,
    created_user_id                          INT NOT NULL,
    active                                    BOOLEAN NOT NULL DEFAULT TRUE,
    state                                     BOOLEAN NOT NULL DEFAULT TRUE
);

COMMENT ON TABLE vecino_licencia_control_recordatorio IS 'Recordatorios (N días antes de vencer) de una licencia; puede haber varios activos a la vez por licencia';

CREATE INDEX ix_vecino_licencia_control_recordatorio_licencia
    ON vecino_licencia_control_recordatorio (vecino_licencia_control_id);

-- Migra el recordatorio único que ya existiera en cada licencia vigente.
INSERT INTO vecino_licencia_control_recordatorio
    (vecino_licencia_control_id, dias_antes, fecha_recordatorio, enviado_date_time,
     created_date_time, created_user_id, active, state)
SELECT
    vecino_licencia_control_id, dias_antes, fecha_recordatorio, recordatorio_enviado_date_time,
    created_date_time, created_user_id, TRUE, TRUE
FROM vecino_licencia_control
WHERE fecha_recordatorio IS NOT NULL;

ALTER TABLE vecino_licencia_control
    DROP COLUMN fecha_recordatorio,
    DROP COLUMN dias_antes,
    DROP COLUMN recordatorio_enviado_date_time;

COMMIT;
