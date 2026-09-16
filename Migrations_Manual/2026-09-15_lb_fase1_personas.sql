-- Fase 1 (CONTEXT_LOGISTICA.md sección 4.3) — nunca se guardó como archivo, vivía solo en el
-- documento. Se extrae acá para poder aplicarla en producción igual que las demás migraciones.

CREATE TABLE lb_persona (
    id              SERIAL PRIMARY KEY,
    nombres         VARCHAR(150) NOT NULL,
    apellidos       VARCHAR(150) NOT NULL,
    tipo_documento  VARCHAR(20) NOT NULL DEFAULT 'DNI',
    numero_documento VARCHAR(20) NOT NULL,
    fecha_nacimiento DATE,
    telefono        VARCHAR(20),
    email_personal  VARCHAR(150),
    foto_url        TEXT,
    activo          BOOLEAN NOT NULL DEFAULT true,
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now(),
    actualizado_en  TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (tipo_documento, numero_documento)
);

CREATE TABLE lb_tipo_vinculo (
    id              SMALLSERIAL PRIMARY KEY,
    codigo          VARCHAR(30) NOT NULL UNIQUE,
    nombre          VARCHAR(80) NOT NULL,
    requiere_emo        BOOLEAN NOT NULL DEFAULT false,
    requiere_epp        BOOLEAN NOT NULL DEFAULT false,
    requiere_induccion  BOOLEAN NOT NULL DEFAULT false,
    requiere_sctr       BOOLEAN NOT NULL DEFAULT false,
    permite_acceso_obra BOOLEAN NOT NULL DEFAULT false,
    activo          BOOLEAN NOT NULL DEFAULT true
);

INSERT INTO lb_tipo_vinculo (codigo, nombre, requiere_emo, requiere_epp, requiere_induccion, requiere_sctr, permite_acceso_obra) VALUES
('PLANILLA',    'Personal en Planilla',        true,  true,  true,  true,  true),
('CONTRATISTA', 'Contratista / Tercero',       true,  true,  true,  true,  true),
('LOCADOR',     'Locador de Servicios',        false, false, false, false, false),
('PRACTICANTE', 'Practicante',                 true,  true,  true,  false, true);

CREATE TABLE lb_empresa_contratista (
    id              SERIAL PRIMARY KEY,
    razon_social    VARCHAR(200) NOT NULL,
    ruc             VARCHAR(11) UNIQUE,
    activo          BOOLEAN NOT NULL DEFAULT true
);

CREATE TABLE lb_cargo (
    id              SERIAL PRIMARY KEY,
    nombre          VARCHAR(100) NOT NULL UNIQUE,
    activo          BOOLEAN NOT NULL DEFAULT true
);

CREATE TABLE lb_vinculo_laboral (
    id              SERIAL PRIMARY KEY,
    persona_id      INT NOT NULL REFERENCES lb_persona(id),
    tipo_vinculo_id SMALLINT NOT NULL REFERENCES lb_tipo_vinculo(id),
    empresa_contratista_id INT REFERENCES lb_empresa_contratista(id),
    cargo_id        INT REFERENCES lb_cargo(id),
    fecha_inicio    DATE NOT NULL,
    fecha_fin       DATE,
    estado          VARCHAR(20) NOT NULL DEFAULT 'ACTIVO',
    motivo_cese     VARCHAR(200),
    registrado_por_usuario_sistema_id BIGINT,
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_vinculo_persona ON lb_vinculo_laboral(persona_id);
CREATE INDEX idx_vinculo_vigente ON lb_vinculo_laboral(persona_id, estado) WHERE fecha_fin IS NULL;
CREATE UNIQUE INDEX uq_vinculo_laboral_vigente_por_persona
    ON lb_vinculo_laboral(persona_id) WHERE fecha_fin IS NULL;

CREATE TABLE lb_usuario_sistema (
    id              BIGSERIAL PRIMARY KEY,
    persona_id      INT NOT NULL REFERENCES lb_persona(id),
    email_login     VARCHAR(150) NOT NULL UNIQUE,
    password_hash   TEXT NOT NULL,
    estado          VARCHAR(20) NOT NULL DEFAULT 'ACTIVO',
    ultimo_acceso   TIMESTAMPTZ,
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now()
);
ALTER TABLE lb_usuario_sistema ADD CONSTRAINT uq_usuario_sistema_persona UNIQUE (persona_id);

CREATE TABLE lb_rol (
    id              SMALLSERIAL PRIMARY KEY,
    codigo          VARCHAR(30) NOT NULL UNIQUE,
    nombre          VARCHAR(80) NOT NULL,
    descripcion     TEXT,
    es_global       BOOLEAN NOT NULL DEFAULT false,
    activo          BOOLEAN NOT NULL DEFAULT true
);

INSERT INTO lb_rol (codigo, nombre, es_global) VALUES
('GERENTE_GENERAL', 'Gerente General',        true),
('LOGISTICA',       'Logística Central Lima', true),
('COMPRAS',         'Compras',                true),
('RESIDENTE',       'Residente de Proyecto',  false),
('ALMACENERO',      'Almacenero de Proyecto', false),
('ADMIN',           'Administrador del Sistema', true);

CREATE TABLE lb_permiso (
    id              SERIAL PRIMARY KEY,
    codigo          VARCHAR(50) NOT NULL UNIQUE,
    descripcion     VARCHAR(150)
);
CREATE TABLE lb_rol_permiso (
    rol_id      SMALLINT NOT NULL REFERENCES lb_rol(id),
    permiso_id  INT NOT NULL REFERENCES lb_permiso(id),
    PRIMARY KEY (rol_id, permiso_id)
);

CREATE TABLE lb_proyecto (
    id              SERIAL PRIMARY KEY,
    codigo          VARCHAR(20) NOT NULL UNIQUE,
    nombre          VARCHAR(150) NOT NULL,
    ubicacion       VARCHAR(200),
    estado          VARCHAR(20) NOT NULL DEFAULT 'ACTIVO',
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE TABLE lb_almacen (
    id              SERIAL PRIMARY KEY,
    codigo          VARCHAR(20) NOT NULL UNIQUE,
    nombre          VARCHAR(150) NOT NULL,
    tipo            VARCHAR(20) NOT NULL,
    proyecto_id     INT REFERENCES lb_proyecto(id),
    activo          BOOLEAN NOT NULL DEFAULT true
);

CREATE TABLE lb_usuario_asignacion (
    id                  BIGSERIAL PRIMARY KEY,
    usuario_sistema_id  BIGINT NOT NULL REFERENCES lb_usuario_sistema(id),
    rol_id              SMALLINT NOT NULL REFERENCES lb_rol(id),
    proyecto_id         INT REFERENCES lb_proyecto(id),
    almacen_id          INT REFERENCES lb_almacen(id),
    fecha_inicio        DATE NOT NULL DEFAULT CURRENT_DATE,
    fecha_fin           DATE,
    otorgado_por_usuario_sistema_id BIGINT REFERENCES lb_usuario_sistema(id),
    creado_en           TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_asignacion_usuario ON lb_usuario_asignacion(usuario_sistema_id) WHERE fecha_fin IS NULL;
CREATE INDEX idx_asignacion_proyecto_rol ON lb_usuario_asignacion(proyecto_id, rol_id) WHERE fecha_fin IS NULL;
