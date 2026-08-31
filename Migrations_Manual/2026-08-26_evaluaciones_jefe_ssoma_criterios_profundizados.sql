-- ============================================================================
-- Evaluación anónima al Jefe SSOMA — profundiza ev_jefe_ssoma_plantilla
--
-- Los 5 criterios originales (2026-08-20) eran los más genéricos del módulo.
-- Este ajuste los alinea en calidad/profundidad con ev_gestion_ssoma_plantilla
-- (7 criterios de liderazgo, usados en /evaluaciones/gestion-ssoma para las
-- evaluaciones D1-D4 dentro del propio equipo SSOMA) y agrega los que son
-- específicos del rol de Jefe SSOMA (tope de la jerarquía SSOMA): toma de
-- decisiones basada en indicadores y representación de la cultura de
-- seguridad ante Gerencia.
--
-- No toca esquema ni las evaluaciones ya registradas (ev_evaluacion_jefe_ssoma_detalle
-- guarda el texto del criterio en el momento de evaluar, no una referencia viva).
--
-- Idempotente: UPDATE por orden (reafirma el mismo valor si ya corrió) +
-- INSERT de los criterios nuevos guardado con NOT EXISTS.
-- ============================================================================

BEGIN;

UPDATE ev_jefe_ssoma_plantilla SET criterio = 'Muestra liderazgo y disponibilidad para resolver oportunamente los problemas que surgen en campo' WHERE orden = 1 AND activo = true;
UPDATE ev_jefe_ssoma_plantilla SET criterio = 'Comunica con claridad las prioridades y objetivos SSOMA, sin generar confusión en el equipo' WHERE orden = 2 AND activo = true;
UPDATE ev_jefe_ssoma_plantilla SET criterio = 'Brinda los recursos necesarios (EPP, materiales, personal) cuando el equipo los solicita' WHERE orden = 3 AND activo = true;
UPDATE ev_jefe_ssoma_plantilla SET criterio = 'Reconoce el buen desempeño y los logros del equipo SSOMA cuando corresponde' WHERE orden = 4 AND activo = true;
UPDATE ev_jefe_ssoma_plantilla SET criterio = 'Maneja de forma justa e imparcial las observaciones o incidentes reportados' WHERE orden = 5 AND activo = true;

INSERT INTO ev_jefe_ssoma_plantilla (criterio, orden)
SELECT * FROM (VALUES
    ('Da feedback específico y oportuno sobre el desempeño del equipo, no solo cuando hay un error grave', 6),
    ('Está disponible y accesible ante consultas, dudas o propuestas del equipo', 7),
    ('Impulsa el desarrollo profesional del equipo SSOMA (capacitaciones, nuevas responsabilidades)', 8),
    ('Prioriza la seguridad incluso bajo presión de plazos o producción, sin sacrificar controles por avanzar más rápido', 9),
    ('Toma decisiones basadas en el análisis de los indicadores SSOMA (proactivos y reactivos) y hace seguimiento a los planes de acción', 10),
    ('Representa y defiende la cultura de seguridad ante la Gerencia y otras áreas de la empresa', 11)
) AS v(criterio, orden)
WHERE NOT EXISTS (SELECT 1 FROM ev_jefe_ssoma_plantilla WHERE orden IN (6,7,8,9,10,11));

COMMIT;

-- ============================================================================
-- Verificación (correr después; no modifica nada)
-- ============================================================================
-- SELECT orden, criterio FROM ev_jefe_ssoma_plantilla WHERE activo = true ORDER BY orden;
-- Esperado: 11 filas.
