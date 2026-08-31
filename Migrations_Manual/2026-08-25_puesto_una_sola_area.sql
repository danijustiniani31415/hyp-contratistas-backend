-- ═══════════════════════════════════════════════════════════════════════════
-- Configuración GTH · Un puesto pertenece a UNA sola área
-- ═══════════════════════════════════════════════════════════════════════════
--
-- Hoy la relación puesto ↔ área es N:N (`puesto_area_scope`, migración
-- 2026-08-21_puesto_area_scope.sql). GTH pidió que sea 1:1 — cada puesto vive
-- en un área y nada más. Los puestos que hoy tienen varias áreas no se
-- recortan: se DUPLICAN, uno por área, para no perder ninguna.
--
--   ARQUITECTO DE PROYECTOS  {Arquitectura, Unidad de Proyectos}
--     → ARQUITECTO DE PROYECTOS (Arquitectura)      ← conserva el puesto_id
--     → ARQUITECTO DE PROYECTOS (Unidad de Proyectos) ← puesto_id nuevo
--
-- Qué cambia en el modelo:
--   * El área pasa a ser una COLUMNA de `puesto` (`puesto.area_scope_id`,
--     nullable). Una tabla intermedia para un vínculo 1:1 sobra, y sobre todo
--     no permite el índice que se pide abajo: un UNIQUE no puede cruzar dos
--     tablas. Sigue siendo nullable porque los ~190 puestos de obra nunca
--     tuvieron área y así se quedan («Sin área» en pantalla).
--   * `puesto_area_scope` queda como espejo mientras el código viejo la siga
--     escribiendo, y recién se baja en el PASO 5 (después del deploy).
--
-- Los índices, que es lo que amarra la regla:
--   * FUERA `ix_puesto_nombre_vivo` UNIQUE (nombre) WHERE state — prohibía el
--     nombre repetido en toda la tabla, o sea prohibía justamente las copias.
--   * ENTRA `ux_puesto_nombre_area_vivo` UNIQUE (nombre, area_scope_id)
--     NULLS NOT DISTINCT WHERE state → el mismo nombre puede repetirse en
--     áreas distintas, pero nunca dos veces dentro de la misma área.
--     El `NULLS NOT DISTINCT` (PG 15+) es lo que hace que el bolsón «Sin área»
--     también quede protegido: sin él, Postgres trata cada NULL como distinto
--     y dejaría meter «ALMACENERO» sin área tantas veces como se quiera.
--   * ENTRA `ux_puesto_area_scope_una_area` UNIQUE (puesto_id) WHERE state
--     sobre la intermedia → candado para que el código viejo no pueda volver a
--     asignarle dos áreas a un puesto durante la ventana previa al deploy.
--
-- A quién arrastra el corte:
--   * `workers` → cada trabajador se muda a la copia cuya área coincide con la
--     suya (`workers.area_scope_id`). Los que no coinciden con ninguna, o que
--     no tienen área, se quedan en el original: no hay dato para decidir y
--     adivinar sería peor.
--   * `reunion_tema_puesto` → los temas de reunión del original se replican a
--     cada copia. Sin esto el tema dejaría de cubrir a media plantilla sin que
--     nadie se entere.
--   * `gth_requerimiento` → NO se toca. Su área se deduce del puesto que se
--     pidió; los requerimientos ya cerrados deben seguir mostrando el área con
--     la que se pidieron, que es la que conserva el puesto original.
--
-- ⚠ El código todavía lee y escribe `puesto_area_scope` (CatalogosHabilitacion
--   Repository, ReclutamientoRepository, FftFlujo). Este script deja un trigger
--   (PASO 3) que mantiene la columna sincronizada mientras tanto, así que se
--   puede correr HOY sin esperar al deploy. El PASO 5 va después.
--
-- Correr PASO por PASO en pgAdmin. Cada paso es idempotente.
-- ═══════════════════════════════════════════════════════════════════════════


-- ═══════════════════════════════════════════════════════════════════════════
-- PASO 0 — Diagnóstico. No modifica nada: correrlo y leer la salida.
-- ═══════════════════════════════════════════════════════════════════════════

-- 0a. Los puestos que se van a partir, y en cuántos quedará cada uno.
SELECT p.puesto_id,
       p.nombre,
       count(*)                                                     AS areas_hoy,
       count(*) - 1                                                 AS copias_a_crear,
       string_agg(ai.area_item_name, ' | ' ORDER BY ai.area_item_name) AS areas
FROM puesto p
JOIN puesto_area_scope pas ON pas.puesto_id = p.puesto_id AND pas.state
JOIN area_scope s          ON s.area_scope_id = pas.area_scope_id
JOIN area_item  ai         ON ai.area_item_id = s.area_item_id
WHERE p.state
GROUP BY p.puesto_id, p.nombre
HAVING count(*) > 1
ORDER BY p.nombre;

-- 0b. Vista previa exacta del corte: qué área se queda con el puesto_id
--     original y cuáles se llevan una copia nueva. El criterio es la cantidad
--     de trabajadores que hoy están en cada área — el original se queda con la
--     mayoritaria para mover la menor cantidad de fichas posible; a igual
--     cantidad manda el area_scope_id más bajo.
WITH vinculo AS (
    SELECT pas.puesto_id, pas.area_scope_id
    FROM puesto_area_scope pas
    JOIN puesto p ON p.puesto_id = pas.puesto_id AND p.state
    WHERE pas.state
), ranked AS (
    SELECT v.puesto_id,
           v.area_scope_id,
           (SELECT count(*) FROM workers w
             WHERE w.puesto_id = v.puesto_id
               AND w.area_scope_id = v.area_scope_id)  AS trabajadores_del_area,
           count(*)     OVER (PARTITION BY v.puesto_id) AS areas_hoy,
           row_number() OVER (PARTITION BY v.puesto_id
                              ORDER BY (SELECT count(*) FROM workers w
                                         WHERE w.puesto_id = v.puesto_id
                                           AND w.area_scope_id = v.area_scope_id) DESC,
                                       v.area_scope_id) AS rn
    FROM vinculo v
)
SELECT r.puesto_id, p.nombre, ai.area_item_name AS area,
       r.trabajadores_del_area,
       CASE WHEN r.rn = 1 THEN 'conserva el puesto_id ' || r.puesto_id
            ELSE 'copia nueva' END AS resultado
FROM ranked r
JOIN puesto p     ON p.puesto_id = r.puesto_id
JOIN area_scope s ON s.area_scope_id = r.area_scope_id
JOIN area_item ai ON ai.area_item_id = s.area_item_id
WHERE r.areas_hoy > 1
ORDER BY p.nombre, r.rn;

-- 0c. Trabajadores de esos puestos que NO se van a mover porque su propia área
--     no es ninguna de las del puesto (o no tienen área). Se quedan en el
--     puesto original. Si la lista sorprende, revisar el área de esas fichas
--     ANTES de correr el PASO 2 — después ya no hay dato para decidir.
SELECT p.nombre                                   AS puesto,
       coalesce(ai.area_item_name, '— sin área —') AS area_de_la_ficha,
       count(*)                                   AS fichas
FROM workers w
JOIN puesto p ON p.puesto_id = w.puesto_id AND p.state
LEFT JOIN area_scope s ON s.area_scope_id = w.area_scope_id
LEFT JOIN area_item ai ON ai.area_item_id = s.area_item_id
WHERE p.puesto_id IN (SELECT puesto_id FROM puesto_area_scope
                       WHERE state GROUP BY puesto_id HAVING count(*) > 1)
  AND NOT EXISTS (SELECT 1 FROM puesto_area_scope pas
                   WHERE pas.puesto_id = w.puesto_id AND pas.state
                     AND pas.area_scope_id = w.area_scope_id)
GROUP BY p.nombre, ai.area_item_name
ORDER BY p.nombre, fichas DESC;

-- 0d. Control: el índice que se va a botar tiene que existir y ser el que dice
--     acá. Si ya no está, el PASO 2 se corrió antes.
SELECT indexname, indexdef FROM pg_indexes
WHERE schemaname = 'public' AND tablename IN ('puesto', 'puesto_area_scope')
ORDER BY tablename, indexname;


-- ═══════════════════════════════════════════════════════════════════════════
-- PASO 1 — `puesto.area_scope_id`: el área deja de estar en una tabla aparte.
-- ═══════════════════════════════════════════════════════════════════════════
-- Nullable a propósito: el puesto sin área es un caso válido, no un pendiente.
-- Acá solo se rellenan los puestos que YA tienen una sola área; los multi-área
-- los resuelve el PASO 2.

BEGIN;

ALTER TABLE puesto ADD COLUMN IF NOT EXISTS area_scope_id integer;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint
                    WHERE conname = 'puesto_area_scope_id_fkey') THEN
        ALTER TABLE puesto
            ADD CONSTRAINT puesto_area_scope_id_fkey
            FOREIGN KEY (area_scope_id) REFERENCES area_scope(area_scope_id);
    END IF;
END $$;

COMMENT ON COLUMN puesto.area_scope_id IS
    'Área a la que pertenece el puesto. NULL = sin área (los puestos de obra nunca la tuvieron). Reemplaza a la tabla puesto_area_scope: desde 2026-08-25 un puesto pertenece a un área y solo una.';

UPDATE puesto p
   SET area_scope_id = u.area_scope_id
  FROM (SELECT puesto_id, min(area_scope_id) AS area_scope_id
          FROM puesto_area_scope
         WHERE state
         GROUP BY puesto_id
        HAVING count(*) = 1) u
 WHERE u.puesto_id = p.puesto_id
   AND p.area_scope_id IS DISTINCT FROM u.area_scope_id;

COMMIT;


-- ═══════════════════════════════════════════════════════════════════════════
-- PASO 2 — El corte: duplicar los multi-área y cambiar los índices.
-- ═══════════════════════════════════════════════════════════════════════════
-- Todo en una sola transacción y en este orden obligado: el índice viejo
-- prohíbe los nombres repetidos, así que sale ANTES de crear las copias; el
-- nuevo entra DESPUÉS, cuando ya no hay nada que viole la regla. Si algo
-- revienta a mitad de camino, el ROLLBACK deja la tabla con su índice viejo
-- intacto — nunca queda sin candado.

BEGIN;

-- 2a. Fuera el índice que prohibía el nombre repetido en toda la tabla.
DROP INDEX IF EXISTS ix_puesto_nombre_vivo;

-- 2b. Una copia del puesto por cada área extra, y arrastre de lo que colgaba.
DO $$
DECLARE
    r          record;
    v_nuevo    integer;
    v_n        integer;
    v_copias   integer := 0;
    v_fichas   integer := 0;
    v_temas    integer := 0;
BEGIN
    -- El reparto se calcula UNA vez y se congela: dentro del bucle se insertan
    -- filas en puesto_area_scope y no puede afectar al propio criterio.
    CREATE TEMP TABLE tmp_split ON COMMIT DROP AS
    WITH vinculo AS (
        SELECT pas.puesto_id, pas.area_scope_id
        FROM puesto_area_scope pas
        JOIN puesto p ON p.puesto_id = pas.puesto_id AND p.state
        WHERE pas.state
    ), ranked AS (
        SELECT v.puesto_id,
               v.area_scope_id,
               count(*)     OVER (PARTITION BY v.puesto_id) AS areas_hoy,
               row_number() OVER (PARTITION BY v.puesto_id
                                  ORDER BY (SELECT count(*) FROM workers w
                                             WHERE w.puesto_id = v.puesto_id
                                               AND w.area_scope_id = v.area_scope_id) DESC,
                                           v.area_scope_id) AS rn
        FROM vinculo v
    )
    SELECT puesto_id AS original_id, area_scope_id
    FROM ranked
    WHERE areas_hoy > 1 AND rn > 1;   -- rn = 1 es el área que conserva el original

    FOR r IN SELECT * FROM tmp_split ORDER BY original_id, area_scope_id
    LOOP
        -- Copia del puesto, ya apuntando a su única área. Se clonan categoría,
        -- orden y active: es el mismo cargo, solo que de otra área.
        INSERT INTO puesto (nombre, categoria_id, orden, active, state,
                            area_scope_id, created_date_time)
        SELECT p.nombre, p.categoria_id, p.orden, p.active, true,
               r.area_scope_id, now()
          FROM puesto p
         WHERE p.puesto_id = r.original_id
        RETURNING puesto_id INTO v_nuevo;
        v_copias := v_copias + 1;

        -- El vínculo con esa área se muda del original a la copia. Espejo para
        -- el código que todavía lee la intermedia; el PASO 5 la baja.
        INSERT INTO puesto_area_scope (puesto_id, area_scope_id, created_date_time)
        VALUES (v_nuevo, r.area_scope_id, now());

        UPDATE puesto_area_scope
           SET state = false, updated_date_time = now()
         WHERE puesto_id = r.original_id
           AND area_scope_id = r.area_scope_id
           AND state;

        -- Trabajadores cuya propia área es la de la copia: se mudan a ella.
        -- Los demás no se tocan (ver PASO 0c).
        UPDATE workers
           SET puesto_id = v_nuevo, updated_at = now()
         WHERE puesto_id = r.original_id
           AND area_scope_id = r.area_scope_id;
        GET DIAGNOSTICS v_n = ROW_COUNT;
        v_fichas := v_fichas + v_n;

        -- Los temas de reunión que apuntaban al puesto siguen aplicando a la
        -- copia: es el mismo cargo partido, no un cargo nuevo.
        INSERT INTO reunion_tema_puesto (reunion_tema_id, puesto_id,
                                         created_date_time, created_user_id,
                                         active, state, reunion_tema_regla_id)
        SELECT rtp.reunion_tema_id, v_nuevo, now(), rtp.created_user_id,
               rtp.active, true, rtp.reunion_tema_regla_id
          FROM reunion_tema_puesto rtp
         WHERE rtp.puesto_id = r.original_id AND rtp.state;
        GET DIAGNOSTICS v_n = ROW_COUNT;
        v_temas := v_temas + v_n;
    END LOOP;

    RAISE NOTICE 'Copias creadas: %  ·  fichas mudadas: %  ·  temas replicados: %',
                 v_copias, v_fichas, v_temas;
END $$;

-- 2c. Ya no quedan multi-área: la columna se rellena para todos.
UPDATE puesto p
   SET area_scope_id = pas.area_scope_id
  FROM puesto_area_scope pas
 WHERE pas.puesto_id = p.puesto_id
   AND pas.state
   AND p.area_scope_id IS DISTINCT FROM pas.area_scope_id;

-- 2d. Los índices nuevos. Si acá salta un error de duplicado es que quedó un
--     nombre repetido dentro de una misma área: el COMMIT no ocurre y no se
--     pierde nada.
CREATE UNIQUE INDEX IF NOT EXISTS ux_puesto_nombre_area_vivo
    ON puesto (nombre, area_scope_id) NULLS NOT DISTINCT WHERE state;

CREATE INDEX IF NOT EXISTS ix_puesto_area ON puesto (area_scope_id) WHERE state;

-- Candado sobre la intermedia: el código viejo ya no puede meterle una segunda
-- área a un puesto (falla el guardado en vez de romper la regla en silencio).
CREATE UNIQUE INDEX IF NOT EXISTS ux_puesto_area_scope_una_area
    ON puesto_area_scope (puesto_id) WHERE state;

COMMIT;


-- ═══════════════════════════════════════════════════════════════════════════
-- PASO 3 — Puente temporal, solo mientras el código siga escribiendo la
--          intermedia. Se borra en el PASO 5.
-- ═══════════════════════════════════════════════════════════════════════════
-- Sin esto, cada puesto que GTH cree o edite desde Configuración entre este
-- script y el deploy grabaría el área SOLO en `puesto_area_scope` y dejaría
-- `puesto.area_scope_id` en NULL: al día siguiente esos puestos aparecerían
-- «Sin área» y nadie ataría el cabo. El trigger refleja la intermedia en la
-- columna, así que las dos dicen lo mismo pase lo que pase.

BEGIN;

CREATE OR REPLACE FUNCTION puesto_area_scope_sync() RETURNS trigger AS $$
DECLARE
    v_ids integer[];
BEGIN
    -- Con IF y no con un CASE dentro del UPDATE: en un DELETE el registro NEW
    -- no existe y plpgsql revienta con solo nombrarlo, aunque la rama no corra.
    IF    TG_OP = 'INSERT' THEN v_ids := ARRAY[NEW.puesto_id];
    ELSIF TG_OP = 'DELETE' THEN v_ids := ARRAY[OLD.puesto_id];
    ELSE  v_ids := ARRAY[OLD.puesto_id, NEW.puesto_id];  -- el UPDATE puede mover el vínculo de puesto
    END IF;

    UPDATE puesto p
       SET area_scope_id = (SELECT pas.area_scope_id
                              FROM puesto_area_scope pas
                             WHERE pas.puesto_id = p.puesto_id AND pas.state
                             LIMIT 1)
     WHERE p.puesto_id = ANY(v_ids);
    RETURN NULL;
END $$ LANGUAGE plpgsql;

COMMENT ON FUNCTION puesto_area_scope_sync() IS
    'TEMPORAL (2026-08-25): refleja puesto_area_scope en puesto.area_scope_id mientras el backend siga escribiendo la tabla intermedia. Se elimina junto con puesto_area_scope.';

DROP TRIGGER IF EXISTS trg_puesto_area_scope_sync ON puesto_area_scope;
CREATE TRIGGER trg_puesto_area_scope_sync
    AFTER INSERT OR UPDATE OR DELETE ON puesto_area_scope
    FOR EACH ROW EXECUTE FUNCTION puesto_area_scope_sync();

COMMIT;


-- ═══════════════════════════════════════════════════════════════════════════
-- PASO 4 — Verificación. Las cuatro consultas tienen que salir vacías o en 0.
-- ═══════════════════════════════════════════════════════════════════════════

-- 4a. Ningún puesto con más de un área. Tiene que salir 0 filas.
SELECT puesto_id, count(*) FROM puesto_area_scope
WHERE state GROUP BY puesto_id HAVING count(*) > 1;

-- 4b. Columna e intermedia dicen lo mismo. Tiene que salir 0 filas.
SELECT p.puesto_id, p.nombre, p.area_scope_id, pas.area_scope_id AS en_la_intermedia
FROM puesto p
LEFT JOIN puesto_area_scope pas ON pas.puesto_id = p.puesto_id AND pas.state
WHERE p.state AND p.area_scope_id IS DISTINCT FROM pas.area_scope_id;

-- 4c. Nombre repetido dentro de una misma área. Tiene que salir 0 filas
--     (el índice ya no lo permitiría, es un control de que quedó puesto).
SELECT nombre, area_scope_id, count(*)
FROM puesto WHERE state
GROUP BY nombre, area_scope_id HAVING count(*) > 1;

-- 4d. El resultado del corte, para pasárselo a GTH.
SELECT p.nombre,
       coalesce(ai.area_item_name, '— sin área —') AS area,
       (SELECT count(*) FROM workers w WHERE w.puesto_id = p.puesto_id) AS trabajadores
FROM puesto p
LEFT JOIN area_scope s ON s.area_scope_id = p.area_scope_id
LEFT JOIN area_item ai ON ai.area_item_id = s.area_item_id
WHERE p.state
  AND p.nombre IN (SELECT nombre FROM puesto WHERE state
                    GROUP BY nombre HAVING count(*) > 1)
ORDER BY p.nombre, area;


-- ═══════════════════════════════════════════════════════════════════════════
-- PASO 5 — SOLO DESPUÉS DE DESPLEGAR EL BACKEND Y EL FRONT.
-- ═══════════════════════════════════════════════════════════════════════════
-- No correr antes: mientras el código siga leyendo `puesto_area_scope`, botarla
-- tumba Solicitud de Personal y Configuración → Puestos. Correrlo cuando el
-- código ya lea `puesto.area_scope_id`.
--
-- Los datos no se pierden: el vínculo vivo de cada puesto quedó copiado en
-- `puesto.area_scope_id` (verificado en 4b) y las copias creadas en el PASO 2
-- conservan cada área que existía. Lo único que desaparece son los vínculos ya
-- dados de baja, que son el rastro de un modelo que deja de existir.
--
-- BEGIN;
-- DROP TRIGGER IF EXISTS trg_puesto_area_scope_sync ON puesto_area_scope;
-- DROP FUNCTION IF EXISTS puesto_area_scope_sync();
-- DROP TABLE IF EXISTS puesto_area_scope;
-- COMMIT;
