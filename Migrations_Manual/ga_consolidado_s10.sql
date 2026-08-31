-- Consolidado del S10: PDF de respaldo que devuelve el S10 una vez que la planilla de
-- rendicion quedo registrada en el sistema contable. Se adjunta desde las dos pantallas de
-- salidas (Solicitud de Salidas y Gestion de Salidas) a una salida YA rendida.
--
-- El ambito es excluyente:
--   rendicion_id -> el PDF cubre toda la planilla (todas las salidas de ese batch). Es el
--                   caso normal y el que viene preseleccionado en la pantalla: una planilla
--                   equivale a un registro en el S10.
--   solicitud_id -> el PDF cubre solo esa salida puntual.
--
-- Reemplazar el archivo no borra el anterior: la fila vieja queda con state = false
-- (auditoria) y los indices unicos parciales garantizan a lo sumo un consolidado vigente
-- por rendicion y uno por solicitud.
--
-- No necesita configuracion extra: el PDF se sube a la misma carpeta de SharePoint que las
-- planillas de rendicion (ga_rendicion_folder), que ya esta registrada en produccion.

CREATE TABLE IF NOT EXISTS ga_consolidado_s10 (
    id              serial PRIMARY KEY,
    rendicion_id    integer NULL REFERENCES ga_rendicion(id),
    solicitud_id    integer NULL REFERENCES ga_solicitud_salida(id),
    pdf_url         text NOT NULL,
    pdf_item_id     text NULL,
    pdf_drive_id    text NULL,
    pdf_filename    text NOT NULL,
    uploaded_by_id  integer NOT NULL REFERENCES app_user(user_id),
    uploaded_at     timestamptz NOT NULL DEFAULT now(),
    state           boolean NOT NULL DEFAULT true,
    CONSTRAINT chk_ga_consolidado_s10_ambito_unico
        CHECK ( (rendicion_id IS NOT NULL AND solicitud_id IS NULL)
             OR (rendicion_id IS NULL AND solicitud_id IS NOT NULL) )
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_ga_consolidado_s10_rendicion_vigente
    ON ga_consolidado_s10 (rendicion_id) WHERE state AND rendicion_id IS NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_ga_consolidado_s10_solicitud_vigente
    ON ga_consolidado_s10 (solicitud_id) WHERE state AND solicitud_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_ga_consolidado_s10_uploaded_by_id
    ON ga_consolidado_s10 (uploaded_by_id);

COMMENT ON TABLE ga_consolidado_s10 IS
  'PDF Consolidado del S10 de una salida ya rendida. Se asocia a la rendicion/planilla completa (rendicion_id) o solo a una salida puntual (solicitud_id), nunca a ambos. state=false = version reemplazada.';
