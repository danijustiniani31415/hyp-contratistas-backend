-- Carga inicial del catálogo de cargos (lb_cargo estaba vacía — no tenía seed ni CRUD).
-- Extraído de la planilla real de agosto 2026 (columna CARGO, 56 trabajadores, 13 cargos únicos).
-- Idempotente: no duplica si se corre más de una vez.

INSERT INTO lb_cargo (nombre)
SELECT v.nombre FROM (VALUES
    ('ADMINISTRADOR'),
    ('ASIST. ADMINISTRATIVA'),
    ('AYUDANTE PERFORISTA'),
    ('BODEGUERO'),
    ('CONDUCTOR DE CAMIONETA'),
    ('GERENTE DE OPERACIONES'),
    ('INGENIERO DE SEGURIDAD'),
    ('INGENIERO RESIDENTE'),
    ('JEFE DE GUARDIA'),
    ('LIDER PERFORISTA'),
    ('MAESTRO PERFORISTA'),
    ('MECANICO DE MAQUINAS NEUMATICAS'),
    ('SUPERVISOR DE OPERACIONES')
) AS v(nombre)
WHERE NOT EXISTS (SELECT 1 FROM lb_cargo c WHERE c.nombre = v.nombre);
