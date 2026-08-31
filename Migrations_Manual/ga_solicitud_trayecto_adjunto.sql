-- Tabla de documentos adjuntos (prueba) por trayecto de una solicitud de salida.
-- Modelo N:1 (N adjuntos por trayecto). Reemplaza al modelo anterior de 1 adjunto
-- embebido en ga_solicitud_trayecto (columnas adjunto_* legacy, que se conservan
-- para no perder los adjuntos históricos).
CREATE TABLE IF NOT EXISTS ga_solicitud_trayecto_adjunto (
    id               serial                   PRIMARY KEY,
    trayecto_id      integer                  NOT NULL,
    adjunto_url      text                     NOT NULL,
    adjunto_item_id  text                     NULL,
    adjunto_drive_id text                     NULL,
    adjunto_filename text                     NOT NULL,
    uploaded_by_id   integer                  NULL,
    uploaded_at      timestamp with time zone NOT NULL DEFAULT now(),
    CONSTRAINT ga_solicitud_trayecto_adjunto_trayecto_id_fkey
        FOREIGN KEY (trayecto_id) REFERENCES ga_solicitud_trayecto(id) ON DELETE CASCADE,
    CONSTRAINT ga_solicitud_trayecto_adjunto_uploaded_by_id_fkey
        FOREIGN KEY (uploaded_by_id) REFERENCES app_user(user_id)
);

CREATE INDEX IF NOT EXISTS ix_ga_solicitud_trayecto_adjunto_trayecto_id
    ON ga_solicitud_trayecto_adjunto (trayecto_id);
