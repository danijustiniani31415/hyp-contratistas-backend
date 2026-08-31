-- ============================================================================
-- Fichas duplicadas de `workers`: una sola queda viva, las demas se dan de baja
-- ============================================================================
-- Continua a 2026-08-25_workers_periodo_laboral.sql, que dejo el modelo nuevo
-- (una fila por paso por Abril en workers_periodo_laboral) pero AVISABA en su
-- PASO 0d que no juntaba las fichas que ya existian. Esto es eso.
--
-- El problema: mientras las fechas de ingreso y retiro fueron dos columnas de la
-- ficha, un reingreso obligaba a abrir OTRA fila en `workers` para la misma
-- persona. En prod hay 119 personas con mas de una ficha y 127 fichas de mas
-- (4 personas llegan a tener tres); en dev, 72 personas y 73 fichas. Verificado
-- contra las dos bases el 2026-08-25.
--
-- Lo que hace este script:
--   1. `workers` gana la columna `state` (soft delete), que no tenia.
--   2. Por cada persona con varias fichas se elige UNA viva y las demas quedan
--      con state = false. Que ficha vive: la que esta adentro; si ninguna o
--      varias, la que llego a ingresar; a igualdad, la de mayor id (la ultima
--      creada, que es la que tiene el area, el puesto y la empresa de hoy).
--   3. Los periodos laborales de las fichas dadas de baja se MUEVEN a la ficha
--      viva. Es lo unico que se mueve: asi el reingreso queda como dos periodos
--      de una misma ficha, que es exactamente el modelo nuevo, y ninguna fecha
--      de ingreso o salida se pierde al ocultar la ficha vieja.
--   4. Queda `workers_ficha_fusionada` diciendo que ficha se fusiono con cual.
--
-- Lo que NO hace, a proposito: NO repunta las 56 llaves foraneas que apuntan a
-- `workers` (EMOs, inducciones, amonestaciones, habilitacion, vinculaciones,
-- charlas, tareo...). Todo eso se queda colgando de la ficha dada de baja como
-- dato historico. Repuntarlo significaria decidir, por ejemplo, cual de los dos
-- documentos de habilitacion del mismo item es el bueno -- una decision de SSOMA,
-- no de una migracion, y en prod son cientos de filas.
--
-- ORDEN DE DESPLIEGUE -- el PASO 1 va SI O SI ANTES de desplegar el backend:
--
--   1. 2026-08-25_workers_periodo_laboral.sql, PASOS 0-4.
--   2. Este script, PASO 1 (agrega la columna `state`).
--   3. Desplegar el backend.
--   4. 2026-08-25_workers_periodo_laboral.sql, PASOS 5-6 (baja las dos columnas).
--   5. Este script, PASOS 0 y 2-5 (la fusion propiamente dicha).
--
-- El PASO 1 no puede ir despues del deploy: el backend nuevo filtra `workers` con
-- un query filter global de EF sobre `state`, asi que si la columna no existe
-- TODA consulta de trabajadores sale con 42703 y se cae la aplicacion entera, no
-- solo esta pantalla. Al reves no pasa nada: la columna existiendo, el backend
-- viejo simplemente la ignora.
--
-- Y al reves de eso, la fusion (PASOS 2-5) conviene que vaya DESPUES del deploy:
-- hasta que el filtro este arriba las fichas dadas de baja se siguen viendo igual
-- que antes, y el backend viejo todavia escribe en `workers.fecha_retiro`, que ya
-- no seria la fuente de verdad.
--
-- Correr PASO por PASO en pgAdmin. Cada paso es idempotente.
-- ============================================================================


-- ============================================================================
-- PASO 0 - Diagnostico. No modifica nada: correrlo y leer la salida.
--
-- Cuenta solo fichas VIVAS, para que volver a correrlo despues de la fusion diga
-- 0 y no repita el diagnostico de antes. Como tambien tiene que poder correrse
-- ANTES del PASO 1 -- cuando la columna `state` todavia no existe y un
-- `WHERE state` seria un 42703 -- se lee via `to_jsonb`, que devuelve NULL en vez
-- de fallar cuando la columna no esta: "no es false" = viva, o columna ausente.
-- ============================================================================

-- 0a. Cuantas personas tienen ficha repetida y cuantas fichas sobran.
SELECT count(*)                    AS personas_con_ficha_repetida,
       sum(n_fichas)               AS fichas_involucradas,
       sum(n_fichas) - count(*)    AS fichas_que_se_dan_de_baja
FROM (SELECT w.person_id, count(*) AS n_fichas
      FROM workers w
      WHERE w.person_id IS NOT NULL AND to_jsonb(w) ->> 'state' IS DISTINCT FROM 'false'
      GROUP BY w.person_id HAVING count(*) > 1) t;

-- 0b. La lista completa, con la ficha que se queda marcada. Revisar que la
--     columna `se_queda` caiga siempre en la ficha correcta antes de seguir.
WITH ranked AS (
    SELECT w.id, w.person_id, w.workers_estado_id, w.created_at, w.email_corporativo,
           row_number() OVER (PARTITION BY w.person_id
                              ORDER BY we.esta_adentro DESC, we.llego_a_ingresar DESC, w.id DESC) AS rn
    FROM workers w
    JOIN workers_estado we ON we.workers_estado_id = w.workers_estado_id
    WHERE w.person_id IS NOT NULL
      AND to_jsonb(w) ->> 'state' IS DISTINCT FROM 'false'
      AND w.person_id IN (SELECT w2.person_id FROM workers w2
                          WHERE w2.person_id IS NOT NULL
                            AND to_jsonb(w2) ->> 'state' IS DISTINCT FROM 'false'
                          GROUP BY w2.person_id HAVING count(*) > 1)
)
SELECT r.person_id, p.document_identity_code AS dni, p.full_name,
       r.id AS worker_id, we.codigo AS estado, r.created_at::date AS ficha_creada,
       (r.rn = 1) AS se_queda,
       (SELECT count(*) FROM workers_periodo_laboral pl WHERE pl.worker_id = r.id AND pl.state) AS periodos,
       (SELECT count(*) FROM worker_vinculaciones v WHERE v.worker_id = r.id) AS vinculaciones
FROM ranked r
JOIN workers_estado we ON we.workers_estado_id = r.workers_estado_id
LEFT JOIN person p ON p.person_id = r.person_id
ORDER BY r.person_id, r.rn;

-- 0c. Personas con DOS fichas adentro a la vez (las dos ACTIVO o INHABILITADO).
--     No es lo normal -- lo normal es una retirada y una activa -- asi que
--     conviene mirarlas una por una: son las que mas probablemente tengan datos
--     recientes en la ficha que se va a dar de baja. En dev es 1.
WITH dup AS (SELECT w.person_id FROM workers w
             WHERE w.person_id IS NOT NULL AND to_jsonb(w) ->> 'state' IS DISTINCT FROM 'false'
             GROUP BY w.person_id HAVING count(*) > 1)
SELECT w.person_id, p.full_name, w.id AS worker_id, we.codigo AS estado,
       w.created_at::date AS ficha_creada, w.email_corporativo, w.puesto_id
FROM workers w
JOIN dup ON dup.person_id = w.person_id
JOIN workers_estado we ON we.workers_estado_id = w.workers_estado_id
LEFT JOIN person p ON p.person_id = w.person_id
WHERE w.person_id IN (
    SELECT w2.person_id FROM workers w2
    JOIN workers_estado we2 ON we2.workers_estado_id = w2.workers_estado_id
    JOIN dup d2 ON d2.person_id = w2.person_id
    WHERE we2.esta_adentro GROUP BY w2.person_id HAVING count(*) > 1)
ORDER BY w.person_id, w.id;

-- 0d. Los periodos laborales que van a chocar al juntarse en una sola ficha.
--     `misma_fecha_ingreso` = el mismo ingreso registrado en las dos fichas (un
--     alta doble, no un reingreso): se conserva el de la ficha que se queda y el
--     otro se archiva con state = false. `varios_abiertos` = mas de un periodo
--     sin fecha de retiro: nadie esta adentro dos veces, asi que el anterior se
--     cierra el dia en que empieza el siguiente. En dev: 8 y 4.
WITH ranked AS (
    SELECT w.id, w.person_id,
           row_number() OVER (PARTITION BY w.person_id
                              ORDER BY we.esta_adentro DESC, we.llego_a_ingresar DESC, w.id DESC) AS rn
    FROM workers w
    JOIN workers_estado we ON we.workers_estado_id = w.workers_estado_id
    WHERE w.person_id IS NOT NULL
      AND to_jsonb(w) ->> 'state' IS DISTINCT FROM 'false'
      AND w.person_id IN (SELECT w2.person_id FROM workers w2
                          WHERE w2.person_id IS NOT NULL
                            AND to_jsonb(w2) ->> 'state' IS DISTINCT FROM 'false'
                          GROUP BY w2.person_id HAVING count(*) > 1)
), mapa AS (
    SELECT r.id AS viejo, c.id AS canon
    FROM ranked r JOIN (SELECT person_id, id FROM ranked WHERE rn = 1) c ON c.person_id = r.person_id
    WHERE r.rn > 1
), consolidado AS (
    SELECT m.canon, pl.fecha_ingreso, pl.fecha_retiro FROM mapa m
    JOIN workers_periodo_laboral pl ON pl.worker_id = m.viejo AND pl.state
    UNION ALL
    SELECT DISTINCT m.canon, pl.fecha_ingreso, pl.fecha_retiro FROM mapa m
    JOIN workers_periodo_laboral pl ON pl.worker_id = m.canon AND pl.state
)
SELECT 'misma_fecha_ingreso' AS choque, count(*) AS fichas
FROM (SELECT canon FROM consolidado GROUP BY canon, fecha_ingreso HAVING count(*) > 1) t
UNION ALL
SELECT 'varios_abiertos', count(*)
FROM (SELECT canon FROM consolidado WHERE fecha_retiro IS NULL GROUP BY canon HAVING count(*) > 1) t;


-- ============================================================================
-- PASO 1 - `workers` gana la columna `state`, y el unico de email la respeta.
--
-- El indice de email corporativo se rehace porque su predicado solo miraba el
-- texto `estado`: sin esto, una ficha dada de baja que quedo en ACTIVO seguiria
-- reservando su correo y bloquearia un alta futura con ese mismo correo.
-- ============================================================================
BEGIN;

ALTER TABLE workers ADD COLUMN IF NOT EXISTS state boolean NOT NULL DEFAULT true;

COMMENT ON COLUMN workers.state IS
    'Soft delete. false = ficha eliminada, no se muestra en ninguna pantalla. Se usa para las fichas duplicadas que existian por el modelo viejo de fecha_ingreso/fecha_retiro: al reingresar se abria una ficha nueva en vez de un periodo nuevo. La fusion queda registrada en workers_ficha_fusionada.';

DROP INDEX IF EXISTS ux_workers_email_corporativo_vigente;
CREATE UNIQUE INDEX ux_workers_email_corporativo_vigente
    ON workers (lower(btrim(email_corporativo)))
    WHERE email_corporativo IS NOT NULL
      AND btrim(email_corporativo) <> ''
      AND state
      AND COALESCE(estado, 'ACTIVO') <> 'RETIRADO'
      AND (lower(btrim(email_corporativo)) LIKE '%@abril.pe'
           OR (contrata_casa = 'Casa' AND obra_oficina_staff_id = ANY (ARRAY[2, 3])));

CREATE INDEX IF NOT EXISTS ix_workers_state ON workers (state) WHERE NOT state;

COMMIT;


-- ============================================================================
-- PASO 2 - Registrar que ficha se fusiona con cual.
--
-- Se guarda en una tabla y no se recalcula en cada paso a proposito: los pasos
-- siguientes tienen que trabajar sobre EXACTAMENTE el mismo conjunto, y una vez
-- que el PASO 4 marca state = false el criterio de eleccion ya no se puede
-- reproducir. Ademas queda como auditoria de que se hizo y contra que ficha
-- buscar el historial viejo de una persona.
-- ============================================================================
BEGIN;

CREATE TABLE IF NOT EXISTS workers_ficha_fusionada (
    workers_ficha_fusionada_id serial       PRIMARY KEY,
    worker_id_eliminado        integer      NOT NULL UNIQUE REFERENCES workers(id),
    worker_id_canonico         integer      NOT NULL REFERENCES workers(id),
    person_id                  integer      NOT NULL REFERENCES person(person_id),
    created_date_time          timestamptz  NOT NULL DEFAULT now(),
    created_user_id            integer      NULL,
    updated_date_time          timestamptz  NULL,
    updated_user_id            integer      NULL,
    active                     boolean      NOT NULL DEFAULT true,
    state                      boolean      NOT NULL DEFAULT true,
    CONSTRAINT ck_workers_ficha_fusionada_distintas CHECK (worker_id_eliminado <> worker_id_canonico)
);

COMMENT ON TABLE workers_ficha_fusionada IS
    'Que ficha duplicada de workers se dio de baja y contra que ficha viva. Existe porque el historial (EMOs, inducciones, amonestaciones, habilitacion, vinculaciones) NO se repunto: sigue colgando de worker_id_eliminado, y esta tabla es la unica forma de llegar a el desde la ficha viva.';

CREATE INDEX IF NOT EXISTS ix_workers_ficha_fusionada_canonico
    ON workers_ficha_fusionada (worker_id_canonico);

INSERT INTO workers_ficha_fusionada (worker_id_eliminado, worker_id_canonico, person_id)
WITH ranked AS (
    SELECT w.id, w.person_id,
           row_number() OVER (PARTITION BY w.person_id
                              ORDER BY we.esta_adentro DESC, we.llego_a_ingresar DESC, w.id DESC) AS rn
    FROM workers w
    JOIN workers_estado we ON we.workers_estado_id = w.workers_estado_id
    WHERE w.person_id IS NOT NULL
      AND w.state
      AND w.person_id IN (SELECT w2.person_id FROM workers w2
                          WHERE w2.person_id IS NOT NULL AND w2.state
                          GROUP BY w2.person_id HAVING count(*) > 1)
)
SELECT r.id, c.id, r.person_id
FROM ranked r
JOIN (SELECT person_id, id FROM ranked WHERE rn = 1) c ON c.person_id = r.person_id
WHERE r.rn > 1
ON CONFLICT (worker_id_eliminado) DO NOTHING;

COMMIT;

-- 2b. Control: tiene que dar el mismo numero que `fichas_que_se_dan_de_baja` del
--     PASO 0a, y ninguna ficha puede ser a la vez eliminada y canonica.
SELECT count(*) AS fichas_a_dar_de_baja,
       count(DISTINCT worker_id_canonico) AS fichas_que_se_quedan,
       (SELECT count(*) FROM workers_ficha_fusionada a
         WHERE EXISTS (SELECT 1 FROM workers_ficha_fusionada b
                        WHERE b.worker_id_canonico = a.worker_id_eliminado)) AS debe_ser_0
FROM workers_ficha_fusionada;


-- ============================================================================
-- PASO 3 - Juntar los periodos laborales en la ficha que se queda.
--
-- Va en una sola transaccion y en este orden:
--   a) se archivan los periodos repetidos (misma fecha de ingreso);
--   b) se cierran los periodos abiertos que dejaran de ser el ultimo;
--   c) se mueven todos los periodos de las fichas de baja.
--
-- El cierre va ANTES del movimiento y no despues, aunque leerlo al reves suene
-- mas natural: los indices unicos se evaluan en cada UPDATE, no al final de la
-- transaccion, asi que mover primero revienta contra
-- ux_workers_periodo_laboral_abierto en cuanto el segundo periodo abierto aterriza
-- en la ficha que se queda. Por eso el calculo de "cual es el ultimo" se hace
-- sobre la ficha DESTINO mientras las filas todavia estan en la de origen.
--
-- El CHECK de rango se baja y se vuelve a poner NOT VALID igual que estaba: hay
-- periodos migrados con la fecha de retiro ANTERIOR a la de ingreso (los 25 que
-- el script anterior dejo pasar en su PASO 0b) y un UPDATE sobre esas filas los
-- vuelve a validar aunque el constraint sea NOT VALID, o sea que la migracion
-- fallaria por filas que ya estaban torcidas antes de empezar.
-- ============================================================================
BEGIN;

-- Antes de mover nada: si alguna ficha involucrada llego a ingresar pero no tiene
-- ningun periodo, su fecha de ingreso se quedaria sin donde vivir al dar de baja
-- la ficha. Esto NO lo arregla inventando un periodo -- corta la migracion y lo
-- dice, porque de donde sacar esa fecha es justo lo que resuelve el PASO 2 de
-- 2026-08-25_workers_periodo_laboral.sql, que tiene que haber corrido antes.
--
-- Mira solo fichas vivas: las ya dadas de baja por una corrida anterior estan sin
-- periodos a proposito (se movieron), y contarlas haria fallar la reejecucion.
DO $mig$
DECLARE
    sin_periodo int;
BEGIN
    SELECT count(*) INTO sin_periodo
    FROM workers w
    JOIN workers_estado we ON we.workers_estado_id = w.workers_estado_id
    WHERE w.state
      AND we.llego_a_ingresar
      AND w.id IN (SELECT worker_id_eliminado FROM workers_ficha_fusionada
                   UNION SELECT worker_id_canonico FROM workers_ficha_fusionada)
      AND NOT EXISTS (SELECT 1 FROM workers_periodo_laboral pl WHERE pl.worker_id = w.id);

    IF sin_periodo > 0 THEN
        RAISE EXCEPTION
            'Hay % ficha(s) a fusionar que llegaron a ingresar y no tienen ningun periodo laboral. Correr antes el PASO 2 de 2026-08-25_workers_periodo_laboral.sql; no se fusiona nada hasta que esa fecha de ingreso este guardada.',
            sin_periodo;
    END IF;
END
$mig$;

ALTER TABLE workers_periodo_laboral DROP CONSTRAINT IF EXISTS ck_workers_periodo_laboral_rango;

-- 3a. El mismo ingreso registrado en las dos fichas: se conserva uno solo. Gana
--     el de la ficha que se queda; entre dos fichas de baja, el mas nuevo. El
--     perdedor no se borra, se archiva con state = false -- los dos indices
--     unicos de la tabla filtran por `state`, asi que deja de estorbar sin
--     perder el registro.
WITH consolidado AS (
    SELECT pl.workers_periodo_laboral_id AS pid,
           COALESCE(f.worker_id_canonico, pl.worker_id) AS canon,
           pl.fecha_ingreso,
           (f.worker_id_eliminado IS NULL) AS es_de_la_que_se_queda
    FROM workers_periodo_laboral pl
    LEFT JOIN workers_ficha_fusionada f ON f.worker_id_eliminado = pl.worker_id
    WHERE pl.state
      AND (pl.worker_id IN (SELECT worker_id_eliminado FROM workers_ficha_fusionada)
        OR pl.worker_id IN (SELECT worker_id_canonico FROM workers_ficha_fusionada))
), rk AS (
    SELECT pid, row_number() OVER (PARTITION BY canon, fecha_ingreso
                                   ORDER BY es_de_la_que_se_queda DESC, pid DESC) AS rn
    FROM consolidado
)
UPDATE workers_periodo_laboral pl
SET state = false, updated_date_time = now()
FROM rk
WHERE rk.pid = pl.workers_periodo_laboral_id AND rk.rn > 1;

-- 3b. Nadie esta adentro dos veces: de los periodos que van a quedar juntos en
--     una misma ficha, solo el ultimo puede seguir abierto. Los anteriores se
--     cierran el dia en que empieza el siguiente, que es el unico dato duro que
--     hay -- el que reingresa el dia X ya estaba afuera antes del dia X. La
--     fecha_fin de la vinculacion no sirve para esto: en dev hay una que cae
--     DESPUES del siguiente ingreso y dejaria dos periodos solapados.
--
--     El PARTITION BY va por la ficha DESTINO, no por la actual: es lo que
--     permite cerrarlos antes de moverlos (ver la nota de arriba).
WITH ord AS (
    SELECT pl.workers_periodo_laboral_id AS pid,
           lead(pl.fecha_ingreso) OVER (
               PARTITION BY COALESCE(f.worker_id_canonico, pl.worker_id)
               ORDER BY pl.fecha_ingreso, pl.workers_periodo_laboral_id) AS sig_ingreso
    FROM workers_periodo_laboral pl
    LEFT JOIN workers_ficha_fusionada f ON f.worker_id_eliminado = pl.worker_id
    WHERE pl.state
      AND (pl.worker_id IN (SELECT worker_id_eliminado FROM workers_ficha_fusionada)
        OR pl.worker_id IN (SELECT worker_id_canonico FROM workers_ficha_fusionada))
)
UPDATE workers_periodo_laboral pl
SET fecha_retiro = ord.sig_ingreso, updated_date_time = now()
FROM ord
WHERE ord.pid = pl.workers_periodo_laboral_id
  AND pl.fecha_retiro IS NULL
  AND ord.sig_ingreso IS NOT NULL;

-- 3c. Mover. Se mueven tambien los archivados del paso 3a: el periodo laboral de
--     una persona queda entero en su ficha viva, cuente o no para el sistema.
UPDATE workers_periodo_laboral pl
SET worker_id = f.worker_id_canonico, updated_date_time = now()
FROM workers_ficha_fusionada f
WHERE f.worker_id_eliminado = pl.worker_id;

ALTER TABLE workers_periodo_laboral
    ADD CONSTRAINT ck_workers_periodo_laboral_rango
    CHECK (fecha_retiro IS NULL OR fecha_retiro >= fecha_ingreso) NOT VALID;

COMMIT;


-- ============================================================================
-- PASO 4 - Dar de baja las fichas duplicadas.
--
-- Solo `state`. El `workers_estado_id` y el texto `estado` se dejan como estan:
-- son el estado en que quedo esa ficha y se conservan para auditoria. Una ficha
-- con state = false no se muestra en ningun lado, asi que ya no importa que diga
-- ACTIVO.
--
-- La vinculacion vigente que quedara colgando de una ficha de baja tambien se
-- cierra: una vinculacion sin fecha_fin significa "trabaja hoy en esta empresa",
-- y en una ficha eliminada eso es falso. Se cierra con la fecha en que empieza la
-- vinculacion siguiente de la persona, o con el ultimo retiro registrado.
-- ============================================================================
BEGIN;

UPDATE worker_vinculaciones v
SET fecha_fin = sub.cierre, updated_at = now()
FROM (
    SELECT v2.id,
           COALESCE(
               (SELECT min(v3.fecha_inicio) FROM worker_vinculaciones v3
                 WHERE v3.worker_id = f.worker_id_canonico AND v3.fecha_inicio >= v2.fecha_inicio),
               (SELECT max(pl.fecha_retiro) FROM workers_periodo_laboral pl
                 WHERE pl.worker_id = f.worker_id_canonico AND pl.state)
           ) AS cierre
    FROM worker_vinculaciones v2
    JOIN workers_ficha_fusionada f ON f.worker_id_eliminado = v2.worker_id
    WHERE v2.fecha_fin IS NULL
) sub
WHERE sub.id = v.id AND sub.cierre IS NOT NULL AND sub.cierre >= v.fecha_inicio;

UPDATE workers w
SET state = false, updated_at = now()
FROM workers_ficha_fusionada f
WHERE f.worker_id_eliminado = w.id AND w.state;

COMMIT;


-- ============================================================================
-- PASO 5 - Verificacion. Correr entero y leer cada linea.
-- ============================================================================

-- 5a. Ya no hay dos fichas vivas para la misma persona. Tiene que dar 0 filas.
SELECT person_id, count(*) AS fichas_vivas
FROM workers WHERE person_id IS NOT NULL AND state
GROUP BY person_id HAVING count(*) > 1;

-- 5b. Cuadre. `dadas_de_baja` tiene que ser igual a `registradas_en_la_fusion`,
--     y `periodos_huerfanos` tiene que ser 0: ninguna ficha de baja puede
--     quedarse con un periodo laboral, porque es lo unico que se movio.
SELECT (SELECT count(*) FROM workers WHERE NOT state)                    AS dadas_de_baja,
       (SELECT count(*) FROM workers_ficha_fusionada)                    AS registradas_en_la_fusion,
       (SELECT count(*) FROM workers_periodo_laboral pl
         WHERE pl.worker_id IN (SELECT worker_id_eliminado FROM workers_ficha_fusionada)) AS periodos_huerfanos;

-- 5c. Las invariantes de workers_periodo_laboral siguen en pie. Las tres columnas
--     tienen que dar 0.
SELECT (SELECT count(*) FROM (SELECT worker_id FROM workers_periodo_laboral
                               WHERE fecha_retiro IS NULL AND state
                               GROUP BY worker_id HAVING count(*) > 1) t)      AS con_dos_abiertos,
       (SELECT count(*) FROM (SELECT worker_id, fecha_ingreso FROM workers_periodo_laboral
                               WHERE state GROUP BY worker_id, fecha_ingreso
                               HAVING count(*) > 1) t)                          AS ingresos_repetidos,
       (SELECT count(*) FROM workers_periodo_laboral pl
          JOIN workers w ON w.id = pl.worker_id
         WHERE pl.state AND NOT w.state)                                        AS periodos_vivos_en_ficha_de_baja;

-- 5d. Ninguna ficha viva se quedo sin periodo laboral tras la fusion: si el
--     reingreso estaba registrado, ahora esta en la ficha que sobrevive.
SELECT count(*) AS fichas_vivas_sin_periodo
FROM (SELECT DISTINCT worker_id_canonico FROM workers_ficha_fusionada) f
WHERE NOT EXISTS (SELECT 1 FROM workers_periodo_laboral pl WHERE pl.worker_id = f.worker_id_canonico);

-- 5e. Como queda el periodo laboral de las personas fusionadas. Un reingreso
--     tiene que verse como dos filas de la MISMA ficha: la primera cerrada y la
--     segunda abierta.
SELECT f.person_id, p.full_name, f.worker_id_canonico AS ficha_viva,
       pl.fecha_ingreso, pl.fecha_retiro, pl.state AS cuenta_en_el_sistema
FROM workers_ficha_fusionada f
JOIN person p ON p.person_id = f.person_id
JOIN workers_periodo_laboral pl ON pl.worker_id = f.worker_id_canonico
ORDER BY p.full_name, pl.fecha_ingreso, pl.workers_periodo_laboral_id;

-- 5f. Periodos que se solapan dentro de una misma ficha: el reingreso quedo
--     registrado ANTES de la fecha de retiro del paso anterior. No lo produce la
--     fusion, ya venia asi en las dos fichas (a alguien lo retiraron en el
--     sistema cuatro dias despues de que ya habia reingresado); la fusion solo lo
--     pone a la vista al dejar los dos periodos juntos. No se corrige aca porque
--     cual de las dos fechas esta mal lo sabe GTH, no el script: se corrige desde
--     la pantalla de trabajadores. En dev son 5.
SELECT a.worker_id, p.full_name,
       a.fecha_ingreso AS ingreso_1, a.fecha_retiro AS retiro_1,
       b.fecha_ingreso AS ingreso_2, b.fecha_retiro AS retiro_2
FROM workers_periodo_laboral a
JOIN workers_periodo_laboral b
  ON b.worker_id = a.worker_id AND b.state
 AND b.workers_periodo_laboral_id <> a.workers_periodo_laboral_id
 AND b.fecha_ingreso > a.fecha_ingreso
 AND b.fecha_ingreso < COALESCE(a.fecha_retiro, DATE '9999-12-31')
JOIN workers w ON w.id = a.worker_id
LEFT JOIN person p ON p.person_id = w.person_id
WHERE a.state
ORDER BY p.full_name;

-- 5g. LO UNICO QUE HAY QUE ACCIONAR: programaciones de EMO todavia ABIERTAS
--     colgando de una ficha dada de baja. Al no repuntar el historial, estas
--     quedan sin ficha visible y salen en la bandeja de Habilitacion con el
--     nombre y el DNI en blanco (la consulta hace `p.Worker?.Person`, asi que no
--     revienta, pero tampoco dice de quien es).
--
--     Son 11 en prod y 0 en dev, y hay que resolverlas a mano: cancelarlas y, si
--     el EMO sigue haciendo falta, reprogramarlo sobre la ficha viva
--     (workers_ficha_fusionada dice cual es). El resto de tablas del 5h no tiene
--     este problema porque sus pantallas ya no las listaban.
SELECT pe.id AS programacion_id, pe.estado, pe.fecha_programada,
       f.worker_id_eliminado AS ficha_de_baja, f.worker_id_canonico AS reprogramar_sobre,
       p.full_name, p.document_identity_code AS dni
FROM ss_programacion_emos pe
JOIN workers_ficha_fusionada f ON f.worker_id_eliminado = pe.worker_id
JOIN person p ON p.person_id = f.person_id
WHERE pe.state
  AND pe.estado NOT IN ('Completado', 'Cancelado', 'Rechazado por Clínica', 'No se presentó')
ORDER BY pe.fecha_programada;

-- 5h. Que quedo colgando de las fichas dadas de baja. NO es un error: es el
--     historial que se decidio no repuntar. Sirve para saber cuanto hay y de que
--     tipo; para llegar a el desde la ficha viva se pasa por
--     workers_ficha_fusionada.
SELECT 'ss_hab_trabajador'   AS tabla, count(*) AS filas FROM ss_hab_trabajador   t JOIN workers_ficha_fusionada f ON f.worker_id_eliminado = t.worker_id
UNION ALL SELECT 'worker_vinculaciones',  count(*) FROM worker_vinculaciones  t JOIN workers_ficha_fusionada f ON f.worker_id_eliminado = t.worker_id
UNION ALL SELECT 'worker_emos',           count(*) FROM worker_emos           t JOIN workers_ficha_fusionada f ON f.worker_id_eliminado = t.worker_id
UNION ALL SELECT 'ss_induccion',          count(*) FROM ss_induccion          t JOIN workers_ficha_fusionada f ON f.worker_id_eliminado = t.worker_id
UNION ALL SELECT 'ssoma_amonestaciones',  count(*) FROM ssoma_amonestaciones  t JOIN workers_ficha_fusionada f ON f.worker_id_eliminado = t.worker_id
UNION ALL SELECT 'ss_charla_asistencia',  count(*) FROM ss_charla_asistencia  t JOIN workers_ficha_fusionada f ON f.worker_id_eliminado = t.worker_id
UNION ALL SELECT 'worker_eventos',        count(*) FROM worker_eventos        t JOIN workers_ficha_fusionada f ON f.worker_id_eliminado = t.worker_id
ORDER BY 2 DESC;
