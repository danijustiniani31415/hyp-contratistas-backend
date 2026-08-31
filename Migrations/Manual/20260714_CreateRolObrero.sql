-- Migración manual (pgAdmin) — nuevo tipo de sesión OBRERO: una cuenta por
-- trabajador de campo (Worker/Person), no por empresa contratista. Se usa
-- para el "levantamiento" de Observaciones sin selector manual de nombre —
-- el sistema detecta al trabajador desde el JWT (claim workerId).
--
-- Alcance inicial: solo Lista + Levantar de Observaciones (arquitectura
-- comercial). El Dashboard queda fuera a propósito (agrega datos de otros
-- supervisores/proyectos que un obrero individual no necesita ver).

INSERT INTO role (role_description, created_date_time, created_user_id, active, state)
SELECT 'OBRERO', now(), 1, TRUE, TRUE
WHERE NOT EXISTS (SELECT 1 FROM role WHERE role_description = 'OBRERO');

INSERT INTO role_feature (role_id, feature_id)
SELECT r.role_id, f.feature_id
FROM role r
CROSS JOIN feature f
WHERE r.role_description = 'OBRERO'
  AND f.feature_key IN ('arquitectura-comercial.observaciones', 'arquitectura-comercial.observaciones.lista')
  AND NOT EXISTS (
      SELECT 1 FROM role_feature rf WHERE rf.role_id = r.role_id AND rf.feature_id = f.feature_id
  );
