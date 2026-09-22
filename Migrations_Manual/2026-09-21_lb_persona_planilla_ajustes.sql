-- Ajustes a Fase 1 de Planillas pedidos por el usuario 2026-09-21:
-- 1) categoria_laboral (OBRERO/EMPLEADO) — el Excel real de H&P separa boleta de obrero y de
--    empleado (hojas BOLT_OBR / BOLET_EMPL), son regímenes distintos (CTS, gratificación, etc.)
-- 2) codigo_trabajador pasa a ser correlativo autogenerado (H&P-E001, H&P-E002...), no texto libre.

ALTER TABLE lb_persona_planilla ADD COLUMN IF NOT EXISTS categoria_laboral VARCHAR(10);

CREATE SEQUENCE IF NOT EXISTS lb_trabajador_correlativo START 1;
