-- ============================================================================
-- Gestión GTH · Reclutamiento — Enlace de Google Maps del lugar de entrevista
-- Fecha: 2026-08-20
--
-- Nueva columna gth_lugar_entrevista.maps_url: el enlace al mapa del lugar al
-- que se cita al postulante. Va en el catálogo y no como constante en el
-- backend porque es un dato DEL lugar: cuando GTH dé de alta una sala de venta
-- o una obra, su mapa es otro, y un enlace fijo mandaría a todos a la oficina
-- principal.
--
-- El correo de invitación a entrevista lo muestra como "Ver en Google Maps"
-- debajo del nombre del lugar. Si la columna está vacía, el correo sale igual
-- que hoy (solo el nombre), así que un lugar nuevo sin mapa no rompe nada.
--
-- Idempotente. Aplicar en dev y prod.
-- ============================================================================

ALTER TABLE gth_lugar_entrevista
    ADD COLUMN IF NOT EXISTS maps_url text NULL;

COMMENT ON COLUMN gth_lugar_entrevista.maps_url IS
    'Enlace al mapa del lugar (Google Maps). Se muestra en el correo de invitación a la entrevista; null = el correo solo muestra el nombre.';

-- Oficina principal ("Calle Mama Ocllo 2647"), el único lugar del catálogo hoy. Se busca por
-- coincidencia y no por el nombre exacto para que no dependa de cómo esté escrita la dirección
-- en cada base. Solo rellena los que aún no tienen mapa: volver a correrlo no pisa lo cargado.
UPDATE gth_lugar_entrevista
   SET maps_url          = 'https://share.google/XGjPKFZQjFFVVHw6z',
       updated_date_time = now()
 WHERE nombre ILIKE '%Mama Ocllo%'
   AND state
   AND (maps_url IS NULL OR btrim(maps_url) = '');
