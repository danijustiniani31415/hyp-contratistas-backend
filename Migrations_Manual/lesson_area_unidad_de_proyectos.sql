-- ⚠️ NOTA (2026-08-04): los pasos que usaban TABLAS TEMPORALES (ON COMMIT DROP)
--    solo funcionan si TODO el archivo corre dentro de una sola transacción.
--    Para aplicarlo sentencia por sentencia (sin transacción, para no cargar la
--    RAM de la VPS) usar en su lugar:
--        Migrations_Manual/obra_oficina_resto_sin_transaccion.sql
--    que reemplaza a este archivo desde el paso 5b en adelante.

-- ============================================================================
-- POSTERIOR a Migrations_Manual/workers_obra_oficina_staff.sql — no correr antes.
--
-- Al eliminar el tipo "Área Obra_Oficina", "Unidad de Proyectos" quedó como nodo
-- INTERMEDIO (sus hijos son Ingeniería BIM y Planeamiento BIM). En el formulario
-- de lecciones la cascada solo acepta HOJAS, así que quien trabaja en Unidad de
-- Proyectos "puro" se quedaba sin sección Clasificación.
--
-- Se le crea un hijo "Unidad de Proyectos" (Área Estándar, misma convención de
-- "<Área> puro") con la MISMA plantilla que tenía el padre, y se le mueven sus
-- lecciones. El padre queda solo como contenedor del filtro.
--
-- Ejecutar TODO el bloque de una sola vez (es una única transacción).
-- Aplicado en DEV el 2026-08-04.
-- ============================================================================

BEGIN;

-- ---------------------------------------------------------------------------
-- 0. Nodo padre: "Unidad de Proyectos" de tipo Área Estándar (vivo).
-- ---------------------------------------------------------------------------
CREATE TEMP TABLE udp (
    padre_scope_id       integer,
    padre_lesson_area_id integer,
    hijo_scope_id        integer,
    hijo_lesson_area_id  integer
) ON COMMIT DROP;

INSERT INTO udp (padre_scope_id, padre_lesson_area_id)
SELECT s.area_scope_id,
       (SELECT la.lesson_area_id FROM lesson_area la WHERE la.area_scope_id = s.area_scope_id)
FROM area_scope s
JOIN area_item ai ON ai.area_item_id = s.area_item_id
JOIN area_type at ON at.area_type_id = ai.area_type_id
WHERE s.state AND ai.state AND at.state
  AND lower(btrim(ai.area_item_name)) = 'unidad de proyectos'
  AND at.area_type_name = 'Área Estándar'
ORDER BY s.area_scope_id
LIMIT 1;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM udp WHERE padre_scope_id IS NOT NULL) THEN
        RAISE EXCEPTION 'No se encontró el nodo "Unidad de Proyectos" (Área Estándar) vivo en area_scope.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM udp WHERE padre_lesson_area_id IS NOT NULL) THEN
        RAISE EXCEPTION 'El nodo "Unidad de Proyectos" no tiene lesson_area; corre primero workers_obra_oficina_staff.sql.';
    END IF;
END $$;

-- ---------------------------------------------------------------------------
-- 1. Nodo hijo "Unidad de Proyectos" (reutiliza el mismo area_item que el padre,
--    igual que hacían los pares Calidad > Calidad, SSOMA > SSOMA, etc.).
--    El índice ux_area_scope_item_parent_alive es (area_item_id, padre), así que
--    (28, 41) no choca con el (28, 17) del propio padre.
-- ---------------------------------------------------------------------------
INSERT INTO area_scope (area_item_id, area_scope_parent_id, display_order, active, state)
SELECT p.area_item_id, u.padre_scope_id, 0, true, true
FROM udp u
JOIN area_scope p ON p.area_scope_id = u.padre_scope_id
WHERE NOT EXISTS (
    SELECT 1 FROM area_scope x
    WHERE x.area_scope_parent_id = u.padre_scope_id
      AND x.area_item_id = p.area_item_id
      AND x.state
);

UPDATE udp u
SET hijo_scope_id = (
    SELECT x.area_scope_id
    FROM area_scope x
    JOIN area_scope p ON p.area_scope_id = u.padre_scope_id
    WHERE x.area_scope_parent_id = u.padre_scope_id
      AND x.area_item_id = p.area_item_id
      AND x.state
    ORDER BY x.area_scope_id
    LIMIT 1
);

-- ---------------------------------------------------------------------------
-- 2. lesson_area del hijo: activa y disponible en el formulario.
-- ---------------------------------------------------------------------------
INSERT INTO lesson_area (area_scope_id, active, created_at,
                         include_in_form, include_descendants, include_as_independent)
SELECT u.hijo_scope_id, true, now(), true, false, false
FROM udp u
WHERE NOT EXISTS (SELECT 1 FROM lesson_area la WHERE la.area_scope_id = u.hijo_scope_id);

UPDATE udp u
SET hijo_lesson_area_id = (SELECT la.lesson_area_id FROM lesson_area la WHERE la.area_scope_id = u.hijo_scope_id);

-- Por si la fila ya existía apagada de una corrida anterior.
UPDATE lesson_area la
SET active                 = true,
    include_in_form        = true,
    include_descendants    = false,
    include_as_independent = false
FROM udp u
WHERE la.lesson_area_id = u.hijo_lesson_area_id;

-- ---------------------------------------------------------------------------
-- 3. Clonar la plantilla (scope_item) del padre al hijo conservando la jerarquía.
--    Truco: cada id nuevo = id viejo + offset. Como ningún scope_item referencia
--    a un padre de otra lesson_area (verificado), desplazar padre e hijo por el
--    mismo offset reproduce el árbol exacto.
-- ---------------------------------------------------------------------------
INSERT INTO scope_item (scope_item_id, lesson_area_id, catalog_item_id,
                        scope_item_parent_id, display_order, active)
SELECT si.scope_item_id + off.v,
       u.hijo_lesson_area_id,
       si.catalog_item_id,
       si.scope_item_parent_id + off.v,   -- NULL + n = NULL: las raíces siguen siendo raíces
       si.display_order,
       si.active
FROM udp u
JOIN scope_item si ON si.lesson_area_id = u.padre_lesson_area_id
CROSS JOIN (SELECT COALESCE(max(scope_item_id), 0) AS v FROM scope_item) off
WHERE NOT EXISTS (
    SELECT 1 FROM scope_item x WHERE x.lesson_area_id = u.hijo_lesson_area_id
);

SELECT setval('scope_item_scope_item_id_seq', (SELECT max(scope_item_id) FROM scope_item));

-- ---------------------------------------------------------------------------
-- 4. Las lecciones del padre pasan al hijo (son las de "Unidad de Proyectos puro":
--    las que antes colgaban del nodo Obra_Oficina "Unidad de Proyectos").
--    Sus catalog_item_id resuelven igual porque la plantilla es la misma.
-- ---------------------------------------------------------------------------
UPDATE lesson l
SET lesson_area_id = u.hijo_lesson_area_id
FROM udp u
WHERE l.lesson_area_id = u.padre_lesson_area_id;

-- ---------------------------------------------------------------------------
-- 5. El padre deja de ser opción del formulario (ya no es hoja) y queda como
--    contenedor del filtro: detenerse en él sigue agrupando a todas sus subáreas.
-- ---------------------------------------------------------------------------
UPDATE lesson_area la
SET include_in_form     = false,
    include_descendants = true
FROM udp u
WHERE la.lesson_area_id = u.padre_lesson_area_id;

COMMIT;
