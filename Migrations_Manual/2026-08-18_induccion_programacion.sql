-- Programación de Inducciones SSOMA: rotación circular de proyectos + calendario generado
-- automáticamente (L/M/V, saltando feriados) + aviso por correo. Ver
-- Features/SsomaModule/InduccionProgramacionFeature/. Ejecutar manualmente en pgAdmin.

BEGIN;

-- Proyectos en la cola de rotación, con su orden. El orden se define una vez a mano y la
-- generación automática lo sigue solo; un proyecto nuevo se agrega al final.
CREATE TABLE IF NOT EXISTS ss_induccion_rotacion_proyecto (
    id           serial PRIMARY KEY,
    proyecto_id  integer NOT NULL REFERENCES project (project_id),
    orden        integer NOT NULL,
    activo       boolean NOT NULL DEFAULT true,
    created_at   timestamp NOT NULL DEFAULT now(),
    updated_at   timestamp NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_induccion_rotacion_proyecto
    ON ss_induccion_rotacion_proyecto (proyecto_id);

-- Fila única (id = 1): recuerda hasta dónde avanzó la generación automática, para que agregar
-- un proyecto nuevo a la rotación no reinicie el orden de los demás.
CREATE TABLE IF NOT EXISTS ss_induccion_rotacion_cursor (
    id                          integer PRIMARY KEY DEFAULT 1,
    ultimo_proyecto_rotacion_id integer NULL REFERENCES ss_induccion_rotacion_proyecto (id),
    ultima_fecha_generada       date NULL,
    updated_at                  timestamp NULL,
    CONSTRAINT ck_induccion_rotacion_cursor_id CHECK (id = 1)
);

-- Una fecha concreta de inducción para un proyecto. Generada automáticamente por la rotación;
-- editable a mano (reasignar/cancelar/reprogramar) sin afectar el avance de la cola.
CREATE TABLE IF NOT EXISTS ss_induccion_programacion (
    id                    serial PRIMARY KEY,
    fecha                 date NOT NULL,
    proyecto_id           integer NOT NULL REFERENCES project (project_id),
    estado                varchar(20) NOT NULL DEFAULT 'Programada',
    es_manual             boolean NOT NULL DEFAULT false,
    motivo_cambio         varchar(500) NULL,
    aviso_enviado         boolean NOT NULL DEFAULT false,
    fecha_aviso_enviado   timestamp NULL,
    created_at            timestamp NOT NULL DEFAULT now(),
    updated_at            timestamp NULL
);

CREATE INDEX IF NOT EXISTS ix_induccion_programacion_fecha ON ss_induccion_programacion (fecha);

COMMIT;
