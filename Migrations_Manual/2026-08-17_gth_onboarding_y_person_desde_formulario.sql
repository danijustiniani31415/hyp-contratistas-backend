-- ============================================================================
-- Gestión GTH · Onboarding + sincronización del formulario del postulante con
-- la base maestra (person)
--
-- Dos cambios que van juntos porque el segundo alimenta al primero:
--
-- 1. `gth_postulante_formulario.person_id` — enlace a la ficha de `person` que
--    se crea/actualiza cuando GTH APRUEBA el formulario del postulante. Hasta
--    ahora los datos que el postulante declaraba se quedaban solo en
--    `gth_postulante_formulario` (tabla "declarada", puede traer cualquier
--    cosa); recién con la validación de GTH pasan a `person`, que es la data
--    maestra. Se copian ÚNICAMENTE los campos que ya tenían columna en
--    `person` — no se agregó ninguna columna nueva allá:
--
--      nombres_completos     -> full_name (en mayúsculas)
--      numero_documento      -> document_identity_code  (llave de coincidencia)
--      gth_tipo_documento    -> document_identity_type_id (codigo ↔ abbreviation)
--      correo_electronico    -> email        ← el que usa Onboarding
--      numero_celular        -> phone_number (solo dígitos)
--      fecha_nacimiento      -> fecha_nacimiento
--      gth_distrito          -> distrito (texto)
--      gth_grado_academico   -> grado_academico_id (por nombre)
--      gth_universidad       -> universidad_id    (por nombre)
--      profesion (texto)     -> profesion_id      (por nombre)
--
--    Se quedan sin equivalente (y sin inventar columnas): estado civil,
--    pretensiones salariales, disponibilidad, LinkedIn, portafolio, número de
--    colegiatura, toda la experiencia laboral y los dos consentimientos.
--
-- 2. Onboarding: `gth_onboarding_fase` + `gth_onboarding_estado` (catálogos) y
--    `gth_onboarding` (la tabla del proceso). Un onboarding solo se puede abrir
--    para un candidato con resultado SELECCIONADO cuyo requerimiento ya quedó
--    CERRADO, y arranca con el envío de la carta oferta al correo personal del
--    colaborador (person.email).
--
-- 3. La feature `gestion-gth.onboarding` con los mismos roles que hoy tienen
--    `gestion-gth.reclutamiento` (1 y 77).
--
-- Idempotente: se puede correr varias veces.
-- ============================================================================

BEGIN;

-- ── 1. Enlace del formulario del postulante con la base maestra ─────────────

ALTER TABLE gth_postulante_formulario
    ADD COLUMN IF NOT EXISTS person_id integer;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint
                    WHERE conname = 'fk_gth_postulante_formulario_person') THEN
        ALTER TABLE gth_postulante_formulario
            ADD CONSTRAINT fk_gth_postulante_formulario_person
            FOREIGN KEY (person_id) REFERENCES person(person_id);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_gth_postulante_formulario_person_id
    ON gth_postulante_formulario (person_id);

-- ── 2.1 Catálogo de fases del onboarding ────────────────────────────────────

CREATE TABLE IF NOT EXISTS gth_onboarding_fase (
    gth_onboarding_fase_id serial PRIMARY KEY,
    codigo                 varchar(60)  NOT NULL,
    nombre                 varchar(120) NOT NULL,
    descripcion            text,
    orden                  integer      NOT NULL DEFAULT 0,
    created_date_time      timestamptz  NOT NULL DEFAULT now(),
    created_user_id        integer,
    updated_date_time      timestamptz,
    updated_user_id        integer,
    active                 boolean      NOT NULL DEFAULT true,
    state                  boolean      NOT NULL DEFAULT true
);

-- Puede haber varias filas dadas de baja con el mismo código, pero solo una viva.
CREATE UNIQUE INDEX IF NOT EXISTS ix_gth_onboarding_fase_codigo
    ON gth_onboarding_fase (codigo) WHERE state = true;

INSERT INTO gth_onboarding_fase (codigo, nombre, descripcion, orden)
SELECT v.codigo, v.nombre, v.descripcion, v.orden
  FROM (VALUES
        ('CARTA_OFERTA_FIRMADA', 'Carta oferta firmada',
         'GTH envía la carta oferta al correo personal del colaborador y espera recibirla firmada.', 1),
        ('FILE_DIGITAL', 'File digital',
         'Se arma el file del colaborador en SharePoint con la carta firmada y sus documentos.', 2),
        ('CORREO_BIENVENIDA', 'Correo de bienvenida',
         'Se le envía la bienvenida con los datos de su primer día y sus accesos.', 3),
        ('FORMULARIO_WEB', 'Formulario web',
         'El colaborador completa el formulario con los datos que alimentan la base maestra.', 4),
        ('PREINICIO', 'Preinicio',
         'Coordinaciones previas al ingreso: equipos, accesos y exámenes médicos.', 5),
        ('CIERRE_ONBOARDING', 'Cierre onboarding',
         'GTH revisa que el checklist esté completo y cierra el proceso.', 6),
        ('BASE_MAESTRA', 'Base maestra',
         'La información del colaborador queda registrada en la base maestra.', 7)
       ) AS v(codigo, nombre, descripcion, orden)
 WHERE NOT EXISTS (SELECT 1 FROM gth_onboarding_fase f
                    WHERE f.codigo = v.codigo AND f.state = true);

-- ── 2.2 Catálogo del estado del onboarding ──────────────────────────────────

CREATE TABLE IF NOT EXISTS gth_onboarding_estado (
    gth_onboarding_estado_id serial PRIMARY KEY,
    codigo                   varchar(60)  NOT NULL,
    nombre                   varchar(120) NOT NULL,
    orden                    integer      NOT NULL DEFAULT 0,
    created_date_time        timestamptz  NOT NULL DEFAULT now(),
    created_user_id          integer,
    updated_date_time        timestamptz,
    updated_user_id          integer,
    active                   boolean      NOT NULL DEFAULT true,
    state                    boolean      NOT NULL DEFAULT true
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_gth_onboarding_estado_codigo
    ON gth_onboarding_estado (codigo) WHERE state = true;

INSERT INTO gth_onboarding_estado (codigo, nombre, orden)
SELECT v.codigo, v.nombre, v.orden
  FROM (VALUES
        ('CARTA_ENVIADA', 'Carta oferta enviada', 1),
        ('EN_PROCESO',    'En proceso',           2),
        ('COMPLETO',      'Completo',             3)
       ) AS v(codigo, nombre, orden)
 WHERE NOT EXISTS (SELECT 1 FROM gth_onboarding_estado e
                    WHERE e.codigo = v.codigo AND e.state = true);

-- ── 2.3 Onboarding del colaborador ──────────────────────────────────────────
-- Los datos de la vacante (código, puesto, área, razón social, jefe directo) NO
-- se copian: se leen por gth_candidato -> gth_requerimiento, que es donde viven.

CREATE TABLE IF NOT EXISTS gth_onboarding (
    gth_onboarding_id        serial PRIMARY KEY,
    gth_candidato_id         integer NOT NULL REFERENCES gth_candidato(gth_candidato_id),
    person_id                integer REFERENCES person(person_id),
    gth_onboarding_fase_id   integer NOT NULL REFERENCES gth_onboarding_fase(gth_onboarding_fase_id),
    gth_onboarding_estado_id integer NOT NULL REFERENCES gth_onboarding_estado(gth_onboarding_estado_id),
    fecha_ingreso            date,
    carta_oferta_nombre      varchar(300),
    carta_oferta_url         text,
    carta_oferta_item_id     varchar(200),
    carta_oferta_drive_id    varchar(200),
    carta_oferta_correo      varchar(200),
    carta_oferta_enviada_date_time timestamptz,
    carta_oferta_enviada_user_id   integer,
    observacion              text,
    created_date_time        timestamptz NOT NULL DEFAULT now(),
    created_user_id          integer,
    updated_date_time        timestamptz,
    updated_user_id          integer,
    active                   boolean NOT NULL DEFAULT true,
    state                    boolean NOT NULL DEFAULT true
);

-- Un candidato seleccionado no puede tener dos onboardings abiertos a la vez.
CREATE UNIQUE INDEX IF NOT EXISTS ix_gth_onboarding_candidato
    ON gth_onboarding (gth_candidato_id) WHERE state = true;

CREATE INDEX IF NOT EXISTS ix_gth_onboarding_person_id
    ON gth_onboarding (person_id);
CREATE INDEX IF NOT EXISTS ix_gth_onboarding_fase_id
    ON gth_onboarding (gth_onboarding_fase_id);
CREATE INDEX IF NOT EXISTS ix_gth_onboarding_estado_id
    ON gth_onboarding (gth_onboarding_estado_id);

-- ── 3. Feature y roles ──────────────────────────────────────────────────────
-- module_id 15 = Gestión GTH (el mismo de gestion-gth.reclutamiento).

INSERT INTO feature (feature_key, module_id)
SELECT 'gestion-gth.onboarding', 15
 WHERE NOT EXISTS (SELECT 1 FROM feature WHERE feature_key = 'gestion-gth.onboarding');

-- Mismos roles que hoy ven Reclutamiento: así GTH entra a Onboarding sin un
-- paso extra de configuración.
INSERT INTO role_feature (role_id, feature_id)
SELECT rf.role_id, nueva.feature_id
  FROM role_feature rf
  JOIN feature f     ON f.feature_id = rf.feature_id
  CROSS JOIN (SELECT feature_id FROM feature WHERE feature_key = 'gestion-gth.onboarding') AS nueva
 WHERE f.feature_key = 'gestion-gth.reclutamiento'
   AND NOT EXISTS (SELECT 1 FROM role_feature x
                    WHERE x.role_id = rf.role_id AND x.feature_id = nueva.feature_id);

COMMIT;

-- ============================================================================
-- Verificación (correr después; no modifica nada)
-- ============================================================================
-- SELECT orden, codigo, nombre FROM gth_onboarding_fase WHERE state ORDER BY orden;
--   Esperado: 7 fases, orden 1..7 (Carta oferta firmada → Base maestra).
--
-- SELECT orden, codigo, nombre FROM gth_onboarding_estado WHERE state ORDER BY orden;
--   Esperado: 3 estados (Carta oferta enviada / En proceso / Completo).
--
-- SELECT rf.role_id FROM role_feature rf JOIN feature f USING (feature_id)
--  WHERE f.feature_key = 'gestion-gth.onboarding' ORDER BY 1;
--   Esperado: los mismos role_id que tiene gestion-gth.reclutamiento.
--
-- Candidatos que ya deberían aparecer en el desplegable «Nuevo ingreso»:
-- SELECT r.codigo, c.nombre, pe.email AS correo_maestra, fo.correo_electronico AS correo_declarado
--   FROM gth_candidato c
--   JOIN gth_candidato_evaluacion ev ON ev.gth_candidato_id = c.gth_candidato_id AND ev.state
--   JOIN gth_candidato_resultado res ON res.gth_candidato_resultado_id = ev.gth_candidato_resultado_id
--   JOIN gth_requerimiento r ON r.gth_requerimiento_id = c.gth_requerimiento_id AND r.state
--   JOIN gth_estado_requerimiento e ON e.gth_estado_requerimiento_id = r.gth_estado_requerimiento_id
--   LEFT JOIN gth_postulante_formulario fo ON fo.gth_candidato_id = c.gth_candidato_id AND fo.state
--   LEFT JOIN person pe ON pe.person_id = fo.person_id
--  WHERE c.state AND res.codigo = 'SELECCIONADO' AND e.codigo = 'CERRADO'
--    AND NOT EXISTS (SELECT 1 FROM gth_onboarding o
--                     WHERE o.gth_candidato_id = c.gth_candidato_id AND o.state);
