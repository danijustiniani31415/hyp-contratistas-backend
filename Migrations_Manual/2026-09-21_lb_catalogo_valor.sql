-- Catálogo genérico para listas fijas de opciones (banco, tipo AFP/ONP, categoría laboral, y
-- cualquier otra que aparezca) — pedido explícito del usuario 2026-09-21: "todo debe ser
-- modificable desde el frontend", nada de <option> hardcodeado en el HTML que obligue a tocar
-- código para agregar un valor nuevo. Cargo/TipoVinculo/EmpresaContratista NO entran acá porque
-- ya son tablas propias con integridad referencial (FK desde lb_vinculo_laboral) — este catálogo
-- es solo para texto plano sin relaciones, como categoria_laboral/banco/tipo_afp_onp hoy.

CREATE TABLE lb_catalogo_valor (
    id      SERIAL PRIMARY KEY,
    tipo    VARCHAR(40) NOT NULL,   -- CATEGORIA_LABORAL, BANCO, TIPO_AFP_ONP, ...
    valor   VARCHAR(100) NOT NULL,
    orden   INT NOT NULL DEFAULT 0,
    activo  BOOLEAN NOT NULL DEFAULT true,
    UNIQUE (tipo, valor)
);

INSERT INTO lb_catalogo_valor (tipo, valor, orden) VALUES
    ('CATEGORIA_LABORAL', 'OBRERO', 1),
    ('CATEGORIA_LABORAL', 'EMPLEADO', 2),
    ('BANCO', 'BCP', 1),
    ('BANCO', 'BBVA', 2),
    ('BANCO', 'INTERBANK', 3),
    ('BANCO', 'SCOTIABANK', 4),
    ('BANCO', 'MI BANCO', 5),
    ('TIPO_AFP_ONP', 'ONP', 1),
    ('TIPO_AFP_ONP', 'AFP INTEGRA', 2),
    ('TIPO_AFP_ONP', 'AFP PRIMA', 3),
    ('TIPO_AFP_ONP', 'AFP PROFUTURO', 4),
    ('TIPO_AFP_ONP', 'AFP HABITAT', 5)
ON CONFLICT (tipo, valor) DO NOTHING;
