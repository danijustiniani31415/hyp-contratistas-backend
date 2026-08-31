-- Complemento a 2026-08-19_ratio_proyecto_driver.sql: el motor ahora calcula HH y dotacion
-- desde el Tareo real (SsTareo) en vez de los campos tipeados a mano en Project, y necesita
-- guardar cuantos dias de Tareo respaldan cada ratio (para que el responsable pueda juzgar
-- si un proyecto con pocos dias registrados es una muestra confiable o no).

ALTER TABLE ss_ratio_proyecto_driver ADD COLUMN dias_registrados INTEGER NOT NULL DEFAULT 0;
