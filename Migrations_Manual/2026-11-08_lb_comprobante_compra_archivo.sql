-- Copia del PDF/foto de cada comprobante, guardada en la propia base (el servidor no tiene almacenamiento
-- de archivos configurado y su disco se reinicia en cada deploy). Las fotos se reducen antes de subirse, así
-- que cada archivo pesa unos cientos de KB. Los archivos leídos pero nunca confirmados se borran solos al día siguiente.

CREATE TABLE IF NOT EXISTS lb_comprobante_compra_archivo (
    id                      bigserial PRIMARY KEY,
    comprobante_compra_id   bigint        NULL REFERENCES lb_comprobante_compra(id) ON DELETE CASCADE,
    nombre                  varchar(200)  NOT NULL,
    mime                    varchar(60)   NOT NULL,
    contenido               bytea         NOT NULL,
    creado_en               timestamptz   NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_lb_comprobante_compra_archivo_comprobante ON lb_comprobante_compra_archivo (comprobante_compra_id);
