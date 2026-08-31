-- ============================================================================
-- Gestión GTH · Solicitud de Personal — Salario bruto mensual por vacante
--
-- El área usuaria pidió que cada «Puesto solicitado N» del modal «Nueva solicitud
-- de personal» declare el salario bruto mensual de la vacante. Es un dato por
-- vacante y no por solicitud: dos vacantes de la misma solicitud pueden ser de
-- puestos distintos y cobrar distinto, así que vive en gth_requerimiento.
--
-- Se muestra en:
--   · Modal de decisión de Aprobaciones GG: es lo que el gerente del área y
--     Gerencia General aprueban junto con la vacante.
--   · Correo a los gerentes y correo a GTH: columna «Salario bruto».
--   · Detalle del requerimiento (GTH): línea «Salario bruto mensual».
--   · Seguimiento del solicitante: tarjeta «Salario bruto mensual».
--
-- A propósito NO va en el correo a TI: TI recibe el aviso para alistar equipo,
-- usuario y accesos, y la remuneración no es parte de eso.
--
-- NULLABLE aunque el formulario lo exija: los 29 requerimientos ya registrados
-- (y los de producción) no tienen el dato, y un NOT NULL dejaría el histórico
-- inconsistente además de romper cualquier INSERT que no lo mande. La
-- obligatoriedad vive en el backend (ReclutamientoService.Create), igual que
-- gth_solicitud.justificacion.
--
-- numeric(12,2) como el resto del dinero del sistema (ga_trayecto.monto,
-- ga_solicitud_captura.monto). El CHECK deja fuera el 0 y los negativos: un
-- salario declarado en 0 no es un salario, es un dato sin llenar — y para eso
-- está NULL.
--
-- Idempotente: se puede correr más de una vez.
-- ============================================================================

BEGIN;

ALTER TABLE gth_requerimiento
    ADD COLUMN IF NOT EXISTS salario_bruto_mensual numeric(12,2) NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_gth_requerimiento_salario_bruto_mensual'
    ) THEN
        ALTER TABLE gth_requerimiento
            ADD CONSTRAINT ck_gth_requerimiento_salario_bruto_mensual
            CHECK (salario_bruto_mensual IS NULL OR salario_bruto_mensual > 0);
    END IF;
END $$;

COMMENT ON COLUMN gth_requerimiento.salario_bruto_mensual IS
    'Salario bruto mensual declarado para la vacante, en soles. Obligatorio en el formulario de solicitud desde 2026-08; NULL en los requerimientos anteriores a este campo.';

COMMIT;
