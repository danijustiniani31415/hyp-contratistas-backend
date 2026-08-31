-- worker_emos.requiere_lectura_abril: marca los EMOs cuya lectura la hace el médico
-- ocupacional de Abril Grupo Inmobiliario en vez de la clínica.
--
-- Antes, la lectura de un EMO (fecha_lectura + url_resultado) siempre la subía la clínica
-- al completar el EMO (o, más raro, se re-subía después vía "Documentos"). Ahora hay casos
-- donde la clínica no hace la lectura y queda en manos del médico interno. Se necesita un
-- flag para: (1) que el formulario de la clínica / el modal de edición puedan marcar "esto lo
-- lee el médico de Abril" en vez de exigir el archivo ahí mismo, y (2) que la pantalla de EMOs
-- pueda listar, en una subtab aparte, los EMOs pendientes de esa lectura interna.
--
-- "Pendiente de lectura por Abril" = requiere_lectura_abril = true AND url_resultado IS NULL
-- (no hace falta un segundo flag de "completado": se reutiliza url_resultado, el mismo campo
-- que ya usa el filtro existente "Sin Lectura EMO").
--
-- NOT NULL DEFAULT false: todos los EMOs existentes siguen con la lectura a cargo de la
-- clínica (comportamiento actual); el flag se activa explícitamente caso por caso.

ALTER TABLE worker_emos
    ADD COLUMN IF NOT EXISTS requiere_lectura_abril boolean NOT NULL DEFAULT false;

COMMENT ON COLUMN worker_emos.requiere_lectura_abril IS
    'true = la lectura de este EMO la hace el medico ocupacional de Abril Grupo Inmobiliario, no la clinica. Pendiente de lectura mientras sea true y url_resultado sea NULL.';
