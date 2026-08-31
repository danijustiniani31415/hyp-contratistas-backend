-- Catálogo de tipos de equipo (Volquete, Excavadora de Oruga, ...) y
-- entregables personalizados por tipo. Ejecutar manualmente en pgAdmin.

-- 1) Catálogo de tipos de equipo
CREATE TABLE ss_tipo_equipo (
    id SERIAL PRIMARY KEY,
    nombre VARCHAR(150) NOT NULL UNIQUE,
    orden INT NOT NULL DEFAULT 0,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- 2) Sembrar el catálogo con los valores de texto que ya existen en ss_equipo.tipo
INSERT INTO ss_tipo_equipo (nombre)
SELECT DISTINCT trim(tipo)
FROM ss_equipo
WHERE tipo IS NOT NULL AND trim(tipo) <> ''
ON CONFLICT (nombre) DO NOTHING;

-- 3) ss_equipo: agregar la FK, migrar los datos y retirar la columna de texto
ALTER TABLE ss_equipo ADD COLUMN tipo_equipo_id INT REFERENCES ss_tipo_equipo(id);

UPDATE ss_equipo e
SET tipo_equipo_id = t.id
FROM ss_tipo_equipo t
WHERE trim(e.tipo) = t.nombre;

-- Verificar antes de continuar que no haya quedado ninguna fila sin mapear:
-- SELECT id, tipo FROM ss_equipo WHERE tipo_equipo_id IS NULL;

ALTER TABLE ss_equipo ALTER COLUMN tipo_equipo_id SET NOT NULL;
ALTER TABLE ss_equipo DROP COLUMN tipo;

-- 4) ss_item_equipo: relación opcional al tipo de equipo.
--    NULL = ítem genérico, se exige a TODOS los equipos (caso más común hoy).
--    Con valor = ítem específico de ese tipo (ej. solo para "Volquete").
ALTER TABLE ss_item_equipo ADD COLUMN tipo_equipo_id INT REFERENCES ss_tipo_equipo(id);

-- Ejemplo para marcar un ítem como específico de un tipo (ajustar ids reales):
-- UPDATE ss_item_equipo SET tipo_equipo_id = (SELECT id FROM ss_tipo_equipo WHERE nombre = 'Volquete')
-- WHERE id IN (/* ids de los ítems propios de volquete */);
