-- ============================================================================
-- Planeamiento BIM · Carga Diaria — cumplimiento parcial por porcentaje
--
-- Punto #3 de las observaciones de Planeamiento (prioridad alta), 4 decisiones
-- confirmadas con el ingeniero de Planeamiento el 2026-08-25:
--   1. Migrar la columna existente (no agregar una nueva): bim_registro_diario.
--      cumplida (boolean) -> porcentaje_avance (numeric).
--   2. Backfill en el mismo script: true->100, false->0, sin dejar null.
--   3. Causa obligatoria si porcentaje_avance < 100 (umbral estricto, sin relajar).
--   4. Set fijo de valores permitidos (0/25/50/75/100) — validado en
--      PlaneamientoBimCargaDiariaService.PorcentajesValidos, NO como CHECK acá:
--      así ajustar el set en el futuro no exige una migración.
--
-- No pasa por una migración EF: el model snapshot (Migrations/) tiene deuda
-- acumulada de varias sesiones/desarrolladores sin `dotnet ef migrations add`
-- corrido — generar una migración acá arrastraba ~2300 líneas de diffs no
-- relacionados (incluidos DROP TABLE de otras features). Este script es la
-- fuente de verdad del cambio real, como el resto de Migrations_Manual/.
--
-- Backfill verificado ANTES de escribir este script, de solo lectura contra los
-- datos reales (3 registros en producción al momento de escribir esto: 2
-- cumplida=true, 1 cumplida=false): se comparó, agrupado por zona/nivel/sector
-- (como GetAvance), por fecha (como GetPpcHistorico) y por macro_actividad (como
-- GetPlanMaestro), la fórmula vieja (COUNT(cumplida=true)*100/COUNT(*)) contra la
-- fórmula nueva simulada (AVG(CASE WHEN cumplida THEN 100 ELSE 0 END)) — 0
-- discrepancias en los 3 agrupamientos. El backfill no altera ningún número ya
-- reportado históricamente.
--
-- El código (BimRegistroDiario.cs, CargaDiariaDtos.cs,
-- PlaneamientoBimCargaDiariaRepository/Service.cs,
-- PlaneamientoBimDashboardRepository.cs, PlaneamientoBimPortafolioRepository.cs,
-- PlaneamientoBimReportePdfService.cs) ya está migrado a PorcentajeAvance — este
-- SQL es lo único que falta para que quede funcional de punta a punta.
--
-- NO idempotente en el sentido de "correr 2 veces sin efecto" (un ALTER COLUMN
-- no admite eso), pero sí seguro: si ya se corrió, la segunda corrida falla con
-- "column cumplida does not exist" en vez de dañar el dato.
--
-- Plan de reversión: todo el script corre en una sola transacción — si algo
-- falla a mitad de camino, PostgreSQL hace DDL transaccional (a diferencia de
-- MySQL) y revierte automáticamente TODO (incluido el backup) con un ROLLBACK
-- implícito. Eso cubre el caso "falla durante el ALTER".
-- El caso que NO cubre el rollback automático es "el script corrió y confirmó
-- bien, pero días después notamos que el backfill no era lo que queríamos" —
-- para ese caso queda la tabla de respaldo del Paso 0. Reversión manual con eso:
--   BEGIN;
--   ALTER TABLE bim_registro_diario DROP COLUMN porcentaje_avance;
--   ALTER TABLE bim_registro_diario ADD COLUMN cumplida boolean;
--   UPDATE bim_registro_diario r SET cumplida = b.porcentaje_avance = 100
--     FROM bim_registro_diario_backup_20260825 b WHERE b.id = r.id;
--   ALTER TABLE bim_registro_diario ALTER COLUMN cumplida SET NOT NULL;
--   COMMIT;
-- (Solo son 3 registros hoy — bajo riesgo real — pero el backup se hace igual:
-- es la práctica correcta para cuando esta tabla tenga volumen real.)
-- ============================================================================

BEGIN;

-- Paso 0: respaldo completo de la tabla, previo a tocar nada. Vive fuera de la
-- migración normal — es un artefacto de seguridad, se puede borrar (DROP TABLE
-- bim_registro_diario_backup_20260825) una vez confirmado que todo quedó bien.
CREATE TABLE bim_registro_diario_backup_20260825 AS
SELECT * FROM bim_registro_diario;

-- USING hace el cast Y el backfill en un solo paso: true->100, false->0. No hay
-- pérdida de información — el booleano no tenía más granularidad que capturar.
ALTER TABLE bim_registro_diario
    ALTER COLUMN cumplida TYPE numeric
    USING (CASE WHEN cumplida THEN 100 ELSE 0 END);

ALTER TABLE bim_registro_diario
    RENAME COLUMN cumplida TO porcentaje_avance;

COMMIT;

-- ── Verificación ────────────────────────────────────────────────────────────
-- SELECT id, porcentaje_avance, causa_id FROM bim_registro_diario ORDER BY id;
-- Todos los porcentaje_avance deben ser 100 o 0 (no hay valores parciales
-- históricos todavía) y coincidir 1:1 con lo que antes decía cumplida.
--
-- SELECT count(*) FROM bim_registro_diario_backup_20260825;
-- Debe devolver el mismo número de filas que tenía bim_registro_diario antes
-- del ALTER (3, al momento de escribir esto).
