-- ============================================================================
-- Salidas: nuevo estado de aprobación "Cancelado" + auditoría de cancelación
-- ----------------------------------------------------------------------------
-- El propio trabajador puede cancelar una solicitud de salida SUYA que aún esté
-- en estado Pendiente (registró una que no debía, o al final no salió). El
-- estado se guarda de forma normalizada como una fila más del catálogo
-- ga_estado_aprobacion (id = 4), apuntado por el FK ya existente
-- ga_solicitud_salida.estado_aprobacion_id.
--
-- Además se agregan dos columnas de auditoría (quién / cuándo canceló), espejo
-- del patrón *_registrada_por_id / *_registrada_at ya usado en esta tabla.
-- FK a app_user(user_id).
--
-- Idempotente: seguro de correr más de una vez.
-- ============================================================================

-- 1. Catálogo: fila del nuevo estado terminal "Cancelado".
INSERT INTO ga_estado_aprobacion (id, descripcion, activo)
VALUES (4, 'Cancelado', true)
ON CONFLICT (id) DO NOTHING;

-- 2. Columnas de auditoría de la cancelación.
ALTER TABLE ga_solicitud_salida
    ADD COLUMN IF NOT EXISTS cancelada_por_id integer NULL,
    ADD COLUMN IF NOT EXISTS cancelada_at     timestamp with time zone NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'ga_solicitud_salida_cancelada_por_id_fkey'
          AND table_name = 'ga_solicitud_salida'
    ) THEN
        ALTER TABLE ga_solicitud_salida
            ADD CONSTRAINT ga_solicitud_salida_cancelada_por_id_fkey
            FOREIGN KEY (cancelada_por_id) REFERENCES app_user (user_id);
    END IF;
END $$;
