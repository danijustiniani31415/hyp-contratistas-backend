-- Permite que un mismo proyecto tenga más de un turno en la rotación de inducciones (ej.
-- "Oficina Central" cubierto por dos personas que se alternan) y agrega a quién se asigna cada
-- turno. Ver Features/SsomaModule/InduccionProgramacionFeature/. Ejecutar en pgAdmin.

BEGIN;

-- Ya no se limita a un turno por proyecto: se elimina la unicidad simple por proyecto_id.
DROP INDEX IF EXISTS ux_induccion_rotacion_proyecto;

ALTER TABLE ss_induccion_rotacion_proyecto
    ADD COLUMN IF NOT EXISTS responsable_worker_id integer NULL REFERENCES workers (id);

-- Evita el duplicado accidental exacto (mismo proyecto + mismo responsable dos veces), pero sí
-- permite el mismo proyecto con responsables distintos (o varios turnos sin responsable puntual,
-- ya que NULL nunca choca consigo mismo en un índice único de Postgres).
CREATE UNIQUE INDEX IF NOT EXISTS ux_induccion_rotacion_proyecto_responsable
    ON ss_induccion_rotacion_proyecto (proyecto_id, responsable_worker_id);

COMMIT;
