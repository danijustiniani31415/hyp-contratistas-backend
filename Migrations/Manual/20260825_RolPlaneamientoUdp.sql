-- ============================================================================
-- Rol PLANEAMIENTO UDP
--
-- Contexto: Planeamiento (área) pidió un rol con acceso EXCLUSIVO a Planeamiento
-- BIM: las 4 pantallas de configuracion-inicial/carga-diaria/bloqueos/dashboard
-- (comparten el feature_key 'planeamiento-bim.configuracion-inicial' — no hay
-- feature_key por pantalla) + Portafolio BIM ('planeamiento-bim.portafolio').
-- Nada más: sin Cronograma Actividades, Dashboard UDP, IVTs, Cuaderno de Obra,
-- Respuesta de Informes, Residentes ni Configuraciones.
--
-- Backend: los 5 controllers de PlaneamientoBimFeature ya autorizan vía
-- [RequireFeature(...)] contra role_feature (migrados desde [Authorize(Roles=...)]
-- hardcodeado en esta misma sesión). Con este SQL el rol queda funcional de punta
-- a punta: no hace falta tocar más C#.
--
-- A diferencia de otros scripts de creación de rol (p. ej.
-- 20260805_RolUsuarioRevisorSalidas.sql, rol 78), acá el role_id NO se fija
-- explícito — se deja al sequence normal. Esa técnica existe solo para roles que
-- algún [Authorize(Roles=...)] o algún `roles: [...]` del frontend referencia por
-- ID numérico fijo. Verificado que no es el caso aquí:
--   - Los 5 controllers autorizan por feature_key vía RequireFeature, no por ID.
--   - roleGuard (Abril-Frontend core/guards/role.guard.ts) resuelve featureKey
--     ANTES que `roles` y retorna en cuanto featureKey matchea `allowed_features`
--     — el único `roles: [...]` que toca este feature (ruta de Portafolio) queda
--     como fallback muerto para un rol al que ya le sembramos el feature_key.
--     Mismo orden en navigation.service.ts (isNavEntryAllowed).
-- Por eso el mismo script corre igual en local y producción sin importar qué IDs
-- estén libres en cada uno — no hace falta guard de "ID ya ocupado".
--
-- El feature_id (igual que el role_id) se DERIVA, nunca se hardcodea.
--
-- Idempotente: se puede correr más de una vez sin duplicar nada.
-- No crea filas en user_role — no se pidió asignar el rol a usuarios todavía.
-- ============================================================================

BEGIN;

-- 1) El rol (sequence normal, sin ID fijo).
INSERT INTO role (role_description, created_user_id)
SELECT 'PLANEAMIENTO UDP', 1
WHERE NOT EXISTS (SELECT 1 FROM role WHERE role_description = 'PLANEAMIENTO UDP');

-- 2) Sus 2 features: todo Planeamiento BIM (4 pantallas bajo un mismo
--    feature_key) + Portafolio BIM. role_id y feature_id, ambos derivados.
INSERT INTO role_feature (role_id, feature_id)
SELECT r.role_id, f.feature_id
FROM role r
CROSS JOIN feature f
WHERE r.role_description = 'PLANEAMIENTO UDP'
  AND f.feature_key IN ('planeamiento-bim.configuracion-inicial', 'planeamiento-bim.portafolio')
ON CONFLICT (role_id, feature_id) DO NOTHING;

COMMIT;

-- ── Verificación ────────────────────────────────────────────────────────────
-- SELECT r.role_id, r.role_description, f.feature_key
-- FROM role r
-- LEFT JOIN role_feature rf ON rf.role_id = r.role_id
-- LEFT JOIN feature f ON f.feature_id = rf.feature_id
-- WHERE r.role_description = 'PLANEAMIENTO UDP'
-- ORDER BY f.feature_key;
--
-- Debe devolver exactamente 2 filas (el role_id real lo asigna el sequence):
--   <role_id> | PLANEAMIENTO UDP | planeamiento-bim.configuracion-inicial
--   <role_id> | PLANEAMIENTO UDP | planeamiento-bim.portafolio
-- (Ya corrido en producción el 2026-08-25: quedó role_id = 80.)


-- ============================================================================
-- Paso 2 (2026-08-25) — Asignación a los 4 Ingenieros de Planeamiento BIM
--
-- Candidatos identificados por SELECT de solo lectura contra workers.puesto /
-- puesto.nombre / workers.subarea (ILIKE '%planeamiento%' OR '%bim%') y
-- confirmados por nombre con el usuario. Se excluyó a propósito a 2 personas que
-- el comodín también trajo pero son de otra subárea ("Ingeniería BIM", no
-- "Planeamiento BIM"): Asencios (Modelador BIM) y Padilla (Arquitecto BIM).
--
-- Los 4 asignados, puesto = 'INGENIERO DE PLANEAMIENTO BIM', subarea =
-- 'Planeamiento BIM', todos ACTIVO:
--   user_id 114 — Dulanto Martinez Jean Franco   — jdulanto@abril.pe
--   user_id 306 — Haro Jesus Jherson Steven      — jharo@abril.pe
--   user_id 239 — Portilla Velasquez Lidis Dayana Marlene — lportilla@abril.pe
--   user_id 243 — Sanchez Taipe Arturo           — asanchez@abril.pe
--
-- role_id se DERIVA por SELECT sobre role_description (no se hardcodea el 80,
-- aunque ya lo confirmamos). ON CONFLICT DO NOTHING: si alguno ya tuviera el rol
-- asignado, no duplica ni pisa nada (uk_user_role_user_role es índice único real
-- sobre (user_id, role_id), verificado antes de escribir esto).
-- ============================================================================

INSERT INTO user_role (user_id, role_id, created_user_id)
SELECT u.user_id, r.role_id, 1
FROM (VALUES (114), (306), (239), (243)) AS u(user_id)
CROSS JOIN role r
WHERE r.role_description = 'PLANEAMIENTO UDP'
ON CONFLICT (user_id, role_id) DO NOTHING;

-- ── Verificación ────────────────────────────────────────────────────────────
-- SELECT ur.user_id, p.full_name, r.role_description
-- FROM user_role ur
-- JOIN role r ON r.role_id = ur.role_id
-- JOIN person p ON p.user_id = ur.user_id
-- WHERE r.role_description = 'PLANEAMIENTO UDP'
-- ORDER BY p.full_name;
--
-- Debe devolver exactamente 4 filas: Dulanto, Haro, Portilla, Sanchez.
