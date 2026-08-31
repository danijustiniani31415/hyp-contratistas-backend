-- ============================================================================
-- Gestión GTH · Reclutamiento — Fases de resultado del EMO de ingreso
--
-- El EMO de ingreso ya no cierra el proceso por su cuenta: lo deja en la fase
-- que dice cómo salió el examen, y el cierre lo confirma GTH desde el detalle
-- del requerimiento ("Cerrar proceso y continuar a Onboarding"). Cerrar es lo
-- que pone al seleccionado en la bandeja de Onboarding, así que esa decisión la
-- toma una persona con el resultado del examen a la vista.
--
-- Siembra cuatro estados nuevos en `gth_estado_requerimiento`:
--
--   EMO_APTO                → el examen salió Apto. Falta que GTH cierre.
--   EMO_APTO_RESTRICCIONES  → salió Apto con Restricciones. Igual que el
--                             anterior; es un estado aparte solo para que el
--                             badge diga cuál de las dos fue.
--   EMO_OBSERVADO           → quedó Observado: el candidato fue derivado a
--                             interconsulta. Ni cierra ni continúa con otro,
--                             porque este todavía puede resultar apto.
--   CERRADO_SIN_CUBRIR      → el proceso terminó sin cubrir la vacante: un
--                             ingreso directo (FFT) que salió No Apto y no tiene
--                             rechazados que retomar ni long list a la que
--                             volver.
--
-- Los tres primeros llevan `orden` = 10, el mismo de EMO_INGRESO, y el cuarto
-- `orden` = 11, el mismo de CERRADO. Todos con `active = false`, igual que
-- EMO_NO_APTO y RECHAZADO_GG: son estados del requerimiento, no pasos propios
-- de la línea de tiempo del seguimiento (que solo lista las fases activas), y
-- compartir el orden es lo que hace que esa línea marque el paso correcto como
-- el vigente en vez de darlo todo por cumplido.
--
-- CERRADO_SIN_CUBRIR es un estado aparte de CERRADO a propósito: CERRADO
-- significa "la vacante se cubrió y el seleccionado pasa a onboarding" y es lo
-- que cuenta la tarjeta "Procesos cerrados". Meter ahí una vacante que nunca se
-- llenó la contaría como un cierre exitoso.
--
-- No hay backfill: los requerimientos existentes se quedan donde están. Los que
-- estén en EMO_INGRESO pasarán solos a la fase que corresponda cuando la clínica
-- registre (o corrija) la aptitud del examen.
--
-- Idempotente: se puede correr múltiples veces sin duplicar ni romper nada.
-- ============================================================================

BEGIN;

INSERT INTO gth_estado_requerimiento (codigo, nombre, orden, active, state, descripcion)
SELECT v.codigo, v.nombre, v.orden, v.active, v.state, v.descripcion
FROM (VALUES
  ('EMO_APTO',
   'EMO apto',
   10, false, true,
   'El examen médico de ingreso del finalista salió Apto. Falta que GTH cierre el proceso para que el seleccionado pase a onboarding.'),

  ('EMO_APTO_RESTRICCIONES',
   'EMO apto con restricciones',
   10, false, true,
   'El examen médico de ingreso del finalista salió Apto con Restricciones. Falta que GTH cierre el proceso para que el seleccionado pase a onboarding.'),

  ('EMO_OBSERVADO',
   'EMO observado',
   10, false, true,
   'El examen médico de ingreso del finalista quedó Observado: fue derivado a interconsulta y su aptitud se define con ese resultado. El proceso no cierra ni continúa con otro candidato hasta entonces.'),

  ('CERRADO_SIN_CUBRIR',
   'Cerrado sin cubrir',
   11, false, true,
   'El proceso terminó sin cubrir la vacante: el ingreso directo (FFT) salió No Apto en el EMO y no hay otros candidatos ni long list a la que volver. Para volver a pedir la vacante hay que registrar una nueva solicitud.')
) AS v(codigo, nombre, orden, active, state, descripcion)
WHERE NOT EXISTS (
  SELECT 1 FROM gth_estado_requerimiento e WHERE e.codigo = v.codigo AND e.state
);

COMMIT;

-- Verificación --------------------------------------------------------------
-- SELECT gth_estado_requerimiento_id, codigo, nombre, orden, active, state
-- FROM gth_estado_requerimiento
-- ORDER BY orden, gth_estado_requerimiento_id;
