-- ============================================================================
-- Evaluaciones de Gestión SSOMA — flujo D, interno al equipo SSOMA, paralelo a
-- los flujos A/B/C ya existentes (ver 2026-08-20_evaluaciones_ssoma_supervisores_jefe_prevencionistas.sql):
--
--   D1. Jefe SSOMA (rol 9)            -> evalúa a TODOS los Prevencionistas (rol 72)
--   D2. Jefe SSOMA (rol 9)            -> evalúa a TODOS los Coordinadores SSOMA (rol 70)
--   D3. Coordinador SSOMA (rol 70)    -> evalúa a los Prevencionistas de su mismo proyecto
--   D4. Prevencionista (rol 72)       -> evalúa a su Coordinador SSOMA del mismo proyecto (ANÓNIMO)
--
-- D1-D3 son identificadas (evaluador_user_id poblado). D4 es anónima: se
-- guarda con evaluador_user_id = NULL (igual que ev_evaluacion_jefe_ssoma no
-- guarda evaluador) y el cumplimiento ("ya evaluó a su coordinador") se
-- registra aparte, en una tabla sin FK hacia la nota, para que nada en el
-- esquema permita unir autor con respuesta — mismo patrón que
-- ev_evaluacion_jefe_ssoma_cumplimiento.
--
-- Las 4 relaciones comparten una sola plantilla (ev_gestion_ssoma_plantilla):
-- miden competencias de liderazgo/gestión de personas, aplicables en
-- cualquier dirección jerárquica.
--
-- Comparte el calendario de ev_periodo (mismo período mensual que el resto).
--
-- Idempotente: usa IF NOT EXISTS / ON CONFLICT DO NOTHING, se puede re-correr.
-- ============================================================================

BEGIN;

-- ── Plantilla compartida D1-D4 ──────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS ev_gestion_ssoma_plantilla (
    id          serial PRIMARY KEY,
    criterio    varchar(300) NOT NULL,
    orden       int NOT NULL DEFAULT 0,
    activo      boolean NOT NULL DEFAULT true,
    created_at  timestamp NOT NULL DEFAULT now()
);

INSERT INTO ev_gestion_ssoma_plantilla (criterio, orden)
SELECT * FROM (VALUES
    ('Comunica instrucciones y prioridades con claridad, sin generar confusión ni necesidad de repetir', 1),
    ('Da feedback específico y oportuno sobre el desempeño (no solo cuando hay un error grave)', 2),
    ('Está disponible y accesible ante consultas, dudas o propuestas del equipo', 3),
    ('Reconoce el buen desempeño y los logros del equipo cuando corresponde', 4),
    ('Aborda desacuerdos o problemas de forma directa y respetuosa, sin evitarlos ni escalarlos innecesariamente', 5),
    ('Impulsa el desarrollo profesional de su equipo (capacitación, nuevas responsabilidades)', 6),
    ('Prioriza la seguridad incluso bajo presión de plazos o producción, sin sacrificar controles por avanzar más rápido', 7)
) AS v(criterio, orden)
WHERE NOT EXISTS (SELECT 1 FROM ev_gestion_ssoma_plantilla);

-- ── Evaluación (D1, D2, D3 identificadas; D4 anónima con evaluador_user_id NULL) ──

CREATE TABLE IF NOT EXISTS ev_evaluacion_gestion_ssoma (
    id                  serial PRIMARY KEY,
    periodo_id          int NOT NULL REFERENCES ev_periodo(id),
    evaluador_user_id   int REFERENCES app_user(user_id),   -- NULL en filas D4 (anónimo)
    evaluador_rol       varchar(10) NOT NULL,                -- '9' Jefe SSOMA | '70' Coordinador SSOMA | '72' Prevencionista
    evaluado_user_id    int NOT NULL REFERENCES app_user(user_id),
    evaluado_rol        varchar(10) NOT NULL,
    proyecto_id         int REFERENCES project(project_id),  -- relevante en D3/D4 (mismo proyecto); NULL en D1/D2 (alcance compañía)
    nota                numeric(5,2),
    fortalezas          text,
    oportunidades_mejora text,
    created_at          timestamp NOT NULL DEFAULT now()
);

-- Por si esta migración ya se corrió antes de que se agregaran estos dos
-- campos (reemplazan a un "comentario" genérico por observaciones estructuradas).
ALTER TABLE ev_evaluacion_gestion_ssoma ADD COLUMN IF NOT EXISTS fortalezas text;
ALTER TABLE ev_evaluacion_gestion_ssoma ADD COLUMN IF NOT EXISTS oportunidades_mejora text;
ALTER TABLE ev_evaluacion_gestion_ssoma DROP COLUMN IF EXISTS comentario;

CREATE TABLE IF NOT EXISTS ev_evaluacion_gestion_ssoma_detalle (
    id                          serial PRIMARY KEY,
    evaluacion_gestion_ssoma_id int NOT NULL REFERENCES ev_evaluacion_gestion_ssoma(id),
    plantilla_id                int REFERENCES ev_gestion_ssoma_plantilla(id),
    criterio                    varchar(300) NOT NULL,
    puntaje                     int NOT NULL
);

-- Marca de "ya evaluó a su coordinador" para D4: deliberadamente sin FK
-- hacia/desde ev_evaluacion_gestion_ssoma, igual que el cumplimiento de Jefe SSOMA.
CREATE TABLE IF NOT EXISTS ev_evaluacion_gestion_ssoma_cumplimiento (
    id                  serial PRIMARY KEY,
    periodo_id          int NOT NULL REFERENCES ev_periodo(id),
    evaluador_user_id   int NOT NULL REFERENCES app_user(user_id),
    completado_at       timestamp NOT NULL DEFAULT now(),
    UNIQUE (periodo_id, evaluador_user_id)
);

CREATE INDEX IF NOT EXISTS ix_ev_eval_gestion_ssoma_periodo_evaluador
    ON ev_evaluacion_gestion_ssoma (periodo_id, evaluador_user_id);

CREATE INDEX IF NOT EXISTS ix_ev_eval_gestion_ssoma_periodo_evaluado
    ON ev_evaluacion_gestion_ssoma (periodo_id, evaluado_user_id);

COMMIT;

-- ============================================================================
-- Ajustes de redacción — plantilla de Residentes, área SSOMA (ev_plantilla)
-- Corrige ítems que hablaban solo del residente y no incluían a su staff y
-- las contratistas donde correspondía; corrige el ítem 12 (antes "(prisa)",
-- referido a que una mala planificación de producción obliga a trabajar
-- apurado y sin tiempo para implementar controles de seguridad).
-- ============================================================================

BEGIN;

UPDATE ev_plantilla SET criterio = 'Reporta oportunamente los incidentes y exige lo mismo a su staff y las contratistas', updated_at = now()
WHERE area_nombre = 'SSOMA' AND orden = 2 AND activo = true;

UPDATE ev_plantilla SET criterio = 'Cumple, junto con su staff y las contratistas, con las metas de los indicadores proactivos SSOMA', updated_at = now()
WHERE area_nombre = 'SSOMA' AND orden = 3 AND activo = true;

UPDATE ev_plantilla SET criterio = 'Cumple, junto con su staff y las contratistas, con las metas de los indicadores reactivos SSOMA', updated_at = now()
WHERE area_nombre = 'SSOMA' AND orden = 4 AND activo = true;

UPDATE ev_plantilla SET criterio = 'Reporta y exige el reporte de actos y condiciones inseguras al staff y las contratistas', updated_at = now()
WHERE area_nombre = 'SSOMA' AND orden = 5 AND activo = true;

UPDATE ev_plantilla SET criterio = 'Usa correctamente su EPP y exige lo mismo a su staff y las contratistas', updated_at = now()
WHERE area_nombre = 'SSOMA' AND orden = 6 AND activo = true;

UPDATE ev_plantilla SET criterio = 'Mantiene el orden y limpieza en la obra y exige lo mismo a su staff y las contratistas', updated_at = now()
WHERE area_nombre = 'SSOMA' AND orden = 9 AND activo = true;

UPDATE ev_plantilla SET criterio = 'Cumple y hace cumplir los horarios de obra a su staff y las contratistas', updated_at = now()
WHERE area_nombre = 'SSOMA' AND orden = 11 AND activo = true;

UPDATE ev_plantilla SET criterio = 'Evita que una gestión inadecuada o mala planificación de producción genere trabajos apresurados o superpuestos que impidan implementar controles de seguridad adecuados', updated_at = now()
WHERE area_nombre = 'SSOMA' AND orden = 12 AND activo = true;

COMMIT;

-- ============================================================================
-- Ajustes — plantilla de Supervisor de Contratista (ev_supervisor_contratista_plantilla)
-- Corrige el ítem 1 (decía IPERC, debía decir ATS/PETAR) y el ítem 2 (incluye
-- al propio supervisor, no solo a su cuadrilla); agrega 3 ítems nuevos.
-- ============================================================================

BEGIN;

UPDATE ev_supervisor_contratista_plantilla
SET criterio = 'Revisa adecuadamente el ATS (Análisis de Trabajo Seguro) y el PETAR (Permiso Escrito de Trabajo de Alto Riesgo) antes de iniciar labores'
WHERE orden = 1 AND activo = true;

UPDATE ev_supervisor_contratista_plantilla
SET criterio = 'Uso correcto de EPP por parte del supervisor y su cuadrilla'
WHERE orden = 2 AND activo = true;

INSERT INTO ev_supervisor_contratista_plantilla (criterio, orden)
SELECT * FROM (VALUES
    ('Sube el dossier de cierre en las fechas correctas', 6),
    ('Cumple con el control de ingreso al proyecto de solo personal y empresa habilitada', 7),
    ('Cumple con las herramientas de gestión proactiva SSOMA (inspecciones, auditoría de ATS, etc.)', 8)
) AS v(criterio, orden)
WHERE NOT EXISTS (
    SELECT 1 FROM ev_supervisor_contratista_plantilla WHERE orden IN (6, 7, 8)
);

COMMIT;

-- ============================================================================
-- Verificación (correr después; no modifica nada)
-- ============================================================================
-- SELECT table_name FROM information_schema.tables
-- WHERE table_name IN (
--   'ev_gestion_ssoma_plantilla','ev_evaluacion_gestion_ssoma',
--   'ev_evaluacion_gestion_ssoma_detalle','ev_evaluacion_gestion_ssoma_cumplimiento'
-- );
-- Esperado: las 4 filas.
--
-- SELECT count(*) FROM ev_gestion_ssoma_plantilla;               -- Esperado: 7
-- SELECT count(*) FROM ev_supervisor_contratista_plantilla;      -- Esperado: 8
-- SELECT orden, criterio FROM ev_plantilla WHERE area_nombre = 'SSOMA' ORDER BY orden; -- revisar los 12 ítems
