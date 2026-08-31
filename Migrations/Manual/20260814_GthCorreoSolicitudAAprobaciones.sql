-- ============================================================================
-- Gestión GTH — El correo SOLICITUD pasa a configurarse desde Aprobaciones
-- Fecha: 2026-08-14
--
-- El correo de código SOLICITUD (el aviso que le llega a GTH con las vacantes
-- que Gerencia General aprobó) se administraba desde
-- /gestion-gth/solicitud-personal/configuracion, junto con los correos que
-- dispara el solicitante. Pero ese correo no lo dispara el solicitante: sale
-- recién cuando Gerencia decide, así que su configuración ahora vive en la
-- pantalla donde se toma esa decisión, /gestion-gth/aprobaciones/configuracion.
--
-- El reparto de correos por pantalla es código (CorreoConfigService.
-- CorreosPorPantalla), no un dato de la BD: acá solo cambia el nombre visible
-- de la sección, que sí sale de gth_correo_tipo.nombre.
--
--   «Nueva solicitud de personal»  →  «Aprobación de Gerencia a GTH»
--
-- El código, la descripción y los destinatarios no se tocan: el correo es el
-- mismo y sigue enviándose igual. No hay cambios de esquema.
--
-- Idempotente. Aplicar en dev y prod.
-- ============================================================================

UPDATE gth_correo_tipo
SET nombre           = 'Aprobación de Gerencia a GTH',
    updated_date_time = now()
WHERE codigo = 'SOLICITUD'
  AND state
  AND nombre <> 'Aprobación de Gerencia a GTH';
