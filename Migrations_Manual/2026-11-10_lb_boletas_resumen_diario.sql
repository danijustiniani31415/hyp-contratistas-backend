-- Boletas de venta (03) y resumen diario (RC). Las boletas no se envían una por una: se agrupan por fecha en un resumen
-- que SUNAT procesa de forma asíncrona (ticket). Ver DISENO-FACTURACION-SUNAT.md.

CREATE TABLE IF NOT EXISTS lb_cpe_resumen (
    id                bigserial PRIMARY KEY,
    identificador     varchar(30)  NOT NULL UNIQUE,         -- RC-AAAAMMDD-n
    fecha_referencia  date         NOT NULL,                -- fecha de emisión de las boletas que resume
    fecha_generacion  date         NOT NULL,
    estado            varchar(20)  NOT NULL DEFAULT 'ENVIADO',   -- ENVIADO, ACEPTADO, RECHAZADO
    ticket            varchar(40)  NULL,
    xml_firmado       text         NULL,
    cdr_xml           text         NULL,
    sunat_codigo      varchar(10)  NULL,
    sunat_descripcion text         NULL,
    cantidad          integer      NOT NULL DEFAULT 0,
    creado_por_usuario_sistema_id bigint NOT NULL,
    creado_en         timestamptz  NOT NULL DEFAULT now(),
    respondido_en     timestamptz  NULL
);

-- Estados nuevos de lb_cpe para boletas: POR_ENVIAR (emitida, falta el resumen), EN_RESUMEN (ticket en proceso), BAJA_SOLICITADA (irá en el próximo resumen).
ALTER TABLE lb_cpe ADD COLUMN IF NOT EXISTS resumen_id bigint NULL REFERENCES lb_cpe_resumen(id);
CREATE INDEX IF NOT EXISTS ix_lb_cpe_resumen ON lb_cpe (resumen_id);
CREATE INDEX IF NOT EXISTS ix_lb_cpe_tipo_estado_fecha ON lb_cpe (tipo_documento, estado, fecha_emision);

INSERT INTO lb_cpe_serie (tipo_documento, serie, ultimo_numero) VALUES ('03', 'B001', 0) ON CONFLICT DO NOTHING;
