-- Diagnóstico: por qué el combobox de proyectos en "Evaluar Supervisor de Contratista"
-- solo muestra 4 proyectos. Compara distintos niveles de filtro para ver dónde se
-- pierden registros. No modifica nada.

-- 1) Total de ss_contratista_usuario con rol de negocio "supervisor" (por nombre),
--    sin importar el rol de sistema (user_role) ni el estado activo.
SELECT r.nombre AS rol_negocio, count(*) AS total
FROM ss_contratista_usuario scu
JOIN ss_contratista_rol r ON r.id = scu.rol_id
GROUP BY r.nombre
ORDER BY total DESC;

-- 2) De los que SÍ tienen rol de sistema 74 (SUPERVISOR DE CAMPO) en user_role,
--    cuántos están activos y cuántos no.
SELECT scu.activo AS scu_activo, ur.active AS user_role_active, ur.state AS user_role_state, count(*) AS total
FROM ss_contratista_usuario scu
JOIN user_role ur ON ur.user_id = scu.user_id AND ur.role_id = 74
GROUP BY scu.activo, ur.active, ur.state;

-- 3) Cuántos ss_contratista_usuario con rol 74 activo NO tienen ninguna fila en
--    ss_contratista_usuario_proyecto (es decir, no están vinculados a un proyecto).
SELECT count(*) AS supervisores_sin_proyecto
FROM ss_contratista_usuario scu
JOIN user_role ur ON ur.user_id = scu.user_id AND ur.role_id = 74 AND ur.active = TRUE AND ur.state = TRUE
WHERE scu.activo = TRUE
  AND NOT EXISTS (SELECT 1 FROM ss_contratista_usuario_proyecto scup WHERE scup.contratista_usuario_id = scu.id);

-- 4) La query real que usa el backend (ANY project) — proyectos distintos que trae hoy.
SELECT DISTINCT scup.proyecto_id, pr.project_description
FROM ss_contratista_usuario scu
JOIN user_role ur ON ur.user_id = scu.user_id AND ur.role_id = 74 AND ur.active = TRUE AND ur.state = TRUE
JOIN ss_contratista_usuario_proyecto scup ON scup.contratista_usuario_id = scu.id
JOIN project pr ON pr.project_id = scup.proyecto_id
WHERE scu.activo = TRUE
ORDER BY pr.project_description;

-- 5) Cuántos ss_contratista_usuario en total tienen rol de negocio con nombre que
--    contenga 'supervis' (case-insensitive), para comparar contra (4).
SELECT count(*) AS total_supervisores_por_nombre_rol
FROM ss_contratista_usuario scu
JOIN ss_contratista_rol r ON r.id = scu.rol_id
WHERE r.nombre ILIKE '%supervis%';
