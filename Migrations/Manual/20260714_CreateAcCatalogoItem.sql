-- Migración manual (pgAdmin) — catálogos de Observaciones (Partida, Área
-- Responsable). Antes eran texto libre / valores distintos ya usados; ahora
-- son listas curadas, editables desde un modal en el frontend.

CREATE TABLE IF NOT EXISTS ac_catalogo_items (
    id          SERIAL PRIMARY KEY,
    tipo        VARCHAR(30)  NOT NULL,
    nombre      VARCHAR(150) NOT NULL,
    orden       INT          NOT NULL DEFAULT 0,
    activo      BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_ac_catalogo_items_tipo ON ac_catalogo_items (tipo);

-- Seed: Partidas (PartidasArqComercial.csv)
INSERT INTO ac_catalogo_items (tipo, nombre, orden, activo)
SELECT 'Partida', v.nombre, v.orden, TRUE
FROM (VALUES
    ('Carpintería en madera', 1),
    ('Albañilería', 2),
    ('Acero', 3),
    ('Pintura', 4),
    ('Estructura Metalica', 5),
    ('Drywall', 6),
    ('Melamina', 7),
    ('Sanitario', 8),
    ('Instalaciones eléctricas', 9),
    ('Áreas Verdes', 10),
    ('Piso Laminado', 11),
    ('Enchape', 12),
    ('Puertas', 13),
    ('Tablero de piedra', 14),
    ('Aire Acondicionado', 15),
    ('Papel Mural', 16),
    ('Piso SPC / Zócalo', 17),
    ('Decoración', 18),
    ('Accesorios', 19),
    ('Vidrio / Mampara / Espejo', 20),
    ('Área de Marketing', 21),
    ('Área de TI', 22),
    ('Almacenamiento', 23),
    ('Ssoma', 24),
    ('Limpieza', 25),
    ('Mobiliario', 26),
    ('Casevip', 27),
    ('Rollers / Cortina', 28),
    ('Otros', 29),
    ('Área de ventas', 30),
    ('Equipamiento', 31)
) AS v(nombre, orden)
WHERE NOT EXISTS (
    SELECT 1 FROM ac_catalogo_item WHERE tipo = 'Partida' AND nombre = v.nombre
);

-- Seed: Área Responsable (AreaResponsable (1).csv)
INSERT INTO ac_catalogo_item (tipo, nombre, orden, activo)
SELECT 'AreaResponsable', v.nombre, v.orden, TRUE
FROM (VALUES
    ('--', 1),
    ('Arquitectura Comercial', 2),
    ('Mantenimiento', 3),
    ('Área de Marketing', 4),
    ('Área de TI', 5)
) AS v(nombre, orden)
WHERE NOT EXISTS (
    SELECT 1 FROM ac_catalogo_item WHERE tipo = 'AreaResponsable' AND nombre = v.nombre
);
