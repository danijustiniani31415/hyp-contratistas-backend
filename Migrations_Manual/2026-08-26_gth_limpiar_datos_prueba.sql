-- ═══════════════════════════════════════════════════════════════════════════
-- Gestión GTH · Reclutamiento: BORRADO de los datos de prueba
-- ═══════════════════════════════════════════════════════════════════════════
--
-- ⚠️  ESTE SCRIPT BORRA FILAS DE VERDAD (DELETE, no soft-delete). No hay vuelta
--     atrás una vez que se hace COMMIT. Hacer `pg_dump` de la base ANTES.
--
-- ── La excepción que se conserva ───────────────────────────────────────────
-- TODO el árbol de Reclutamiento es data de prueba MENOS una solicitud: la de
-- los dos DESARROLLADOR FULL STACK JUNIOR de Tecnología de la Información
-- (REQ-2026-0015 y REQ-2026-0016), registrada el 2026-08-26. Esa es real y se
-- queda TAL CUAL: mismos códigos, misma aprobación (id 5).
--
-- Nada de eso se renumera, a propósito: el correo de aprobación ya salió con
-- esos códigos y su botón apunta a /gestion-gth/aprobaciones/5. Renumerar
-- obligaría a reenviarlo.
--
-- ── Cómo sigue la numeración ───────────────────────────────────────────────
-- Como el resto desaparece, el año queda con un hueco al revés: ocupado el 15 y
-- el 16, libre todo lo anterior. Para que los próximos registros no arranquen en
-- el 17 este script deja las secuencias en 1, y lo que ya está tomado se SALTEA:
--
--   • Códigos REQ-2026-NNNN → lo resuelve el backend, que desde este cambio
--     toma el MENOR número libre del año en vez de MAX+1. Así la próxima
--     solicitud es REQ-2026-0001, y al llegar al 15 salta al 17.
--   • IDs de solicitud / requerimiento / aprobación → se reinician las
--     secuencias a 1 y un disparador salta el id que ya exista. Sin él, la
--     quinta aprobación chocaría contra la id 5 que se conservó.
--
-- Qué borra:
--   1) El árbol transaccional de Reclutamiento de TODAS las demás solicitudes:
--      requerimientos, aprobaciones, candidatos, formularios de postulante,
--      entrevistas, evaluaciones, anexos y onboardings.
--   2) Las fichas de trabajador de PRE-INGRESO que nacieron de esos procesos
--      (workers_estado_id 4 = finalista aprobado, 5 = no ingresó) junto con lo
--      que les cuelga: EMO programado, resultado del EMO y sus detalles, y su
--      expediente de habilitación.
--   3) Las filas de `person` que quedaron huérfanas: las que creó este flujo y
--      que ya no tienen ninguna ficha en `workers` ni usuario del sistema.
--   4) Las campanitas de Reclutamiento que apuntaban a lo borrado.
--
-- Qué NO borra (a propósito):
--   • La solicitud real de arriba y todo lo suyo.
--   • Los catálogos del módulo (estados, tipos, prioridades, distritos,
--     universidades, canales, carpetas, y la configuración de correos).
--   • Ninguna ficha de trabajador con vinculación (`worker_vinculaciones`): esas
--     son personas que sí ingresaron a Abril y no salen de este flujo.
--   • Ninguna `person` que tenga usuario del sistema o alguna otra ficha viva.
--   • Los archivos ya subidos a SharePoint (CVs, sustentos, cartas oferta):
--     esos hay que borrarlos a mano desde SharePoint si también estorban.
--
-- Cómo correrlo:
--   PASO 1 — vista previa (solo lee, no borra nada). Ver el bloque de abajo.
--   PASO 2 — el borrado, que va entero en una transacción: si algo que no
--            previmos todavía apunta a una de estas filas, la FK aborta TODO y
--            no se pierde nada. En ese caso, avisar con el mensaje del error.
--
-- No es idempotente A PROPÓSITO: la segunda corrida encuentra que ya no queda
-- nada por borrar y aborta sin tocar nada, en vez de arriesgarse con la data que
-- se haya registrado después.
-- ═══════════════════════════════════════════════════════════════════════════


-- ───────────────────────────────────────────────────────────────────────────
-- PASO 1 · VISTA PREVIA — correr esto solo, primero, y revisar los números
-- ───────────────────────────────────────────────────────────────────────────
-- -- 1.a) Lo que se CONSERVA (tienen que salir exactamente estas dos filas):
-- SELECT r.codigo, p.nombre AS puesto, s.area_nombre, r.created_date_time
-- FROM   gth_requerimiento r
-- JOIN   gth_solicitud s ON s.gth_solicitud_id = r.gth_solicitud_id
-- JOIN   puesto p        ON p.puesto_id        = r.puesto_id
-- WHERE  r.gth_solicitud_id = (SELECT gth_solicitud_id FROM gth_requerimiento
--                               WHERE codigo = 'REQ-2026-0015')
-- ORDER  BY r.numero;
--
-- -- 1.b) Lo que se BORRA, por tabla:
-- SELECT 'gth_solicitud'  AS tabla, count(*) FROM gth_solicitud
--         WHERE gth_solicitud_id <> (SELECT gth_solicitud_id FROM gth_requerimiento WHERE codigo='REQ-2026-0015')
-- UNION ALL SELECT 'gth_requerimiento', count(*) FROM gth_requerimiento
--         WHERE gth_solicitud_id <> (SELECT gth_solicitud_id FROM gth_requerimiento WHERE codigo='REQ-2026-0015')
-- UNION ALL SELECT 'gth_candidato',     count(*) FROM gth_candidato
-- UNION ALL SELECT 'gth_postulante_formulario', count(*) FROM gth_postulante_formulario
-- UNION ALL SELECT 'gth_onboarding',    count(*) FROM gth_onboarding
-- ORDER BY 1;
--
-- -- 1.c) Las fichas de pre-ingreso que se van a borrar (revisar que sean las esperadas):
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
-- verificar a ojo contra el correo que ya salió.
CREATE TEMP TABLE tmp_req_conservar ON COMMIT DROP AS
SELECT r.gth_requerimiento_id, r.gth_solicitud_id, r.codigo, r.numero, r.anio
FROM   gth_requerimiento r
WHERE  r.gth_solicitud_id = (SELECT gth_solicitud_id
                               FROM gth_requerimiento
                              WHERE codigo = 'REQ-2026-0015');

-- Freno: si lo que se iba a conservar no es exactamente lo esperado, se aborta
-- sin borrar nada. Vale tanto para una base distinta de la que se revisó como
-- para una segunda corrida (donde además ya no habría nada que borrar).
DO $chk$
DECLARE codigos text; sobran int;
BEGIN
    SELECT string_agg(codigo, ', ' ORDER BY codigo) INTO codigos FROM tmp_req_conservar;

    IF codigos IS NULL THEN
        RAISE EXCEPTION
            'No se encontro REQ-2026-0015. Esta no es la base esperada. No se borro nada.';
    END IF;

    IF codigos <> 'REQ-2026-0015, REQ-2026-0016' THEN
        RAISE EXCEPTION
            'La solicitud que se iba a conservar tiene otras vacantes (%). Revisar antes de correr.',
            codigos;
    END IF;

    SELECT count(*) INTO sobran FROM gth_solicitud
     WHERE gth_solicitud_id NOT IN (SELECT gth_solicitud_id FROM tmp_req_conservar);
    IF sobran = 0 THEN
        RAISE EXCEPTION
            'No hay nada que borrar: solo queda la solicitud real. Este script ya se corrio.';
    END IF;
END
$chk$;

-- Candidatos de los requerimientos que SÍ se borran. La solicitud que se
-- conserva es de hoy y todavía no tiene ninguno, pero se calcula igual en vez de
-- asumirlo: si mañana tuviera, este script no puede llevárselo por delante.
CREATE TEMP TABLE tmp_candidatos_borrar ON COMMIT DROP AS
SELECT c.gth_candidato_id
FROM   gth_candidato c
WHERE  c.gth_requerimiento_id NOT IN (SELECT gth_requerimiento_id FROM tmp_req_conservar);

-- Fichas de trabajador de pre-ingreso SIN vinculación. La vinculación es la
-- frontera: quien la tiene llegó a entrar a Abril y no se toca, tenga el estado
-- que tenga.
CREATE TEMP TABLE tmp_workers_borrar ON COMMIT DROP AS
SELECT w.id, w.person_id
FROM   workers w
WHERE  w.workers_estado_id IN (4, 5)
  AND  NOT EXISTS (SELECT 1 FROM worker_vinculaciones wv WHERE wv.worker_id = w.id);

-- Personas tocadas por el flujo (las que escribió el formulario del postulante,
-- las del onboarding y las de las fichas de arriba) que no le sirven a nadie
-- más: sin usuario del sistema y sin ninguna otra ficha que sobreviva.
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
        UNION SELECT person_id FROM tmp_workers_borrar WHERE person_id IS NOT NULL)
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
DELETE FROM ss_programacion_emos  WHERE worker_id IN (SELECT id FROM tmp_workers_borrar);
DELETE FROM worker_emos           WHERE id        IN (SELECT emo_id FROM tmp_emos_borrar);
DELETE FROM worker_eventos        WHERE worker_id IN (SELECT id FROM tmp_workers_borrar);
DELETE FROM workers_periodo_laboral WHERE worker_id IN (SELECT id FROM tmp_workers_borrar);

-- Habilitación: al aprobarse el finalista se le puede haber abierto ya su
-- expediente de documentos, aunque todavía no haya ingresado.
DELETE FROM ss_hab_documento_version
 WHERE hab_trabajador_id IN (SELECT ht.id FROM ss_hab_trabajador ht
                              WHERE ht.worker_id IN (SELECT id FROM tmp_workers_borrar));
DELETE FROM ss_hab_trabajador WHERE worker_id IN (SELECT id FROM tmp_workers_borrar);

-- ── 4) Las fichas y, por último, las personas huérfanas ────────────────────
-- Red de seguridad antes del DELETE: si alguna tabla que este script no previó
-- todavía apunta a estas fichas, se aborta con el nombre exacto de la tabla en
-- vez de con un error de FK crudo. Recorre TODAS las FK hacia `workers`, así que
-- también avisa de tablas nuevas que se agreguen después de escribir esto.
DO $guard$
DECLARE r record; n bigint; pendientes text := '';
BEGIN
    FOR r IN
        SELECT c.conrelid::regclass::text AS tbl, a.attname AS col
        FROM   pg_constraint c
        JOIN   unnest(c.conkey) WITH ORDINALITY AS k(attnum, ord) ON true
        JOIN   pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = k.attnum
        WHERE  c.contype = 'f' AND c.confrelid::regclass::text = 'workers'
    LOOP
        EXECUTE format(
            'SELECT count(*) FROM %I t JOIN tmp_workers_borrar g ON g.id = t.%I', r.tbl, r.col)
        INTO n;
        IF n > 0 THEN
            pendientes := pendientes || format('%s.%s (%s filas), ', r.tbl, r.col, n);
        END IF;
    END LOOP;

    IF pendientes <> '' THEN
        RAISE EXCEPTION
            'No se puede borrar las fichas de pre-ingreso: todavia las referencian %. '
            'Pasale esta lista a Claude para que agregue el DELETE que falta.', rtrim(pendientes, ', ');
    END IF;
END
$guard$;

DELETE FROM workers WHERE id IN (SELECT id FROM tmp_workers_borrar);
DELETE FROM person  WHERE person_id IN (SELECT person_id FROM tmp_persons_borrar);

-- ── 5) Que los ids vuelvan a empezar en 1 ──────────────────────────────────
-- Disparador que salta un id ya ocupado. Hace falta porque las secuencias se
-- reinician en 1 pero quedaron filas vivas con ids bajos (la aprobación 5, la
-- solicitud 10, los requerimientos 15 y 16): sin esto, la quinta aprobación que
-- se registre reventaría contra la PK.
--
-- Es genérico y permanente: se le pasa el nombre de la columna y sirve para
-- cualquier tabla con IDENTITY. Una vez que la secuencia pasa el id vivo más
-- alto se vuelve un EXISTS por INSERT sobre una PK — inapreciable en tablas de
-- decenas de filas al año.
--
-- Ojo: si algún día una consulta insertara un id EXPLÍCITO que ya existe, esto
-- le asignaría uno nuevo en vez de fallar. Hoy nadie lo hace (EF deja que lo
-- genere la base) y un INSERT así siempre fue un bug, pero conviene saberlo.
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
-- el ALTER solo acepta las primeras. `setval(seq, 1, false)` deja el próximo
-- nextval en 1 en los dos casos.
--
-- Las cuatro primeras tienen filas vivas y las protege el disparador de arriba;
-- las demás quedaron vacías, así que no hay con qué chocar.
DO $seqs$
DECLARE t record; seq text;
BEGIN
    FOR t IN
        SELECT * FROM (VALUES
            ('gth_solicitud',                    'gth_solicitud_id'),
            ('gth_requerimiento',                'gth_requerimiento_id'),
            ('gth_aprobacion_gg',                'gth_aprobacion_gg_id'),
            ('gth_aprobacion_gg_detalle',        'gth_aprobacion_gg_detalle_id'),
            ('gth_candidato',                    'gth_candidato_id'),
            ('gth_candidato_anexo',              'gth_candidato_anexo_id'),
            ('gth_candidato_evaluacion',         'gth_candidato_evaluacion_id'),
            ('gth_candidato_evaluacion_archivo', 'gth_candidato_evaluacion_archivo_id'),
            ('gth_entrevista',                   'gth_entrevista_id'),
            ('gth_postulante_formulario',        'gth_postulante_formulario_id'),
            ('gth_onboarding',                   'gth_onboarding_id'),
            ('gth_requerimiento_canal',          'gth_requerimiento_canal_id')
        ) AS v(tabla, col)
    LOOP
        seq := pg_get_serial_sequence(t.tabla, t.col);
        IF seq IS NULL THEN
            RAISE EXCEPTION 'La columna %.% no tiene secuencia; revisar antes de correr.', t.tabla, t.col;
        END IF;
        PERFORM setval(seq, 1, false);
    END LOOP;
END
$seqs$;

-- ── Comprobación antes de confirmar ────────────────────────────────────────
-- Tiene que quedar 1 solicitud, sus 2 requerimientos con los códigos INTACTOS
-- (0015 y 0016), la aprobación con id 5, y todo lo demás en cero.
SELECT 'gth_solicitud'       AS tabla, count(*) AS quedan FROM gth_solicitud
UNION ALL SELECT 'gth_requerimiento',   count(*) FROM gth_requerimiento
UNION ALL SELECT 'gth_aprobacion_gg',   count(*) FROM gth_aprobacion_gg
UNION ALL SELECT 'gth_candidato',       count(*) FROM gth_candidato
UNION ALL SELECT 'gth_onboarding',      count(*) FROM gth_onboarding
UNION ALL SELECT 'workers pre-ingreso', count(*) FROM workers WHERE workers_estado_id IN (4,5)
ORDER BY 1;

SELECT r.codigo, r.numero, r.gth_requerimiento_id, a.gth_aprobacion_gg_id
FROM   gth_requerimiento r
JOIN   gth_aprobacion_gg a ON a.gth_solicitud_id = r.gth_solicitud_id
ORDER  BY r.numero;

COMMIT;
