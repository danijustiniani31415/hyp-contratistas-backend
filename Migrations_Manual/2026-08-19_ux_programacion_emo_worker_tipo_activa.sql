-- Índice único parcial: un trabajador no puede tener dos programaciones "activas" (no
-- terminales) del mismo tipo de EMO a la vez.
--
-- Hoy esa regla solo vivía en memoria: ProgramacionEmoRepository.Create y
-- EmoAutoProgramacionService.ProcesarAutoProgramacion hacen un SELECT de "¿ya tiene una
-- programación activa?" antes del INSERT, pero cada uno abre su propio DbContext
-- (IDbContextFactory, sin lock ni transacción compartida). Si el cron de auto-programación
-- (corre ~7am) y una programación manual caen casi al mismo tiempo para el mismo
-- trabajador/tipo — o alguien hace doble click en "Programar" — ambos pasan el SELECT antes
-- de que el primero confirme el INSERT, y quedan dos filas activas para la misma persona.
-- Este índice es la garantía real, igual que ux_tareo_worker_fecha_tipo en
-- ArquitecturaComercialTareoRepository. El backend que acompaña este script traduce la
-- violación (23505) a un 409 "Este trabajador ya tiene una programación activa..." en vez de
-- un 500 crudo.
--
-- ORDEN DE APLICACIÓN: correr este script ANTES de desplegar el backend que lo acompaña
-- (agrega el HasIndex equivalente en AppDbContext + el catch de DbUpdateException en
-- ProgramacionEmoRepository.Create). Si el código sale primero, el catch nunca dispara porque
-- el índice todavía no existe y el error vuelve a ser un 500 genérico — no rompe nada, pero
-- no protege contra la carrera hasta que este script corra.
--
-- PRE-CHEQUEO: si ya existen duplicados activos en producción (p. ej. el caso reportado de
-- Fiorella Mendoza), el CREATE UNIQUE INDEX de abajo falla con "could not create unique index"
-- y hay que resolverlos primero (dar de baja la fila sobrante con state = false, igual que el
-- script 2026-08-10_ss_programacion_emos_state.sql). Correr esto antes de intentar el índice:
--
--   SELECT worker_id, tipo_emo_id, array_agg(id ORDER BY created_at) AS ids,
--          array_agg(estado ORDER BY created_at) AS estados,
--          array_agg(created_at ORDER BY created_at) AS creados
--   FROM ss_programacion_emos
--   WHERE state = true
--     AND estado NOT IN ('Completado', 'Cancelado', 'Rechazado por Clínica', 'No se presentó')
--   GROUP BY worker_id, tipo_emo_id
--   HAVING count(*) > 1;

-- Duplicado real encontrado al intentar crear el índice: worker_id 11955 (Casildo Solorzano
-- Marco Aurelio), tipo_emo_id 6, dos filas activas "Aceptado por Clínica" — id 1149 (Manual,
-- creada 11/08, fecha 22/08) e id 1293 (Automática, creada 18/08, corregida/aceptada 19/08,
-- fecha 20/08). No fue una carrera cron/manual: ClinicaAccion "Aceptar" permitía corregir el
-- tipo de EMO de una programación sin volver a chequear duplicados contra las demás filas
-- activas del trabajador (ya corregido en el backend que acompaña este script). Se decidió
-- mantener la 1149 (la coordinada primero, 22/08) y dar de baja la 1293 como duplicado.
--
-- El WHERE repite los datos identificatorios a propósito: si no coincide con esta programación
-- puntual, no actualiza nada en vez de dar de baja la fila equivocada.

UPDATE ss_programacion_emos
SET state      = false,
    estado     = 'Cancelado',
    notas      = concat_ws(
                     E'\n',
                     nullif(notas, ''),
                     'Baja 19/08/2026: duplicado de la programación #1149 para el mismo trabajador/tipo de EMO (tipo 6). Se mantiene la #1149 (22/08).'
                 ),
    updated_at = now()
WHERE id               = 1293
  AND worker_id        = 11955
  AND tipo_emo_id      = 6
  AND fecha_programada = DATE '2026-08-20'
  AND state            = true;

-- Comprobación posterior: debe devolver 1 fila con state = f y estado = 'Cancelado'.
--   SELECT id, worker_id, tipo_emo_id, fecha_programada, estado, state, notas, updated_at
--   FROM ss_programacion_emos WHERE id = 1293;

CREATE UNIQUE INDEX IF NOT EXISTS ux_programacion_emo_worker_tipo_activa
    ON ss_programacion_emos (worker_id, tipo_emo_id)
    WHERE state = true
      AND estado NOT IN ('Completado', 'Cancelado', 'Rechazado por Clínica', 'No se presentó');

-- Comprobación posterior: debe existir el índice.
--   SELECT indexname, indexdef FROM pg_indexes
--   WHERE tablename = 'ss_programacion_emos' AND indexname = 'ux_programacion_emo_worker_tipo_activa';
