-- ============================================================================
-- Evaluaciones SSOMA — 3 flujos nuevos, paralelos a evaluar-residente /
-- evaluar-contratista que ya existen en ev_evaluacion_residente / ev_evaluacion_contratista:
--
--   A. ev_evaluacion_supervisor_contratista
--      Prevencionista/Coordinador SSOMA evalúan al supervisor de campo del
--      contratista (persona en ss_contratista_usuario, rol de sistema 74),
--      por proyecto. Solo el Jefe SSOMA (rol 9) ve el consolidado.
--
--   B. ev_evaluacion_jefe_ssoma (+ _cumplimiento)
--      El equipo SSOMA evalúa al Jefe SSOMA. Anónimo y obligatorio: la nota y
--      la marca de "ya evaluó" viven en tablas separadas SIN llave foránea
--      entre sí, para que nada en el esquema permita unir autor con respuesta.
--
--   C. ev_evaluacion_prevencionista
--      El supervisor de campo del contratista (sesión tipo=CONTRATISTA)
--      evalúa al Prevencionista/Coordinador SSOMA asignado a su proyecto.
--      Sí guarda la identidad del evaluador (empresa + persona) porque el
--      Jefe SSOMA la necesita para gestión; el anonimato acá es solo de cara
--      al evaluado (lo aplica el backend, no el esquema: el endpoint
--      "mi-perfil" del prevencionista nunca selecciona esas columnas).
--
-- Todas comparten el calendario de ev_periodo (mismo período mensual que
-- Residentes/Contratistas).
--
-- Idempotente: usa IF NOT EXISTS / ON CONFLICT DO NOTHING, se puede re-correr.
-- ============================================================================

BEGIN;

-- ── A. Supervisores de contratista ──────────────────────────────────────────

CREATE TABLE IF NOT EXISTS ev_supervisor_contratista_plantilla (
    id          serial PRIMARY KEY,
    criterio    varchar(300) NOT NULL,
    orden       int NOT NULL DEFAULT 0,
    activo      boolean NOT NULL DEFAULT true,
    created_at  timestamp NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS ev_evaluacion_supervisor_contratista (
    id                                     serial PRIMARY KEY,
    periodo_id                             int NOT NULL REFERENCES ev_periodo(id),
    proyecto_id                            int NOT NULL REFERENCES project(project_id),
    contributor_id                         int NOT NULL REFERENCES contributor(contributor_id),
    supervisor_ss_contratista_usuario_id   int NOT NULL REFERENCES ss_contratista_usuario(id),
    supervisor_nombre                      varchar(300) NOT NULL DEFAULT '',
    evaluador_user_id                      int NOT NULL REFERENCES app_user(user_id),
    nota                                   numeric(5,2),
    comentario                             text,
    no_aplica                              boolean NOT NULL DEFAULT false,
    no_aplica_motivo                       text,
    created_at                             timestamp NOT NULL DEFAULT now(),
    updated_at                             timestamp
);

CREATE TABLE IF NOT EXISTS ev_evaluacion_supervisor_contratista_detalle (
    id                                      serial PRIMARY KEY,
    evaluacion_supervisor_contratista_id    int NOT NULL REFERENCES ev_evaluacion_supervisor_contratista(id),
    plantilla_id                            int REFERENCES ev_supervisor_contratista_plantilla(id),
    criterio                                varchar(300) NOT NULL,
    puntaje                                 int,
    es_na                                   boolean NOT NULL DEFAULT false
);

CREATE INDEX IF NOT EXISTS ix_ev_eval_supervisor_periodo_evaluador
    ON ev_evaluacion_supervisor_contratista (periodo_id, evaluador_user_id);

INSERT INTO ev_supervisor_contratista_plantilla (criterio, orden)
SELECT * FROM (VALUES
    ('Cumplimiento de IPERC / permisos de trabajo', 1),
    ('Uso correcto de EPP en su cuadrilla', 2),
    ('Participación en charlas e inducciones', 3),
    ('Orden y limpieza en el frente de trabajo', 4),
    ('Reporte oportuno de incidentes/condiciones inseguras', 5)
) AS v(criterio, orden)
WHERE NOT EXISTS (SELECT 1 FROM ev_supervisor_contratista_plantilla);

-- ── B. Evaluación anónima y obligatoria al Jefe SSOMA ───────────────────────

CREATE TABLE IF NOT EXISTS ev_jefe_ssoma_plantilla (
    id          serial PRIMARY KEY,
    criterio    varchar(300) NOT NULL,
    orden       int NOT NULL DEFAULT 0,
    activo      boolean NOT NULL DEFAULT true,
    created_at  timestamp NOT NULL DEFAULT now()
);

-- Sin evaluador_user_id a propósito: ver nota de anonimato al inicio del archivo.
CREATE TABLE IF NOT EXISTS ev_evaluacion_jefe_ssoma (
    id          serial PRIMARY KEY,
    periodo_id  int NOT NULL REFERENCES ev_periodo(id),
    nota        numeric(5,2),
    comentario  text,
    created_at  timestamp NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS ev_evaluacion_jefe_ssoma_detalle (
    id                       serial PRIMARY KEY,
    evaluacion_jefe_ssoma_id int NOT NULL REFERENCES ev_evaluacion_jefe_ssoma(id),
    plantilla_id             int REFERENCES ev_jefe_ssoma_plantilla(id),
    criterio                 varchar(300) NOT NULL,
    puntaje                  int NOT NULL
);

-- Marca de "ya evaluó": deliberadamente sin FK hacia/desde ev_evaluacion_jefe_ssoma.
CREATE TABLE IF NOT EXISTS ev_evaluacion_jefe_ssoma_cumplimiento (
    id                  serial PRIMARY KEY,
    periodo_id          int NOT NULL REFERENCES ev_periodo(id),
    evaluador_user_id   int NOT NULL REFERENCES app_user(user_id),
    completado_at       timestamp NOT NULL DEFAULT now(),
    UNIQUE (periodo_id, evaluador_user_id)
);

INSERT INTO ev_jefe_ssoma_plantilla (criterio, orden)
SELECT * FROM (VALUES
    ('Liderazgo y disponibilidad para resolver problemas de campo', 1),
    ('Claridad al comunicar prioridades y objetivos SSOMA', 2),
    ('Apoyo con recursos (EPP, materiales, personal) cuando se solicita', 3),
    ('Reconocimiento del trabajo bien hecho', 4),
    ('Manejo justo de observaciones o incidentes', 5)
) AS v(criterio, orden)
WHERE NOT EXISTS (SELECT 1 FROM ev_jefe_ssoma_plantilla);

-- ── C. Contratistas evalúan a Prevencionistas/Coordinadores SSOMA ───────────

CREATE TABLE IF NOT EXISTS ev_prevencionista_plantilla (
    id          serial PRIMARY KEY,
    criterio    varchar(300) NOT NULL,
    orden       int NOT NULL DEFAULT 0,
    activo      boolean NOT NULL DEFAULT true,
    created_at  timestamp NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS ev_evaluacion_prevencionista (
    id                                     serial PRIMARY KEY,
    periodo_id                             int NOT NULL REFERENCES ev_periodo(id),
    proyecto_id                            int NOT NULL REFERENCES project(project_id),
    evaluado_user_id                       int NOT NULL REFERENCES app_user(user_id),
    evaluador_contributor_id               int NOT NULL REFERENCES contributor(contributor_id),
    evaluador_ss_contratista_usuario_id    int NOT NULL REFERENCES ss_contratista_usuario(id),
    nota                                   numeric(5,2),
    comentario                             text,
    created_at                             timestamp NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS ev_evaluacion_prevencionista_detalle (
    id                              serial PRIMARY KEY,
    evaluacion_prevencionista_id    int NOT NULL REFERENCES ev_evaluacion_prevencionista(id),
    plantilla_id                    int REFERENCES ev_prevencionista_plantilla(id),
    criterio                        varchar(300) NOT NULL,
    puntaje                         int NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_ev_eval_prevencionista_evaluado
    ON ev_evaluacion_prevencionista (evaluado_user_id, periodo_id);

INSERT INTO ev_prevencionista_plantilla (criterio, orden)
SELECT * FROM (VALUES
    ('Presencia y acompañamiento en campo', 1),
    ('Claridad al explicar observaciones de seguridad', 2),
    ('Trato respetuoso hacia el personal del contratista', 3),
    ('Rapidez para resolver o escalar temas de seguridad', 4),
    ('Conocimiento técnico de los riesgos de la obra', 5)
) AS v(criterio, orden)
WHERE NOT EXISTS (SELECT 1 FROM ev_prevencionista_plantilla);

COMMIT;

-- ============================================================================
-- Verificación (correr después; no modifica nada)
-- ============================================================================
-- SELECT table_name FROM information_schema.tables
-- WHERE table_name IN (
--   'ev_supervisor_contratista_plantilla','ev_evaluacion_supervisor_contratista','ev_evaluacion_supervisor_contratista_detalle',
--   'ev_jefe_ssoma_plantilla','ev_evaluacion_jefe_ssoma','ev_evaluacion_jefe_ssoma_detalle','ev_evaluacion_jefe_ssoma_cumplimiento',
--   'ev_prevencionista_plantilla','ev_evaluacion_prevencionista','ev_evaluacion_prevencionista_detalle'
-- );
-- Esperado: las 10 filas.
--
-- SELECT 'A' AS flujo, count(*) FROM ev_supervisor_contratista_plantilla
-- UNION ALL SELECT 'B', count(*) FROM ev_jefe_ssoma_plantilla
-- UNION ALL SELECT 'C', count(*) FROM ev_prevencionista_plantilla;
-- Esperado: 5 criterios sembrados en cada uno.
