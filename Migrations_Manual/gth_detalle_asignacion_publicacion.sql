-- ============================================================================
-- Gestión GTH · Reclutamiento — Detalle del requerimiento (modal de la bandeja)
--
-- 1) `contributor.operativo`: flag de negocio "razón social activa/inactiva".
--    (`active` ya existe pero es de sistema —si sale o no en desplegables— y
--    `state` es soft delete; `operativo` sigue la esencia del mismo campo que
--    ya existe en `project`.)
-- 2) Seed de las 24 razones sociales activas (Excel base maestra): inserta las
--    que falten y SOLO rellena campos null/vacíos de las existentes (no
--    sobreescribe datos ya registrados). Todas quedan operativo = true.
-- 3) Seed de proyectos del Excel: inserta los que falten (GERANIO, ROBLES,
--    ALAMO, GENOVA, FICUS, CEIBOS) y SOLO rellena campos null/vacíos de los
--    existentes (dirección/distrito/provincia/departamento).
-- 4) Catálogos nuevos del modal de detalle:
--      - gth_responsable_proceso (miembros GTH responsables del proceso)
--      - gth_tipo_proceso        (Junior 20d / Semisenior 25d / Senior 35d)
--      - gth_canal_publicacion   (Boomerang / LinkedIn / Computrabajo)
--      - gth_requerimiento_canal (publicaciones registradas por requerimiento)
-- 5) Columnas nuevas en gth_requerimiento: responsable, tipo de proceso y
--    razón social asignada (todas nullable, las asigna GTH desde el modal).
--
-- Idempotente: se puede correr múltiples veces sin duplicar ni romper nada.
-- ============================================================================

BEGIN;

-- ============================================================================
-- 1) contributor.operativo
-- ============================================================================
ALTER TABLE contributor
    ADD COLUMN IF NOT EXISTS operativo boolean NOT NULL DEFAULT false;

COMMENT ON COLUMN contributor.operativo IS
    'Razón social activa/inactiva a nivel de negocio (operación vigente del grupo). Distinto de active (visibilidad en desplegables del sistema) y de state (soft delete).';

-- ============================================================================
-- 2) Razones sociales activas (24 del Excel de la base maestra)
-- ============================================================================
-- 2a) Insertar las que no existan (con los datos por defecto del grupo Abril).
WITH datos(ruc, nombre) AS (
    VALUES
        ('20610737227', 'AQUITANIA INMOBILIARIA SAC'),
        ('20552898835', 'AZUL GRUPO INMOBILIARIO SAC'),
        ('20609070804', 'BAHIA DE ORO INMOBILIARIA S.A.C.'),
        ('20606944935', 'BENEVENTO INMOBILIA SAC'),
        ('20610710248', 'CAMPITELLI INMOBILIARIA SAC'),
        ('20612028096', 'CARPI INMOBILIARIA SAC'),
        ('20605891714', 'CATANIA INMOBILIARIA SAC'),
        ('20518128249', 'CORPORACION INMOBILIARIA NERIDA MARIA SAC'),
        ('20605489231', 'CORPORACION LUXOR INMOBILIARIA SAC'),
        ('20609696258', 'ECO VITAL INMOBILIARIA SAC'),
        ('20607005550', 'FLORENCIA INMOBILIARIA SAC'),
        ('20552897863', 'GRECO INMOBILIARIA SAC'),
        ('20613240021', 'INMOBILIARIA LA VID S.A.C.'),
        ('20610463780', 'LARES INMOBILIARIA SAC'),
        ('20610744801', 'MARSELLA INMOBILIARIA SAC'),
        ('20613474804', 'NARESH INMOBILIARIA S.A.C'),
        ('20601678579', 'NEO INVERSIONES INMOBILIARIAS SAC'),
        ('20613467778', 'OPORTO INMOBILIARIA SAC'),
        ('20613181297', 'PRINCIPE DE PAZ INMOBILIARIA SAC'),
        ('20611820691', 'SALERNO INMOBILIARIA SAC'),
        ('20604826854', 'SESHAT INMOBILIARIA SAC'),
        ('20604827079', 'SOLARE INMOBILIARIA SAC'),
        ('20605487450', 'THABIT INMOBILIARIA SAC'),
        ('20613222902', 'VERBO INMOBILIARIA SAC')
)
INSERT INTO contributor (
    contributor_ruc, contributor_name, contributor_address,
    contributor_district, contributor_province, contributor_department,
    contributor_economic_activity_description, contributor_nombre_comercial,
    es_abril, operativo, created_date_time, active, state)
SELECT
    d.ruc, d.nombre, 'CAL. MAMA OCLLO NRO 2647 URB. RISSO',
    'LINCE', 'LIMA', 'LIMA',
    'ACTIVIDADES INMOBILIARIAS REALIZADAS CON BIENES PROPIOS O ARRENDADOS',
    'ABRIL GRUPO INMOBILIARIO',
    true, true, now(), true, true
FROM datos d
WHERE NOT EXISTS (SELECT 1 FROM contributor c WHERE c.contributor_ruc = d.ruc);

-- 2b) Rellenar SOLO campos null/vacíos de las existentes y marcarlas operativas.
--     (No se toca contributor_name ni ningún campo que ya tenga valor.)
UPDATE contributor c
SET contributor_address = COALESCE(NULLIF(TRIM(c.contributor_address), ''), 'CAL. MAMA OCLLO NRO 2647 URB. RISSO'),
    contributor_district = COALESCE(NULLIF(TRIM(c.contributor_district), ''), 'LINCE'),
    contributor_province = COALESCE(NULLIF(TRIM(c.contributor_province), ''), 'LIMA'),
    contributor_department = COALESCE(NULLIF(TRIM(c.contributor_department), ''), 'LIMA'),
    contributor_economic_activity_description = COALESCE(NULLIF(TRIM(c.contributor_economic_activity_description), ''), 'ACTIVIDADES INMOBILIARIAS REALIZADAS CON BIENES PROPIOS O ARRENDADOS'),
    contributor_nombre_comercial = COALESCE(NULLIF(TRIM(c.contributor_nombre_comercial), ''), 'ABRIL GRUPO INMOBILIARIO'),
    operativo = true,
    updated_date_time = now()
WHERE c.contributor_ruc IN (
    '20610737227','20552898835','20609070804','20606944935','20610710248',
    '20612028096','20605891714','20518128249','20605489231','20609696258',
    '20607005550','20552897863','20613240021','20610463780','20610744801',
    '20613474804','20601678579','20613467778','20613181297','20611820691',
    '20604826854','20604827079','20605487450','20613222902')
  AND c.state = true;

-- ============================================================================
-- 3) Proyectos del Excel
-- ============================================================================
-- 3a) Insertar los que no existan (comparación por nombre sin mayúsculas).
--     created_user_id es NOT NULL en project → se usa el primer usuario del sistema.
WITH nuevos(nombre, ruc, direccion, distrito) AS (
    VALUES
        ('GERANIO', '20605489231', 'AV. JOSÉ LEAL 1169, LINCE',        'LINCE'),
        ('ROBLES',  '20518128249', NULL,                               NULL),
        ('ALAMO',   '20518128249', NULL,                               NULL),
        ('GENOVA',  '20518128249', NULL,                               NULL),
        ('FICUS',   '20552897863', 'AV. CESAR VALLEJO Nº 223, LINCE',  'LINCE'),
        ('CEIBOS',  '20552897863', 'AV. HORACIO URTEAGA 432, JESÚS MARÍA', 'JESÚS MARÍA')
)
INSERT INTO project (
    project_description, contributor_id, project_location, project_district,
    project_province, project_department, estado, operativo,
    created_date_time, created_user_id, active, state)
SELECT
    n.nombre,
    (SELECT c.contributor_id FROM contributor c WHERE c.contributor_ruc = n.ruc AND c.state = true LIMIT 1),
    n.direccion,
    n.distrito,
    CASE WHEN n.distrito IS NULL THEN NULL ELSE 'LIMA' END,
    CASE WHEN n.distrito IS NULL THEN NULL ELSE 'LIMA' END,
    'ACTIVO', true,
    now(),
    (SELECT MIN(user_id) FROM app_user),
    true, true
FROM nuevos n
WHERE NOT EXISTS (
    SELECT 1 FROM project p
    WHERE UPPER(TRIM(p.project_description)) = n.nombre AND p.state = true);

-- 3b) Rellenar SOLO campos null/vacíos de proyectos existentes (dirección del Excel).
WITH dir(nombre, direccion, distrito) AS (
    VALUES
        ('AMANCAE',       'AV. JOSÉ PARDO N°1051, MIRAFLORES',           'MIRAFLORES'),
        ('BARONET',       'JIRON CAPAC YUPANQUI 1641, LINCE',            'LINCE'),
        ('BARONETH',      'JIRON CAPAC YUPANQUI 1641, LINCE',            'LINCE'),
        ('LE SAULE DEUX', 'JR. MANUEL VILLAVICENCIO NRO. 843, LINCE',    'LINCE'),
        ('LOS LAURELES',  'JR. TALARA Nº 153, JESÚS MARÍA',              'JESÚS MARÍA'),
        ('9 NOGALES',     'CALLE BALTAZAR LA TORRE 530-532, SAN ISIDRO', 'SAN ISIDRO')
)
UPDATE project p
SET project_location   = COALESCE(NULLIF(TRIM(p.project_location), ''), d.direccion),
    project_district   = COALESCE(NULLIF(TRIM(p.project_district), ''), d.distrito),
    project_province   = COALESCE(NULLIF(TRIM(p.project_province), ''), 'LIMA'),
    project_department = COALESCE(NULLIF(TRIM(p.project_department), ''), 'LIMA'),
    updated_date_time  = now()
FROM dir d
WHERE UPPER(TRIM(p.project_description)) = d.nombre
  AND p.state = true;

-- ============================================================================
-- 4a) gth_responsable_proceso — miembros GTH que pueden ser responsables
-- ============================================================================
CREATE TABLE IF NOT EXISTS gth_responsable_proceso (
    gth_responsable_proceso_id integer GENERATED BY DEFAULT AS IDENTITY,
    worker_id         integer     NOT NULL,
    orden             integer     NOT NULL DEFAULT 0,
    created_date_time timestamptz NOT NULL DEFAULT now(),
    created_user_id   integer     NULL,
    updated_date_time timestamptz NULL,
    updated_user_id   integer     NULL,
    active            boolean     NOT NULL DEFAULT true,
    state             boolean     NOT NULL DEFAULT true,
    CONSTRAINT pk_gth_responsable_proceso PRIMARY KEY (gth_responsable_proceso_id),
    CONSTRAINT fk_gth_responsable_proceso_worker FOREIGN KEY (worker_id) REFERENCES workers(id)
);

-- Solo un registro "vivo" (state = true) por trabajador.
CREATE UNIQUE INDEX IF NOT EXISTS ix_gth_responsable_proceso_worker_vivo
    ON gth_responsable_proceso (worker_id) WHERE (state = true);

-- Seed: miembros del equipo GTH, resueltos por correo corporativo (estable entre
-- dev y prod). Si un correo no existe en workers (dev desfasada) simplemente no
-- se inserta esa fila.
INSERT INTO gth_responsable_proceso (worker_id, orden, created_date_time, active, state)
SELECT w.id, v.orden, now(), true, true
FROM (VALUES
        ('avrivera@abril.pe',  1),   -- Aldo Rivera
        ('amantilla@abril.pe', 2),   -- Andrea Mantilla
        ('mchumbe@abril.pe',   3),   -- Marifé Chumbe
        ('morozco@abril.pe',   4)    -- Marcos Orozco
     ) AS v(email, orden)
JOIN LATERAL (
    SELECT id FROM workers w
    WHERE LOWER(TRIM(w.email_corporativo)) = v.email
    ORDER BY (w.estado = 'ACTIVO') DESC, w.id DESC
    LIMIT 1) w ON true
WHERE NOT EXISTS (
    SELECT 1 FROM gth_responsable_proceso rp
    WHERE rp.worker_id = w.id AND rp.state = true);

-- ============================================================================
-- 4b) gth_tipo_proceso — tipo de proceso con su SLA referencial
-- ============================================================================
CREATE TABLE IF NOT EXISTS gth_tipo_proceso (
    gth_tipo_proceso_id integer   GENERATED BY DEFAULT AS IDENTITY,
    codigo            text        NOT NULL,
    nombre            text        NOT NULL,
    sla_dias          integer     NOT NULL,
    descripcion       text        NULL,
    orden             integer     NOT NULL DEFAULT 0,
    created_date_time timestamptz NOT NULL DEFAULT now(),
    created_user_id   integer     NULL,
    updated_date_time timestamptz NULL,
    updated_user_id   integer     NULL,
    active            boolean     NOT NULL DEFAULT true,
    state             boolean     NOT NULL DEFAULT true,
    CONSTRAINT pk_gth_tipo_proceso PRIMARY KEY (gth_tipo_proceso_id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_gth_tipo_proceso_codigo_vivo
    ON gth_tipo_proceso (codigo) WHERE (state = true);

INSERT INTO gth_tipo_proceso (codigo, nombre, sla_dias, descripcion, orden, created_date_time, active, state)
VALUES
    ('JUNIOR',     'Junior',     20, 'Procesos de mayor volumen o perfiles de entrada.',                              1, now(), true, true),
    ('SEMISENIOR', 'Semisenior', 25, 'Perfiles con experiencia intermedia y especialización moderada.',              2, now(), true, true),
    ('SENIOR',     'Senior',     35, 'Perfiles especializados, de mayor responsabilidad o de búsqueda más difícil.', 3, now(), true, true)
ON CONFLICT (codigo) WHERE (state = true)
DO UPDATE SET
    nombre            = EXCLUDED.nombre,
    sla_dias          = EXCLUDED.sla_dias,
    descripcion       = EXCLUDED.descripcion,
    orden             = EXCLUDED.orden,
    active            = true,
    updated_date_time = now();

-- ============================================================================
-- 4c) gth_canal_publicacion — canales donde se publica la vacante
-- ============================================================================
CREATE TABLE IF NOT EXISTS gth_canal_publicacion (
    gth_canal_publicacion_id integer GENERATED BY DEFAULT AS IDENTITY,
    codigo            text        NOT NULL,
    nombre            text        NOT NULL,
    api_disponible    boolean     NOT NULL DEFAULT false,
    orden             integer     NOT NULL DEFAULT 0,
    created_date_time timestamptz NOT NULL DEFAULT now(),
    created_user_id   integer     NULL,
    updated_date_time timestamptz NULL,
    updated_user_id   integer     NULL,
    active            boolean     NOT NULL DEFAULT true,
    state             boolean     NOT NULL DEFAULT true,
    CONSTRAINT pk_gth_canal_publicacion PRIMARY KEY (gth_canal_publicacion_id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_gth_canal_publicacion_codigo_vivo
    ON gth_canal_publicacion (codigo) WHERE (state = true);

-- Corrección de nombre del portal (2026-07-22): el canal es Bumeran, no "Boomerang".
-- Renombra la fila vigente si fue sembrada con el codigo antiguo (BDs ya sembradas);
-- en BDs frescas no hace nada y el INSERT de abajo ya trae BUMERAN.
UPDATE gth_canal_publicacion
SET codigo            = 'BUMERAN',
    nombre            = 'Bumeran',
    updated_date_time = now()
WHERE codigo = 'BOOMERANG'
  AND state = true
  AND NOT EXISTS (SELECT 1 FROM gth_canal_publicacion WHERE codigo = 'BUMERAN' AND state = true);

INSERT INTO gth_canal_publicacion (codigo, nombre, api_disponible, orden, created_date_time, active, state)
VALUES
    ('BUMERAN',      'Bumeran',      true,  1, now(), true, true),
    ('LINKEDIN',     'LinkedIn',     false, 2, now(), true, true),
    ('COMPUTRABAJO', 'Computrabajo', false, 3, now(), true, true)
ON CONFLICT (codigo) WHERE (state = true)
DO UPDATE SET
    nombre            = EXCLUDED.nombre,
    api_disponible    = EXCLUDED.api_disponible,
    orden             = EXCLUDED.orden,
    active            = true,
    updated_date_time = now();

-- ============================================================================
-- 4d) gth_requerimiento_canal — publicaciones registradas por requerimiento
-- ============================================================================
CREATE TABLE IF NOT EXISTS gth_requerimiento_canal (
    gth_requerimiento_canal_id integer GENERATED BY DEFAULT AS IDENTITY,
    gth_requerimiento_id       integer NOT NULL,
    gth_canal_publicacion_id   integer NOT NULL,
    created_date_time timestamptz NOT NULL DEFAULT now(),
    created_user_id   integer     NULL,
    updated_date_time timestamptz NULL,
    updated_user_id   integer     NULL,
    active            boolean     NOT NULL DEFAULT true,
    state             boolean     NOT NULL DEFAULT true,
    CONSTRAINT pk_gth_requerimiento_canal PRIMARY KEY (gth_requerimiento_canal_id),
    CONSTRAINT fk_gth_req_canal_requerimiento FOREIGN KEY (gth_requerimiento_id) REFERENCES gth_requerimiento(gth_requerimiento_id),
    CONSTRAINT fk_gth_req_canal_canal FOREIGN KEY (gth_canal_publicacion_id) REFERENCES gth_canal_publicacion(gth_canal_publicacion_id)
);

-- Solo una publicación "viva" (state = true) por requerimiento + canal.
CREATE UNIQUE INDEX IF NOT EXISTS ix_gth_requerimiento_canal_vivo
    ON gth_requerimiento_canal (gth_requerimiento_id, gth_canal_publicacion_id) WHERE (state = true);

CREATE INDEX IF NOT EXISTS ix_gth_requerimiento_canal_req
    ON gth_requerimiento_canal (gth_requerimiento_id);

-- ============================================================================
-- 5) Columnas nuevas en gth_requerimiento (asignación interna de GTH)
-- ============================================================================
ALTER TABLE gth_requerimiento
    ADD COLUMN IF NOT EXISTS gth_responsable_proceso_id integer NULL,
    ADD COLUMN IF NOT EXISTS gth_tipo_proceso_id        integer NULL,
    ADD COLUMN IF NOT EXISTS contributor_id             integer NULL;

COMMENT ON COLUMN gth_requerimiento.contributor_id IS
    'Razón social activa asignada por GTH para la contratación (FK a contributor).';

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_gth_req_responsable_proceso') THEN
        ALTER TABLE gth_requerimiento
            ADD CONSTRAINT fk_gth_req_responsable_proceso
            FOREIGN KEY (gth_responsable_proceso_id) REFERENCES gth_responsable_proceso(gth_responsable_proceso_id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_gth_req_tipo_proceso') THEN
        ALTER TABLE gth_requerimiento
            ADD CONSTRAINT fk_gth_req_tipo_proceso
            FOREIGN KEY (gth_tipo_proceso_id) REFERENCES gth_tipo_proceso(gth_tipo_proceso_id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_gth_req_contributor') THEN
        ALTER TABLE gth_requerimiento
            ADD CONSTRAINT fk_gth_req_contributor
            FOREIGN KEY (contributor_id) REFERENCES contributor(contributor_id);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_gth_requerimiento_responsable ON gth_requerimiento (gth_responsable_proceso_id);
CREATE INDEX IF NOT EXISTS ix_gth_requerimiento_tipo_proceso ON gth_requerimiento (gth_tipo_proceso_id);
CREATE INDEX IF NOT EXISTS ix_gth_requerimiento_contributor ON gth_requerimiento (contributor_id);

COMMIT;
