-- Migración manual (pgAdmin) — módulo Gestión de Revisiones de Arquitectura Comercial.
-- Ejecutar directamente contra la BD PostgreSQL. No usar dotnet ef.
--
-- Tablas nuevas y separadas de ac_observaciones (decisión: no reusar la tabla
-- de Observaciones, para no arriesgar ese módulo ya en producción).

CREATE TABLE IF NOT EXISTS ac_revisiones (
    id            SERIAL PRIMARY KEY,
    proyecto_id   INTEGER NOT NULL REFERENCES project (project_id) ON DELETE CASCADE,
    tipo          TEXT NOT NULL,   -- R1 | R2 | R1-AC | R2-AC | RF-AC (lista fija en código)
    lugar         TEXT NOT NULL,   -- catálogo (AcCatalogoItem tipo LugarRevision) o texto libre
    nombre        TEXT NOT NULL,   -- generado: "{tipo}-{proyecto}-{lugar}"
    activo        BOOLEAN NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMP NOT NULL DEFAULT (now() AT TIME ZONE 'utc')
);

CREATE INDEX IF NOT EXISTS ix_ac_revisiones_proyecto_id ON ac_revisiones (proyecto_id);

CREATE TABLE IF NOT EXISTS ac_revision_observaciones (
    id                    SERIAL PRIMARY KEY,
    revision_id           INTEGER NOT NULL REFERENCES ac_revisiones (id) ON DELETE CASCADE,
    fecha                 TIMESTAMP NOT NULL,
    persona_reporta       TEXT,
    zona_ambiente         TEXT,
    partida_reportada     TEXT,
    descripcion           TEXT NOT NULL,
    estado                TEXT NOT NULL DEFAULT 'Pendiente',
    plazo_levantamiento   TIMESTAMP,
    creado_por            TEXT,
    origen                TEXT NOT NULL DEFAULT 'Nuevo',
    created_at            TIMESTAMP NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    fecha_levantamiento   TIMESTAMP,
    levanta_por_worker_id INTEGER REFERENCES workers (id)
);

-- Índices desde el día 1 (fecha se usa en ORDER BY y filtros de rango en
-- lista/dashboard/stats — con Observaciones nos faltó y hubo que agregarlo
-- después con la lista ya en producción).
CREATE INDEX IF NOT EXISTS ix_ac_revision_observaciones_revision_id       ON ac_revision_observaciones (revision_id);
CREATE INDEX IF NOT EXISTS ix_ac_revision_observaciones_estado            ON ac_revision_observaciones (estado);
CREATE INDEX IF NOT EXISTS ix_ac_revision_observaciones_fecha             ON ac_revision_observaciones (fecha DESC);
CREATE INDEX IF NOT EXISTS ix_ac_revision_observaciones_partida_reportada ON ac_revision_observaciones (partida_reportada);
CREATE INDEX IF NOT EXISTS ix_ac_revision_observaciones_levanta_por       ON ac_revision_observaciones (levanta_por_worker_id);

CREATE TABLE IF NOT EXISTS ac_revision_observacion_fotos (
    id                       SERIAL PRIMARY KEY,
    revision_observacion_id  INTEGER NOT NULL REFERENCES ac_revision_observaciones (id) ON DELETE CASCADE,
    tipo                     TEXT NOT NULL DEFAULT 'Observacion',
    url                      TEXT NOT NULL,
    orden                    INTEGER NOT NULL DEFAULT 0,
    created_at               TIMESTAMP NOT NULL DEFAULT (now() AT TIME ZONE 'utc')
);

CREATE INDEX IF NOT EXISTS ix_ac_revision_observacion_fotos_revision_observacion_id
    ON ac_revision_observacion_fotos (revision_observacion_id);

-- Seed del catálogo "Lugar a revisar" (AcCatalogoItem, tipo LugarRevision) — valores
-- históricos tomados de TipodeRevision.csv. Cada INSERT valida por separado que ese
-- nombre no exista ya, para poder correr la migración más de una vez sin duplicar.

INSERT INTO ac_catalogo_items (tipo, nombre, orden, activo, created_at)
SELECT 'LugarRevision', 'Sala de ventas', 1, TRUE, (now() AT TIME ZONE 'utc')
WHERE NOT EXISTS (SELECT 1 FROM ac_catalogo_items WHERE tipo = 'LugarRevision' AND nombre = 'Sala de ventas');

INSERT INTO ac_catalogo_items (tipo, nombre, orden, activo, created_at)
SELECT 'LugarRevision', 'Pilotos', 2, TRUE, (now() AT TIME ZONE 'utc')
WHERE NOT EXISTS (SELECT 1 FROM ac_catalogo_items WHERE tipo = 'LugarRevision' AND nombre = 'Pilotos');

INSERT INTO ac_catalogo_items (tipo, nombre, orden, activo, created_at)
SELECT 'LugarRevision', 'Sala de juegos de niños', 3, TRUE, (now() AT TIME ZONE 'utc')
WHERE NOT EXISTS (SELECT 1 FROM ac_catalogo_items WHERE tipo = 'LugarRevision' AND nombre = 'Sala de juegos de niños');

INSERT INTO ac_catalogo_items (tipo, nombre, orden, activo, created_at)
SELECT 'LugarRevision', 'Comedor', 4, TRUE, (now() AT TIME ZONE 'utc')
WHERE NOT EXISTS (SELECT 1 FROM ac_catalogo_items WHERE tipo = 'LugarRevision' AND nombre = 'Comedor');

INSERT INTO ac_catalogo_items (tipo, nombre, orden, activo, created_at)
SELECT 'LugarRevision', 'Areas comunes', 5, TRUE, (now() AT TIME ZONE 'utc')
WHERE NOT EXISTS (SELECT 1 FROM ac_catalogo_items WHERE tipo = 'LugarRevision' AND nombre = 'Areas comunes');
