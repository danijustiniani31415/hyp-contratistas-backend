-- ⚠️ NOTA (2026-08-04): los pasos que usaban TABLAS TEMPORALES (ON COMMIT DROP)
--    solo funcionan si TODO el archivo corre dentro de una sola transacción.
--    Para aplicarlo sentencia por sentencia (sin transacción, para no cargar la
--    RAM de la VPS) usar en su lugar:
--        Migrations_Manual/obra_oficina_resto_sin_transaccion.sql
--    que reemplaza a este archivo desde el paso 5b en adelante.

-- ============================================================================
-- Normalización de workers.obra_oficina  +  eliminación del tipo de área
-- "Área Obra_Oficina" (area_type_id = 3).
--
-- Idea: dejar de deducir si un trabajador es de Obra / Staff (Oficina Técnica) /
-- Oficina Central a partir del ÚLTIMO NODO del árbol area_scope, y pasarlo a una
-- columna dedicada y normalizada de workers (FK a workers_obra_oficina_staff).
--
-- Ejecutar TODO el bloque de una sola vez (es una única transacción).
-- Aplicado en DEV el 2026-08-04.
-- ============================================================================

BEGIN;

-- ---------------------------------------------------------------------------
-- 1. Catálogo workers_obra_oficina_staff
--    IDs fijos y explícitos para que dev y prod coincidan (el backend los usa
--    como constantes en Shared/Constants/ObraOficinaStaffIds.cs).
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS workers_obra_oficina_staff (
    workers_obra_oficina_staff_id serial PRIMARY KEY,
    name                          text    NOT NULL,
    display_order                 integer NOT NULL DEFAULT 0,
    active                        boolean NOT NULL DEFAULT true,
    state                         boolean NOT NULL DEFAULT true
);

-- Un solo registro vivo por nombre (patrón estándar del proyecto).
CREATE UNIQUE INDEX IF NOT EXISTS ux_workers_obra_oficina_staff_name_alive
    ON workers_obra_oficina_staff (lower(btrim(name)))
    WHERE state = true;

INSERT INTO workers_obra_oficina_staff (workers_obra_oficina_staff_id, name, display_order)
VALUES (1, 'Obra', 1),
       (2, 'Staff', 2),
       (3, 'Oficina Central', 3)
ON CONFLICT (workers_obra_oficina_staff_id) DO NOTHING;

SELECT setval('workers_obra_oficina_staff_workers_obra_oficina_staff_id_seq',
              (SELECT max(workers_obra_oficina_staff_id) FROM workers_obra_oficina_staff));

-- ---------------------------------------------------------------------------
-- 2. workers.obra_oficina_staff_id  (FK) + backfill desde el texto plano
--    'Ninguno' (18 filas en prod) se trata como "sin valor" -> NULL.
-- ---------------------------------------------------------------------------
ALTER TABLE workers
    ADD COLUMN IF NOT EXISTS obra_oficina_staff_id integer;

ALTER TABLE workers
    DROP CONSTRAINT IF EXISTS fk_workers_obra_oficina_staff;
ALTER TABLE workers
    ADD CONSTRAINT fk_workers_obra_oficina_staff
    FOREIGN KEY (obra_oficina_staff_id)
    REFERENCES workers_obra_oficina_staff (workers_obra_oficina_staff_id);

CREATE INDEX IF NOT EXISTS ix_workers_obra_oficina_staff_id
    ON workers (obra_oficina_staff_id);

UPDATE workers w
SET obra_oficina_staff_id = c.workers_obra_oficina_staff_id
FROM workers_obra_oficina_staff c
WHERE lower(btrim(w.obra_oficina)) = lower(c.name)
  AND w.obra_oficina_staff_id IS NULL;

-- ---------------------------------------------------------------------------
-- 3. Índice único de correo corporativo: dependía del texto obra_oficina.
--    Se recrea contra la FK antes de borrar la columna.
-- ---------------------------------------------------------------------------
DROP INDEX IF EXISTS ux_workers_email_corporativo_vigente;

CREATE UNIQUE INDEX ux_workers_email_corporativo_vigente
    ON workers (lower(btrim(email_corporativo)))
    WHERE email_corporativo IS NOT NULL
      AND btrim(email_corporativo) <> ''
      AND COALESCE(estado, 'ACTIVO') <> 'RETIRADO'
      AND (lower(btrim(email_corporativo)) LIKE '%@abril.pe'
           OR (contrata_casa = 'Casa' AND obra_oficina_staff_id IN (2, 3)));  -- Staff / Oficina Central

-- ---------------------------------------------------------------------------
-- 4. lesson.obra_oficina_staff_id
--    Antes esta diferenciación vivía en el nodo hoja Obra_Oficina apuntado por
--    lesson.lesson_area_id; ahora es una columna propia de la lección.
-- ---------------------------------------------------------------------------
ALTER TABLE lesson
    ADD COLUMN IF NOT EXISTS obra_oficina_staff_id integer;

ALTER TABLE lesson
    DROP CONSTRAINT IF EXISTS fk_lesson_obra_oficina_staff;
ALTER TABLE lesson
    ADD CONSTRAINT fk_lesson_obra_oficina_staff
    FOREIGN KEY (obra_oficina_staff_id)
    REFERENCES workers_obra_oficina_staff (workers_obra_oficina_staff_id);

CREATE INDEX IF NOT EXISTS ix_lesson_obra_oficina_staff_id
    ON lesson (obra_oficina_staff_id);

-- ---------------------------------------------------------------------------
-- 5. Mapa de migración lesson_area: nodo Obra_Oficina -> nodo PADRE.
--    5a. Crear el lesson_area del padre si todavía no existe.
-- ---------------------------------------------------------------------------
INSERT INTO lesson_area (area_scope_id, active, created_at,
                         include_in_form, include_descendants, include_as_independent)
SELECT DISTINCT p.area_scope_id, true, now(), false, false, false
FROM lesson_area la
JOIN area_scope s  ON s.area_scope_id = la.area_scope_id
JOIN area_item  ai ON ai.area_item_id = s.area_item_id AND ai.area_type_id = 3
JOIN area_scope p  ON p.area_scope_id = s.area_scope_parent_id
WHERE NOT EXISTS (SELECT 1 FROM lesson_area x WHERE x.area_scope_id = p.area_scope_id);

--    5b. Mapa old_lesson_area -> new_lesson_area (+ nombre del nodo, que es lo
--        que determina el valor Obra/Staff/Oficina Central de las lecciones).
CREATE TEMP TABLE la_map ON COMMIT DROP AS
SELECT la.lesson_area_id                          AS old_id,
       pla.lesson_area_id                         AS new_id,
       ai.area_item_name                          AS nodo_nombre,
       la.active                                  AS old_active,
       la.include_in_form                         AS old_include_in_form,
       la.include_as_independent                  AS old_include_as_independent,
       -- 'Oficina Técnica' -> Staff (2). El resto de nodos Obra_Oficina repiten
       -- el nombre del padre (Calidad > Calidad, UdP > UdP, ...) y correspondían
       -- a la sede: Oficina Central (3). Verificado contra el obra_oficina real
       -- de los autores de las 126 lecciones afectadas en prod.
       CASE WHEN lower(ai.area_item_name) LIKE 'oficina t%cnica' THEN 2 ELSE 3 END AS obra_oficina_staff_id
FROM lesson_area la
JOIN area_scope s   ON s.area_scope_id = la.area_scope_id
JOIN area_item  ai  ON ai.area_item_id = s.area_item_id AND ai.area_type_id = 3
JOIN area_scope p   ON p.area_scope_id = s.area_scope_parent_id
JOIN lesson_area pla ON pla.area_scope_id = p.area_scope_id;

-- ---------------------------------------------------------------------------
-- 6. Repuntar las lecciones al lesson_area del padre y fijar su Obra/Oficina.
-- ---------------------------------------------------------------------------
UPDATE lesson l
SET lesson_area_id        = m.new_id,
    obra_oficina_staff_id = m.obra_oficina_staff_id
FROM la_map m
WHERE l.lesson_area_id = m.old_id;

-- ---------------------------------------------------------------------------
-- 7. Relaciones (scope_item) de las áreas Obra_Oficina.
--    Si el padre todavía no tiene plantilla propia, se le traspasa la del hijo
--    (el de menor id cuando hay varios hermanos con el mismo árbol).
-- ---------------------------------------------------------------------------
WITH targets AS (
    SELECT m.new_id, MIN(m.old_id) AS src_old_id
    FROM la_map m
    WHERE EXISTS (SELECT 1 FROM scope_item si  WHERE si.lesson_area_id  = m.old_id AND si.active)
      AND NOT EXISTS (SELECT 1 FROM scope_item si2 WHERE si2.lesson_area_id = m.new_id AND si2.active)
    GROUP BY m.new_id
)
UPDATE scope_item si
SET lesson_area_id = t.new_id
FROM targets t
WHERE si.lesson_area_id = t.src_old_id;

-- El padre hereda la visibilidad que tenía el hijo (activa / en formulario).
UPDATE lesson_area t
SET active          = true,
    include_in_form = true
FROM (SELECT DISTINCT new_id FROM la_map WHERE old_active AND old_include_in_form) x
WHERE t.lesson_area_id = x.new_id;

-- Las relaciones que quedaron colgando del hijo se desactivan.
UPDATE scope_item SET active = false
WHERE lesson_area_id IN (SELECT old_id FROM la_map) AND active;

-- Y el lesson_area del nodo Obra_Oficina deja de existir para la app.
UPDATE lesson_area
SET active                 = false,
    include_in_form        = false,
    include_descendants    = false,
    include_as_independent = false
WHERE lesson_area_id IN (SELECT old_id FROM la_map);

-- ---------------------------------------------------------------------------
-- 8. Backfill del resto de lecciones (las que nunca colgaron de un nodo
--    Obra_Oficina): se toma el valor del trabajador que las creó.
-- ---------------------------------------------------------------------------
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

-- ---------------------------------------------------------------------------
-- 9. workers.area_scope_id: los que apuntaban a un nodo Obra_Oficina pasan a
--    apuntar al nodo padre (o a NULL si ese nodo era raíz).
-- ---------------------------------------------------------------------------
UPDATE workers w
SET area_scope_id = s.area_scope_parent_id
FROM area_scope s
JOIN area_item ai ON ai.area_item_id = s.area_item_id AND ai.area_type_id = 3
WHERE w.area_scope_id = s.area_scope_id;

-- ---------------------------------------------------------------------------
-- 10. Baja lógica del tipo de área "Área Obra_Oficina" y de todo lo suyo.
--     (state = false: nada se borra físicamente, se conserva para auditoría.)
-- ---------------------------------------------------------------------------
UPDATE area_scope s
SET state = false, active = false
FROM area_item ai
WHERE ai.area_item_id = s.area_item_id AND ai.area_type_id = 3 AND s.state;

UPDATE area_item SET state = false, active = false WHERE area_type_id = 3 AND state;

UPDATE area_type SET state = false, active = false WHERE area_type_id = 3 AND state;

-- ---------------------------------------------------------------------------
-- 11. Se retira el texto plano workers.obra_oficina (ya normalizado en el paso 2).
-- ---------------------------------------------------------------------------
ALTER TABLE workers DROP COLUMN IF EXISTS obra_oficina;

COMMIT;
