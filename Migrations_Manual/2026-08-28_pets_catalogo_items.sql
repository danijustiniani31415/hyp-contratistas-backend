-- Catálogo de ítems reusables entre PETS, para las secciones que van con checkbox
-- en vez de texto libre: Marco Legal, EPP (básico/específico según tarea/emergencia)
-- y Recursos (equipo/herramienta/material).
--
-- ssoma_catalogo_item es la lista GLOBAL (compartida entre todos los PETS). Cada
-- PETS elige de ahí en ssoma_pet_item_seleccionado (catalogo_item_id apunta al
-- catálogo). Si un PETS necesita algo que no está en el catálogo, se guarda como
-- ítem propio de ese PETS (catalogo_item_id NULL + descripcion_personalizada) sin
-- tocar el catálogo global.
--
-- "Eliminar" un ítem del catálogo global es desactivarlo (activo = FALSE): deja de
-- aparecer como opción para futuras selecciones pero no rompe los PETS que ya lo
-- tenían seleccionado (el FK sigue siendo válido). "Eliminar" de un PETS puntual es
-- desactivar su fila en ssoma_pet_item_seleccionado, sin tocar el catálogo global.
--
-- Idempotente: se puede correr más de una vez. Ejecutar manualmente en pgAdmin.

BEGIN;

CREATE TABLE IF NOT EXISTS ssoma_catalogo_item (
    id            SERIAL PRIMARY KEY,
    grupo         VARCHAR(20) NOT NULL,   -- marco_legal | epp | recurso
    tipo          VARCHAR(20) NULL,       -- epp: basico|especifico|emergencia ; recurso: equipo|herramienta|material ; marco_legal: NULL
    descripcion   TEXT NOT NULL,
    activo        BOOLEAN NOT NULL DEFAULT TRUE,
    orden         INTEGER NOT NULL DEFAULT 0,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ NULL
);

CREATE INDEX IF NOT EXISTS ix_ssoma_catalogo_item_grupo_tipo ON ssoma_catalogo_item(grupo, tipo);

CREATE TABLE IF NOT EXISTS ssoma_pet_item_seleccionado (
    id                          SERIAL PRIMARY KEY,
    pet_id                      INTEGER NOT NULL REFERENCES ssoma_pet(id),
    grupo                       VARCHAR(20) NOT NULL,
    tipo                        VARCHAR(20) NULL,
    catalogo_item_id            INTEGER NULL REFERENCES ssoma_catalogo_item(id),
    descripcion_personalizada   TEXT NULL,  -- solo cuando catalogo_item_id es NULL (ítem propio de este PETS)
    orden                       INTEGER NOT NULL DEFAULT 0,
    activo                      BOOLEAN NOT NULL DEFAULT TRUE,
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_ssoma_pet_item_seleccionado_pet_grupo ON ssoma_pet_item_seleccionado(pet_id, grupo, tipo);

COMMIT;
