-- ss_programacion_emos.state: soft delete de programaciones de EMO.
--
-- La tabla era una de las pocas tablas transaccionales de SSOMA sin columna de soft delete
-- (ss_accidente_trabajo, ss_descanso_medico, ss_cita_medica, ss_topico_atencion,
-- ssoma_amonestaciones... todas ya tienen `state`). Lo único que existía para "sacar" una
-- programación era estado = 'Cancelado', que es un estado del flujo (la clínica/SSOMA la
-- canceló), no una baja del registro: sigue listándose en Programaciones y cuenta en el
-- total del resumen. Se necesitaba poder dar de baja una fila mal creada —p. ej. una
-- programación de prueba— sin borrarla, porque ningún registro se elimina de la BD.
--
-- NOT NULL DEFAULT true = todas las programaciones existentes siguen vigentes; la baja es
-- explícita. Es un flag booleano al estilo de person.state / workers.state, no un estado ni
-- un tipo, por eso no lleva tabla de catálogo (el catálogo de flujo sigue siendo `estado`).
--
-- ORDEN DE APLICACIÓN: este script va ANTES de desplegar el backend que lo acompaña. Ese
-- deploy agrega SsProgramacionEmo.State y filtra `p.State` en los 17 puntos de lectura
-- (ProgramacionEmoRepository, DashboardRepository, EmoResumenDiarioService,
-- HabTrabajadorRepository, EmoAutoProgramacionService, EmoRepository, InterconsultaRepository);
-- si el código sale primero, EF pide una columna que todavía no existe y revienta.
--
-- La baja de abajo pone además estado = 'Cancelado'. No es redundante: state saca la fila de
-- la aplicación, y 'Cancelado' deja el flujo coherente para quien lea la fila en auditoría
-- (quedarse en 'Aceptado por Clínica' diría que la cita seguía en pie).

ALTER TABLE ss_programacion_emos
    ADD COLUMN IF NOT EXISTS state boolean NOT NULL DEFAULT true;

COMMENT ON COLUMN ss_programacion_emos.state IS
    'Soft delete al estilo del resto de tablas SSOMA: false = programacion dada de baja, se conserva la fila por auditoria y no debe listarse. Distinto de estado (flujo: Programado/Aceptado por Clinica/Cancelado/...).';

-- Baja de la programación de prueba #1126 (ALVAREZ VILLEGAS CHRISTIAN CIRO, DNI 70400087,
-- Periódico Anual del 15/08/2026 07:00 en ServiSalud). Se creó el 10/08/2026 solo para
-- demostrar la funcionalidad y quedó viva: la clínica la aceptó y disparó el correo
-- "[EMO Confirmado]". No dejó rastros que limpiar (emo_resultado_id NULL, ninguna fila en
-- worker_emos / ss_interconsultas / ss_alertas_emo, y ss_hab_trabajador item 4 intacto).
--
-- El WHERE repite los datos identificatorios a propósito: si el id no coincide con esa
-- programación, no actualiza nada en vez de dar de baja la fila equivocada.

UPDATE ss_programacion_emos
SET state      = false,
    estado     = 'Cancelado',
    notas      = concat_ws(
                     E'\n',
                     nullif(notas, ''),
                     'Baja 10/08/2026: programación de prueba creada para demostrar la funcionalidad, no corresponde a un EMO real.'
                 ),
    updated_at = now()
WHERE id               = 1126
  AND worker_id        = 13267
  AND tipo_emo_id      = 6
  AND fecha_programada = DATE '2026-08-15'
  AND state            = true;

-- Comprobación posterior: debe devolver 1 fila con state = f y estado = 'Cancelado'.
--   SELECT id, worker_id, fecha_programada, hora_programada, estado, state, notas, updated_at
--   FROM ss_programacion_emos WHERE id = 1126;
