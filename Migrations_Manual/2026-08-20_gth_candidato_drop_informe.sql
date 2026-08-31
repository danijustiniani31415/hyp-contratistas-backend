-- ═══════════════════════════════════════════════════════════════════════════
-- Long list: se elimina el informe del postulante (hard delete de columnas)
-- ═══════════════════════════════════════════════════════════════════════════
--
-- GTH pidió retirar el "Informe del candidato" que se adjuntaba, opcionalmente,
-- junto al CV al cargar la long list. Ya no se captura en la carga de GTH, ya
-- no se adjunta al correo del solicitante, ya no se sube a SharePoint y ya no
-- se sirve al frontend: el único archivo del candidato queda siendo su CV.
--
-- Es un DROP COLUMN y no un soft delete a propósito: la regla de auditoría de
-- la base protege FILAS (state = false), no columnas. Al dejar de capturarse el
-- dato, la columna no aporta trazabilidad de nada que siga vivo — solo cuatro
-- columnas muertas que el próximo que lea gth_candidato tendría que descartar.
--
-- OJO — los archivos ya subidos NO se borran de SharePoint. Viven en la carpeta
-- de reclutamiento (gth_sustento_folder → "Long list REQ-AAAA-NNNN") con el
-- prefijo informe_. Si GTH quiere purgarlos, es una limpieza manual aparte;
-- este script solo suelta la referencia desde la base.
--
-- Idempotente: se puede correr más de una vez sin error.
-- ═══════════════════════════════════════════════════════════════════════════

BEGIN;

ALTER TABLE gth_candidato
    DROP COLUMN IF EXISTS informe_nombre,
    DROP COLUMN IF EXISTS informe_url,
    DROP COLUMN IF EXISTS informe_item_id,
    DROP COLUMN IF EXISTS informe_drive_id;

COMMIT;
