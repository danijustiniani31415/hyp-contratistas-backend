-- ============================================================================
-- workers.fecha_ingreso / fecha_retiro se bajan: pasan a workers_periodo_laboral
-- ============================================================================
-- El problema: las dos fechas eran columnas sueltas de la ficha, o sea que solo
-- podian guardar UN paso por Abril. Cuando alguien reingresaba habia que elegir
-- entre pisar las fechas viejas (perder el paso anterior) o abrir otra fila en
-- `workers` -- y se hacia lo segundo. Eso parte en dos el historial (EMOs,
-- inducciones, amonestaciones, vinculaciones) de una misma persona: en prod hay
-- 118 personas con mas de una ficha, 122 fichas de mas (verificado el 2026-08-25).
--
-- Modelo que queda:
--   workers                  -> UNA ficha por persona.
--   workers_periodo_laboral  -> un paso por Abril: ingreso -> retiro. El vigente
--                               es el que tiene fecha_retiro NULL, y hay como
--                               maximo uno por ficha.
--   worker_vinculaciones     -> NO se toca. Es mas fina: a que razon social y a
--                               que proyecto esta vinculado. Un mismo periodo
--                               laboral puede tener varias vinculaciones (mover a
--                               alguien de Carpi a Salerno no lo saca del grupo);
--                               en prod hay fichas con 39 vinculaciones y un solo
--                               ingreso.
--
-- "La" fecha de ingreso de una ficha es la del ULTIMO periodo, ordenando por
-- fecha_ingreso DESC, id DESC. Ese orden esta escrito igual en el backend
-- (Infrastructure/Models/WorkersPeriodoLaboral.cs y
-- Shared/Constants/WorkersPeriodoLaboralSql.cs).
--
-- OJO con el orden de despliegue:
--   PASOS 0-4  -> ANTES de desplegar el backend nuevo (crean y llenan la tabla;
--                 el backend viejo sigue funcionando porque las columnas siguen).
--   Desplegar el backend.
--   PASOS 5-6  -> DESPUES del deploy (bajan las columnas; el backend viejo las lee).
--   PASO 7     -> opcional, cuando GTH decida que hacer con los datos torcidos.
--
-- Correr PASO por PASO en pgAdmin. Cada paso es idempotente.
-- ============================================================================


-- ============================================================================
-- PASO 0 - Diagnostico. No modifica nada: correrlo y leer la salida.
-- ============================================================================

-- 0a. Estado de las dos columnas. En prod al 2026-08-25: 3449 fichas, 13 sin
--     fecha de ingreso, 873 con retiro, 4 con retiro pero sin ingreso y 25 con
--     el retiro ANTERIOR al ingreso.
SELECT count(*)                                                                   AS fichas,
       count(*) FILTER (WHERE fecha_ingreso IS NULL)                              AS sin_ingreso,
       count(*) FILTER (WHERE fecha_retiro IS NOT NULL)                           AS con_retiro,
       count(*) FILTER (WHERE fecha_retiro IS NOT NULL AND fecha_ingreso IS NULL) AS retiro_sin_ingreso,
       count(*) FILTER (WHERE fecha_retiro IS NOT NULL
                          AND fecha_retiro < fecha_ingreso)                       AS retiro_antes_del_ingreso
FROM workers;

-- 0b. Las fichas con el retiro ANTERIOR al ingreso. No es un error de captura del
--     retiro: en las 25 de prod la fecha_ingreso es una fecha futura o de relleno
--     (una llega a 2026-12-31) que nadie corrigio, y la vinculacion si cuadra con
--     el retiro. Se migran TAL CUAL para no perder el dato original; el CHECK del
--     PASO 3 entra NOT VALID justamente por ellas y el PASO 7 las arregla.
SELECT w.id AS worker_id, p.full_name, w.fecha_ingreso, w.fecha_retiro,
       (SELECT min(v.fecha_inicio) FROM worker_vinculaciones v WHERE v.worker_id = w.id) AS primera_vinculacion
FROM workers w
LEFT JOIN person p ON p.person_id = w.person_id
WHERE w.fecha_retiro IS NOT NULL AND w.fecha_retiro < w.fecha_ingreso
ORDER BY (w.fecha_ingreso - w.fecha_retiro) DESC;

-- 0c. Las fichas sin fecha de ingreso que SI llegaron a ingresar: el PASO 2 les
--     deduce el ingreso (primera vinculacion, y si tampoco hay, la fecha en que se
--     creo la ficha). Las de pre-ingreso (finalista aprobado / no ingreso) no
--     salen aca a proposito: esas no llevan ningun periodo. (El PASO 2 usa la
--     misma columna workers_estado.llego_a_ingresar que este SELECT.)
SELECT w.id AS worker_id, p.full_name, we.nombre AS estado, w.fecha_retiro,
       (SELECT min(v.fecha_inicio) FROM worker_vinculaciones v WHERE v.worker_id = w.id) AS primera_vinculacion,
       w.created_at::date AS ficha_creada
FROM workers w
JOIN workers_estado we ON we.workers_estado_id = w.workers_estado_id
LEFT JOIN person p ON p.person_id = w.person_id
WHERE w.fecha_ingreso IS NULL AND we.llego_a_ingresar
ORDER BY w.id;

-- 0d. El problema de fondo: personas con mas de una ficha en `workers`. La tabla
--     nueva evita que sigan apareciendo, pero NO junta las que ya existen -- eso
--     es una migracion aparte (hay que repuntar todas las FK a workers.id).
SELECT n_fichas, count(*) AS personas
FROM (SELECT person_id, count(*) AS n_fichas FROM workers WHERE person_id IS NOT NULL GROUP BY person_id) t
WHERE n_fichas > 1
GROUP BY n_fichas ORDER BY n_fichas;


-- ============================================================================
-- PASO 1 - Crear workers_periodo_laboral.
--
-- Sobre los indices:
--   ux_..._abierto  un solo periodo abierto por ficha. Es LA invariante de la
--                   tabla: nadie puede estar adentro dos veces a la vez.
--   ux_..._ingreso  no dos periodos con la misma fecha de ingreso en una ficha.
--   Los dos filtran por `state` para que el soft delete pueda dejar varias filas
--   dadas de baja y una sola viva, como en el resto del esquema.
--
-- El CHECK de fecha_retiro >= fecha_ingreso NO se crea aca sino en el PASO 3,
-- despues de migrar: NOT VALID solo se salta las filas que YA estaban, no las
-- que inserta el PASO 2, asi que puesto aca las 25 fichas del PASO 0b harian
-- fallar la migracion entera.
-- ============================================================================
BEGIN;

CREATE TABLE IF NOT EXISTS workers_periodo_laboral (
    workers_periodo_laboral_id serial       PRIMARY KEY,
    worker_id                  integer      NOT NULL REFERENCES workers(id),
    fecha_ingreso              date         NOT NULL,
    fecha_retiro               date         NULL,
    created_date_time          timestamptz  NOT NULL DEFAULT now(),
    created_user_id            integer      NULL,
    updated_date_time          timestamptz  NULL,
    updated_user_id            integer      NULL,
    active                     boolean      NOT NULL DEFAULT true,
    state                      boolean      NOT NULL DEFAULT true
);

COMMENT ON TABLE workers_periodo_laboral IS
    'Un paso del trabajador por Abril (ingreso -> retiro). Reemplaza a workers.fecha_ingreso / workers.fecha_retiro, que al ser columnas de la ficha solo podian guardar uno y obligaban a abrir otra ficha en cada reingreso. No confundir con worker_vinculaciones, que es la razon social / proyecto dentro de un mismo periodo.';
COMMENT ON COLUMN workers_periodo_laboral.fecha_retiro IS
    'NULL = periodo vigente (el trabajador sigue adentro).';

CREATE INDEX IF NOT EXISTS ix_workers_periodo_laboral_worker_id
    ON workers_periodo_laboral (worker_id);

CREATE UNIQUE INDEX IF NOT EXISTS ux_workers_periodo_laboral_abierto
    ON workers_periodo_laboral (worker_id)
    WHERE fecha_retiro IS NULL AND state;

CREATE UNIQUE INDEX IF NOT EXISTS ux_workers_periodo_laboral_ingreso
    ON workers_periodo_laboral (worker_id, fecha_ingreso)
    WHERE state;

COMMIT;


-- ============================================================================
-- PASO 2 - Migrar las fechas que hoy estan en `workers`.
--
-- Lleva un periodo cada ficha que llego a ingresar (estado ACTIVO, RETIRADO o
-- INHABILITADO_SSOMA, o sea workers_estado.llego_a_ingresar) y tambien cualquier
-- ficha que tenga alguna de las dos fechas, aunque su estado diga otra cosa.
--
-- Las de pre-ingreso sin fechas (finalistas aprobados) quedan SIN periodo a
-- proposito: no ingresaron, y "sin periodo" es exactamente como el backend lee
-- hoy la columna en NULL.
--
-- La fecha de ingreso sale de la columna; si esta vacia se cae a la primera
-- vinculacion y, si tampoco hay, a la fecha en que se creo la ficha. La fecha de
-- retiro se copia tal cual, sin tocar (incluidas las 25 del PASO 0b).
--
-- Idempotente: no inserta si la ficha ya tiene algun periodo.
-- ============================================================================
BEGIN;

INSERT INTO workers_periodo_laboral (worker_id, fecha_ingreso, fecha_retiro, created_date_time)
SELECT w.id,
       COALESCE(
           w.fecha_ingreso,
           (SELECT min(v.fecha_inicio) FROM worker_vinculaciones v WHERE v.worker_id = w.id),
           w.created_at::date,
           w.fecha_retiro),
       w.fecha_retiro,
       COALESCE(w.created_at, now())
FROM workers w
JOIN workers_estado we ON we.workers_estado_id = w.workers_estado_id
WHERE (we.llego_a_ingresar OR w.fecha_ingreso IS NOT NULL OR w.fecha_retiro IS NOT NULL)
  AND NOT EXISTS (SELECT 1 FROM workers_periodo_laboral pl WHERE pl.worker_id = w.id);

COMMIT;


-- ============================================================================
-- PASO 3 - El CHECK de rango, ya con la data adentro.
--
-- Entra NOT VALID porque las 25 filas del PASO 0b lo violan y no se van a
-- inventar fechas para ellas. NOT VALID no es "apagado": todo INSERT/UPDATE de
-- ahora en adelante SI se valida; lo unico que se salta es la revision de lo que
-- acaba de migrar. El PASO 7 lo deja VALID.
-- ============================================================================
BEGIN;

ALTER TABLE workers_periodo_laboral
    DROP CONSTRAINT IF EXISTS ck_workers_periodo_laboral_rango;
ALTER TABLE workers_periodo_laboral
    ADD CONSTRAINT ck_workers_periodo_laboral_rango
    CHECK (fecha_retiro IS NULL OR fecha_retiro >= fecha_ingreso) NOT VALID;

COMMIT;


-- ============================================================================
-- PASO 4 - Verificacion de la migracion. Correr ANTES de desplegar el backend.
-- ============================================================================

-- 4a. Cuadre de totales. `fichas_con_periodo` tiene que ser igual a
--     `deberian_tener_periodo`, y `periodos` igual que `fichas_con_periodo`
--     (a esta altura hay exactamente un periodo por ficha).
SELECT (SELECT count(*) FROM workers_periodo_laboral)                       AS periodos,
       (SELECT count(DISTINCT worker_id) FROM workers_periodo_laboral)      AS fichas_con_periodo,
       (SELECT count(*) FROM workers w
          JOIN workers_estado we ON we.workers_estado_id = w.workers_estado_id
         WHERE we.llego_a_ingresar OR w.fecha_ingreso IS NOT NULL
            OR w.fecha_retiro IS NOT NULL)                                  AS deberian_tener_periodo;

-- 4b. Ninguna fecha se desvio de la que estaba en la ficha. Tiene que dar 0 filas.
--     (fecha_ingreso solo puede diferir donde la columna estaba en NULL.)
SELECT w.id AS worker_id, w.fecha_ingreso AS ficha_ingreso, pl.fecha_ingreso AS periodo_ingreso,
       w.fecha_retiro AS ficha_retiro,  pl.fecha_retiro AS periodo_retiro
FROM workers w
JOIN workers_periodo_laboral pl ON pl.worker_id = w.id
WHERE (w.fecha_ingreso IS NOT NULL AND pl.fecha_ingreso IS DISTINCT FROM w.fecha_ingreso)
   OR (pl.fecha_retiro IS DISTINCT FROM w.fecha_retiro);

-- 4c. Las fichas que quedaron sin periodo. En prod tienen que ser SOLO las de
--     pre-ingreso (3 finalistas aprobados al 2026-08-25).
SELECT we.nombre AS estado, count(*) AS fichas_sin_periodo
FROM workers w
JOIN workers_estado we ON we.workers_estado_id = w.workers_estado_id
WHERE NOT EXISTS (SELECT 1 FROM workers_periodo_laboral pl WHERE pl.worker_id = w.id)
GROUP BY we.nombre ORDER BY 2 DESC;

-- 4d. Fichas donde el estado y el periodo no dicen lo mismo. En prod da 15
--     (11 activos con fecha de retiro + 4 retirados sin ella) y NO es una
--     regresion: son las mismas incoherencias que ya tenian las columnas. Se
--     listan para que GTH las corrija desde la pantalla de trabajadores.
SELECT w.id AS worker_id, p.full_name, we.nombre AS estado,
       pl.fecha_ingreso, pl.fecha_retiro
FROM workers w
JOIN workers_estado we ON we.workers_estado_id = w.workers_estado_id
JOIN workers_periodo_laboral pl ON pl.worker_id = w.id
LEFT JOIN person p ON p.person_id = w.person_id
WHERE (we.esta_adentro AND pl.fecha_retiro IS NOT NULL)
   OR (NOT we.esta_adentro AND we.llego_a_ingresar AND pl.fecha_retiro IS NULL)
ORDER BY we.nombre, w.id;


-- ============================================================================
-- PASO 5 - Bajar las dos columnas de `workers`.
--
-- Se borran, no se congelan: la copia integra vive en workers_periodo_laboral
-- (PASO 4b lo verifica fila por fila) y una columna congelada seria peor -- el
-- codigo compila, corre y devuelve vacio sin que nadie se entere.
--
-- Correr DESPUES de desplegar el backend nuevo: el viejo las lee.
-- ============================================================================
BEGIN;

ALTER TABLE workers DROP COLUMN IF EXISTS fecha_ingreso;
ALTER TABLE workers DROP COLUMN IF EXISTS fecha_retiro;

COMMIT;


-- ============================================================================
-- PASO 6 - Verificacion final.
-- ============================================================================

-- 6a. Las columnas ya no existen. Tiene que dar 0.
SELECT count(*) AS debe_ser_0
FROM information_schema.columns
WHERE table_name = 'workers' AND column_name IN ('fecha_ingreso', 'fecha_retiro');

-- 6b. Un solo periodo abierto por ficha. Tiene que dar 0 filas.
SELECT worker_id, count(*) AS periodos_abiertos
FROM workers_periodo_laboral
WHERE fecha_retiro IS NULL AND state
GROUP BY worker_id HAVING count(*) > 1;

-- 6c. Como lo ve la aplicacion de ahora en adelante.
SELECT we.nombre AS estado,
       count(*)                                              AS fichas,
       count(pl.workers_periodo_laboral_id)                  AS con_periodo,
       count(*) FILTER (WHERE pl.fecha_retiro IS NULL
                          AND pl.workers_periodo_laboral_id IS NOT NULL) AS periodo_vigente
FROM workers w
JOIN workers_estado we ON we.workers_estado_id = w.workers_estado_id
LEFT JOIN LATERAL (
    SELECT pl.workers_periodo_laboral_id, pl.fecha_retiro
    FROM workers_periodo_laboral pl
    WHERE pl.worker_id = w.id AND pl.state
    ORDER BY pl.fecha_ingreso DESC, pl.workers_periodo_laboral_id DESC
    LIMIT 1
) pl ON true
GROUP BY we.nombre ORDER BY fichas DESC;


-- ============================================================================
-- PASO 7 - OPCIONAL, y solo cuando GTH lo confirme: enderezar las 25 fichas del
-- PASO 0b y dejar el CHECK validado.
--
-- No va junto con la migracion a proposito: cambia datos que hoy existen tal
-- cual, y la lectura de que el ingreso "bueno" es el de la primera vinculacion
-- es una interpretacion, no un dato duro. Correrlo despues de revisar la lista
-- del PASO 0b con GTH.
--
-- Mientras no se corra, esas 25 filas siguen ahi y el CHECK sigue NOT VALID: lo
-- unico que no se puede hacer es agregar filas nuevas torcidas.
-- ============================================================================
-- BEGIN;
--
-- UPDATE workers_periodo_laboral pl
-- SET fecha_ingreso = v.primera,
--     updated_date_time = now()
-- FROM (SELECT worker_id, min(fecha_inicio) AS primera
--       FROM worker_vinculaciones GROUP BY worker_id) v
-- WHERE v.worker_id = pl.worker_id
--   AND pl.fecha_retiro IS NOT NULL
--   AND pl.fecha_retiro < pl.fecha_ingreso
--   AND v.primera <= pl.fecha_retiro;
--
-- -- Las que no tengan vinculacion utilizable quedan como un periodo de un dia.
-- UPDATE workers_periodo_laboral
-- SET fecha_ingreso = fecha_retiro, updated_date_time = now()
-- WHERE fecha_retiro IS NOT NULL AND fecha_retiro < fecha_ingreso;
--
-- ALTER TABLE workers_periodo_laboral VALIDATE CONSTRAINT ck_workers_periodo_laboral_rango;
--
-- COMMIT;
