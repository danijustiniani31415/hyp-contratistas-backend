-- PILOTO: catálogo de pasos del PETS (ssoma_pet_paso), para que OPT (y a futuro
-- Estándares/IPERC con el mismo patrón) jalen automáticamente la estructura del PETS
-- en vez de tipearla a mano en cada observación. "orden" es solo clave de ordenamiento
-- interna: el número que ve el usuario se calcula por posición en el frontend/DTO,
-- nunca se guarda como texto fijo, así insertar un paso en medio no requiere
-- renumerar nada a mano (solo se corren los "orden" >= la posición insertada).
--
-- Ejecutar manualmente en pgAdmin contra la base local para probar, y luego contra
-- producción cuando se valide. Idempotente: se puede correr más de una vez.

BEGIN;

ALTER TABLE ssoma_pet ADD COLUMN IF NOT EXISTS updated_at timestamptz NULL;

CREATE TABLE IF NOT EXISTS ssoma_pet_paso (
    id            SERIAL PRIMARY KEY,
    pet_id        INTEGER NOT NULL REFERENCES ssoma_pet(id),
    descripcion   TEXT NOT NULL,
    imagen_url    TEXT NULL,
    orden         INTEGER NOT NULL DEFAULT 1,
    activo        BOOLEAN NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at    TIMESTAMPTZ NULL
);

CREATE INDEX IF NOT EXISTS ix_ssoma_pet_paso_pet_id ON ssoma_pet_paso(pet_id);

-- Feature de la pantalla "PETS" (SSOMA → Gestión), otorgada a los mismos roles que
-- ya administran SSOMA: Jefe SSOMA (9), Coordinador SSOMA (70), Prevencionista (72)
-- y Administrador del Sistema (1) para poder probarlo de inmediato en local.
INSERT INTO feature (feature_key, module_id)
SELECT 'ssoma.gestion.pets', m.module_id
FROM module m
WHERE m.module_name = 'SSOMA'
  AND NOT EXISTS (
      SELECT 1 FROM feature WHERE feature_key = 'ssoma.gestion.pets'
  );

INSERT INTO role_feature (role_id, feature_id)
SELECT r.role_id, f.feature_id
FROM role r
CROSS JOIN feature f
WHERE f.feature_key = 'ssoma.gestion.pets'
  AND r.role_id IN (1, 9, 70, 72) -- Administrador del Sistema, Jefe SSOMA, Coordinador SSOMA, Prevencionista
  AND NOT EXISTS (
      SELECT 1 FROM role_feature rf WHERE rf.role_id = r.role_id AND rf.feature_id = f.feature_id
  );

COMMIT;

-- Verificar después de correr (opcional):
-- SELECT r.role_id, r.role_description, f.feature_key
-- FROM role_feature rf
-- JOIN role r ON r.role_id = rf.role_id
-- JOIN feature f ON f.feature_id = rf.feature_id
-- WHERE f.feature_key = 'ssoma.gestion.pets';
