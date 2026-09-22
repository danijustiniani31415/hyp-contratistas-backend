-- Fase 2 del motor de Planillas (acordado con el usuario 2026-09-21): tareo/asistencia diaria,
-- reemplaza las 31 columnas de días de la planilla Excel. Equivale a hr.attendance en Odoo.
-- Un registro por persona por día. tipo_dia además de NORMAL trae los códigos reales que ya usa
-- H&P en su Excel: DL (descanso libre), F (falta), P (permiso), VC (vacaciones), DM (descanso médico).

CREATE TABLE lb_tareo (
    id                  BIGSERIAL PRIMARY KEY,
    persona_id          INT NOT NULL REFERENCES lb_persona(id),
    fecha               DATE NOT NULL,
    tipo_dia            VARCHAR(10) NOT NULL DEFAULT 'NORMAL', -- NORMAL, DL, F, P, VC, DM
    horas_trabajadas    NUMERIC(4,2), -- solo tiene sentido con tipo_dia = NORMAL
    horas_extra         NUMERIC(4,2) NOT NULL DEFAULT 0,
    registrado_por_usuario_sistema_id BIGINT,
    creado_en           TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (persona_id, fecha)
);
CREATE INDEX idx_tareo_persona_fecha ON lb_tareo(persona_id, fecha);
