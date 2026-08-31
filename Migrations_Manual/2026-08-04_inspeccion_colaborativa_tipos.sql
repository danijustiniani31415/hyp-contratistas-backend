-- Tipos de inspección colaborativa (gerencial/cruzada/coordinadores SSOMA):
-- sin checklist fijo, varios coordinadores agregan hallazgos sueltos al mismo
-- registro mientras esté "Abierta". Ver 2026-08-04_inspeccion_colaborativa.sql.

INSERT INTO ssoma_inspeccion_tipo (nombre, ambito, activo, es_colaborativa, created_at)
VALUES
    ('Inspección Gerencial', 'Seguridad', true, true, now()),
    ('Inspección Cruzada', 'Seguridad', true, true, now()),
    ('Coordinadores SSOMA', 'Seguridad', true, true, now());
