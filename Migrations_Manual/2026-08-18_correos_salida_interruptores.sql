-- ─────────────────────────────────────────────────────────────────────────────
-- Correos de Solicitud de Salidas: interruptores de la pantalla
-- "Gestión Administrativa › Configuración › Correos".
--
-- Hasta ahora la pantalla solo dejaba configurar las copias (ga_correo_regla) y
-- el correo siempre salía, siempre a su destinatario principal (el revisor en el
-- correo REVISOR, el solicitante en los demás), calculado en código.
--
-- Se agregan dos interruptores por correo:
--   1) el correo completo (ga_correo_evento.active, columna que ya existía y no
--      se usaba): apagado ⇒ ese correo no se envía.
--   2) su destinatario principal (destinatario_principal_activo): apagado ⇒ el
--      correo se manda solo a los destinatarios configurados. Si al final no
--      queda ningún destinatario, no se envía nada.
--
-- Qué correo permite qué interruptor lo decide la BD (permite_desactivar_*), no
-- el código: hoy solo REVISOR y CONFIRMACION son apagables y solo REVISOR deja
-- apagar su principal. Habilitar los otros dos a futuro es un UPDATE, no un deploy.
-- ─────────────────────────────────────────────────────────────────────────────

ALTER TABLE ga_correo_evento
    ADD COLUMN IF NOT EXISTS destinatario_principal_nombre  VARCHAR(150),
    ADD COLUMN IF NOT EXISTS destinatario_principal_activo  BOOLEAN NOT NULL DEFAULT TRUE,
    ADD COLUMN IF NOT EXISTS permite_desactivar_envio       BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS permite_desactivar_principal   BOOLEAN NOT NULL DEFAULT FALSE;

COMMENT ON COLUMN ga_correo_evento.active IS
    'Interruptor maestro: false = este correo no se envia (se conserva su configuracion).';
COMMENT ON COLUMN ga_correo_evento.destinatario_principal_nombre IS
    'Etiqueta del destinatario principal que calcula el backend (el revisor, el solicitante). Solo informativa para la pantalla.';
COMMENT ON COLUMN ga_correo_evento.destinatario_principal_activo IS
    'false = el correo NO se manda a su destinatario principal, solo a los destinatarios configurados en ga_correo_regla.';
COMMENT ON COLUMN ga_correo_evento.permite_desactivar_envio IS
    'true = la pantalla muestra el interruptor maestro de este correo.';
COMMENT ON COLUMN ga_correo_evento.permite_desactivar_principal IS
    'true = la pantalla muestra el interruptor del destinatario principal de este correo.';

-- Al crear la solicitud: el correo al revisor es el unico que ademas deja apagar
-- a su destinatario principal (dejarlo solo en manos de las copias configuradas).
UPDATE ga_correo_evento
SET destinatario_principal_nombre = 'El revisor que aprueba la solicitud',
    permite_desactivar_envio      = TRUE,
    permite_desactivar_principal  = TRUE,
    updated_at                    = now()
WHERE codigo = 'REVISOR' AND state;

UPDATE ga_correo_evento
SET destinatario_principal_nombre = 'El solicitante',
    permite_desactivar_envio      = TRUE,
    updated_at                    = now()
WHERE codigo = 'CONFIRMACION' AND state;

-- Aprobada / Rechazada: por ahora no se pueden apagar desde la pantalla; se les
-- pone igual la etiqueta de su destinatario principal para cuando se habiliten.
UPDATE ga_correo_evento
SET destinatario_principal_nombre = 'El solicitante',
    updated_at                    = now()
WHERE codigo IN ('APROBADA', 'RECHAZADA') AND state;
