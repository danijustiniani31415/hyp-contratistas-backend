-- ============================================================================
-- Funcionalidad: Gestión de Vecinos
-- Crea catálogos (tipo de construcción, colindancia), tabla vecino,
-- módulo/feature de navegación y rol ADMINISTRADOR DE OBRA.
-- Idempotente en lo posible para poder reejecutarse con seguridad.
-- ============================================================================

BEGIN;

-- ── Catálogo: Tipo de construcción ──────────────────────────────────────────
CREATE TABLE IF NOT EXISTS vecino_tipo_construccion (
    vecino_tipo_construccion_id serial PRIMARY KEY,
    descripcion                 varchar(100) NOT NULL,
    active                      boolean      NOT NULL DEFAULT true,
    state                       boolean      NOT NULL DEFAULT true
);

INSERT INTO vecino_tipo_construccion (descripcion)
SELECT v.descripcion
FROM (VALUES ('Material noble'), ('Mixto'), ('Quincha, adobe u otros')) AS v(descripcion)
WHERE NOT EXISTS (
    SELECT 1 FROM vecino_tipo_construccion t WHERE t.descripcion = v.descripcion
);

-- ── Catálogo: Colindancia ───────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS vecino_colindancia (
    vecino_colindancia_id serial PRIMARY KEY,
    descripcion           varchar(100) NOT NULL,
    active                boolean      NOT NULL DEFAULT true,
    state                 boolean      NOT NULL DEFAULT true
);

INSERT INTO vecino_colindancia (descripcion)
SELECT v.descripcion
FROM (VALUES ('Colindante'), ('No colindante')) AS v(descripcion)
WHERE NOT EXISTS (
    SELECT 1 FROM vecino_colindancia c WHERE c.descripcion = v.descripcion
);

-- ── Tabla principal: vecino ─────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS vecino (
    vecino_id                   serial PRIMARY KEY,
    project_id                  int          NOT NULL REFERENCES project(project_id),
    predio                      varchar(150),
    direccion                   varchar(250) NOT NULL,
    interior_departamento       varchar(150),
    nombre_propietario          varchar(200) NOT NULL,
    dni                         varchar(8)   NOT NULL,
    celular                     varchar(20),
    vecino_colindancia_id       int          NOT NULL REFERENCES vecino_colindancia(vecino_colindancia_id),
    vecino_tipo_construccion_id int          NOT NULL REFERENCES vecino_tipo_construccion(vecino_tipo_construccion_id),
    created_date_time           timestamp    NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    created_user_id             int          NOT NULL REFERENCES app_user(user_id),
    updated_date_time           timestamp,
    updated_user_id             int          REFERENCES app_user(user_id),
    active                      boolean      NOT NULL DEFAULT true,
    state                       boolean      NOT NULL DEFAULT true
);

CREATE INDEX IF NOT EXISTS ix_vecino_project_id ON vecino(project_id);

-- ── Navegación: módulo + feature ────────────────────────────────────────────
-- Resincroniza las secuencias serial por si quedaron desfasadas respecto al max(id).
SELECT setval(pg_get_serial_sequence('module',  'module_id'),  (SELECT COALESCE(MAX(module_id),  1) FROM module));
SELECT setval(pg_get_serial_sequence('feature', 'feature_id'), (SELECT COALESCE(MAX(feature_id), 1) FROM feature));
SELECT setval(pg_get_serial_sequence('role',    'role_id'),    (SELECT COALESCE(MAX(role_id),    1) FROM role));

INSERT INTO module (module_name)
SELECT 'Vecinos'
WHERE NOT EXISTS (SELECT 1 FROM module WHERE module_name = 'Vecinos');

INSERT INTO feature (feature_key, module_id)
SELECT 'vecinos.gestion', m.module_id
FROM module m
WHERE m.module_name = 'Vecinos'
  AND NOT EXISTS (SELECT 1 FROM feature WHERE feature_key = 'vecinos.gestion');

-- ── Rol: ADMINISTRADOR DE OBRA ──────────────────────────────────────────────
-- created_user_id apunta a un usuario administrador existente (ajustar si aplica).
INSERT INTO role (role_description, created_user_id)
SELECT 'ADMINISTRADOR DE OBRA', 1
WHERE NOT EXISTS (SELECT 1 FROM role WHERE role_description = 'ADMINISTRADOR DE OBRA');

-- ── Permisos: ADMINISTRADOR DE OBRA + ADMINISTRADOR DEL SISTEMA → feature ────
INSERT INTO role_feature (role_id, feature_id)
SELECT r.role_id, f.feature_id
FROM role r
CROSS JOIN feature f
WHERE f.feature_key = 'vecinos.gestion'
  AND r.role_description IN ('ADMINISTRADOR DE OBRA', 'ADMINISTRADOR DEL SISTEMA')
  AND NOT EXISTS (
      SELECT 1 FROM role_feature rf WHERE rf.role_id = r.role_id AND rf.feature_id = f.feature_id
  );

COMMIT;
