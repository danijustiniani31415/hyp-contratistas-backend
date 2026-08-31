-- Reinicia el calendario de inducciones (generado durante las pruebas, con el orden viejo y
-- sin responsables) para que arranque limpio desde el VIERNES 21 de agosto de 2026 con el
-- orden y los responsables ya definidos en ss_induccion_rotacion_proyecto. Ejecutar en pgAdmin,
-- una sola vez, antes de que el cron de avisos empiece a correr en serio.

BEGIN;

-- Borra las fechas que se generaron solo para probar la pantalla.
DELETE FROM ss_induccion_programacion;

-- Deja el cursor apuntando a "nada generado aún, el próximo turno es el primero de la lista",
-- y la última fecha en 2026-08-20 (jueves) para que la generación arranque el 2026-08-21 (viernes).
UPDATE ss_induccion_rotacion_cursor
SET ultimo_proyecto_rotacion_id = NULL,
    ultima_fecha_generada = '2026-08-20',
    updated_at = now()
WHERE id = 1;

-- Si la fila del cursor no existe todavía (nunca se generó nada), la crea.
INSERT INTO ss_induccion_rotacion_cursor (id, ultimo_proyecto_rotacion_id, ultima_fecha_generada, updated_at)
SELECT 1, NULL, '2026-08-20', now()
WHERE NOT EXISTS (SELECT 1 FROM ss_induccion_rotacion_cursor WHERE id = 1);

COMMIT;
