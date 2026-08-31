-- ============================================================================
-- Gestión GTH · Solicitud de Personal — Baja de la fecha requerida de ingreso
--
-- El área usuaria pidió que la «Fecha requerida de ingreso» deje de pedirse en el
-- modal «Nueva solicitud de personal». Como el dato ya no se captura en ningún
-- otro lado, se elimina la columna y se saca de todo lo que la mostraba:
--
--   · Bandeja de Reclutamiento (GTH): columna «Fecha requerida» de la tabla y el
--     bloque de fechas de las tarjetas.
--   · Detalle del requerimiento (GTH): línea «Fecha requerida:».
--   · Modal de decisión de Aprobaciones GG: el chip de la fecha en cada vacante.
--   · Correos: la columna «Ingreso requerido» de las tres tablas de Aprobaciones
--     GG (gerentes, TI y GTH) y la línea de la fecha en el correo de long list.
--   · Onboarding: «Nuevo ingreso» ya no propone una fecha; GTH la escribe a mano
--     (gth_onboarding.fecha_ingreso siempre fue nullable, así que nada se rompe).
--
-- No se toca gth_requerimiento.categoria_id: la llenaba el modo «Puesto
-- personalizado» del mismo formulario, que también se dio de baja en este cambio.
-- Se queda congelada con los datos que ya tiene (auditoría) y en los
-- requerimientos nuevos entra NULL — nada del backend la lee.
--
-- Tampoco se toca gth_solicitud.justificacion: pasó a ser obligatoria, pero la
-- validación vive en el backend (ReclutamientoService.Create) y no en un NOT NULL,
-- porque las solicitudes ya registradas pueden tener NULL y un NOT NULL las
-- dejaría inconsistentes con el histórico.
--
-- Ojo: al no haber fecha, el correo a TI dejó de tener «para cuándo» y su texto se
-- reescribió — ahora dice que GTH confirmará la fecha al cerrar el proceso.
--
-- Idempotente: se puede correr más de una vez.
-- ============================================================================

BEGIN;

ALTER TABLE gth_requerimiento
    DROP COLUMN IF EXISTS fecha_requerida_ingreso;

COMMENT ON COLUMN gth_requerimiento.categoria_id IS
    'Categoria real declarada para la vacante. Congelada para auditoria: la llenaba el modo "Puesto personalizado" del formulario de solicitud, dado de baja. En los requerimientos nuevos queda NULL y quien contrate al seleccionado cae a puesto.categoria_id.';

COMMIT;
