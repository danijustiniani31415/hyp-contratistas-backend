-- ============================================================================
-- CONTINUACIÓN de la migración Obra/Oficina — versión SIN transacciones ni
-- tablas temporales (cada sentencia commitea sola, para no inflar la RAM de la VPS).
--
-- Sustituye a todo lo que quedó pendiente de:
--   Migrations_Manual/workers_obra_oficina_staff.sql        (pasos 5b a 11)
--   Migrations_Manual/lesson_area_unidad_de_proyectos.sql   (completo)
--
-- Ya se dio por ejecutado en PROD:
--   • workers_obra_oficina_staff + workers.obra_oficina_staff_id + backfill
--   • ux_workers_email_corporativo_vigente recreado
--   • lesson.obra_oficina_staff_id + FK + índice
--   • el INSERT que crea el lesson_area del nodo padre (paso 5a)
--
-- El mapa hijo→padre que antes vivía en la tabla temporal `la_map` ahora se
-- recalcula como CTE en cada sentencia. Es estable: no depende de `state` ni de
-- `active`, así que sigue devolviendo lo mismo aunque los pasos posteriores vayan
-- dando de baja los nodos.
--
-- ⚠️ EJECUTAR EN ORDEN. Cada bloque es idempotente: si uno falla se puede repetir.
--    Con psql:  psql -h localhost -p 5544 -U abril -d abril -f este_archivo.sql
--    (psql commitea sentencia por sentencia; NO pegar todo junto en pgAdmin,
--     porque ahí el buffer entero se manda como una sola transacción implícita.)
-- ============================================================================


-- ─────────────────────────────────────────────────────────────────────────────
-- 0. VERIFICACIÓN PREVIA (solo lectura). Debe devolver 6 filas: los lesson_area
--    de nodos Obra_Oficina con el lesson_area del padre ya resuelto (new_id NO
--    puede ser NULL; si lo es, falta correr el paso 5a de más abajo).
-- ─────────────────────────────────────────────────────────────────────────────
SELECT la.lesson_area_id          AS old_id,
       pla.lesson_area_id         AS new_id,
       ai.area_item_name          AS nodo,
       la.active                  AS old_active,
       la.include_in_form         AS old_include_in_form,
       (SELECT count(*) FROM lesson l WHERE l.lesson_area_id = la.lesson_area_id) AS lecciones
FROM lesson_area la
JOIN area_scope s   ON s.area_scope_id = la.area_scope_id
JOIN area_item  ai  ON ai.area_item_id = s.area_item_id AND ai.area_type_id = 3
JOIN area_scope p   ON p.area_scope_id = s.area_scope_parent_id
LEFT JOIN lesson_area pla ON pla.area_scope_id = p.area_scope_id
ORDER BY la.lesson_area_id;


-- ─────────────────────────────────────────────────────────────────────────────
-- 5a (repetición inocua). Crea el lesson_area del nodo PADRE cuando no existe.
--     Si ya se ejecutó, no inserta nada.
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO lesson_area (area_scope_id, active, created_at,
                         include_in_form, include_descendants, include_as_independent)
SELECT DISTINCT p.area_scope_id, true, now(), false, false, false
FROM lesson_area la
JOIN area_scope s  ON s.area_scope_id = la.area_scope_id
JOIN area_item  ai ON ai.area_item_id = s.area_item_id AND ai.area_type_id = 3
JOIN area_scope p  ON p.area_scope_id = s.area_scope_parent_id
WHERE NOT EXISTS (SELECT 1 FROM lesson_area x WHERE x.area_scope_id = p.area_scope_id);


-- ─────────────────────────────────────────────────────────────────────────────
-- 6. Las lecciones de los nodos Obra_Oficina pasan al lesson_area del padre y
--    fijan su Obra/Oficina.
--    'Oficina Técnica' -> Staff (2). El resto de nodos Obra_Oficina repiten el
--    nombre del padre (Calidad > Calidad, UdP > UdP, ...) y correspondían a la
--    sede: Oficina Central (3).
-- ─────────────────────────────────────────────────────────────────────────────
WITH la_map AS (
    SELECT la.lesson_area_id  AS old_id,
           pla.lesson_area_id AS new_id,
           CASE WHEN lower(ai.area_item_name) LIKE 'oficina t%cnica' THEN 2 ELSE 3 END AS obra_oficina_staff_id
    FROM lesson_area la
    JOIN area_scope s   ON s.area_scope_id = la.area_scope_id
    JOIN area_item  ai  ON ai.area_item_id = s.area_item_id AND ai.area_type_id = 3
    JOIN area_scope p   ON p.area_scope_id = s.area_scope_parent_id
    JOIN lesson_area pla ON pla.area_scope_id = p.area_scope_id
)
UPDATE lesson l
SET lesson_area_id        = m.new_id,
    obra_oficina_staff_id = m.obra_oficina_staff_id
FROM la_map m
WHERE l.lesson_area_id = m.old_id;


-- ─────────────────────────────────────────────────────────────────────────────
-- 7a. Si el padre no tiene plantilla propia, hereda la del hijo (el de menor id
--     cuando hay varios hermanos con el mismo árbol).
-- ─────────────────────────────────────────────────────────────────────────────
WITH la_map AS (
    SELECT la.lesson_area_id AS old_id, pla.lesson_area_id AS new_id
    FROM lesson_area la
    JOIN area_scope s   ON s.area_scope_id = la.area_scope_id
    JOIN area_item  ai  ON ai.area_item_id = s.area_item_id AND ai.area_type_id = 3
    JOIN area_scope p   ON p.area_scope_id = s.area_scope_parent_id
    JOIN lesson_area pla ON pla.area_scope_id = p.area_scope_id
),
targets AS (
    SELECT m.new_id, MIN(m.old_id) AS src_old_id
    FROM la_map m
    WHERE EXISTS     (SELECT 1 FROM scope_item si  WHERE si.lesson_area_id  = m.old_id AND si.active)
      AND NOT EXISTS (SELECT 1 FROM scope_item si2 WHERE si2.lesson_area_id = m.new_id AND si2.active)
    GROUP BY m.new_id
)
UPDATE scope_item si
SET lesson_area_id = t.new_id
FROM targets t
WHERE si.lesson_area_id = t.src_old_id;


-- ─────────────────────────────────────────────────────────────────────────────
-- 7b. El padre hereda la visibilidad que tenía el hijo (activa / en formulario).
--     ⚠️ TIENE QUE CORRER ANTES DEL 7d, que es el que apaga esos flags en el hijo.
-- ─────────────────────────────────────────────────────────────────────────────
WITH la_map AS (
    SELECT la.lesson_area_id AS old_id, pla.lesson_area_id AS new_id,
           la.active AS old_active, la.include_in_form AS old_include_in_form
    FROM lesson_area la
    JOIN area_scope s   ON s.area_scope_id = la.area_scope_id
    JOIN area_item  ai  ON ai.area_item_id = s.area_item_id AND ai.area_type_id = 3
    JOIN area_scope p   ON p.area_scope_id = s.area_scope_parent_id
    JOIN lesson_area pla ON pla.area_scope_id = p.area_scope_id
),
x AS (SELECT DISTINCT new_id FROM la_map WHERE old_active AND old_include_in_form)
UPDATE lesson_area t
SET active          = true,
    include_in_form = true
FROM x
WHERE t.lesson_area_id = x.new_id;


-- ─────────────────────────────────────────────────────────────────────────────
-- 7c. Las relaciones que quedaron colgando del hijo se desactivan.
-- ─────────────────────────────────────────────────────────────────────────────
WITH la_map AS (
    SELECT la.lesson_area_id AS old_id
    FROM lesson_area la
    JOIN area_scope s   ON s.area_scope_id = la.area_scope_id
    JOIN area_item  ai  ON ai.area_item_id = s.area_item_id AND ai.area_type_id = 3
    JOIN area_scope p   ON p.area_scope_id = s.area_scope_parent_id
    JOIN lesson_area pla ON pla.area_scope_id = p.area_scope_id
)
UPDATE scope_item
SET active = false
WHERE lesson_area_id IN (SELECT old_id FROM la_map) AND active;


-- ─────────────────────────────────────────────────────────────────────────────
-- 7d. Y el lesson_area del nodo Obra_Oficina deja de existir para la app.
-- ─────────────────────────────────────────────────────────────────────────────
WITH la_map AS (
    SELECT la.lesson_area_id AS old_id
    FROM lesson_area la
    JOIN area_scope s   ON s.area_scope_id = la.area_scope_id
    JOIN area_item  ai  ON ai.area_item_id = s.area_item_id AND ai.area_type_id = 3
    JOIN area_scope p   ON p.area_scope_id = s.area_scope_parent_id
    JOIN lesson_area pla ON pla.area_scope_id = p.area_scope_id
)
UPDATE lesson_area
SET active                 = false,
    include_in_form        = false,
    include_descendants    = false,
    include_as_independent = false
WHERE lesson_area_id IN (SELECT old_id FROM la_map);


-- ─────────────────────────────────────────────────────────────────────────────
-- 8. Backfill del resto de lecciones (las que nunca colgaron de un nodo
--    Obra_Oficina): se toma el valor del trabajador que las creó.
-- ─────────────────────────────────────────────────────────────────────────────
UPDATE lesson l
SET obra_oficina_staff_id = sub.obra_oficina_staff_id
FROM (
    SELECT DISTINCT ON (p.user_id) p.user_id, w.obra_oficina_staff_id
    FROM person p
    JOIN workers w ON w.person_id = p.person_id
    WHERE p.user_id IS NOT NULL AND w.obra_oficina_staff_id IS NOT NULL
    ORDER BY p.user_id, w.id
) sub
WHERE l.created_user_id = sub.user_id
  AND l.obra_oficina_staff_id IS NULL;


-- ─────────────────────────────────────────────────────────────────────────────
-- 9. workers.area_scope_id: los que apuntaban a un nodo Obra_Oficina pasan al
--    nodo padre (o a NULL si ese nodo era raíz).
-- ─────────────────────────────────────────────────────────────────────────────
UPDATE workers w
SET area_scope_id = s.area_scope_parent_id
FROM area_scope s
JOIN area_item ai ON ai.area_item_id = s.area_item_id AND ai.area_type_id = 3
WHERE w.area_scope_id = s.area_scope_id;


-- ─────────────────────────────────────────────────────────────────────────────
-- 10. Baja lógica del tipo de área "Área Obra_Oficina" y de todo lo suyo.
--     (state = false: nada se borra físicamente, se conserva para auditoría.)
-- ─────────────────────────────────────────────────────────────────────────────
UPDATE area_scope s
SET state = false, active = false
FROM area_item ai
WHERE ai.area_item_id = s.area_item_id AND ai.area_type_id = 3 AND s.state;

UPDATE area_item SET state = false, active = false WHERE area_type_id = 3 AND state;

UPDATE area_type SET state = false, active = false WHERE area_type_id = 3 AND state;


-- ─────────────────────────────────────────────────────────────────────────────
-- 11. Se retira el texto plano workers.obra_oficina (ya normalizado).
--     ⚠️ El backend actualmente en producción TODAVÍA lee esta columna: correr
--        este bloque junto con el deploy del backend nuevo.
-- ─────────────────────────────────────────────────────────────────────────────
ALTER TABLE workers DROP COLUMN IF EXISTS obra_oficina;


-- ═════════════════════════════════════════════════════════════════════════════
-- SEGUNDA PARTE: "Unidad de Proyectos" puro
--
-- Al eliminar el tipo Obra_Oficina, "Unidad de Proyectos" quedó como nodo
-- INTERMEDIO (sus hijos son Ingeniería BIM y Planeamiento BIM). La cascada del
-- formulario solo acepta HOJAS, así que quien trabaja en Unidad de Proyectos
-- "puro" se quedaba sin sección Clasificación. Se le crea un hijo homónimo con
-- la misma plantilla y se le mueven sus lecciones.
-- ═════════════════════════════════════════════════════════════════════════════


-- ─────────────────────────────────────────────────────────────────────────────
-- 12. VERIFICACIÓN (solo lectura). Debe devolver 1 fila (el nodo padre), con
--     padre_lesson_area_id NO nulo. Si no, no sigas: revisa los pasos anteriores.
-- ─────────────────────────────────────────────────────────────────────────────
SELECT s.area_scope_id AS padre_scope_id,
       s.area_item_id,
       (SELECT la.lesson_area_id FROM lesson_area la WHERE la.area_scope_id = s.area_scope_id) AS padre_lesson_area_id
FROM area_scope s
JOIN area_item ai ON ai.area_item_id = s.area_item_id
JOIN area_type at ON at.area_type_id = ai.area_type_id
WHERE s.state AND ai.state AND at.state
  AND lower(btrim(ai.area_item_name)) = 'unidad de proyectos'
  AND at.area_type_name = 'Área Estándar'
  -- Excluye al propio hijo si este script ya se corrió antes: el padre es el que
  -- NO cuelga de otro nodo con el mismo area_item.
  AND NOT EXISTS (SELECT 1 FROM area_scope pp
                   WHERE pp.area_scope_id = s.area_scope_parent_id
                     AND pp.area_item_id = s.area_item_id);


-- ─────────────────────────────────────────────────────────────────────────────
-- 13. Nodo hijo "Unidad de Proyectos" (reutiliza el mismo area_item que el padre,
--     igual que hacían los pares Calidad > Calidad, SSOMA > SSOMA, etc.).
--     El índice ux_area_scope_item_parent_alive es (area_item_id, padre), así que
--     no choca con el propio padre, que cuelga de la gerencia.
-- ─────────────────────────────────────────────────────────────────────────────
WITH padre AS (
    SELECT s.area_scope_id, s.area_item_id
    FROM area_scope s
    JOIN area_item ai ON ai.area_item_id = s.area_item_id
    JOIN area_type at ON at.area_type_id = ai.area_type_id
    WHERE s.state AND ai.state AND at.state
      AND lower(btrim(ai.area_item_name)) = 'unidad de proyectos'
      AND at.area_type_name = 'Área Estándar'
      -- El padre es el que NO cuelga de otro nodo con el mismo area_item; así el
      -- hijo creado por este mismo script nunca se confunde con él al re-ejecutar.
      AND NOT EXISTS (SELECT 1 FROM area_scope pp
                       WHERE pp.area_scope_id = s.area_scope_parent_id
                         AND pp.area_item_id = s.area_item_id)
    ORDER BY s.area_scope_id
    LIMIT 1
)
INSERT INTO area_scope (area_item_id, area_scope_parent_id, display_order, active, state)
SELECT p.area_item_id, p.area_scope_id, 0, true, true
FROM padre p
WHERE NOT EXISTS (
    SELECT 1 FROM area_scope x
    WHERE x.area_scope_parent_id = p.area_scope_id
      AND x.area_item_id = p.area_item_id
      AND x.state
);


-- ─────────────────────────────────────────────────────────────────────────────
-- 14. lesson_area del hijo: activa y disponible en el formulario.
-- ─────────────────────────────────────────────────────────────────────────────
WITH padre AS (
    SELECT s.area_scope_id, s.area_item_id
    FROM area_scope s
    JOIN area_item ai ON ai.area_item_id = s.area_item_id
    JOIN area_type at ON at.area_type_id = ai.area_type_id
    WHERE s.state AND ai.state AND at.state
      AND lower(btrim(ai.area_item_name)) = 'unidad de proyectos'
      AND at.area_type_name = 'Área Estándar'
      -- El padre es el que NO cuelga de otro nodo con el mismo area_item; así el
      -- hijo creado por este mismo script nunca se confunde con él al re-ejecutar.
      AND NOT EXISTS (SELECT 1 FROM area_scope pp
                       WHERE pp.area_scope_id = s.area_scope_parent_id
                         AND pp.area_item_id = s.area_item_id)
    ORDER BY s.area_scope_id
    LIMIT 1
),
hijo AS (
    SELECT x.area_scope_id
    FROM area_scope x JOIN padre p ON x.area_scope_parent_id = p.area_scope_id
    WHERE x.area_item_id = p.area_item_id AND x.state
    ORDER BY x.area_scope_id
    LIMIT 1
)
INSERT INTO lesson_area (area_scope_id, active, created_at,
                         include_in_form, include_descendants, include_as_independent)
SELECT h.area_scope_id, true, now(), true, false, false
FROM hijo h
WHERE NOT EXISTS (SELECT 1 FROM lesson_area la WHERE la.area_scope_id = h.area_scope_id);


-- ─────────────────────────────────────────────────────────────────────────────
-- 15. Por si la fila del hijo ya existía apagada de una corrida anterior.
-- ─────────────────────────────────────────────────────────────────────────────
WITH padre AS (
    SELECT s.area_scope_id, s.area_item_id
    FROM area_scope s
    JOIN area_item ai ON ai.area_item_id = s.area_item_id
    JOIN area_type at ON at.area_type_id = ai.area_type_id
    WHERE s.state AND ai.state AND at.state
      AND lower(btrim(ai.area_item_name)) = 'unidad de proyectos'
      AND at.area_type_name = 'Área Estándar'
      -- El padre es el que NO cuelga de otro nodo con el mismo area_item; así el
      -- hijo creado por este mismo script nunca se confunde con él al re-ejecutar.
      AND NOT EXISTS (SELECT 1 FROM area_scope pp
                       WHERE pp.area_scope_id = s.area_scope_parent_id
                         AND pp.area_item_id = s.area_item_id)
    ORDER BY s.area_scope_id
    LIMIT 1
),
hijo AS (
    SELECT x.area_scope_id
    FROM area_scope x JOIN padre p ON x.area_scope_parent_id = p.area_scope_id
    WHERE x.area_item_id = p.area_item_id AND x.state
    ORDER BY x.area_scope_id
    LIMIT 1
)
UPDATE lesson_area la
SET active                 = true,
    include_in_form        = true,
    include_descendants    = false,
    include_as_independent = false
FROM hijo h
WHERE la.area_scope_id = h.area_scope_id;


-- ─────────────────────────────────────────────────────────────────────────────
-- 16. Clonar la plantilla (scope_item) del padre al hijo conservando la jerarquía.
--     Truco: cada id nuevo = id viejo + offset. Como ningún scope_item referencia
--     a un padre de otra lesson_area (verificado), desplazar padre e hijo por el
--     mismo offset reproduce el árbol exacto.
-- ─────────────────────────────────────────────────────────────────────────────
WITH padre AS (
    SELECT s.area_scope_id, s.area_item_id
    FROM area_scope s
    JOIN area_item ai ON ai.area_item_id = s.area_item_id
    JOIN area_type at ON at.area_type_id = ai.area_type_id
    WHERE s.state AND ai.state AND at.state
      AND lower(btrim(ai.area_item_name)) = 'unidad de proyectos'
      AND at.area_type_name = 'Área Estándar'
      -- El padre es el que NO cuelga de otro nodo con el mismo area_item; así el
      -- hijo creado por este mismo script nunca se confunde con él al re-ejecutar.
      AND NOT EXISTS (SELECT 1 FROM area_scope pp
                       WHERE pp.area_scope_id = s.area_scope_parent_id
                         AND pp.area_item_id = s.area_item_id)
    ORDER BY s.area_scope_id
    LIMIT 1
),
hijo AS (
    SELECT x.area_scope_id
    FROM area_scope x JOIN padre p ON x.area_scope_parent_id = p.area_scope_id
    WHERE x.area_item_id = p.area_item_id AND x.state
    ORDER BY x.area_scope_id
    LIMIT 1
),
ids AS (
    SELECT (SELECT la.lesson_area_id FROM lesson_area la JOIN padre p ON la.area_scope_id = p.area_scope_id) AS padre_la,
           (SELECT la.lesson_area_id FROM lesson_area la JOIN hijo  h ON la.area_scope_id = h.area_scope_id) AS hijo_la
),
off AS (SELECT COALESCE(max(scope_item_id), 0) AS v FROM scope_item)
INSERT INTO scope_item (scope_item_id, lesson_area_id, catalog_item_id,
                        scope_item_parent_id, display_order, active)
SELECT si.scope_item_id + off.v,
       ids.hijo_la,
       si.catalog_item_id,
       si.scope_item_parent_id + off.v,   -- NULL + n = NULL: las raíces siguen siendo raíces
       si.display_order,
       si.active
FROM ids
CROSS JOIN off
JOIN scope_item si ON si.lesson_area_id = ids.padre_la
WHERE ids.hijo_la IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM scope_item x WHERE x.lesson_area_id = ids.hijo_la);

SELECT setval('scope_item_scope_item_id_seq', (SELECT max(scope_item_id) FROM scope_item));


-- ─────────────────────────────────────────────────────────────────────────────
-- 17. Las lecciones del padre pasan al hijo (son las de "Unidad de Proyectos
--     puro": las que antes colgaban del nodo Obra_Oficina homónimo). Sus
--     catalog_item_id resuelven igual porque la plantilla es la misma.
-- ─────────────────────────────────────────────────────────────────────────────
WITH padre AS (
    SELECT s.area_scope_id, s.area_item_id
    FROM area_scope s
    JOIN area_item ai ON ai.area_item_id = s.area_item_id
    JOIN area_type at ON at.area_type_id = ai.area_type_id
    WHERE s.state AND ai.state AND at.state
      AND lower(btrim(ai.area_item_name)) = 'unidad de proyectos'
      AND at.area_type_name = 'Área Estándar'
      -- El padre es el que NO cuelga de otro nodo con el mismo area_item; así el
      -- hijo creado por este mismo script nunca se confunde con él al re-ejecutar.
      AND NOT EXISTS (SELECT 1 FROM area_scope pp
                       WHERE pp.area_scope_id = s.area_scope_parent_id
                         AND pp.area_item_id = s.area_item_id)
    ORDER BY s.area_scope_id
    LIMIT 1
),
hijo AS (
    SELECT x.area_scope_id
    FROM area_scope x JOIN padre p ON x.area_scope_parent_id = p.area_scope_id
    WHERE x.area_item_id = p.area_item_id AND x.state
    ORDER BY x.area_scope_id
    LIMIT 1
),
ids AS (
    SELECT (SELECT la.lesson_area_id FROM lesson_area la JOIN padre p ON la.area_scope_id = p.area_scope_id) AS padre_la,
           (SELECT la.lesson_area_id FROM lesson_area la JOIN hijo  h ON la.area_scope_id = h.area_scope_id) AS hijo_la
)
UPDATE lesson l
SET lesson_area_id = ids.hijo_la
FROM ids
WHERE ids.hijo_la IS NOT NULL
  AND l.lesson_area_id = ids.padre_la;


-- ─────────────────────────────────────────────────────────────────────────────
-- 18. El padre deja de ser opción del formulario (ya no es hoja) y queda como
--     contenedor del filtro: detenerse en él sigue agrupando a todas sus subáreas.
-- ─────────────────────────────────────────────────────────────────────────────
WITH padre AS (
    SELECT s.area_scope_id
    FROM area_scope s
    JOIN area_item ai ON ai.area_item_id = s.area_item_id
    JOIN area_type at ON at.area_type_id = ai.area_type_id
    WHERE s.state AND ai.state AND at.state
      AND lower(btrim(ai.area_item_name)) = 'unidad de proyectos'
      AND at.area_type_name = 'Área Estándar'
      -- El padre es el que NO cuelga de otro nodo con el mismo area_item; así el
      -- hijo creado por este mismo script nunca se confunde con él al re-ejecutar.
      AND NOT EXISTS (SELECT 1 FROM area_scope pp
                       WHERE pp.area_scope_id = s.area_scope_parent_id
                         AND pp.area_item_id = s.area_item_id)
    ORDER BY s.area_scope_id
    LIMIT 1
)
UPDATE lesson_area la
SET include_in_form     = false,
    include_descendants = true
FROM padre p
WHERE la.area_scope_id = p.area_scope_id;


-- ─────────────────────────────────────────────────────────────────────────────
-- 19. VERIFICACIÓN FINAL (solo lectura).
--     • nodos_tipo3 debe ser 0
--     • huerfanos_scope_item debe ser 0
--     • lecciones_sin_obra_oficina debe ser 0
--     • debe aparecer "Unidad de Proyectos" con include_in_form = t y ~496 items
-- ─────────────────────────────────────────────────────────────────────────────
SELECT (SELECT count(*) FROM area_scope s
          JOIN area_item ai ON ai.area_item_id = s.area_item_id
         WHERE s.state AND ai.area_type_id = 3)                       AS nodos_tipo3,
       (SELECT count(*) FROM scope_item si
         WHERE si.scope_item_parent_id IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM scope_item p
                            WHERE p.scope_item_id = si.scope_item_parent_id
                              AND p.lesson_area_id = si.lesson_area_id))  AS huerfanos_scope_item,
       (SELECT count(*) FROM lesson WHERE state AND active AND obra_oficina_staff_id IS NULL)
                                                                      AS lecciones_sin_obra_oficina;

SELECT la.lesson_area_id, ai.area_item_name, la.active, la.include_in_form, la.include_descendants,
       (SELECT count(*) FROM scope_item si WHERE si.lesson_area_id = la.lesson_area_id AND si.active) AS items,
       (SELECT count(*) FROM lesson l WHERE l.lesson_area_id = la.lesson_area_id AND l.state)          AS lecciones
FROM lesson_area la
JOIN area_scope s  ON s.area_scope_id = la.area_scope_id
JOIN area_item ai  ON ai.area_item_id = s.area_item_id
WHERE s.state AND la.active
ORDER BY la.lesson_area_id;
