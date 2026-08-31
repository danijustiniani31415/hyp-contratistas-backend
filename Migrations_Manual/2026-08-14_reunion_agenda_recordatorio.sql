-- ─────────────────────────────────────────────────────────────────────────────
-- Agenda de reunión (temas a tratar) + recordatorio automático para que los
-- convocados carguen sus temas antes de la reunión.
--
-- Un tema del catálogo (reunion_tema) pasa a ser también la "plantilla" de la
-- reunión: define si requiere agenda, si esa agenda es fija (siempre el mismo
-- texto, editado una sola vez acá) o dinámica (cada participante propone sus
-- temas antes de cada ocurrencia), y con cuántas horas de anticipación se debe
-- recordar a los convocados que carguen sus temas.
--
-- reunion.reunion_tema_id guarda una referencia al tema del catálogo elegido al
-- agendar (null si el tema fue personalizado y no se guardó como recurrente):
-- así el job de recordatorios puede resolver, para cada reunión ya agendada, la
-- configuración de agenda/recordatorio vigente en su momento.
-- ─────────────────────────────────────────────────────────────────────────────

ALTER TABLE reunion_tema ADD COLUMN IF NOT EXISTS requiere_agenda BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE reunion_tema ADD COLUMN IF NOT EXISTS agenda_fija BOOLEAN NOT NULL DEFAULT FALSE;
ALTER TABLE reunion_tema ADD COLUMN IF NOT EXISTS agenda_texto TEXT;
-- Horas de anticipación para el recordatorio (admite decimales, ej. 15.5 = 15h30m antes).
ALTER TABLE reunion_tema ADD COLUMN IF NOT EXISTS recordatorio_horas_antes NUMERIC(5,2);

ALTER TABLE reunion ADD COLUMN IF NOT EXISTS reunion_tema_id INT REFERENCES reunion_tema(reunion_tema_id);

-- Temas a tratar propuestos por cada participante para una ocurrencia concreta de
-- reunión (solo aplica cuando la reunión es de agenda dinámica).
CREATE TABLE IF NOT EXISTS reunion_agenda_item (
    reunion_agenda_item_id  SERIAL PRIMARY KEY,
    reunion_id               INT NOT NULL REFERENCES reunion(reunion_id),
    worker_id                 INT NOT NULL REFERENCES workers(id),
    descripcion                TEXT NOT NULL,
    orden                       INT NOT NULL DEFAULT 0,
    created_date_time           TIMESTAMPTZ NOT NULL,
    created_user_id               INT NOT NULL,
    updated_date_time              TIMESTAMPTZ,
    updated_user_id                  INT,
    active                             BOOLEAN NOT NULL DEFAULT TRUE,
    state                               BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE INDEX IF NOT EXISTS ix_reunion_agenda_item_reunion ON reunion_agenda_item(reunion_id);

-- Evita reenviar el recordatorio de una misma reunión más de una vez (idempotencia
-- del job, que corre periódicamente vía cron externo).
CREATE TABLE IF NOT EXISTS reunion_recordatorio_log (
    reunion_recordatorio_log_id  SERIAL PRIMARY KEY,
    reunion_id                     INT NOT NULL UNIQUE REFERENCES reunion(reunion_id),
    enviado_date_time                TIMESTAMPTZ NOT NULL
);

-- Tipo de notificación in-app de la campanita para el recordatorio de agenda.
INSERT INTO notificacion_tipo (codigo, nombre, orden, created_date_time, active, state)
VALUES ('ACTAS_REUNION_AGENDA', 'Carga tu agenda de reunión', 100, now(), true, true)
ON CONFLICT (codigo) WHERE (state = true)
DO UPDATE SET nombre = EXCLUDED.nombre, active = true, updated_date_time = now();
