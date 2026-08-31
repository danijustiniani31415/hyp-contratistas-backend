-- ============================================================================
-- Rol USUARIO REVISOR DE SALIDAS (78)
--
-- Contexto: para abrir /gestion-administrativa/gestion-salidas hace falta tener
-- la feature `gestion-administrativa.gestion-salidas`, que hoy solo conceden
-- USUARIO DE RECEPCIÓN (52) y ADMINISTRADOR DE SOLICITUD DE SALIDAS (76). Un
-- revisor designado en Revisores de Áreas necesita entrar a esa pantalla, pero
-- darle el 76 le concedería además 9 features de administración (entre ellas
-- editar los propios revisores de áreas y los overrides de visibilidad).
--
-- Este rol angosto concede EXACTAMENTE una feature: gestión de salidas. La
-- visibilidad de datos dentro de la pantalla no depende de este rol: la resuelve
-- SalidaVisibilityResolver, que ya da a todo revisor de área su nodo + subárbol.
--
-- El role_id se fija en 78 (no se deja al sequence) para que dev y prod usen el
-- mismo ID y las constantes de Roles.cs / roles.ts sean válidas en ambos. Si 78
-- estuviera ocupado por otro rol, el script ABORTA en vez de seguir en silencio.
--
-- El feature_id se DERIVA del feature_key en vez de hardcodearse, por si difiere
-- por entorno.
--
-- Asignación: todos los revisores de área vivos y activos (area_revisores) que
-- tengan usuario del sistema (person.user_id). Los revisores sin app_user no
-- pueden recibir ningún rol — hay que crearles el usuario primero.
--
-- Idempotente: se puede correr más de una vez sin duplicar nada; una fila de
-- user_role dada de baja se revive (state/active = true).
-- Requiere re-login de los usuarios afectados (allowed_features se recalcula al
-- iniciar sesión).
-- ============================================================================

BEGIN;

-- 1) El rol. Aborta si el ID 78 ya lo tomó otro rol.
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM role WHERE role_id = 78 AND role_description <> 'USUARIO REVISOR DE SALIDAS') THEN
        RAISE EXCEPTION 'role_id 78 ya está ocupado por el rol "%". Abortado.',
            (SELECT role_description FROM role WHERE role_id = 78);
    END IF;
END $$;

INSERT INTO role (role_id, role_description, created_user_id)
VALUES (78, 'USUARIO REVISOR DE SALIDAS', 1)
ON CONFLICT (role_id) DO NOTHING;

-- El sequence quedó atrás al insertar con ID explícito: se adelanta para que el
-- próximo rol creado desde el CRUD no choque contra el 78.
SELECT setval('role_role_id_seq', GREATEST((SELECT max(role_id) FROM role), 78), true);

-- 2) Su única feature: gestión de salidas.
INSERT INTO role_feature (role_id, feature_id)
SELECT 78, f.feature_id
FROM feature f
WHERE f.feature_key = 'gestion-administrativa.gestion-salidas'
ON CONFLICT (role_id, feature_id) DO NOTHING;

-- 3) Asignarlo a los revisores de área actuales (con usuario del sistema).
INSERT INTO user_role (user_id, role_id, created_user_id)
SELECT DISTINCT p.user_id, 78, 1
FROM area_revisores ar
JOIN workers w ON w.id = ar.revisor_id
JOIN person  p ON p.person_id = w.person_id
WHERE ar.state AND ar.active AND p.user_id IS NOT NULL
ON CONFLICT (user_id, role_id) DO UPDATE
   SET state             = true,
       active            = true,
       updated_date_time = now(),
       updated_user_id   = 1;

COMMIT;

-- ── Verificación ────────────────────────────────────────────────────────────
-- a) El rol con su única feature:
-- SELECT r.role_id, r.role_description, f.feature_key
-- FROM role r
-- LEFT JOIN role_feature rf ON rf.role_id = r.role_id
-- LEFT JOIN feature f ON f.feature_id = rf.feature_id
-- WHERE r.role_id = 78;
--
-- b) Cuántos revisores quedaron con el rol y cuántos siguen sin usuario:
-- SELECT count(*) FILTER (WHERE p.user_id IS NOT NULL) AS con_usuario,
--        count(*) FILTER (WHERE p.user_id IS NULL)     AS sin_usuario_no_asignables,
--        count(ur.user_role_id)                        AS con_rol_78
-- FROM (SELECT DISTINCT revisor_id FROM area_revisores WHERE state AND active) x
-- JOIN workers w ON w.id = x.revisor_id
-- JOIN person  p ON p.person_id = w.person_id
-- LEFT JOIN user_role ur ON ur.user_id = p.user_id AND ur.role_id = 78 AND ur.state;
--
-- c) Quiénes no se pudieron asignar (les falta app_user):
-- SELECT p.full_name, w.email_corporativo
-- FROM (SELECT DISTINCT revisor_id FROM area_revisores WHERE state AND active) x
-- JOIN workers w ON w.id = x.revisor_id
-- JOIN person  p ON p.person_id = w.person_id
-- WHERE p.user_id IS NULL
-- ORDER BY 1;
