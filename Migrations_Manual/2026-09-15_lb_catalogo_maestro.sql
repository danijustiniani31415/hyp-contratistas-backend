-- Catálogo Maestro (CONTEXT_LOGISTICA.md sección 6, Fase 1 punto 2).

CREATE TABLE lb_categoria_producto (
    id          SERIAL PRIMARY KEY,
    nombre      VARCHAR(100) NOT NULL,
    tipo        VARCHAR(20) NOT NULL -- EPP, MATERIAL, HERRAMIENTA, EQUIPO
);

CREATE TABLE lb_producto (
    id              BIGSERIAL PRIMARY KEY,
    codigo          VARCHAR(30) UNIQUE,
    nombre          VARCHAR(200) NOT NULL,
    descripcion     TEXT,
    categoria_id    INT NOT NULL REFERENCES lb_categoria_producto(id),
    unidad_medida   VARCHAR(20) NOT NULL,
    requiere_talla  BOOLEAN NOT NULL DEFAULT false,
    es_retornable   BOOLEAN NOT NULL DEFAULT false,
    activo          BOOLEAN NOT NULL DEFAULT true,
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Fuzzy search: similarity() para sugerir productos parecidos antes de crear uno nuevo
-- (evita duplicados tipo "Casco blanco" / "Casco Blanco" / "Casco de seguridad blanco").
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE INDEX idx_producto_nombre_trgm ON lb_producto USING gin (nombre gin_trgm_ops);

INSERT INTO lb_categoria_producto (nombre, tipo) VALUES
('Cascos y protección craneal', 'EPP'),
('Protección respiratoria', 'EPP'),
('Protección visual y facial', 'EPP'),
('Guantes', 'EPP'),
('Calzado de seguridad', 'EPP'),
('Arnés y protección contra caídas', 'EPP'),
('Cemento y agregados', 'MATERIAL'),
('Fierro y acero', 'MATERIAL'),
('Madera y encofrado', 'MATERIAL'),
('Eléctrico', 'MATERIAL'),
('Herramienta manual', 'HERRAMIENTA'),
('Herramienta eléctrica', 'HERRAMIENTA'),
('Equipo de medición', 'EQUIPO'),
('Equipo de izaje', 'EQUIPO');
