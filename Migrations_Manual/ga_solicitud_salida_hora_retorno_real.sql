-- ============================================================================
-- ga_solicitud_salida: hora real de retorno (registrada por recepción)
-- ----------------------------------------------------------------------------
-- Espejo de las columnas existentes hora_salida_real / *_registrada_por_id /
-- *_registrada_at. Dato extra opcional; no afecta ningún flujo. Solo lo edita
-- el rol USUARIO DE RECEPCIÓN desde Gestión de Salidas.
-- FK a app_user(user_id) igual que la columna de salida.
-- Idempotente: seguro de correr más de una vez.
-- ============================================================================

ALTER TABLE ga_solicitud_salida
    ADD COLUMN IF NOT EXISTS hora_retorno_real                   time without time zone NULL,
    ADD COLUMN IF NOT EXISTS hora_retorno_real_registrada_por_id integer NULL,
    ADD COLUMN IF NOT EXISTS hora_retorno_real_registrada_at     timestamp with time zone NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'ga_solicitud_salida_hora_retorno_real_registrada_por_id_fkey'
          AND table_name = 'ga_solicitud_salida'
    ) THEN
        ALTER TABLE ga_solicitud_salida
            ADD CONSTRAINT ga_solicitud_salida_hora_retorno_real_registrada_por_id_fkey
            FOREIGN KEY (hora_retorno_real_registrada_por_id) REFERENCES app_user (user_id);
    END IF;
END $$;
