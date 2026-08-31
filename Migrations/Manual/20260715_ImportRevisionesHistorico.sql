-- Migración manual (pgAdmin) — importa el catálogo histórico de revisiones
-- (DBRevisionesComercial.csv, app legacy en Power Apps). Ejecutar directamente
-- contra la BD PostgreSQL. No usar dotnet ef.
--
-- OJO: los IDProyecto del CSV son IDs internos de la lista de SharePoint,
-- NO los project_id reales de Abril (confirmado con datos reales: Kaurí es
-- IDProyecto=43 en SharePoint pero project_id=7 en Abril). Por eso acá se
-- une por NOMBRE de proyecto contra la tabla project real, no por ID —
-- así el join se autovalida: si un nombre no coincide exacto, esa fila
-- simplemente no se inserta (ver query de verificación al final).

INSERT INTO ac_revisiones (proyecto_id, tipo, lugar, nombre, activo, created_at)
SELECT p.project_id, v.tipo, v.lugar, v.nombre_completo, TRUE, (now() AT TIME ZONE 'utc')
FROM (VALUES
    ('9 NOGALES',    'R1',    'Sala de ventas', 'R1-9 Nogales-Sala de ventas'),
    ('9 NOGALES',    'R2',    'Sala de ventas', 'R2-9 Nogales-Sala de ventas'),
    ('BUGAMBILIAS',  'R1',    'Sala de ventas', 'R1-Bugambilias-Sala de ventas'),
    ('BUGAMBILIAS',  'R2',    'Sala de ventas', 'R2-Bugambilias-Sala de ventas'),
    ('KAURÍ',        'R1',    'Pilotos',        'R1-Kauri-Pilotos'),
    ('KAURÍ',        'R2',    'Pilotos',        'R2-Kauri-Pilotos'),
    ('CAMELIA',      'R1',    'Areas comunes',  'R1-Camelia-Areas comunes'),
    ('SAUCE ZEN',    'R1',    'Sala de ventas', 'R1-Sauce Zen-Sala de ventas'),
    ('KAURÍ',        'R1-AC', 'Areas comunes',  'R1-AC-Kauri-Areas comunes'),
    ('KAURÍ',        'R1',    'Sala de ventas', 'R1-Kauri-Sala de ventas'),
    ('AMARANTA',     'R1',    'Pilotos',        'R1-Amaranta-Pilotos'),
    ('CEDRO 33',     'R1',    'Pilotos',        'R1-Cedro 33-Pilotos'),
    ('CEDRO 33',     'R1',    'Sala de ventas', 'R1-Cedro 33-Sala de ventas'),
    ('CEDRO 33',     'R2',    'Sala de ventas', 'R2-Cedro 33-Sala de ventas'),
    ('CEDRO 33',     'R2',    'Pilotos',        'R2-Cedro 33-Pilotos'),
    ('CAPULÍ',       'R1-AC', 'Areas comunes',  'R1-AC-Capuli-Areas comunes'),
    ('EUCALIPTO',    'R1-AC', 'Areas comunes',  'R1-AC-Eucalipto-Areas comunes'),
    ('CAPULÍ',       'R2-AC', 'Areas comunes',  'R2-AC-Capuli-Areas comunes'),
    ('EUCALIPTO',    'R2-AC', 'Areas comunes',  'R2-AC-Eucalipto-Areas comunes'),
    ('SAUCE ZEN',    'R2',    'Sala de ventas', 'R2-Sauce Zen-Sala de ventas'),
    ('BOSQUE REAL',  'R1',    'Sala de ventas', 'R1-Bosque Real-Sala de ventas'),
    ('BOSQUE REAL',  'R2',    'Sala de ventas', 'R2-Bosque Real-Sala de ventas')
) AS v(proyecto_nombre, tipo, lugar, nombre_completo)
JOIN project p ON upper(p.project_description) = v.proyecto_nombre
LEFT JOIN ac_revisiones existente ON existente.nombre = v.nombre_completo
WHERE existente.id IS NULL;

-- Verificación: debe devolver 22 filas. Si devuelve menos, alguno de los
-- nombres de arriba no coincidió exacto contra project_description.
SELECT count(*) AS revisiones_importadas FROM ac_revisiones;
