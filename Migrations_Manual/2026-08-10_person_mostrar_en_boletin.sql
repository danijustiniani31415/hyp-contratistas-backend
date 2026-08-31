-- person.mostrar_en_boletin: controla si el cumpleaños de la persona sale en el calendario
-- del boletín (http://localhost:4200/boletin → BirthdayClubFeature).
--
-- Hasta ahora el calendario mostraba a TODO trabajador con email_corporativo @abril.pe y
-- person.fecha_nacimiento cargada, sin forma de excluir a quien no quiere figurar. El nuevo
-- checkbox "Mostrar en el boletín" (modales Nuevo/Editar trabajador de Gestión de Ingresos)
-- guarda esta columna.
--
-- Va en person y no en workers a propósito: es un dato de la persona, no de su ficha laboral,
-- así que una persona con varias fichas (reingreso) mantiene una sola preferencia. Es un flag
-- booleano al estilo de person.active / person.state / workers.sctr, no un estado ni un tipo,
-- por eso no lleva tabla de catálogo.
--
-- NOT NULL DEFAULT true = todos los que hoy aparecen en el calendario siguen apareciendo; el
-- cambio es opt-out, no opt-in. La fecha de nacimiento no se toca: desmarcar el checkbox solo
-- deja de publicarla (EMO y GTH siguen usándola).
--
-- Un solo statement idempotente; corre igual en psql -f y en pgAdmin.

ALTER TABLE person
    ADD COLUMN IF NOT EXISTS mostrar_en_boletin boolean NOT NULL DEFAULT true;

COMMENT ON COLUMN person.mostrar_en_boletin IS
    'true = su cumpleanos (person.fecha_nacimiento) aparece en el calendario del boletin. Lo controla el checkbox "Mostrar en el boletin" del formulario de trabajadores.';

-- Comprobación posterior: total y cuántos quedan visibles (deben coincidir justo después de correrlo).
--   SELECT count(*) AS total, count(*) FILTER (WHERE mostrar_en_boletin) AS visibles FROM person;
