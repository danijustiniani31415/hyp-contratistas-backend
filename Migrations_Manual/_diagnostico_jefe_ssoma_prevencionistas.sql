-- Diagnóstico de datos reales para Flujo B (Evaluar Jefe SSOMA) y Flujo C
-- (Evaluar Prevencionista/Coordinador), igual que se hizo para Supervisor Contratista.

-- 1) Flujo B: cuántos usuarios con rol 70/72 activos existen en total (son los
--    evaluadores obligatorios del Jefe SSOMA — no depende de proyecto).
SELECT r.role_id, r.role_name, count(*) AS total_usuarios
FROM user_role ur
JOIN role r ON r.role_id = ur.role_id
WHERE ur.role_id IN (70, 72) AND ur.active = TRUE AND ur.state = TRUE
GROUP BY r.role_id, r.role_name;

-- 2) Flujo C: cuántos "candidatos a evaluar" (Prevencionista/Coordinador) trae hoy
--    la query real de EvPrevencionistaRepository — requiere email_corporativo del
--    worker calzando con app_user.email Y vinculación vigente a proyecto.
SELECT count(DISTINCT au.user_id) AS candidatos_flujo_c,
       count(DISTINCT wv.proyecto_id) AS proyectos_distintos
FROM workers w
JOIN person p ON p.person_id = w.person_id
JOIN app_user au ON LOWER(au.email) = LOWER(w.email_corporativo)
JOIN user_role ur ON ur.user_id = au.user_id AND ur.role_id IN (70, 72) AND ur.active = TRUE AND ur.state = TRUE
JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL;

-- 3) De los 70/72 activos (paso 1), ¿cuántos NO calzan por email_corporativo con
--    ningún app_user, o no tienen vinculación vigente? (para saber si hay el mismo
--    tipo de hueco de datos que en Supervisor Contratista).
SELECT
  count(*) AS total_70_72,
  count(w.id) AS con_worker_por_email,
  count(wv.id) AS con_vinculacion_vigente
FROM user_role ur
JOIN app_user au ON au.user_id = ur.user_id
LEFT JOIN workers w ON LOWER(w.email_corporativo) = LOWER(au.email)
LEFT JOIN worker_vinculaciones wv ON wv.worker_id = w.id AND wv.fecha_fin IS NULL
WHERE ur.role_id IN (70, 72) AND ur.active = TRUE AND ur.state = TRUE;
