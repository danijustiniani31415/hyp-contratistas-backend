-- Permite subir varios archivos a un mismo entregable (ej. varios ATS) en vez
-- de reemplazar el único archivo que tenía ss_entregable.url_archivo/nombre_archivo.
-- Las columnas viejas se dejan en ss_entregable (no se usan más desde el código,
-- pero no se borran para no perder el archivo ya subido si algo falla).

CREATE TABLE ss_entregable_archivo (
    id             SERIAL PRIMARY KEY,
    entregable_id  INTEGER NOT NULL REFERENCES ss_entregable(id) ON DELETE CASCADE,
    url_archivo    TEXT NOT NULL,
    nombre_archivo TEXT NOT NULL,
    created_at     TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX ix_ss_entregable_archivo_entregable_id ON ss_entregable_archivo(entregable_id);

-- Migra el archivo único existente (si lo hay) como primer registro de cada entregable.
INSERT INTO ss_entregable_archivo (entregable_id, url_archivo, nombre_archivo, created_at)
SELECT id, url_archivo, COALESCE(nombre_archivo, 'archivo'), COALESCE(updated_at, created_at)
FROM ss_entregable
WHERE url_archivo IS NOT NULL;
