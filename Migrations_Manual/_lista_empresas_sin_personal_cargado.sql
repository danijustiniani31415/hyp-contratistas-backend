-- Empresas contratistas registradas en el sistema que NO tienen ningún
-- trabajador activo cargado en Habilitación (workers.contrata_casa='Contratista',
-- workers_estado_id=1). Estas son las que hay que completar para que puedan
-- aparecer en cualquier flujo de evaluación (no solo Evaluar Supervisor).

SELECT c.contributor_id, c.contributor_name
FROM contributor c
WHERE NOT EXISTS (
    SELECT 1 FROM workers w
    WHERE w.contributor_id = c.contributor_id
      AND w.contrata_casa = 'Contratista'
      AND w.workers_estado_id = 1
)
ORDER BY c.contributor_name;
