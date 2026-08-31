-- ============================================================================
-- categoria.visible_solicitud_personal  +  categoría EMPLEADO
-- ============================================================================
-- El desplegable de Categoría de GTH → Solicitud de Personal (modo "Puesto
-- personalizado") ofrecía el catálogo completo: 41 categorías, la mayoría de
-- obra (Operario, Peón, Oficial, Ayudante…). Esa pantalla contrata planilla de
-- Abril, así que esas opciones solo cargan el combo.
--
-- No se resolvió con `active` porque el catálogo lo comparten tres pantallas
-- (Solicitud de Personal, Habilitación → Trabajadores y Configuración →
-- Workers): apagar ahí una categoría dejaría sin poder clasificar a los 2.036
-- trabajadores activos de obra. Este flag es visibilidad POR PANTALLA; la
-- categoría sigue viva y asignable en la ficha del trabajador.
--
-- Se ofrecen solo las categorías que hoy tienen lógica en el sistema o que se
-- prevé que la tengan, más una categoría nueva EMPLEADO para el personal de
-- planilla que no cae en ninguna de las otras. El resto del catálogo se revisa
-- más adelante.
--
-- EMPLEADO se inserta con id EXPLÍCITO (42) para que dev y prod queden con el
-- mismo id — es el mismo criterio de `workers_obra_oficina_staff` y lo que
-- permite usar ids como constantes en `Shared/Constants/CategoriaIds.cs`.
-- Verificado el 2026-08-13: ambas bases tienen 41 categorías (ids 1..41, cero
-- borradas) y la secuencia en 41, así que el 42 está libre en las dos.
--
-- Idempotente y declarativo: se puede correr más de una vez, y deja el flag
-- exactamente en el estado de acá aunque una corrida anterior haya marcado
-- otras categorías.
-- ============================================================================

BEGIN;

-- ── 1. Columna ──────────────────────────────────────────────────────────────

ALTER TABLE categoria
    ADD COLUMN IF NOT EXISTS visible_solicitud_personal boolean NOT NULL DEFAULT false;

COMMENT ON COLUMN categoria.visible_solicitud_personal IS
    'Se ofrece en el desplegable de GTH -> Solicitud de Personal (planilla de Abril).';

-- ── 2. Categoría EMPLEADO ───────────────────────────────────────────────────
-- Los nombres del catálogo van siempre en MAYÚSCULAS. orden = 0 para que salga
-- primera en el desplegable: es la opción por defecto del personal de planilla
-- (las demás visibles tienen orden 6..31).
--
--
-- No confundir con `categoria_maestra`.EMPLEADO, que es el tipo de vínculo
-- laboral de la Data Maestra de GTH (EMPLEADO / PRACTICANTE PRE-PRO / RCC).
-- Son ejes distintos y viven en tablas distintas.

INSERT INTO categoria (categoria_id, nombre, orden, visible_solicitud_personal)
SELECT 42, 'EMPLEADO', 0, true
WHERE NOT EXISTS (SELECT 1 FROM categoria WHERE categoria_id = 42)
  AND NOT EXISTS (SELECT 1 FROM categoria WHERE upper(nombre) = 'EMPLEADO' AND state);

-- La secuencia sigue en 41 tras un INSERT con id explícito: se adelanta a mano
-- para que la próxima categoría creada desde la pantalla no choque con el 42.
SELECT setval('categoria_categoria_id_seq', GREATEST((SELECT max(categoria_id) FROM categoria), 42), true);

-- ── 3. Visibilidad ──────────────────────────────────────────────────────────
-- Se listan por id y no por nombre para no depender de tildes ni de renames.

UPDATE categoria SET visible_solicitud_personal = true
WHERE categoria_id IN (
     4, -- PRACTICANTE
     8, -- RESIDENTE
    11, -- GERENTE
    17, -- JEFE
    22, -- COORDINADOR
    26, -- MEDICO
    29, -- SUB GERENTE
    42  -- EMPLEADO (nueva)
) AND NOT visible_solicitud_personal;

-- El resto queda fuera. Va explícito para que el script sea la fuente de verdad
-- del flag y no dependa de en qué estado estaba la base.
UPDATE categoria SET visible_solicitud_personal = false
WHERE categoria_id NOT IN (4, 8, 11, 17, 22, 26, 29, 42)
  AND visible_solicitud_personal;

COMMIT;

-- ============================================================================
-- Verificación (correr después; no modifica nada)
-- ============================================================================
-- SELECT visible_solicitud_personal, count(*)
-- FROM categoria WHERE state GROUP BY 1;          -- esperado: true = 8, false = 34
--
-- SELECT categoria_id, nombre, orden
-- FROM categoria WHERE state AND visible_solicitud_personal
-- ORDER BY orden, nombre;                          -- EMPLEADO primero
--
-- SELECT last_value FROM categoria_categoria_id_seq;  -- esperado: 42
