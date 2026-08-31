-- Dossier semanal — destrabar semanas congeladas en "Enviado"
--
-- Contexto: hasta este cambio se podían revisar (aprobar/observar) los documentos de
-- una semana que el contratista todavía no enviaba. El recálculo de estado dentro de
-- RevisarDocumentoAsync empujaba la semana a "Enviado" sin que nadie la enviara, y ahí
-- quedaba congelada:
--   * EnviarAsync solo acepta Borrador/Rechazado, así que el contratista no puede enviarla.
--   * SubirDocumentoAsync solo revierte a Borrador desde Aprobado/Observado, no desde Enviado.
--   * SSOMA no puede terminar de aprobarla porque quedan documentos en "Pendiente"
--     (nunca subidos), y no hay nada que aprobar.
-- El mismo estado se alcanzaba borrando el último archivo de un documento con la semana
-- ya enviada (EliminarArchivoAsync dejaba el documento en "Pendiente" sin tocar la semana).
--
-- Ambos caminos quedaron cerrados en el código. Este script repara las filas que ya
-- quedaron en ese estado: las devuelve a "Borrador" para que el contratista complete
-- lo que falta (subir o marcar No Aplica) y vuelva a enviar. Las aprobaciones que ya
-- hizo SSOMA se conservan: los documentos siguen en "Aprobado".
--
-- Solo toca semanas en "Enviado" con al menos un documento "Pendiente", que es un estado
-- inalcanzable por la vía legítima (EnviarAsync bloquea el envío si queda algo pendiente).

-- 1) Previsualización — revisar el listado antes de ejecutar el UPDATE.
SELECT s.id, c.contributor_name, p.project_description, s.anio, s.numero_semana, s.estado,
       count(*) FILTER (WHERE d.estado = 'Pendiente') AS pendientes,
       count(*) FILTER (WHERE d.estado = 'Aprobado')  AS aprobados
FROM ss_dossier_semana s
JOIN ss_dossier_documento d ON d.dossier_id = s.id
LEFT JOIN contributor c ON c.contributor_id = s.contributor_id
LEFT JOIN project p ON p.project_id = s.proyecto_id
WHERE s.estado = 'Enviado'
GROUP BY s.id, c.contributor_name, p.project_description
HAVING count(*) FILTER (WHERE d.estado = 'Pendiente') > 0
ORDER BY s.anio DESC, s.numero_semana DESC;

-- 2) Reparación.
BEGIN;

UPDATE ss_dossier_semana s
SET estado = 'Borrador',
    updated_at = now()
WHERE s.estado = 'Enviado'
  AND EXISTS (
    SELECT 1 FROM ss_dossier_documento d
    WHERE d.dossier_id = s.id AND d.estado = 'Pendiente'
  );

-- Verificar que quede en 0 antes de confirmar.
SELECT count(*) AS congeladas_restantes
FROM ss_dossier_semana s
WHERE s.estado = 'Enviado'
  AND EXISTS (
    SELECT 1 FROM ss_dossier_documento d
    WHERE d.dossier_id = s.id AND d.estado = 'Pendiente'
  );

COMMIT;
