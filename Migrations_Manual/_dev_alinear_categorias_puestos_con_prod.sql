-- ============================================================================
-- SOLO DEV — Alinear los catalogos `categoria` y `puesto` con los datos de prod
-- ============================================================================
-- Los dos catalogos ya eran casi iguales por NOMBRE (317 puestos en comun, 0 con
-- categoria distinta), pero los IDS habian divergido: solo 20 de los 317 puestos
-- compartian id. Eso hace imposible comparar dev contra prod (un puesto_id no
-- significa lo mismo en cada base) y en dev faltaban 18 puestos.
--
-- Que hace:
--   1. `categoria`: los 42 ids ya coinciden id <-> nombre; solo se sincronizan
--      orden / active / state / visible_solicitud_personal.
--   2. `puesto`: se renumeran los ids de dev a los de prod usando el NOMBRE como
--      llave, se insertan los 18 que faltaban y se sincronizan los campos.
--      Los puestos que solo existen en dev se conservan con ids nuevos por encima
--      del maximo de prod.
--   3. Todo lo que apunta a `puesto` (workers, gth_requerimiento,
--      reunion_tema_puesto) se remapea, asi que ninguna ficha cambia de puesto.
--
-- Requiere el CSV de prod:
--   \copy (SELECT puesto_id, nombre, categoria_id, orden, active, state
--          FROM puesto ORDER BY puesto_id) TO 'prod_puesto.csv' WITH (FORMAT csv, HEADER)
--
-- NO correr en produccion: prod es la fuente, no el destino.
-- ============================================================================

-- ── 0. Staging con el catalogo de prod ──────────────────────────────────────
DROP TABLE IF EXISTS _stg_prod_puesto;
CREATE TABLE _stg_prod_puesto (
    puesto_id    integer PRIMARY KEY,
    nombre       text NOT NULL,
    categoria_id integer,
    orden        integer NOT NULL,
    active       boolean NOT NULL,
    state        boolean NOT NULL
);

\copy _stg_prod_puesto FROM 'prod_puesto.csv' WITH (FORMAT csv, HEADER)

BEGIN;

-- ── 1. categoria: sincronizar flags (los ids ya coinciden) ──────────────────
-- MEDICO tenia visible_solicitud_personal = true en dev y false en prod.
UPDATE categoria SET visible_solicitud_personal = false, updated_date_time = now()
WHERE categoria_id = 26 AND visible_solicitud_personal;

-- ── 2. Mapa dev.puesto_id -> id destino, por nombre ─────────────────────────
CREATE TEMP TABLE _mapa_puesto (old_id integer PRIMARY KEY, new_id integer NOT NULL UNIQUE);

INSERT INTO _mapa_puesto (old_id, new_id)
SELECT d.puesto_id, p.puesto_id
FROM puesto d
JOIN _stg_prod_puesto p ON p.nombre = d.nombre;

-- Los que solo existen en dev van despues del maximo de prod.
INSERT INTO _mapa_puesto (old_id, new_id)
SELECT d.puesto_id,
       (SELECT max(puesto_id) FROM _stg_prod_puesto)
         + row_number() OVER (ORDER BY d.puesto_id)
FROM puesto d
WHERE NOT EXISTS (SELECT 1 FROM _stg_prod_puesto p WHERE p.nombre = d.nombre);

-- ── 3. Renumerar. Las FK se sueltan y se vuelven a poner iguales ────────────
ALTER TABLE workers             DROP CONSTRAINT workers_puesto_id_fkey;
ALTER TABLE gth_requerimiento   DROP CONSTRAINT gth_requerimiento_puesto_id_fkey;
ALTER TABLE reunion_tema_puesto DROP CONSTRAINT reunion_tema_puesto_puesto_id_fkey;

-- 3a. A un rango libre, para que el remap no choque con ids ya ocupados.
UPDATE puesto              SET puesto_id = puesto_id + 500000;
UPDATE workers             SET puesto_id = puesto_id + 500000 WHERE puesto_id IS NOT NULL;
UPDATE gth_requerimiento   SET puesto_id = puesto_id + 500000 WHERE puesto_id IS NOT NULL;
UPDATE reunion_tema_puesto SET puesto_id = puesto_id + 500000 WHERE puesto_id IS NOT NULL;

-- 3b. Al id destino.
UPDATE puesto d              SET puesto_id = m.new_id FROM _mapa_puesto m WHERE d.puesto_id = m.old_id + 500000;
UPDATE workers w             SET puesto_id = m.new_id FROM _mapa_puesto m WHERE w.puesto_id = m.old_id + 500000;
UPDATE gth_requerimiento r   SET puesto_id = m.new_id FROM _mapa_puesto m WHERE r.puesto_id = m.old_id + 500000;
UPDATE reunion_tema_puesto t SET puesto_id = m.new_id FROM _mapa_puesto m WHERE t.puesto_id = m.old_id + 500000;

-- ── 4. Insertar los puestos que faltaban y sincronizar los existentes ───────
INSERT INTO puesto (puesto_id, nombre, categoria_id, orden, active, state)
SELECT p.puesto_id, p.nombre, p.categoria_id, p.orden, p.active, p.state
FROM _stg_prod_puesto p
WHERE NOT EXISTS (SELECT 1 FROM puesto d WHERE d.puesto_id = p.puesto_id);

UPDATE puesto d
SET nombre = p.nombre, categoria_id = p.categoria_id, orden = p.orden,
    active = p.active, state = p.state, updated_date_time = now()
FROM _stg_prod_puesto p
WHERE d.puesto_id = p.puesto_id
  AND (d.nombre, d.categoria_id, d.orden, d.active, d.state)
      IS DISTINCT FROM (p.nombre, p.categoria_id, p.orden, p.active, p.state);

-- ── 5. Secuencia e integridad ───────────────────────────────────────────────
SELECT setval('public.puesto_puesto_id_seq', (SELECT max(puesto_id) FROM puesto));

ALTER TABLE workers
    ADD CONSTRAINT workers_puesto_id_fkey
    FOREIGN KEY (puesto_id) REFERENCES puesto (puesto_id);
ALTER TABLE gth_requerimiento
    ADD CONSTRAINT gth_requerimiento_puesto_id_fkey
    FOREIGN KEY (puesto_id) REFERENCES puesto (puesto_id);
ALTER TABLE reunion_tema_puesto
    ADD CONSTRAINT reunion_tema_puesto_puesto_id_fkey
    FOREIGN KEY (puesto_id) REFERENCES puesto (puesto_id);

COMMIT;

DROP TABLE _stg_prod_puesto;

-- ── Verificacion ────────────────────────────────────────────────────────────
SELECT count(*) AS puestos_dev FROM puesto;
SELECT count(*) AS workers_sin_puesto FROM workers WHERE puesto_id IS NULL;
