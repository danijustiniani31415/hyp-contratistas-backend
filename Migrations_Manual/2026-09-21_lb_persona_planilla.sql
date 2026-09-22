-- Fase 1 del motor de Planillas (CONTEXT_LOGISTICA.md, ampliación acordada con el usuario
-- 2026-09-21): datos FIJOS de planilla por persona, 1:1 con lb_persona. Equivale a los campos
-- fijos de hr.contract en Odoo — todavía no hay cálculo (eso es Fase 3: lb_concepto_planilla +
-- lb_planilla_periodo/detalle). Todos los campos son opcionales a propósito: se puede crear una
-- persona sin esta info y completarla después.

CREATE TABLE lb_persona_planilla (
    persona_id          INT PRIMARY KEY REFERENCES lb_persona(id),
    codigo_trabajador    VARCHAR(20),   -- ej. "H&P-E01"
    banco                VARCHAR(30),   -- BCP, BBVA, INTERBANK, SCOTIABANK, MI BANCO, ...
    numero_cuenta        VARCHAR(30),
    cusp                 VARCHAR(30),   -- código único del sistema previsional (o "SIN REGISTRO")
    tipo_afp_onp         VARCHAR(30),   -- ONP, AFP INTEGRA, AFP PRIMA, AFP PROFUTURO, AFP HABITAT, SIN_REGISTRO
    sueldo_base          NUMERIC(10,2), -- mensual
    jornal               NUMERIC(10,2), -- diario, cuando aplica en vez de sueldo mensual
    asignacion_familiar  BOOLEAN NOT NULL DEFAULT false,
    sctr                 BOOLEAN NOT NULL DEFAULT false,
    actualizado_en       TIMESTAMPTZ NOT NULL DEFAULT now()
);
