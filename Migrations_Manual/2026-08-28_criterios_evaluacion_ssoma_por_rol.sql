-- ============================================================================
-- Criterios de evaluacion SSOMA separados por rol (Coordinador SSOMA vs
-- Prevencionista), en las dos plantillas que hoy usan una sola lista para
-- ambos puestos aunque tienen responsabilidades distintas:
--
--   ev_gestion_ssoma_plantilla    -> Flujo D (evaluacion interna dentro del
--                                    propio equipo SSOMA: Jefe<->Coordinador,
--                                    Coordinador->Prevencionista, y entre pares)
--   ev_prevencionista_plantilla   -> Flujo C (el supervisor de campo de la
--                                    contratista evalua al Prevencionista o
--                                    Coordinador SSOMA de Abril asignado a su
--                                    proyecto)
--
-- Ambas tablas ganan la columna rol_evaluado ('COORDINADOR' | 'PREVENCIONISTA')
-- para poder separar la plantilla segun a quien se esta evaluando. Idempotente:
-- los INSERT solo agregan una fila si el criterio exacto no existe todavia.
-- ============================================================================

BEGIN;

-- ── 1) ev_gestion_ssoma_plantilla ───────────────────────────────────────────

ALTER TABLE ev_gestion_ssoma_plantilla ADD COLUMN IF NOT EXISTS rol_evaluado varchar(20);

-- 1a. Los 7 criterios existentes son todos de liderazgo -- le calzan al
--     Coordinador (lidera Prevencionistas), no al Prevencionista (no lidera
--     a nadie). Se marcan como COORDINADOR y se reordenan; el que no se
--     salvó ("Esta disponible y accesible...") se desactiva pero se conserva.
UPDATE ev_gestion_ssoma_plantilla SET rol_evaluado = 'COORDINADOR', orden = 1
  WHERE criterio = 'Comunica instrucciones y prioridades con claridad, sin generar confusión ni necesidad de repetir';
UPDATE ev_gestion_ssoma_plantilla SET rol_evaluado = 'COORDINADOR', orden = 2,
       criterio = 'Da feedback específico y oportuno al equipo sobre su desempeño, no solo cuando hay un error grave'
  WHERE criterio = 'Da feedback específico y oportuno sobre el desempeño (no solo cuando hay un error grave)';
UPDATE ev_gestion_ssoma_plantilla SET rol_evaluado = 'COORDINADOR', activo = false
  WHERE criterio = 'Está disponible y accesible ante consultas, dudas o propuestas del equipo';
UPDATE ev_gestion_ssoma_plantilla SET rol_evaluado = 'COORDINADOR', orden = 3
  WHERE criterio = 'Reconoce el buen desempeño y los logros del equipo cuando corresponde';
UPDATE ev_gestion_ssoma_plantilla SET rol_evaluado = 'COORDINADOR', orden = 5
  WHERE criterio = 'Aborda desacuerdos o problemas de forma directa y respetuosa, sin evitarlos ni escalarlos innecesariamente';
UPDATE ev_gestion_ssoma_plantilla SET rol_evaluado = 'COORDINADOR', orden = 4
  WHERE criterio = 'Impulsa el desarrollo profesional de su equipo (capacitación, nuevas responsabilidades)';
UPDATE ev_gestion_ssoma_plantilla SET rol_evaluado = 'COORDINADOR', orden = 9
  WHERE criterio = 'Prioriza la seguridad incluso bajo presión de plazos o producción, sin sacrificar controles por avanzar más rápido';

-- 1b. Coordinador: 3 criterios nuevos.
INSERT INTO ev_gestion_ssoma_plantilla (criterio, orden, activo, rol_evaluado)
SELECT v.criterio, v.orden, true, 'COORDINADOR'
FROM (VALUES
  ('Toma decisiones oportunas ante incidentes o desviaciones críticas de seguridad, sin esperar instrucciones de arriba', 6),
  ('Logra compromisos reales de seguridad con Producción/Residencia, no solo cumplimiento documentario de papel', 7),
  ('Distribuye la carga entre sus prevencionistas según el riesgo real de cada frente, no de forma pareja', 8)
) AS v(criterio, orden)
WHERE NOT EXISTS (SELECT 1 FROM ev_gestion_ssoma_plantilla WHERE criterio = v.criterio);

-- 1c. Prevencionista: 8 criterios nuevos (ninguno de los 7 originales aplicaba).
INSERT INTO ev_gestion_ssoma_plantilla (criterio, orden, activo, rol_evaluado)
SELECT v.criterio, v.orden, true, 'PREVENCIONISTA'
FROM (VALUES
  ('Está presente en los frentes de trabajo críticos, no solo en oficina/gabinete', 1),
  ('Identifica riesgos y actos/condiciones subestándar antes de que deriven en incidentes', 2),
  ('Sus reportes, checklists e IPERC son completos y reflejan la realidad del campo, no copiados de plantilla', 3),
  ('Logra que sus observaciones se corrijan de verdad, no solo que queden registradas', 4),
  ('Hace seguimiento a que los compromisos de seguridad se cumplan en el plazo acordado', 5),
  ('Domina la normativa aplicable a su frente (altura, izaje, espacios confinados, eléctrico, etc.) y la explica con criterio', 6),
  ('Mantiene firmeza con el personal de obra y contratistas sin evitar confrontar riesgos por quedar bien', 7),
  ('Está disponible y accesible ante consultas o emergencias durante su turno', 8)
) AS v(criterio, orden)
WHERE NOT EXISTS (SELECT 1 FROM ev_gestion_ssoma_plantilla WHERE criterio = v.criterio);

-- ── 2) ev_prevencionista_plantilla ──────────────────────────────────────────

ALTER TABLE ev_prevencionista_plantilla ADD COLUMN IF NOT EXISTS rol_evaluado varchar(20);

-- 2a. Los 5 criterios existentes sí le calzan al Prevencionista (contacto
--     diario en campo) -- se marcan y se reformulan un poco para precisión.
UPDATE ev_prevencionista_plantilla SET rol_evaluado = 'PREVENCIONISTA', orden = 1,
       criterio = 'Presencia y acompañamiento real en el frente de trabajo'
  WHERE criterio = 'Presencia y acompañamiento en campo';
UPDATE ev_prevencionista_plantilla SET rol_evaluado = 'PREVENCIONISTA', orden = 2,
       criterio = 'Claridad al explicar observaciones de seguridad — se entiende qué corregir y por qué'
  WHERE criterio = 'Claridad al explicar observaciones de seguridad';
UPDATE ev_prevencionista_plantilla SET rol_evaluado = 'PREVENCIONISTA', orden = 3
  WHERE criterio = 'Trato respetuoso hacia el personal del contratista';
UPDATE ev_prevencionista_plantilla SET rol_evaluado = 'PREVENCIONISTA', orden = 4,
       criterio = 'Rapidez para resolver en el momento, o escalar cuando corresponde, temas de seguridad'
  WHERE criterio = 'Rapidez para resolver o escalar temas de seguridad';
UPDATE ev_prevencionista_plantilla SET rol_evaluado = 'PREVENCIONISTA', orden = 5,
       criterio = 'Conocimiento técnico de los riesgos propios de la obra'
  WHERE criterio = 'Conocimiento técnico de los riesgos de la obra';

-- 2b. Prevencionista: 3 criterios nuevos.
INSERT INTO ev_prevencionista_plantilla (criterio, orden, activo, rol_evaluado)
SELECT v.criterio, v.orden, true, 'PREVENCIONISTA'
FROM (VALUES
  ('Ayuda a encontrar una solución práctica ante una observación, no solo detiene el trabajo sin alternativa', 6),
  ('Mantiene el mismo criterio entre visitas — no cambia de opinión sobre lo mismo sin explicación', 7),
  ('Cumple los tiempos que ofrece (revisar un PETAR, responder una consulta, etc.)', 8)
) AS v(criterio, orden)
WHERE NOT EXISTS (SELECT 1 FROM ev_prevencionista_plantilla WHERE criterio = v.criterio);

-- 2c. Coordinador SSOMA: 7 criterios nuevos (no existía ninguno para este rol).
INSERT INTO ev_prevencionista_plantilla (criterio, orden, activo, rol_evaluado)
SELECT v.criterio, v.orden, true, 'COORDINADOR'
FROM (VALUES
  ('Los requisitos de habilitación/documentación que exige son claros y consistentes en el tiempo', 1),
  ('Resuelve con rapidez los escalamientos que el Prevencionista no pudo cerrar en campo', 2),
  ('Está disponible para coordinaciones cuando el proyecto lo necesita, no solo por correo', 3),
  ('Trata por igual a todas las contratistas del proyecto, sin favoritismos', 4),
  ('Comunica con anticipación los cambios de procedimiento o exigencias del proyecto', 5),
  ('Facilita el cumplimiento con orientación práctica, no solo fiscaliza y exige', 6),
  ('Conoce la realidad de la obra de la contratista al tomar decisiones, no solo la norma en abstracto', 7)
) AS v(criterio, orden)
WHERE NOT EXISTS (SELECT 1 FROM ev_prevencionista_plantilla WHERE criterio = v.criterio);

COMMIT;

-- ============================================================================
-- Verificación (correr después; no modifica nada)
-- ============================================================================
-- SELECT 'ev_gestion_ssoma_plantilla' AS tabla, rol_evaluado, count(*) FILTER (WHERE activo) AS activos
--   FROM ev_gestion_ssoma_plantilla GROUP BY rol_evaluado
-- UNION ALL
-- SELECT 'ev_prevencionista_plantilla', rol_evaluado, count(*) FILTER (WHERE activo)
--   FROM ev_prevencionista_plantilla GROUP BY rol_evaluado;
-- Esperado: gestion_ssoma -> COORDINADOR 9, PREVENCIONISTA 8.
--           prevencionista -> COORDINADOR 7, PREVENCIONISTA 8.
