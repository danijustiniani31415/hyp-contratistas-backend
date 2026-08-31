-- ═══════════════════════════════════════════════════════════════════════════
-- Secuencias que quedaron DETRÁS de su max(id) → 23505 al insertar
-- ═══════════════════════════════════════════════════════════════════════════
--
-- ── El síntoma ─────────────────────────────────────────────────────────────
-- "Error del servidor" al enviar una Solicitud de Personal en la BD LOCAL, con
-- este error en el log del backend:
--
--     23505: llave duplicada viola restricción de unicidad «pk_gth_requerimiento»
--
-- No es un bug del código: `ReclutamientoRepository.Create` está bien y el
-- correlativo REQ-AAAA-NNNN se calcula correcto (el INSERT que falla ni siquiera
-- manda el id — lo genera la base). Lo que está mal es la SECUENCIA del id.
--
-- ── La causa ───────────────────────────────────────────────────────────────
-- En la BD local, `gth_requerimiento` tiene 50 filas con ids del 1 al 51, pero
-- su secuencia estaba en 2. El siguiente `nextval` devolvía un id que ya existía
-- y la PK lo rechazaba. Lo mismo en otras 9 tablas del árbol de Reclutamiento
-- (candidato, entrevista, evaluación, onboarding…), así que el error iba a
-- reaparecer en cada paso siguiente del flujo aunque se arreglara solo esta.
--
-- ── Por qué PROD no tiene este problema ────────────────────────────────────
-- En prod las secuencias de `gth_solicitud`, `gth_requerimiento`,
-- `gth_aprobacion_gg` y `gth_aprobacion_gg_detalle` TAMBIÉN están en 1, pero eso
-- es a propósito: es el reinicio que dejó `2026-08-26_gth_limpiar_datos_prueba.sql`
-- para que la numeración volviera a empezar en 1 conservando REQ-2026-0015/0016
-- y la aprobación 5. Ahí no rompe nada porque ese mismo script instaló el
-- disparador `gth_saltar_id_ocupado()`, que salta el id ya ocupado.
--
-- Ese script nunca se corrió en la BD local (la local conserva toda su data de
-- prueba), así que la local quedó SIN el disparador y con las secuencias
-- atrasadas — la peor de las dos mitades.
--
-- ── Qué hace este script ───────────────────────────────────────────────────
--   BLOQUE A → la BD LOCAL / dev. Le pone el disparador que le falta (paridad
--              con prod) y adelanta toda secuencia que quedó detrás de su max.
--   BLOQUE B → PROD. Nada de Reclutamiento: ahí ya está bien. Solo dos
--              secuencias ajenas a esto que la auditoría encontró de paso y que
--              son un 23505 esperando (una de ellas la inserta la app).
--
-- ⚠️  El BLOQUE A **NO** se corre en prod: adelantaría las secuencias de
--     Reclutamiento y se perdería el reinicio en 1 que se pidió.
-- ═══════════════════════════════════════════════════════════════════════════


-- ═══════════════════════════════════════════════════════════════════════════
-- BLOQUE A · SOLO EN LA BD LOCAL / DEV  (Host=localhost;Port=5433)
-- ═══════════════════════════════════════════════════════════════════════════

BEGIN;

SET client_encoding TO 'UTF8';

-- Freno: que nadie corra este bloque contra prod por accidente. En prod
-- `gth_requerimiento` tiene solo las 2 vacantes vivas que se conservaron; en
-- dev tiene decenas de filas de prueba.
DO $guard$
DECLARE n bigint;
BEGIN
    SELECT count(*) INTO n FROM gth_requerimiento;
    IF n <= 2 THEN
        RAISE EXCEPTION
            'Esta base tiene % requerimiento(s): parece PROD, no la local. El bloque A no se corre aca. Ver el bloque B.', n;
    END IF;
END
$guard$;

-- ── A.1) El disparador que prod ya tiene y dev no ──────────────────────────
-- Mismo código que en `2026-08-26_gth_limpiar_datos_prueba.sql`, para que la
-- local se comporte igual que prod cuando se prueba el módulo. Con las
-- secuencias adelantadas (A.2) queda como un EXISTS por INSERT sobre la PK:
-- inapreciable. Se instala igual para que dev reproduzca a prod y no aparezcan
-- diferencias de comportamiento recién en producción.
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

-- ── A.2) Adelantar TODA secuencia que quedó detrás de su max(id) ───────────
-- Genérico a propósito, y no una lista de tablas: la local se restaura de un
-- dump cada tanto y un `pg_dump --data-only` (o unos INSERT con id explícito)
-- deja exactamente este estado. Este bloque se puede volver a correr después de
-- cada restauración sin pensar.
--
-- `setval(seq, max)` deja is_called = true → el próximo nextval es max + 1. No
-- se toca ninguna secuencia cuya tabla esté vacía ni ninguna que ya vaya por
-- delante de su max.
DO $seqs$
DECLARE r record; mx bigint; nx bigint; arregladas int := 0;
BEGIN
    FOR r IN
        SELECT c.relname AS tabla, a.attname AS col, s.relname AS seq
        FROM   pg_class s
        JOIN   pg_depend d    ON d.objid = s.oid
                             AND d.classid = 'pg_class'::regclass
                             AND d.deptype IN ('a','i')   -- 'a' = serial, 'i' = IDENTITY
        JOIN   pg_class c     ON c.oid = d.refobjid
        JOIN   pg_attribute a ON a.attrelid = c.oid AND a.attnum = d.refobjsubid
        WHERE  s.relkind = 'S' AND c.relkind = 'r'
          AND  c.relnamespace = 'public'::regnamespace
        ORDER  BY 1
    LOOP
        EXECUTE format('SELECT coalesce(max(%I), 0) FROM %I', r.col, r.tabla) INTO mx;
        nx := pg_sequence_last_value(('public.' || quote_ident(r.seq))::regclass);

        -- nx IS NULL = la secuencia nunca se usó (nextval jamás corrió): si la
        -- tabla tiene filas, el próximo id sería 1 y choca contra la PK.
        IF mx > 0 AND (nx IS NULL OR nx < mx) THEN
            PERFORM setval(('public.' || quote_ident(r.seq))::regclass, mx);
            arregladas := arregladas + 1;
            RAISE NOTICE 'adelantada  %  ->  proximo id = %  (estaba en %)',
                rpad(r.tabla || '.' || r.col, 46), mx + 1, coalesce(nx::text, 'sin usar');
        END IF;
    END LOOP;

    RAISE NOTICE '=== secuencias adelantadas: % ===', arregladas;
END
$seqs$;

-- ── Comprobación antes de confirmar: no debe quedar ninguna desalineada ────
DO $chk$
DECLARE r record; mx bigint; nx bigint; quedan int := 0;
BEGIN
    FOR r IN
        SELECT c.relname AS tabla, a.attname AS col, s.relname AS seq
        FROM   pg_class s
        JOIN   pg_depend d    ON d.objid = s.oid
                             AND d.classid = 'pg_class'::regclass
                             AND d.deptype IN ('a','i')
        JOIN   pg_class c     ON c.oid = d.refobjid
        JOIN   pg_attribute a ON a.attrelid = c.oid AND a.attnum = d.refobjsubid
        WHERE  s.relkind = 'S' AND c.relkind = 'r'
          AND  c.relnamespace = 'public'::regnamespace
    LOOP
        EXECUTE format('SELECT coalesce(max(%I), 0) FROM %I', r.col, r.tabla) INTO mx;
        nx := pg_sequence_last_value(('public.' || quote_ident(r.seq))::regclass);
        IF mx > 0 AND (nx IS NULL OR nx < mx) THEN quedan := quedan + 1; END IF;
    END LOOP;

    IF quedan > 0 THEN
        RAISE EXCEPTION 'Todavia quedan % secuencias desalineadas. No se confirma.', quedan;
    END IF;
    RAISE NOTICE 'OK: no quedan secuencias desalineadas.';
END
$chk$;

COMMIT;


-- ═══════════════════════════════════════════════════════════════════════════
-- BLOQUE B · PROD  ← lo único que hay que correr allá
-- ═══════════════════════════════════════════════════════════════════════════
--
-- Reclutamiento en prod está BIEN y no se toca: sus secuencias están en 1 a
-- propósito y el disparador `gth_saltar_id_ocupado()` ya está instalado
-- (verificado el 2026-08-26). Adelantarlas rompería el reinicio en 1.
--
-- Lo que sí hay que arreglar son dos secuencias ajenas a Reclutamiento que la
-- auditoría encontró de paso, las dos con la secuencia detrás del max:
--
--   • feriados.id → max = 45 y la secuencia SIN USAR (el próximo id sería 1).
--     Esta es la urgente: la app inserta ahí
--     (`CronogramaActividadesRepository` → `ctx.Feriados.Add`), así que el
--     próximo feriado que alguien registre en Cronograma de Actividades muere
--     con 23505. No viene del trabajo de Reclutamiento — es de antes.
--
--   • contractor_state.contractor_state_id → max = 4, secuencia en 3.
--     Es un catálogo y ningún código inserta ahí, así que hoy no le molesta a
--     nadie; solo reventaría un INSERT a mano. Se incluye porque es una línea.
--
-- Es idempotente: si se corre dos veces, la segunda deja todo igual.

BEGIN;

SELECT setval(pg_get_serial_sequence('feriados', 'id'),
              (SELECT max(id) FROM feriados));

SELECT setval(pg_get_serial_sequence('contractor_state', 'contractor_state_id'),
              (SELECT max(contractor_state_id) FROM contractor_state));

-- Comprobación: las dos tienen que quedar con ult_seq = max_id, o sea el
-- proximo id en max + 1.
SELECT 'feriados' AS tabla,
       (SELECT max(id) FROM feriados) AS max_id,
       pg_sequence_last_value(pg_get_serial_sequence('feriados', 'id')::regclass) AS ult_seq
UNION ALL
SELECT 'contractor_state',
       (SELECT max(contractor_state_id) FROM contractor_state),
       pg_sequence_last_value(pg_get_serial_sequence('contractor_state', 'contractor_state_id')::regclass);

COMMIT;
