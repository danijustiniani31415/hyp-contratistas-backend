-- Guías de Remisión Electrónica - GRE (CONTEXT_LOGISTICA.md sección 3, Fase 3, punto 8).
-- Alcance: emisión electrónica real ante el SEE de SUNAT (sin OSE intermediario), no solo
-- registro interno. El envío/firma/SOAP vive en el módulo GuiasRemisionModule; acá solo el
-- schema del documento (header + items) y los datos maestros que la GRE necesita y que
-- lb_almacen/lb_proyecto todavía no tenían (dirección + ubigeo).

-- lb_almacen y lb_proyecto ya existen (Fase 1) pero sin dirección/ubigeo — la GRE exige punto
-- de partida y punto de llegada con ubigeo (catálogo SUNAT de 6 dígitos) y dirección completa.
-- Nullable: no rompe almacenes/proyectos ya creados; se completa antes de emitir la primera GRE.
ALTER TABLE lb_almacen ADD COLUMN IF NOT EXISTS direccion VARCHAR(250);
ALTER TABLE lb_almacen ADD COLUMN IF NOT EXISTS ubigeo VARCHAR(6);
ALTER TABLE lb_proyecto ADD COLUMN IF NOT EXISTS direccion VARCHAR(250);
ALTER TABLE lb_proyecto ADD COLUMN IF NOT EXISTS ubigeo VARCHAR(6);

CREATE TABLE lb_guia_remision (
    id                      BIGSERIAL PRIMARY KEY,
    -- Serie SUNAT para GRE remitente: alfanumérica de 4 caracteres, empieza con 'T' (ej. T001).
    -- Un solo talonario por ahora (el de SunatGreSettings:Serie) — si más adelante se necesita
    -- más de una serie activa (ej. otra sede), se agrega una segunda secuencia sin tocar esta tabla.
    serie                   VARCHAR(4) NOT NULL,
    numero                  BIGINT NOT NULL,
    estado                  VARCHAR(20) NOT NULL DEFAULT 'BORRADOR',
        -- BORRADOR (creada, aún no transmitida) -> ENVIADA (SOAP disparado, esperando CDR/ticket)
        -- -> ACEPTADA | RECHAZADA (según CDR) -> ANULADA (comunicación de baja posterior)

    -- Catálogo 20 SUNAT (motivo de traslado). Empieza en '04' porque el caso de uso principal de
    -- HP Constructores es traslado propio almacén central -> obra, no venta a terceros.
    motivo_traslado         VARCHAR(2) NOT NULL DEFAULT '04',
    -- Catálogo 18 SUNAT: '01' transporte público (con transportista RUC), '02' privado (vehículo propio)
    modalidad_traslado      VARCHAR(2) NOT NULL DEFAULT '02',
    fecha_traslado          DATE NOT NULL,
    peso_bruto_total        NUMERIC(12,3) NOT NULL,
    peso_bruto_unidad       VARCHAR(3) NOT NULL DEFAULT 'KGM', -- catálogo 03 SUNAT (unidad de medida)
    num_bultos              INT,

    -- Punto de partida: casi siempre un almacén propio.
    almacen_origen_id       INT NOT NULL REFERENCES lb_almacen(id),
    -- Punto de llegada cuando es traslado a otro almacén/obra propia (motivo 04). Si el destino
    -- es un tercero (venta/devolución a proveedor), queda NULL y se usa destinatario_* abajo.
    almacen_destino_id      INT REFERENCES lb_almacen(id),
    destinatario_ruc        VARCHAR(11),
    destinatario_razon_social VARCHAR(200),

    -- Transportista (obligatorio solo si modalidad_traslado = '01' público)
    transportista_ruc           VARCHAR(11),
    transportista_razon_social  VARCHAR(200),
    -- Vehículo/conductor (obligatorio solo si modalidad_traslado = '02' privado)
    vehiculo_placa          VARCHAR(10),
    conductor_nombres       VARCHAR(150),
    conductor_licencia      VARCHAR(20),

    observacion             TEXT,
    -- De dónde nace el traslado (opcional): un Pedido u Orden de Compra que origina el despacho.
    referencia_tipo         VARCHAR(30),
    referencia_id           BIGINT,

    creado_por_usuario_sistema_id BIGINT NOT NULL REFERENCES lb_usuario_sistema(id),
    creado_en               TIMESTAMPTZ NOT NULL DEFAULT now(),

    -- Control de envío a SUNAT
    xml_nombre_archivo      VARCHAR(60),  -- {RUC}-09-{serie}-{numero}
    xml_hash_firma          VARCHAR(100), -- DigestValue de la firma XMLDSig, para trazabilidad
    ticket                  VARCHAR(50),  -- si SUNAT responde de forma asíncrona
    cdr_codigo_respuesta    VARCHAR(10),
    cdr_descripcion         TEXT,
    enviado_en              TIMESTAMPTZ,
    respondido_en           TIMESTAMPTZ,

    UNIQUE (serie, numero)
);
CREATE INDEX idx_guia_remision_estado ON lb_guia_remision(estado);
CREATE INDEX idx_guia_remision_almacen_origen ON lb_guia_remision(almacen_origen_id);
CREATE INDEX idx_guia_remision_referencia ON lb_guia_remision(referencia_tipo, referencia_id);

CREATE TABLE lb_guia_remision_item (
    id                  BIGSERIAL PRIMARY KEY,
    guia_remision_id    BIGINT NOT NULL REFERENCES lb_guia_remision(id),
    producto_id         BIGINT NOT NULL REFERENCES lb_producto(id),
    talla               VARCHAR(10) NOT NULL DEFAULT '',
    cantidad            NUMERIC(12,2) NOT NULL,
    unidad_medida       VARCHAR(3) NOT NULL DEFAULT 'NIU' -- catálogo 03 SUNAT (NIU = unidad)
);
CREATE INDEX idx_guia_remision_item_guia ON lb_guia_remision_item(guia_remision_id);

CREATE SEQUENCE lb_guia_remision_correlativo START 1;

INSERT INTO lb_permiso (codigo, descripcion) VALUES
('GUIA_REMISION_CREAR',  'Crear guías de remisión (borrador) y descontar stock del almacén de origen'),
('GUIA_REMISION_ENVIAR', 'Transmitir la guía de remisión al SEE de SUNAT y anularlas'),
('GUIA_REMISION_VER',    'Ver el listado y detalle de guías de remisión');

INSERT INTO lb_rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id FROM lb_rol r, lb_permiso p
WHERE p.codigo IN ('GUIA_REMISION_CREAR', 'GUIA_REMISION_VER')
  AND r.codigo IN ('ALMACENERO', 'LOGISTICA', 'ADMIN');

INSERT INTO lb_rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id FROM lb_rol r, lb_permiso p
WHERE p.codigo = 'GUIA_REMISION_ENVIAR'
  AND r.codigo IN ('LOGISTICA', 'ADMIN');
