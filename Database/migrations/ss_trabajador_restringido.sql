-- Tabla: trabajadores restringidos de ingreso / reingreso
-- Ejecutar manualmente en Aiven antes de desplegar

CREATE TABLE IF NOT EXISTS ss_trabajador_restringido (
    id              serial PRIMARY KEY,
    dni             varchar(15),
    worker_id       int REFERENCES workers(id),
    apellido_nombre varchar(200),
    motivo          text NOT NULL,
    proyecto_origen varchar(100),
    restringido_por varchar(100),
    fecha_restriccion date,
    activo          boolean NOT NULL DEFAULT true,
    created_at      timestamptz DEFAULT now(),
    updated_at      timestamptz,
    CONSTRAINT uq_restringido_dni UNIQUE (dni)
);

CREATE INDEX IF NOT EXISTS idx_restringido_dni    ON ss_trabajador_restringido(dni)       WHERE activo = true;
CREATE INDEX IF NOT EXISTS idx_restringido_worker ON ss_trabajador_restringido(worker_id) WHERE worker_id IS NOT NULL;
