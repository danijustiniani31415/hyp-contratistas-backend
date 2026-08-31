-- ============================================================================
-- Control de Licencias (rename + rediseño de "Control de Vencimientos")
-- Ejecutar manualmente en pgAdmin. No usar `dotnet ef migrations`.
--
-- Diseño:
--   - vecino_licencia_control_tipo   -> catálogo (plantilla). project_id NULL = tipo
--       base visible en todos los proyectos; project_id NOT NULL = tipo agregado
--       solo para ese proyecto.
--   - vecino_licencia_control_estado -> catálogo fijo: Pendiente / Cargado / No aplica / Vencido.
--   - vecino_licencia_control        -> 1 registro vigente por (project_id, tipo).
--       Guarda el archivo actual, fechas de vencimiento/recordatorio y estado.
--   - vecino_licencia_control_historial -> cada vez que se reemplaza el archivo
--       vigente, la versión anterior se copia aquí ANTES de sobrescribir.
--       Nunca se borra (historial de versiones).
--   - vecino_licencia_control_destinatario -> destinatarios de recordatorio por
--       proyecto + rol (Residente, Administrador, etc.), reutilizable para todas
--       las licencias de ese proyecto.
--
-- Auditoría: estas 5 tablas se agregan a TablasAuditar en
--   Shared/Interceptors/AuditoriaInterceptor.cs (cambio de código, no de SQL)
--   para que INSERT/UPDATE/DELETE queden registrados automáticamente en
--   auditoria_cambios (quién, cuándo, qué cambió, incluido el borrado).
-- ============================================================================

BEGIN;

-- ── Catálogo de tipos de licencia (plantilla base + extras por proyecto) ───
CREATE TABLE vecino_licencia_control_tipo (
    vecino_licencia_control_tipo_id SERIAL PRIMARY KEY,
    project_id                      INT NULL REFERENCES project(project_id),
    descripcion                     TEXT NOT NULL,
    orden                           INT NOT NULL DEFAULT 0,
    created_date_time              TIMESTAMPTZ NOT NULL,
    created_user_id                INT NOT NULL,
    updated_date_time              TIMESTAMPTZ NULL,
    updated_user_id                INT NULL,
    active                          BOOLEAN NOT NULL DEFAULT TRUE,
    state                           BOOLEAN NOT NULL DEFAULT TRUE
);

COMMENT ON TABLE vecino_licencia_control_tipo IS 'Catálogo de tipos de licencia/permiso: plantilla base (project_id NULL) + tipos propios de un proyecto (project_id NOT NULL)';

-- ── Catálogo fijo de estados ────────────────────────────────────────────────
CREATE TABLE vecino_licencia_control_estado (
    vecino_licencia_control_estado_id SERIAL PRIMARY KEY,
    descripcion                        TEXT NOT NULL,
    active                              BOOLEAN NOT NULL DEFAULT TRUE,
    state                               BOOLEAN NOT NULL DEFAULT TRUE
);

INSERT INTO vecino_licencia_control_estado (descripcion, active, state) VALUES
    ('Pendiente', TRUE, TRUE),
    ('Cargado',   TRUE, TRUE),
    ('No aplica', TRUE, TRUE),
    ('Vencido',   TRUE, TRUE);

-- ── Registro vigente por proyecto + tipo ───────────────────────────────────
CREATE TABLE vecino_licencia_control (
    vecino_licencia_control_id        SERIAL PRIMARY KEY,
    project_id                        INT NOT NULL REFERENCES project(project_id),
    vecino_licencia_control_tipo_id   INT NOT NULL REFERENCES vecino_licencia_control_tipo(vecino_licencia_control_tipo_id),
    vecino_licencia_control_estado_id INT NOT NULL REFERENCES vecino_licencia_control_estado(vecino_licencia_control_estado_id),
    archivo_url                        TEXT NULL,
    original_file_name                 TEXT NULL,
    fecha_vencimiento                  DATE NULL,
    fecha_recordatorio                 DATE NULL,
    dias_antes                         INT NULL,
    recordatorio_enviado_date_time     TIMESTAMPTZ NULL,
    created_date_time                  TIMESTAMPTZ NOT NULL,
    created_user_id                    INT NOT NULL,
    updated_date_time                  TIMESTAMPTZ NULL,
    updated_user_id                    INT NULL,
    active                              BOOLEAN NOT NULL DEFAULT TRUE,
    state                               BOOLEAN NOT NULL DEFAULT TRUE
);

COMMENT ON TABLE vecino_licencia_control IS 'Registro vigente de una licencia/permiso por proyecto + tipo (el historial de versiones anteriores va en vecino_licencia_control_historial)';

-- Un solo registro vigente por proyecto+tipo mientras esté activo.
CREATE UNIQUE INDEX ux_vecino_licencia_control_proyecto_tipo
    ON vecino_licencia_control (project_id, vecino_licencia_control_tipo_id)
    WHERE state = TRUE;

-- ── Historial: versiones anteriores del archivo (nunca se borran) ─────────
CREATE TABLE vecino_licencia_control_historial (
    vecino_licencia_control_historial_id SERIAL PRIMARY KEY,
    vecino_licencia_control_id           INT NOT NULL REFERENCES vecino_licencia_control(vecino_licencia_control_id),
    archivo_url                           TEXT NOT NULL,
    original_file_name                    TEXT NULL,
    fecha_vencimiento                     DATE NULL,
    fecha_recordatorio                    DATE NULL,
    dias_antes                            INT NULL,
    motivo                                TEXT NULL,
    created_date_time                     TIMESTAMPTZ NOT NULL,
    created_user_id                       INT NOT NULL,
    active                                 BOOLEAN NOT NULL DEFAULT TRUE,
    state                                  BOOLEAN NOT NULL DEFAULT TRUE
);

COMMENT ON TABLE vecino_licencia_control_historial IS 'Versión anterior de una licencia, archivada automáticamente cada vez que se sube un documento de reemplazo';

CREATE INDEX ix_vecino_licencia_control_historial_licencia
    ON vecino_licencia_control_historial (vecino_licencia_control_id);

-- ── Destinatarios de recordatorio por proyecto + rol ───────────────────────
CREATE TABLE vecino_licencia_control_destinatario (
    vecino_licencia_control_destinatario_id SERIAL PRIMARY KEY,
    project_id          INT NOT NULL REFERENCES project(project_id),
    rol                 TEXT NOT NULL,   -- ej: 'Residente', 'Administrador', 'Supervisor'
    email               TEXT NOT NULL,
    created_date_time   TIMESTAMPTZ NOT NULL,
    created_user_id     INT NOT NULL,
    updated_date_time   TIMESTAMPTZ NULL,
    updated_user_id     INT NULL,
    active               BOOLEAN NOT NULL DEFAULT TRUE,
    state                BOOLEAN NOT NULL DEFAULT TRUE
);

COMMENT ON TABLE vecino_licencia_control_destinatario IS 'Correos por proyecto y rol (Residente, Administrador, etc.) que reciben los recordatorios de vencimiento de ese proyecto';

CREATE INDEX ix_vecino_licencia_control_destinatario_proyecto
    ON vecino_licencia_control_destinatario (project_id);

COMMIT;
