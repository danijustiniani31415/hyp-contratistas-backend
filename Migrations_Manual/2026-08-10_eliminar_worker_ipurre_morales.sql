-- Eliminación TOTAL (no soft-delete) del trabajador "IPURRE MORALES JESUS" (DNI 414325041),
-- creado por error. worker_id = 14161, person_id = 5734.
--
-- Verificado en sesión con el usuario, paso a paso:
--   ss_hab_trabajador          → 16 filas
--   ss_hab_worker_proyecto     → 1 fila
--   ss_programacion_emos       → 2 filas
--   ss_sctr_vidaley_worker     → 6 filas
--   worker_emos                → 1 fila
--   worker_eventos              → 2 filas
--   worker_vinculaciones        → 2 filas
--   ss_hab_documento_version   → 12 filas (hijas de las 16 de ss_hab_trabajador)
--   ss_hab_documento_archivo   → 0 filas (hijas de esas 12 versiones)
--   worker_emo_convalidaciones, ss_emo_examenes_detalle, ss_emo_restricciones,
--   ss_interconsultas, ss_alertas_emo → 0 filas (hijas del único worker_emo)
-- Todo lo demás en la base de datos dio 0 filas para este worker_id.

BEGIN;

DO $$
DECLARE
    v_worker_id  integer := 14161;
    v_person_id  integer := 5734;
    v_count      bigint;
BEGIN
    -- 1) Versiones de documentos de habilitación (hijas de ss_hab_trabajador)
    DELETE FROM ss_hab_documento_version
    WHERE hab_trabajador_id IN (SELECT id FROM ss_hab_trabajador WHERE worker_id = v_worker_id);
    GET DIAGNOSTICS v_count = ROW_COUNT;
    RAISE NOTICE 'ss_hab_documento_version: % filas borradas', v_count;

    -- 2) Items de habilitación del trabajador
    DELETE FROM ss_hab_trabajador WHERE worker_id = v_worker_id;
    GET DIAGNOSTICS v_count = ROW_COUNT;
    RAISE NOTICE 'ss_hab_trabajador: % filas borradas', v_count;

    -- 3) Relación worker-proyecto de habilitación
    DELETE FROM ss_hab_worker_proyecto WHERE worker_id = v_worker_id;
    GET DIAGNOSTICS v_count = ROW_COUNT;
    RAISE NOTICE 'ss_hab_worker_proyecto: % filas borradas', v_count;

    -- 4) Programaciones de EMO
    DELETE FROM ss_programacion_emos WHERE worker_id = v_worker_id;
    GET DIAGNOSTICS v_count = ROW_COUNT;
    RAISE NOTICE 'ss_programacion_emos: % filas borradas', v_count;

    -- 5) Vínculos de póliza SCTR / Vida Ley
    DELETE FROM ss_sctr_vidaley_worker WHERE worker_id = v_worker_id;
    GET DIAGNOSTICS v_count = ROW_COUNT;
    RAISE NOTICE 'ss_sctr_vidaley_worker: % filas borradas', v_count;

    -- 6) El EMO en sí (ya sin hijos, verificado)
    DELETE FROM worker_emos WHERE worker_id = v_worker_id;
    GET DIAGNOSTICS v_count = ROW_COUNT;
    RAISE NOTICE 'worker_emos: % filas borradas', v_count;

    -- 7) Historial de eventos del trabajador
    DELETE FROM worker_eventos WHERE worker_id = v_worker_id;
    GET DIAGNOSTICS v_count = ROW_COUNT;
    RAISE NOTICE 'worker_eventos: % filas borradas', v_count;

    -- 8) Vinculaciones (empresa/proyecto/puesto histórico)
    DELETE FROM worker_vinculaciones WHERE worker_id = v_worker_id;
    GET DIAGNOSTICS v_count = ROW_COUNT;
    RAISE NOTICE 'worker_vinculaciones: % filas borradas', v_count;

    -- 9) El trabajador
    DELETE FROM workers WHERE id = v_worker_id;
    GET DIAGNOSTICS v_count = ROW_COUNT;
    RAISE NOTICE 'workers: % filas borradas', v_count;

    -- 10) Person, SOLO si no quedó vinculado a ningún otro worker.
    SELECT count(*) INTO v_count FROM workers WHERE person_id = v_person_id;
    IF v_count = 0 THEN
        DELETE FROM person WHERE person_id = v_person_id;
        RAISE NOTICE 'person %: eliminado (no estaba vinculado a otro worker)', v_person_id;
    ELSE
        RAISE NOTICE 'person %: NO eliminado — sigue vinculado a % worker(s)', v_person_id, v_count;
    END IF;
END $$;

-- Revisa los NOTICE de arriba: deberían coincidir con los conteos ya verificados
-- (16, 1, 2, 6, 1, 2, 2, 12 y finalmente 1 en "workers").
-- Si todo cuadra:
--   COMMIT;
-- Si algo no coincide o hay un error:
--   ROLLBACK;
