-- Facturación electrónica SUNAT: factura (01), nota de crédito (07), nota de débito (08) y su baja (RA).
-- Ver DISENO-FACTURACION-SUNAT.md. Solo facturas: las boletas y el resumen diario quedan fuera por ahora.

-- Talonarios. El correlativo se toma con bloqueo de fila al emitir; nunca se reutiliza un número ya enviado.
CREATE TABLE IF NOT EXISTS lb_cpe_serie (
    id              serial PRIMARY KEY,
    tipo_documento  varchar(2)  NOT NULL,           -- 01 factura, 07 nota de crédito, 08 nota de débito
    serie           varchar(4)  NOT NULL,           -- F001, FC01, FD01...
    ultimo_numero   bigint      NOT NULL DEFAULT 0, -- último número YA usado; el siguiente es +1
    activa          boolean     NOT NULL DEFAULT true,
    creado_en       timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_lb_cpe_serie UNIQUE (tipo_documento, serie)
);

CREATE TABLE IF NOT EXISTS lb_cpe (
    id                  bigserial PRIMARY KEY,
    tipo_documento      varchar(2)   NOT NULL,
    serie               varchar(4)   NOT NULL,
    numero              bigint       NULL,            -- null mientras es borrador
    estado              varchar(20)  NOT NULL DEFAULT 'BORRADOR',
                                                      -- BORRADOR, ACEPTADO, ACEPTADO_OBS, RECHAZADO, BAJA_PENDIENTE, ANULADO

    fecha_emision       date         NOT NULL,
    hora_emision        time         NULL,
    fecha_vencimiento   date         NULL,
    moneda              varchar(3)   NOT NULL DEFAULT 'PEN',
    tipo_cambio         numeric(10,4) NULL,
    tipo_operacion      varchar(4)   NOT NULL DEFAULT '0101',  -- 0101 venta interna, 1001 sujeta a detracción, 0200 exportación

    cliente_tipo_doc    varchar(1)   NOT NULL DEFAULT '6',     -- 6 RUC, 1 DNI, 4 CE, 7 pasaporte, 0 sin documento
    cliente_numero_doc  varchar(15)  NOT NULL,
    cliente_razon       varchar(200) NOT NULL,
    cliente_direccion   varchar(250) NULL,
    cliente_pais        varchar(2)   NULL,                     -- ISO 3166 (solo exportación)

    total_gravado       numeric(14,2) NOT NULL DEFAULT 0,
    total_exonerado     numeric(14,2) NOT NULL DEFAULT 0,
    total_inafecto      numeric(14,2) NOT NULL DEFAULT 0,
    total_exportacion   numeric(14,2) NOT NULL DEFAULT 0,
    total_descuentos    numeric(14,2) NOT NULL DEFAULT 0,
    igv                 numeric(14,2) NOT NULL DEFAULT 0,
    total               numeric(14,2) NOT NULL DEFAULT 0,
    precios_incluyen_igv boolean     NOT NULL DEFAULT false,

    forma_pago          varchar(10)  NOT NULL DEFAULT 'CONTADO',   -- CONTADO | CREDITO
    cuotas_json         text         NULL,                          -- [{ "monto": 100.00, "fecha": "2026-12-01" }]

    detraccion_codigo       varchar(3)    NULL,                     -- catálogo 54 (ej. 037)
    detraccion_porcentaje   numeric(5,2)  NULL,
    detraccion_monto        numeric(14,2) NULL,                     -- siempre en soles
    detraccion_cuenta       varchar(30)   NULL,                     -- cuenta de H&P en el Banco de la Nación

    -- Solo notas: el comprobante que modifican y el motivo (catálogos 09 y 10).
    ref_cpe_id          bigint       NULL REFERENCES lb_cpe(id),
    ref_tipo_documento  varchar(2)   NULL,
    ref_serie           varchar(4)   NULL,
    ref_numero          bigint       NULL,
    motivo_codigo       varchar(2)   NULL,
    motivo_descripcion  varchar(250) NULL,

    observacion         text         NULL,
    orden_compra_ref    varchar(60)  NULL,
    guia_ref            varchar(60)  NULL,                          -- guía de remisión que respalda el traslado (ej. T001-123)
    contrato_id         integer      NULL,                          -- lb_contrato.id (sin FK declarada para poder aplicar el script con un usuario sin permisos de REFERENCES)
    proyecto_id         integer      NULL,

    -- Respaldo legal: lo enviado y lo que respondió SUNAT se conserva siempre.
    xml_nombre_archivo  varchar(80)  NULL,
    xml_hash_firma      varchar(100) NULL,
    xml_firmado         text         NULL,
    cdr_xml             text         NULL,
    sunat_codigo        varchar(10)  NULL,
    sunat_descripcion   text         NULL,
    enviado_en          timestamptz  NULL,
    respondido_en       timestamptz  NULL,

    -- Baja (comunicación RA): SUNAT responde con un ticket que se consulta después.
    baja_motivo         varchar(250) NULL,
    baja_ticket         varchar(40)  NULL,
    baja_identificador  varchar(30)  NULL,                          -- RA-AAAAMMDD-n
    baja_cdr_xml        text         NULL,
    baja_en             timestamptz  NULL,

    creado_por_usuario_sistema_id bigint      NOT NULL,
    creado_en                     timestamptz NOT NULL DEFAULT now(),
    emitido_por_usuario_sistema_id bigint     NULL
);

ALTER TABLE lb_cpe ADD COLUMN IF NOT EXISTS cliente_pais varchar(2) NULL;

-- Un número, una vez. Los borradores (numero null) no cuentan.
CREATE UNIQUE INDEX IF NOT EXISTS uq_lb_cpe_numero ON lb_cpe (tipo_documento, serie, numero) WHERE numero IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_lb_cpe_estado ON lb_cpe (estado);
CREATE INDEX IF NOT EXISTS ix_lb_cpe_cliente ON lb_cpe (cliente_numero_doc);
CREATE INDEX IF NOT EXISTS ix_lb_cpe_contrato ON lb_cpe (contrato_id);

CREATE TABLE IF NOT EXISTS lb_cpe_item (
    id                  bigserial PRIMARY KEY,
    cpe_id              bigint        NOT NULL REFERENCES lb_cpe(id) ON DELETE CASCADE,
    orden               integer       NOT NULL,
    codigo              varchar(30)   NULL,
    descripcion         varchar(500)  NOT NULL,
    unidad_medida       varchar(3)    NOT NULL DEFAULT 'NIU',      -- catálogo 03 (NIU, ZZ servicio, KGM, MTR...)
    cantidad            numeric(14,4) NOT NULL,
    valor_unitario      numeric(16,6) NOT NULL,                    -- sin IGV
    precio_unitario     numeric(16,6) NOT NULL,                    -- tal como lo digitó el usuario (con o sin IGV según precios_incluyen_igv)
    descuento           numeric(14,2) NOT NULL DEFAULT 0,
    valor_venta         numeric(14,2) NOT NULL,                    -- cantidad x valor unitario - descuento
    afectacion_igv      varchar(2)    NOT NULL DEFAULT '10',       -- catálogo 07: 10 gravado, 20 exonerado, 30 inafecto, 40 exportación
    igv                 numeric(14,2) NOT NULL DEFAULT 0,
    producto_id         integer       NULL,
    partida_id          integer       NULL
);
CREATE INDEX IF NOT EXISTS ix_lb_cpe_item_cpe ON lb_cpe_item (cpe_id);

-- Cada intento de envío a SUNAT, para soporte y para saber si un reintento es seguro.
CREATE TABLE IF NOT EXISTS lb_cpe_envio (
    id            bigserial PRIMARY KEY,
    cpe_id        bigint       NOT NULL REFERENCES lb_cpe(id) ON DELETE CASCADE,
    operacion     varchar(20)  NOT NULL,                  -- ENVIO, CONSULTA_CDR, BAJA, CONSULTA_BAJA
    ambiente      varchar(12)  NOT NULL,
    ok            boolean      NOT NULL,
    codigo        varchar(20)  NULL,
    mensaje       text         NULL,
    creado_en     timestamptz  NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_lb_cpe_envio_cpe ON lb_cpe_envio (cpe_id);

-- Series iniciales. El correlativo real de H&P está PENDIENTE de definir: hay que fijar ultimo_numero con el último número
-- que ya se emitió por otros medios antes de usar el módulo en producción (UPDATE lb_cpe_serie SET ultimo_numero = N ...).
INSERT INTO lb_cpe_serie (tipo_documento, serie, ultimo_numero) VALUES
('01', 'F001', 0),
('07', 'FC01', 0),
('08', 'FD01', 0)
ON CONFLICT DO NOTHING;

-- Los permisos se insertan con id explícito (max + n): la secuencia lb_permiso_id_seq puede estar atrasada respecto a los ids
-- sembrados a mano, y un INSERT con nextval chocaría con la llave primaria sin avisar.
INSERT INTO lb_permiso (id, codigo, descripcion)
SELECT (SELECT COALESCE(MAX(id), 0) FROM lb_permiso) + ROW_NUMBER() OVER (ORDER BY v.codigo), v.codigo, v.descripcion
FROM (VALUES
    ('FACTURACION_VER',     'Ver comprobantes electrónicos emitidos (facturas y notas)'),
    ('FACTURACION_CREAR',   'Crear borradores de factura y notas'),
    ('FACTURACION_EMITIR',  'Emitir a SUNAT, consultar el CDR y dar de baja comprobantes electrónicos')
) AS v(codigo, descripcion)
WHERE NOT EXISTS (SELECT 1 FROM lb_permiso p WHERE p.codigo = v.codigo);

INSERT INTO lb_rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id FROM lb_rol r, lb_permiso p
WHERE p.codigo IN ('FACTURACION_VER', 'FACTURACION_CREAR', 'FACTURACION_EMITIR')
  AND r.codigo = 'ADMIN'
  AND NOT EXISTS (SELECT 1 FROM lb_rol_permiso x WHERE x.rol_id = r.id AND x.permiso_id = p.id);
