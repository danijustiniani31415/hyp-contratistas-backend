-- Dossier semanal — limpieza de historial: marcar como "No aplica" los documentos
-- que quedaron en "Pendiente" en semanas viejas, y enviarlas a revisión de SSOMA.
--
-- Contexto: el botón "Enviar dossier" exige que los 7 tipos de documento estén en
-- Subido/NA/Aprobado. Muchas contratistas nunca marcaron "No aplica" los tipos que
-- no correspondían esa semana (ej. sin accidentes, sin ATS), así que quedaron
-- semanas en "Borrador" acumuladas desde hace meses sin poder enviarse. El frontend
-- ya se corrigió para mostrar explícitamente qué tipos faltan y así evitar que el
-- problema siga creciendo, pero el historial ya generado necesita limpiarse aparte.
--
-- Este script SOLO toca semanas en "Borrador" que tienen documentos en "Pendiente".
-- No cambia nada en semanas Enviado/Aprobado/Rechazado/Observado/NoAplica, ni toca
-- documentos que ya están Subido/NA/Aprobado/Observado.
--
-- IMPORTANTE: revisar el preview (paso 1) antes de correr el UPDATE. Si algún tipo
-- listado en "tipos_a_marcar_na" en realidad SÍ debería estar subido por esa
-- contratista esa semana, hay que excluir esa fila (agregar condición o avisar al
-- contratista para que la resuelva manualmente) antes de ejecutar el paso 2.

-- 1) Previsualización — semanas afectadas y qué se les va a marcar como N/A.
SELECT s.id AS dossier_id, c.contributor_name, p.project_description,
       s.anio, s.numero_semana, s.estado AS estado_actual,
       string_agg(d.tipo_doc, ', ') FILTER (WHERE d.estado = 'Pendiente') AS tipos_a_marcar_na,
       count(*) FILTER (WHERE d.estado = 'Pendiente') AS cantidad_na
FROM ss_dossier_semana s
JOIN ss_dossier_documento d ON d.dossier_id = s.id
LEFT JOIN contributor c ON c.contributor_id = s.contributor_id
LEFT JOIN project p ON p.project_id = s.proyecto_id
WHERE s.estado = 'Borrador'
GROUP BY s.id, c.contributor_name, p.project_description
HAVING count(*) FILTER (WHERE d.estado = 'Pendiente') > 0
ORDER BY p.project_description, c.contributor_name, s.anio DESC, s.numero_semana DESC;

-- 2) Reparación.
BEGIN;

-- 2a) Marcar como "No aplica" todo documento "Pendiente" de una semana en Borrador.
UPDATE ss_dossier_documento d
SET estado = 'NA',
    updated_at = now()
FROM ss_dossier_semana s
WHERE d.dossier_id = s.id
  AND s.estado = 'Borrador'
  AND d.estado = 'Pendiente';

-- 2b) Enviar esas semanas a revisión de SSOMA (ya no quedan documentos Pendiente).
UPDATE ss_dossier_semana s
SET estado = 'Enviado',
    updated_at = now()
WHERE s.estado = 'Borrador'
  AND EXISTS (SELECT 1 FROM ss_dossier_documento d WHERE d.dossier_id = s.id)
  AND NOT EXISTS (
    SELECT 1 FROM ss_dossier_documento d
    WHERE d.dossier_id = s.id AND d.estado = 'Pendiente'
  );

-- Verificar: no debe quedar ninguna semana Borrador con documentos Pendiente.
SELECT count(*) AS borrador_con_pendientes_restantes
FROM ss_dossier_semana s
WHERE s.estado = 'Borrador'
  AND EXISTS (
    SELECT 1 FROM ss_dossier_documento d
    WHERE d.dossier_id = s.id AND d.estado = 'Pendiente'
  );

COMMIT;
