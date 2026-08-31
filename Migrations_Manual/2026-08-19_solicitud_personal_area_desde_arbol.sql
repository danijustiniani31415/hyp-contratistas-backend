-- ============================================================================
-- Solicitud de Personal — "Área del solicitante" desde el árbol de áreas
-- ============================================================================
-- El campo se llenaba con workers.area, texto plano del padrón viejo que dejó de
-- mantenerse: la ficha de un trabajador de "Tecnología de la Información" decía
-- "Proyectos", y antes decía "Administración". Ahora el nombre se resuelve en
-- código desde workers.area_scope_id (ReclutamientoRepository.ResolveAreaNombreInternal):
-- se sube por el árbol hasta el primer nodo que no sea de tipo "Área de Gerencia".
--
-- El cambio NO toca el esquema. Este script es solo el BACKFILL OPCIONAL del
-- snapshot gth_solicitud.area_nombre de las solicitudes ya registradas, que
-- quedaron con el nombre congelado de cuando se crearon. Al 2026-08-19 en prod
-- son 5 filas, todas con area_scope_id = 61 y area_nombre = 'Administración'.
--
-- Sin este backfill, el nuevo nombre aplica solo a las solicitudes futuras y el
-- Seguimiento sigue mostrando el área vieja en las 5 existentes.
--
-- Ejecutar en PRODUCCIÓN. Idempotente: solo escribe donde el valor difiere.
-- ============================================================================

BEGIN;

-- Primer nodo de cada rama que no es "Área de Gerencia", subiendo desde cada
-- area_scope. Misma regla que aplica el backend.
WITH RECURSIVE cadena AS (
    SELECT s.area_scope_id AS origen,
           s.area_scope_id,
           s.area_scope_parent_id,
           ai.area_item_name,
           at.area_type_name,
           0 AS nivel
    FROM area_scope s
    JOIN area_item ai ON ai.area_item_id = s.area_item_id
    JOIN area_type at ON at.area_type_id = ai.area_type_id
    WHERE s.state AND ai.state

    UNION ALL

    SELECT c.origen,
           s.area_scope_id,
           s.area_scope_parent_id,
           ai.area_item_name,
           at.area_type_name,
           c.nivel + 1
    FROM cadena c
    JOIN area_scope s ON s.area_scope_id = c.area_scope_parent_id
    JOIN area_item ai ON ai.area_item_id = s.area_item_id
    JOIN area_type at ON at.area_type_id = ai.area_type_id
    WHERE s.state AND ai.state
),
-- El nodo elegido: el más cercano al trabajador que no sea gerencia. Si toda la
-- rama es de gerencia (el caso del gerente, que cuelga directo de su gerencia),
-- gana el nodo propio — igual que el respaldo del backend.
resuelto AS (
    SELECT DISTINCT ON (origen) origen, area_item_name
    FROM cadena
    WHERE area_type_name <> 'Área de Gerencia'
    ORDER BY origen, nivel
),
propio AS (
    SELECT s.area_scope_id AS origen, ai.area_item_name
    FROM area_scope s
    JOIN area_item ai ON ai.area_item_id = s.area_item_id
)
UPDATE gth_solicitud g
   SET area_nombre = COALESCE(r.area_item_name, p.area_item_name)
  FROM propio p
  LEFT JOIN resuelto r ON r.origen = p.origen
 WHERE p.origen = g.area_scope_id
   AND g.area_scope_id IS NOT NULL
   AND g.area_nombre IS DISTINCT FROM COALESCE(r.area_item_name, p.area_item_name);

-- Control: no debe quedar ninguna solicitud con área desalineada del árbol.
SELECT gth_solicitud_id, area_scope_id, area_nombre
FROM gth_solicitud
ORDER BY gth_solicitud_id;

COMMIT;
