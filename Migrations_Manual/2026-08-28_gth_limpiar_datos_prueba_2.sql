-- ═══════════════════════════════════════════════════════════════════════════
-- Gestión GTH · Reclutamiento: BORRADO de los datos de prueba (2.ª pasada)
-- ═══════════════════════════════════════════════════════════════════════════
--
-- ⚠️  ESTE SCRIPT BORRA FILAS DE VERDAD (DELETE, no soft-delete). No hay vuelta
--     atrás una vez que se hace COMMIT. Hacer `pg_dump` de la base ANTES.
--
-- Es la segunda corrida de la limpieza de 2026-08-26
-- (`2026-08-26_gth_limpiar_datos_prueba.sql`): desde entonces se siguió probando
-- el flujo y volvió a acumularse data de prueba en producción. Mismo criterio y
-- misma excepción, con tres diferencias:
--
--   • `gth_requerimiento_estado_historial` (el historial de fases que escribe el
--     interceptor de EF) no existía en la primera corrida y ahora sí: cuelga del
--     requerimiento y hay que borrarlo antes que él.
--   • Se agregan las `person` de los ingresos directos FFT
--     (`gth_requerimiento.fft_person_id`), que en un FFT es el ÚNICO enlace
--     entre el pedido y la persona — no hay formulario de postulante del que
--     leerla.
--   • Las secuencias solo se reinician en 1 donde es seguro (ver el paso 5).
--
-- ── La excepción que se conserva ───────────────────────────────────────────
-- TODO el árbol de Reclutamiento es data de prueba MENOS los dos DESARROLLADOR
-- FULL STACK JUNIOR de Tecnología de la Información: REQ-2026-0015 y
-- REQ-2026-0016. Esos son reales y se quedan TAL CUAL: mismos códigos, misma
-- solicitud y misma aprobación.
--
-- Nada de eso se renumera, a propósito: el correo de aprobación ya salió con
-- esos códigos y su botón apunta a esa aprobación por id. Renumerar obligaría a
-- reenviarlo.
--
-- Qué borra:
--   1) Las campanitas de Reclutamiento que apuntaban a lo borrado.
--   2) El árbol transaccional de Reclutamiento de TODAS las demás solicitudes:
--      requerimientos, su historial de fases, canales, aprobaciones, candidatos,
--      formularios de postulante, entrevistas, evaluaciones, anexos y
--      onboardings.
--   3) Las fichas de trabajador de PRE-INGRESO que nacieron de esos procesos
--      (workers_estado_id 4 = finalista aprobado, 5 = no ingresó) junto con lo
--      que les cuelga: EMO programado, resultado del EMO y sus detalles, su
--      expediente de habilitación y su rastro de fusión de fichas. Las de los
--      dos requerimientos reales quedan fuera aunque su proceso ya haya llegado
--      hasta ahí.
--   4) Las filas de `person` que quedaron huérfanas: las que creó este flujo y
--      que ya no tienen ninguna ficha en `workers` ni usuario del sistema.
--
-- Qué NO borra (a propósito):
--   • Los dos requerimientos reales de arriba y todo lo suyo.
--   • Los catálogos del módulo (estados, tipos, prioridades, distritos,
--     universidades, canales, carpetas, y la configuración de correos).
--   • Ninguna ficha de trabajador con vinculación (`worker_vinculaciones`): esas
--     son personas que sí ingresaron a Abril y no salen de este flujo.
--   • Ninguna `person` que tenga usuario del sistema, alguna otra ficha viva o
--     cualquier otra fila que todavía la referencie.
--   • Los archivos ya subidos a SharePoint (CVs, sustentos, cartas oferta):
--     esos hay que borrarlos a mano desde SharePoint si también estorban.
--
-- Cómo correrlo:
--   PASO 1 — vista previa (solo lee, no borra nada). Ver el bloque de abajo.
--   PASO 2 — el borrado, que va entero en una transacción: si algo que no
--            previmos todavía apunta a una de estas filas, la FK aborta TODO y
--            no se pierde nada. En ese caso, pasar el mensaje del error.
--
-- No es idempotente A PROPÓSITO: la segunda corrida encuentra que ya no queda
-- nada por borrar y aborta sin tocar nada, en vez de arriesgarse con la data que
-- se haya registrado después.
-- ═══════════════════════════════════════════════════════════════════════════


-- ───────────────────────────────────────────────────────────────────────────
-- PASO 1 · VISTA PREVIA — correr esto solo, primero, y revisar los números
-- ───────────────────────────────────────────────────────────────────────────
-- -- 1.a) Lo que se CONSERVA (tienen que salir exactamente estas dos filas):
-- SELECT r.gth_requerimiento_id, r.codigo, p.nombre AS puesto, s.area_nombre,
--        r.gth_solicitud_id, r.created_date_time
-- FROM   gth_requerimiento r
-- JOIN   gth_solicitud s ON s.gth_solicitud_id = r.gth_solicitud_id
-- JOIN   puesto p        ON p.puesto_id        = r.puesto_id
-- WHERE  r.gth_solicitud_id IN (SELECT gth_solicitud_id FROM gth_requerimiento
--                                WHERE codigo IN ('REQ-2026-0015','REQ-2026-0016'))
-- ORDER  BY r.codigo;
--
-- -- 1.b) Lo que se BORRA, por tabla:
-- WITH conservar AS (
--   SELECT gth_requerimiento_id, gth_solicitud_id FROM gth_requerimiento
--    WHERE gth_solicitud_id IN (SELECT gth_solicitud_id FROM gth_requerimiento
--                                WHERE codigo IN ('REQ-2026-0015','REQ-2026-0016')))
-- SELECT 'gth_solicitud' AS tabla, count(*) FROM gth_solicitud
--         WHERE gth_solicitud_id NOT IN (SELECT gth_solicitud_id FROM conservar)
-- UNION ALL SELECT 'gth_requerimiento', count(*) FROM gth_requerimiento
--         WHERE gth_requerimiento_id NOT IN (SELECT gth_requerimiento_id FROM conservar)
-- UNION ALL SELECT 'gth_candidato', count(*) FROM gth_candidato
--         WHERE gth_requerimiento_id NOT IN (SELECT gth_requerimiento_id FROM conservar)
-- UNION ALL SELECT 'gth_postulante_formulario', count(*) FROM gth_postulante_formulario
-- UNION ALL SELECT 'gth_onboarding', count(*) FROM gth_onboarding
-- ORDER BY 1;
--
-- -- 1.c) Las fichas de pre-ingreso que se van a borrar (revisar que sean las esperadas).
-- --      Ninguna de estas puede ser el finalista de REQ-2026-0015 / REQ-2026-0016;
-- --      el script las excluye, pero conviene verlo acá antes de correrlo:
-- SELECT w.id, w.person_id, w.workers_estado_id, p.full_name, p.document_identity_code
-- FROM   workers w
-- LEFT   JOIN person p ON p.person_id = w.person_id
-- WHERE  w.workers_estado_id IN (4, 5)
--   AND  NOT EXISTS (SELECT 1 FROM worker_vinculaciones wv WHERE wv.worker_id = w.id)
-- ORDER  BY w.id;


-- ───────────────────────────────────────────────────────────────────────────
-- PASO 2 · EL BORRADO
-- ───────────────────────────────────────────────────────────────────────────

BEGIN;

SET client_encoding TO 'UTF8';

-- ── Lo que se conserva, congelado antes de tocar nada ──────────────────────
-- Se identifica por el CÓDIGO y no por el id: el código es lo que se puede
-- verificar a ojo contra el correo que ya salió. Se conservan las solicitudes
-- COMPLETAS de esos dos códigos: borrar media solicitud dejaría una cabecera
-- que no se corresponde con lo que dice su correo.
CREATE TEMP TABLE tmp_req_conservar ON COMMIT DROP AS
SELECT r.gth_requerimiento_id, r.gth_solicitud_id, r.codigo, r.numero, r.anio
FROM   gth_requerimiento r
WHERE  r.gth_solicitud_id IN (SELECT gth_solicitud_id
                                FROM gth_requerimiento
                               WHERE codigo IN ('REQ-2026-0015', 'REQ-2026-0016'));

-- Freno: si lo que se iba a conservar no es exactamente lo esperado, se aborta
-- sin borrar nada. Vale tanto para una base distinta de la que se revisó como
-- para una segunda corrida (donde además ya no habría nada que borrar).
DO $chk$
DECLARE codigos text; sobran int;
BEGIN
    SELECT string_agg(codigo, ', ' ORDER BY codigo) INTO codigos FROM tmp_req_conservar;

    IF codigos IS NULL THEN
        RAISE EXCEPTION
            'No se encontraron REQ-2026-0015 / REQ-2026-0016. Esta no es la base esperada. No se borro nada.';
    END IF;

    -- Puede ser una sola solicitud con las dos vacantes o dos solicitudes de una
    -- cada una; lo que no puede haber es una tercera vacante colgando de ellas,
    -- porque se conservaria data de prueba sin querer.
    IF codigos <> 'REQ-2026-0015, REQ-2026-0016' THEN
        RAISE EXCEPTION
            'Las solicitudes que se iban a conservar tienen otras vacantes (%). Revisar antes de correr.',
            codigos;
    END IF;

    SELECT count(*) INTO sobran FROM gth_solicitud
     WHERE gth_solicitud_id NOT IN (SELECT gth_solicitud_id FROM tmp_req_conservar);
    IF sobran = 0 THEN
        RAISE EXCEPTION
            'No hay nada que borrar: solo quedan las solicitudes reales. Este script ya se corrio.';
    END IF;
END
$chk$;

-- Candidatos de los requerimientos que SÍ se borran. Los de las solicitudes que
-- se conservan quedan intactos, tengan o no proceso avanzado.
CREATE TEMP TABLE tmp_candidatos_borrar ON COMMIT DROP AS
SELECT c.gth_candidato_id
FROM   gth_candidato c
WHERE  c.gth_requerimiento_id NOT IN (SELECT gth_requerimiento_id FROM tmp_req_conservar);

-- Las personas de los requerimientos que SE CONSERVAN. Se calculan aparte y
-- antes que nada porque son la excepción de la excepción: si el proceso de
-- REQ-2026-0015/0016 ya avanzó hasta abrirle su ficha de pre-ingreso a un
-- finalista, esa ficha y esa persona son tan reales como su requerimiento y no
-- se pueden ir con el resto. Los dos caminos del person_id están cubiertos: el
-- formulario del postulante (flujo normal) y `fft_person_id` (ingreso directo).
CREATE TEMP TABLE tmp_persons_conservar ON COMMIT DROP AS
SELECT DISTINCT person_id FROM (
    SELECT f.person_id
    FROM   gth_postulante_formulario f
    JOIN   gth_candidato c ON c.gth_candidato_id = f.gth_candidato_id
    WHERE  f.person_id IS NOT NULL
      AND  c.gth_requerimiento_id IN (SELECT gth_requerimiento_id FROM tmp_req_conservar)
    UNION
    SELECT o.person_id
    FROM   gth_onboarding o
    JOIN   gth_candidato c ON c.gth_candidato_id = o.gth_candidato_id
    WHERE  o.person_id IS NOT NULL
      AND  c.gth_requerimiento_id IN (SELECT gth_requerimiento_id FROM tmp_req_conservar)
    UNION
    SELECT r.fft_person_id
    FROM   gth_requerimiento r
    WHERE  r.fft_person_id IS NOT NULL
      AND  r.gth_requerimiento_id IN (SELECT gth_requerimiento_id FROM tmp_req_conservar)
) AS x(person_id);

-- Fichas de trabajador de pre-ingreso SIN vinculación. La vinculación es la
-- frontera: quien la tiene llegó a entrar a Abril y no se toca, tenga el estado
-- que tenga. Las de los requerimientos que se conservan tampoco.
CREATE TEMP TABLE tmp_workers_borrar ON COMMIT DROP AS
SELECT w.id, w.person_id
FROM   workers w
WHERE  w.workers_estado_id IN (4, 5)
  AND  NOT EXISTS (SELECT 1 FROM worker_vinculaciones wv WHERE wv.worker_id = w.id)
  AND  (w.person_id IS NULL
        OR w.person_id NOT IN (SELECT person_id FROM tmp_persons_conservar));

-- Personas tocadas por el flujo que no le sirven a nadie más: sin usuario del
-- sistema y sin ninguna otra ficha que sobreviva. Son de cuatro procedencias:
--   • el formulario del postulante (flujo normal),
--   • el onboarding,
--   • las fichas de pre-ingreso de arriba,
--   • el propio pedido, en un ingreso directo FFT (`fft_person_id`), que es el
--     único enlace a la persona cuando no hubo formulario.
CREATE TEMP TABLE tmp_persons_borrar ON COMMIT DROP AS
SELECT DISTINCT p.person_id
FROM   person p
WHERE  p.user_id IS NULL
  AND  p.person_id IN (
        SELECT person_id FROM gth_postulante_formulario
         WHERE person_id IS NOT NULL
           AND gth_candidato_id IN (SELECT gth_candidato_id FROM tmp_candidatos_borrar)
        UNION SELECT person_id FROM gth_onboarding
         WHERE person_id IS NOT NULL
           AND gth_candidato_id IN (SELECT gth_candidato_id FROM tmp_candidatos_borrar)
        UNION SELECT fft_person_id FROM gth_requerimiento
         WHERE fft_person_id IS NOT NULL
           AND gth_requerimiento_id NOT IN (SELECT gth_requerimiento_id FROM tmp_req_conservar)
        UNION SELECT person_id FROM tmp_workers_borrar WHERE person_id IS NOT NULL)
  AND  p.person_id NOT IN (SELECT person_id FROM tmp_persons_conservar)
  AND  NOT EXISTS (
        SELECT 1 FROM workers w2
        WHERE  w2.person_id = p.person_id
          AND  w2.id NOT IN (SELECT id FROM tmp_workers_borrar));

-- (la PK de worker_emos se llama `id`; las hijas la referencian como `emo_id`)
CREATE TEMP TABLE tmp_emos_borrar ON COMMIT DROP AS
SELECT e.id AS emo_id FROM worker_emos e
WHERE  e.worker_id IN (SELECT id FROM tmp_workers_borrar);

-- ── 1) Campanitas de lo que se va ──────────────────────────────────────────
DELETE FROM notificacion n
USING  notificacion_tipo t
WHERE  t.notificacion_tipo_id = n.notificacion_tipo_id
  AND  t.codigo IN ('GTH_SOLICITUD_PERSONAL', 'GTH_APROBACION_GG')
  AND  NOT EXISTS (
        SELECT 1 FROM tmp_req_conservar c
        WHERE  n.referencia LIKE '%' || c.codigo || '%');

-- ── 2) Árbol transaccional de Reclutamiento, de las hojas a la raíz ────────
DELETE FROM gth_candidato_evaluacion_archivo
 WHERE gth_candidato_evaluacion_id IN (
        SELECT gth_candidato_evaluacion_id FROM gth_candidato_evaluacion
         WHERE gth_candidato_id IN (SELECT gth_candidato_id FROM tmp_candidatos_borrar));
DELETE FROM gth_candidato_evaluacion  WHERE gth_candidato_id IN (SELECT gth_candidato_id FROM tmp_candidatos_borrar);
DELETE FROM gth_entrevista            WHERE gth_candidato_id IN (SELECT gth_candidato_id FROM tmp_candidatos_borrar);
DELETE FROM gth_candidato_anexo       WHERE gth_candidato_id IN (SELECT gth_candidato_id FROM tmp_candidatos_borrar);
DELETE FROM gth_onboarding            WHERE gth_candidato_id IN (SELECT gth_candidato_id FROM tmp_candidatos_borrar);
DELETE FROM gth_postulante_formulario WHERE gth_candidato_id IN (SELECT gth_candidato_id FROM tmp_candidatos_borrar);
DELETE FROM gth_candidato             WHERE gth_candidato_id IN (SELECT gth_candidato_id FROM tmp_candidatos_borrar);

-- El historial de fases del requerimiento (lo escribe el interceptor de EF, no
-- el código de la feature): cuelga del requerimiento y se va con él.
DELETE FROM gth_requerimiento_estado_historial
 WHERE gth_requerimiento_id NOT IN (SELECT gth_requerimiento_id FROM tmp_req_conservar);
DELETE FROM gth_requerimiento_canal
 WHERE gth_requerimiento_id NOT IN (SELECT gth_requerimiento_id FROM tmp_req_conservar);
DELETE FROM gth_aprobacion_gg_detalle
 WHERE gth_requerimiento_id NOT IN (SELECT gth_requerimiento_id FROM tmp_req_conservar);
DELETE FROM gth_aprobacion_gg
 WHERE gth_solicitud_id NOT IN (SELECT gth_solicitud_id FROM tmp_req_conservar);
DELETE FROM gth_requerimiento
 WHERE gth_requerimiento_id NOT IN (SELECT gth_requerimiento_id FROM tmp_req_conservar);
DELETE FROM gth_solicitud
 WHERE gth_solicitud_id NOT IN (SELECT gth_solicitud_id FROM tmp_req_conservar);

-- ── 3) Lo que le cuelga a las fichas de pre-ingreso ────────────────────────
-- La programación suelta primero su resultado, si no el DELETE del resultado
-- choca contra ss_programacion_emos.emo_resultado_id.
UPDATE ss_programacion_emos SET emo_resultado_id = NULL
 WHERE emo_resultado_id IN (SELECT emo_id FROM tmp_emos_borrar);

DELETE FROM ss_alertas_emo            WHERE emo_id IN (SELECT emo_id FROM tmp_emos_borrar);
DELETE FROM ss_emo_examenes_detalle   WHERE emo_id IN (SELECT emo_id FROM tmp_emos_borrar);
DELETE FROM ss_emo_restricciones      WHERE emo_id IN (SELECT emo_id FROM tmp_emos_borrar);
DELETE FROM ss_interconsultas         WHERE emo_id IN (SELECT emo_id FROM tmp_emos_borrar);
DELETE FROM worker_emo_convalidaciones WHERE emo_id IN (SELECT emo_id FROM tmp_emos_borrar);

DELETE FROM ss_alertas_emo        WHERE worker_id IN (SELECT id FROM tmp_workers_borrar);
DELETE FROM ss_interconsultas     WHERE worker_id IN (SELECT id FROM tmp_workers_borrar);
DELETE FROM ss_programacion_emos  WHERE worker_id IN (SELECT id FROM tmp_workers_borrar);
DELETE FROM worker_emos           WHERE id        IN (SELECT emo_id FROM tmp_emos_borrar);
DELETE FROM worker_eventos        WHERE worker_id IN (SELECT id FROM tmp_workers_borrar);
DELETE FROM workers_periodo_laboral WHERE worker_id IN (SELECT id FROM tmp_workers_borrar);

-- Habilitación: al aprobarse el finalista se le puede haber abierto ya su
-- expediente de documentos, aunque todavía no haya ingresado.
DELETE FROM ss_hab_documento_version
 WHERE hab_trabajador_id IN (SELECT ht.id FROM ss_hab_trabajador ht
                              WHERE ht.worker_id IN (SELECT id FROM tmp_workers_borrar));
DELETE FROM ss_hab_bloqueo_log    WHERE worker_id IN (SELECT id FROM tmp_workers_borrar);
DELETE FROM ss_hab_worker_proyecto WHERE worker_id IN (SELECT id FROM tmp_workers_borrar);
DELETE FROM ss_hab_trabajador     WHERE worker_id IN (SELECT id FROM tmp_workers_borrar);

-- Rastro de fusión de fichas duplicadas: si alguna de estas fichas participó de
-- una fusión (como canónica o como eliminada), su fila de auditoría se va con
-- ella. La ficha del otro lado, si sobrevive, se queda sin ese rastro — que es
-- lo correcto: la fusión que documentaba ya no tiene dos partes.
DELETE FROM workers_ficha_fusionada
 WHERE worker_id_canonico IN (SELECT id FROM tmp_workers_borrar)
    OR worker_id_eliminado IN (SELECT id FROM tmp_workers_borrar)
    OR person_id IN (SELECT person_id FROM tmp_persons_borrar);

-- ── 4) Las fichas y, por último, las personas huérfanas ────────────────────
-- Red de seguridad antes de cada DELETE: si alguna tabla que este script no
-- previó todavía apunta a estas filas, se aborta con el nombre exacto de la
-- tabla en vez de con un error de FK crudo. Recorre TODAS las FK hacia la tabla
-- que se le pase, así que también avisa de tablas nuevas que se agreguen
-- después de escribir esto.
CREATE OR REPLACE FUNCTION pg_temp.gth_chequear_referencias(
    destino text, tmp_tabla text, tmp_col text) RETURNS void
LANGUAGE plpgsql AS $fn$
DECLARE r record; n bigint; pendientes text := '';
BEGIN
    FOR r IN
        SELECT c.conrelid::regclass::text AS tbl, a.attname AS col
        FROM   pg_constraint c
        JOIN   unnest(c.conkey) WITH ORDINALITY AS k(attnum, ord) ON true
        JOIN   pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = k.attnum
        WHERE  c.contype = 'f' AND c.confrelid::regclass::text = destino
    LOOP
        EXECUTE format(
            'SELECT count(*) FROM %s t JOIN %I g ON g.%I = t.%I',
            r.tbl, tmp_tabla, tmp_col, r.col)
        INTO n;
        IF n > 0 THEN
            pendientes := pendientes || format('%s.%s (%s filas), ', r.tbl, r.col, n);
        END IF;
    END LOOP;

    IF pendientes <> '' THEN
        RAISE EXCEPTION
            'No se puede borrar de %: todavia la referencian %. '
            'Pasale esta lista a Claude para que agregue el DELETE que falta.',
            destino, rtrim(pendientes, ', ');
    END IF;
END
$fn$;

SELECT pg_temp.gth_chequear_referencias('workers', 'tmp_workers_borrar', 'id');
DELETE FROM workers WHERE id IN (SELECT id FROM tmp_workers_borrar);

SELECT pg_temp.gth_chequear_referencias('person', 'tmp_persons_borrar', 'person_id');
DELETE FROM person WHERE person_id IN (SELECT person_id FROM tmp_persons_borrar);

-- ── 5) Que los ids vuelvan a empezar en 1 ──────────────────────────────────
-- Disparador que salta un id ya ocupado. Hace falta porque las secuencias se
-- reinician en 1 pero quedan filas vivas con ids bajos (las de los dos
-- requerimientos reales): sin esto, el INSERT que llegue a ese id reventaría
-- contra la PK. Se recrea acá aunque ya exista de la primera corrida, para que
-- el script funcione también en una base que no la tuvo.
CREATE OR REPLACE FUNCTION gth_saltar_id_ocupado() RETURNS trigger
LANGUAGE plpgsql AS $fn$
DECLARE
    col    text   := TG_ARGV[0];
    seq    text   := pg_get_serial_sequence(TG_TABLE_SCHEMA || '.' || TG_TABLE_NAME, TG_ARGV[0]);
    id     bigint;
    existe boolean;
BEGIN
    id := (to_jsonb(NEW) ->> col)::bigint;
    LOOP
        EXECUTE format('SELECT EXISTS (SELECT 1 FROM %I.%I WHERE %I = $1)',
                       TG_TABLE_SCHEMA, TG_TABLE_NAME, col)
           INTO existe USING id;
        EXIT WHEN NOT existe;
        id := nextval(seq);
    END LOOP;
    NEW := jsonb_populate_record(NEW, jsonb_build_object(col, id));
    RETURN NEW;
END
$fn$;

DROP TRIGGER IF EXISTS tg_gth_solicitud_id             ON gth_solicitud;
DROP TRIGGER IF EXISTS tg_gth_requerimiento_id         ON gth_requerimiento;
DROP TRIGGER IF EXISTS tg_gth_aprobacion_gg_id         ON gth_aprobacion_gg;
DROP TRIGGER IF EXISTS tg_gth_aprobacion_gg_detalle_id ON gth_aprobacion_gg_detalle;

CREATE TRIGGER tg_gth_solicitud_id BEFORE INSERT ON gth_solicitud
    FOR EACH ROW EXECUTE FUNCTION gth_saltar_id_ocupado('gth_solicitud_id');
CREATE TRIGGER tg_gth_requerimiento_id BEFORE INSERT ON gth_requerimiento
    FOR EACH ROW EXECUTE FUNCTION gth_saltar_id_ocupado('gth_requerimiento_id');
CREATE TRIGGER tg_gth_aprobacion_gg_id BEFORE INSERT ON gth_aprobacion_gg
    FOR EACH ROW EXECUTE FUNCTION gth_saltar_id_ocupado('gth_aprobacion_gg_id');
CREATE TRIGGER tg_gth_aprobacion_gg_detalle_id BEFORE INSERT ON gth_aprobacion_gg_detalle
    FOR EACH ROW EXECUTE FUNCTION gth_saltar_id_ocupado('gth_aprobacion_gg_detalle_id');

-- Reinicio de las secuencias. Va por `setval` sobre `pg_get_serial_sequence` y no
-- por `ALTER TABLE ... RESTART` porque estas tablas no son todas iguales: unas
-- tienen IDENTITY y otras el `serial` de siempre (gth_onboarding, por ejemplo), y
-- el ALTER solo acepta las primeras.
--
-- Solo se reinician las que es seguro reiniciar:
--   • Las cuatro con disparador de arriba: aunque queden filas vivas con ids
--     bajos, el disparador los saltea.
--   • Las demás, solo si quedaron VACÍAS. Si a los requerimientos reales les
--     quedó algún candidato, entrevista o formulario, reiniciar su secuencia
--     haría que el próximo INSERT chocara contra esa fila, y esas tablas no
--     tienen disparador que lo salte. Ahí la secuencia se deja como está y el
--     script avisa cuáles saltó.
DO $seqs$
DECLARE t record; seq text; filas bigint; saltadas text := '';
BEGIN
    FOR t IN
        SELECT * FROM (VALUES
            ('gth_solicitud',                    'gth_solicitud_id',                    true),
            ('gth_requerimiento',                'gth_requerimiento_id',                true),
            ('gth_aprobacion_gg',                'gth_aprobacion_gg_id',                true),
            ('gth_aprobacion_gg_detalle',        'gth_aprobacion_gg_detalle_id',        true),
            ('gth_requerimiento_estado_historial','gth_requerimiento_estado_historial_id', false),
            ('gth_candidato',                    'gth_candidato_id',                    false),
            ('gth_candidato_anexo',              'gth_candidato_anexo_id',              false),
            ('gth_candidato_evaluacion',         'gth_candidato_evaluacion_id',         false),
            ('gth_candidato_evaluacion_archivo', 'gth_candidato_evaluacion_archivo_id', false),
            ('gth_entrevista',                   'gth_entrevista_id',                   false),
            ('gth_postulante_formulario',        'gth_postulante_formulario_id',        false),
            ('gth_onboarding',                   'gth_onboarding_id',                   false),
            ('gth_requerimiento_canal',          'gth_requerimiento_canal_id',          false)
        ) AS v(tabla, col, protegida)
    LOOP
        seq := pg_get_serial_sequence(t.tabla, t.col);
        IF seq IS NULL THEN
            RAISE EXCEPTION 'La columna %.% no tiene secuencia; revisar antes de correr.', t.tabla, t.col;
        END IF;

        EXECUTE format('SELECT count(*) FROM %I', t.tabla) INTO filas;

        IF t.protegida OR filas = 0 THEN
            PERFORM setval(seq, 1, false);
        ELSE
            saltadas := saltadas || format('%s (%s filas), ', t.tabla, filas);
        END IF;
    END LOOP;

    IF saltadas <> '' THEN
        RAISE NOTICE 'Secuencias NO reiniciadas porque la tabla no quedo vacia: %', rtrim(saltadas, ', ');
    END IF;
END
$seqs$;

-- ── Comprobación antes de confirmar ────────────────────────────────────────
-- Tienen que quedar los 2 requerimientos con los códigos INTACTOS (0015 y 0016)
-- y su(s) solicitud(es), y todo lo demás en cero.
SELECT 'gth_solicitud'       AS tabla, count(*) AS quedan FROM gth_solicitud
UNION ALL SELECT 'gth_requerimiento',   count(*) FROM gth_requerimiento
UNION ALL SELECT 'gth_aprobacion_gg',   count(*) FROM gth_aprobacion_gg
UNION ALL SELECT 'gth_requerimiento_estado_historial', count(*) FROM gth_requerimiento_estado_historial
UNION ALL SELECT 'gth_candidato',       count(*) FROM gth_candidato
UNION ALL SELECT 'gth_onboarding',      count(*) FROM gth_onboarding
UNION ALL SELECT 'workers pre-ingreso', count(*) FROM workers WHERE workers_estado_id IN (4,5)
ORDER BY 1;

SELECT r.codigo, r.numero, r.gth_requerimiento_id, a.gth_aprobacion_gg_id
FROM   gth_requerimiento r
LEFT   JOIN gth_aprobacion_gg a ON a.gth_solicitud_id = r.gth_solicitud_id
ORDER  BY r.numero;

COMMIT;
