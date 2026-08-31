-- ============================================================================
-- Gestión GTH · Onboarding — Carta oferta firmada + checklist operativo
--
-- 1) gth_onboarding: columnas de la carta oferta FIRMADA (la que el colaborador
--    devuelve). Van aparte de las de la carta oferta enviada porque son dos
--    documentos distintos del mismo expediente: la enviada es la propuesta y la
--    firmada es la evidencia que abre el file digital. Se guardan en la MISMA
--    carpeta de SharePoint que la enviada (gth_carta_oferta_folder).
--
--    Esa carpeta (el file digital del colaborador, RF-ONB-04) se persiste en la
--    fila al subir la carta enviada: file_digital_drive_id / file_digital_item_id.
--    Guardarla —en vez de volver a derivarla del nombre— es lo que garantiza que
--    la carta firmada caiga exactamente donde cayó la enviada, aunque el nombre
--    del colaborador cambie después en la base maestra.
--
-- 2) gth_onboarding_actividad: catálogo del «checklist operativo consolidado»
--    del requerimiento funcional (punto 9.2), normalizado por fase. Es lo que
--    pinta el checklist del modal de detalle y de donde salen los contadores de
--    avance («2 de 19 tareas»). `automatica` marca las actividades que el
--    sistema cumple solo, sin acción de GTH (los avisos preventivos a TI y al
--    responsable de obra, que se disparan al registrar la solicitud).
--
-- Idempotente: se puede correr múltiples veces sin duplicar ni romper nada.
-- ============================================================================

BEGIN;

-- ── 1) Carta oferta firmada ────────────────────────────────────────────────
ALTER TABLE gth_onboarding
    ADD COLUMN IF NOT EXISTS carta_firmada_nombre             varchar(300),
    ADD COLUMN IF NOT EXISTS carta_firmada_url                text,
    ADD COLUMN IF NOT EXISTS carta_firmada_item_id            varchar(200),
    ADD COLUMN IF NOT EXISTS carta_firmada_drive_id           varchar(200),
    ADD COLUMN IF NOT EXISTS carta_firmada_subida_date_time   timestamptz,
    ADD COLUMN IF NOT EXISTS carta_firmada_subida_user_id     integer,
    ADD COLUMN IF NOT EXISTS carta_firmada_aprobada_date_time timestamptz,
    ADD COLUMN IF NOT EXISTS carta_firmada_aprobada_user_id   integer,
    ADD COLUMN IF NOT EXISTS file_digital_drive_id            varchar(200),
    ADD COLUMN IF NOT EXISTS file_digital_item_id             varchar(200),
    ADD COLUMN IF NOT EXISTS file_digital_ruta                text;

COMMENT ON COLUMN gth_onboarding.carta_firmada_url IS
    'Carta oferta devuelta firmada por el colaborador. Se guarda en la misma carpeta de SharePoint que la carta oferta enviada.';
COMMENT ON COLUMN gth_onboarding.carta_firmada_aprobada_date_time IS
    'Momento en que GTH aprobó la carta firmada (RF-ONB-02). Sin esto el onboarding no avanza de fase.';
COMMENT ON COLUMN gth_onboarding.file_digital_item_id IS
    'Carpeta de SharePoint del file digital del colaborador. Todos los documentos del onboarding se suben acá.';
COMMENT ON COLUMN gth_onboarding.file_digital_ruta IS
    'Ruta legible de esa carpeta, solo para mostrarla en pantalla.';

-- ── 2) Catálogo del checklist operativo por fase ───────────────────────────
CREATE TABLE IF NOT EXISTS gth_onboarding_actividad (
    gth_onboarding_actividad_id serial       PRIMARY KEY,
    gth_onboarding_fase_id      integer      NOT NULL
        REFERENCES gth_onboarding_fase (gth_onboarding_fase_id),
    codigo                      varchar(60)  NOT NULL,
    nombre                      varchar(300) NOT NULL,
    descripcion                 text,
    orden                       integer      NOT NULL,
    automatica                  boolean      NOT NULL DEFAULT false,
    created_date_time           timestamptz  NOT NULL DEFAULT now(),
    created_user_id             integer,
    updated_date_time           timestamptz,
    updated_user_id             integer,
    active                      boolean      NOT NULL DEFAULT true,
    state                       boolean      NOT NULL DEFAULT true
);

COMMENT ON TABLE gth_onboarding_actividad IS
    'Checklist operativo del onboarding (requerimiento funcional 9.2): actividades obligatorias por fase.';
COMMENT ON COLUMN gth_onboarding_actividad.automatica IS
    'true = la cumple el sistema sin acción de GTH (avisos preventivos disparados desde la solicitud). OJO: ese envío todavía no está implementado (RF-ONB-13/14, sprint S2); el checklist ya las cuenta como hechas porque así está definido el proceso.';

-- Un solo registro vivo por código (mismo criterio que el resto de catálogos GTH).
CREATE UNIQUE INDEX IF NOT EXISTS ix_gth_onboarding_actividad_codigo
    ON gth_onboarding_actividad (codigo) WHERE state = true;
CREATE INDEX IF NOT EXISTS ix_gth_onboarding_actividad_fase_id
    ON gth_onboarding_actividad (gth_onboarding_fase_id);

-- Seed del checklist. El orden dentro de cada fase es el del documento funcional.
INSERT INTO gth_onboarding_actividad (gth_onboarding_fase_id, codigo, nombre, descripcion, orden, automatica)
SELECT f.gth_onboarding_fase_id, v.codigo, v.nombre, v.descripcion, v.orden, v.automatica
FROM (VALUES
    -- Fase 1 · Carta oferta firmada
    ('CARTA_OFERTA_FIRMADA', 'REVISAR_APROBAR_CARTA',
     'Revisar y aprobar carta oferta firmada adjunta por el nuevo colaborador',
     'GTH abre la carta firmada, valida las condiciones y la aprueba antes de continuar.', 1, false),
    ('CARTA_OFERTA_FIRMADA', 'AVISO_TI',
     'Notificación automática enviada a TI al registrar la solicitud para prever equipos y accesos',
     'Aviso preventivo que se genera desde el registro de la solicitud, sin esperar el correo de bienvenida.', 2, true),
    ('CARTA_OFERTA_FIRMADA', 'AVISO_OBRA',
     'Notificación automática enviada al responsable de obra al registrar la solicitud para prever espacio y condiciones de ingreso',
     'Aviso preventivo de espacio al administrador de la obra o sede, con ruta de respaldo si no existe el responsable.', 3, true),

    -- Fase 2 · File digital
    ('FILE_DIGITAL', 'GUARDAR_CARTA_SHAREPOINT',
     'Guardar automáticamente la carta oferta en SharePoint (en el file del colaborador)',
     'Primera evidencia del file digital: se registra ruta, fecha y usuario.', 1, false),

    -- Fase 3 · Correo de bienvenida
    ('CORREO_BIENVENIDA', 'ENVIAR_BIENVENIDA',
     'Enviar correo con documentos normativos y link del formulario',
     'Plantilla de bienvenida con manuales, documentos requeridos, fecha límite y contacto de GTH.', 1, false),
    ('CORREO_BIENVENIDA', 'INFORMAR_CLIENTE_INTERNO',
     'Informar al cliente interno la fecha de ingreso confirmada',
     'Fecha confirmada, puesto, área, destino, razón social y código del requerimiento.', 2, false),

    -- Fase 4 · Formulario web
    ('FORMULARIO_WEB', 'RECIBIR_FORMULARIO',
     'Recibir el formulario completado por el nuevo colaborador', NULL, 1, false),
    ('FORMULARIO_WEB', 'VALIDAR_DATOS',
     'Validar datos personales', NULL, 2, false),
    ('FORMULARIO_WEB', 'VALIDAR_DOCUMENTOS',
     'Validar documentación cargada; aprobar u observar por documento', NULL, 3, false),

    -- Fase 5 · Preinicio
    ('PREINICIO', 'CORREO_ALTA_BIENESTAR',
     'Enviar correo de alta e ingreso a Bienestar y responsables configurados', NULL, 1, false),
    ('PREINICIO', 'ESTADO_EMO',
     'Consultar estado sincronizado de EMO',
     'Se consulta a Salud Ocupacional; Onboarding no duplica la programación.', 2, false),
    ('PREINICIO', 'ESTADO_SSOMA',
     'Consultar estado sincronizado de inducción SSOMA',
     'Se consulta a Gestión SSOMA (fecha, proyecto, asistencia y resultado).', 3, false),
    ('PREINICIO', 'ELABORAR_CONTRATO',
     'Elaborar contrato', NULL, 4, false),
    ('PREINICIO', 'FECHAS_FIRMA_FOTO_INDUCCION',
     'Definir y enviar fecha, hora y lugar de firma, fotografía e inducción', NULL, 5, false),
    ('PREINICIO', 'KIT_EPP_CARTEL',
     'Enviar correo para preparar kit, EPP si aplica y cartel', NULL, 6, false),
    ('PREINICIO', 'CONFIRMAR_ASISTENCIA',
     'Confirmar asistencia, fotografía y EPP', NULL, 7, false),
    ('PREINICIO', 'CONTRATO_FIRMADO_FILE',
     'Adjuntar contrato firmado al file digital', NULL, 8, false),

    -- Fase 6 · Cierre onboarding
    ('CIERRE_ONBOARDING', 'CERRAR_ONBOARDING',
     'Cerrar onboarding con evidencia completa', NULL, 1, false),

    -- Fase 7 · Base maestra
    ('BASE_MAESTRA', 'MIGRAR_BASE_MAESTRA',
     'Migrar información a la base oficial', NULL, 1, false)
) AS v (fase_codigo, codigo, nombre, descripcion, orden, automatica)
JOIN gth_onboarding_fase f ON f.codigo = v.fase_codigo AND f.state = true
WHERE NOT EXISTS (
    SELECT 1 FROM gth_onboarding_actividad a WHERE a.codigo = v.codigo AND a.state = true
);

-- El seed se ata a gth_onboarding_fase por código: si alguna fase no existiera, su bloque de
-- actividades se saltaría en silencio y el avance quedaría mal calculado. Se corta acá si el
-- catálogo no quedó completo.
DO $$
DECLARE total integer;
BEGIN
    SELECT count(*) INTO total FROM gth_onboarding_actividad WHERE state = true AND active = true;
    IF total <> 19 THEN
        RAISE EXCEPTION 'El checklist quedó con % actividades vigentes en vez de 19. Revisa que gth_onboarding_fase tenga las 7 fases con sus códigos.', total;
    END IF;
END $$;

COMMIT;

-- Verificación: 3 / 1 / 2 / 3 / 8 / 1 / 1 actividades por fase, en ese orden.
SELECT f.orden, f.codigo AS fase, count(*) AS actividades
FROM gth_onboarding_actividad a
JOIN gth_onboarding_fase f ON f.gth_onboarding_fase_id = a.gth_onboarding_fase_id
WHERE a.state AND f.state
GROUP BY f.orden, f.codigo
ORDER BY f.orden;
