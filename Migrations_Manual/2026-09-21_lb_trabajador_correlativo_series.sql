-- Corrige el correlativo de codigo_trabajador: EMPLEADO y OBRERO son series INDEPENDIENTES en la
-- empresa real (el Excel real trae H&P-E01..E13 por un lado y H&P-O01..O51 por otro), no una sola
-- numeracion compartida. El seed anterior (seed_personas_planilla_agosto.sql) dejo la unica
-- secuencia que existia (lb_trabajador_correlativo) en 51 -- mezclando ambas series -- porque en
-- ese momento el sistema solo generaba codigos "H&P-E###" para todos. Ya se corrigio el codigo
-- (PersonaService.Create/ActualizarPlanilla) para usar dos secuencias segun categoria_laboral;
-- esta migracion deja los NUMEROS de cada secuencia en su maximo real.
-- Idempotente: seguro correr mas de una vez.

-- Serie EMPLEADO (H&P-E##) -- vuelve a su maximo real, no al 51 que mezclaba las dos series.
SELECT setval('lb_trabajador_correlativo', 13, true);

-- Serie OBRERO (H&P-O##) -- nueva secuencia, arranca en su maximo real.
CREATE SEQUENCE IF NOT EXISTS lb_trabajador_correlativo_obrero START 1;
SELECT setval('lb_trabajador_correlativo_obrero', 51, true);
