-- ============================================================================
-- SOLO DEV — alinear los catálogos `categoria` y `role` con producción
-- ============================================================================
-- No correr en producción: prod es la fuente de estos ids y ya los tiene.
--
-- Por qué existe: `2026-08-27_ga_reembolso_firma_tesorero.sql` se corrió primero
-- en dev creando TESORERO con ids elegidos a ojo (categoria 43, role 80). Al
-- correrlo en prod, su guard abortó: prod ya tenía esos ids ocupados por otras
-- filas creadas entre medio. Como los ids se usan como constantes en el código
-- (`CategoriaIds.Tesorero`, `Roles.Tesorero`), dev tiene que tomar los de prod y
-- no al revés.
--
-- Estado real de prod (verificado el 2026-08-27):
--   categoria  43 ABOGADO · 44 ALMACENERO · 45 TESORERÍA (inactiva) · 46 TESORERO
--   role       80 PLANEAMIENTO UDP · 81 JEFE · 82 GERENTE   (máximo: 82)
-- Así que TESORERO es la categoría 46 —que prod ya creó y ya tiene enganchada al
-- puesto TESORERA— y el rol nuevo pasa a ser el 83, libre en los dos entornos.
--
-- Las filas de dev con ids 43 y 80 se BORRAN duro, no se dan de baja con state:
-- nacieron hoy por error, no existen en prod y no tienen historia que auditar;
-- dejarlas ocupando el id rompería justamente la paridad que se busca.
--
-- Idempotente: se puede correr más de una vez.
-- ============================================================================

BEGIN;

-- ── 1. Categorías: liberar el 43 y traer las cuatro de prod ─────────────────

-- El puesto que había quedado apuntando al TESORERO provisional se aparca en la
-- categoría del catálogo maestro; al final del script vuelve al 46 definitivo.
UPDATE puesto SET categoria_id = 42 WHERE categoria_id = 43;

DELETE FROM categoria
 WHERE categoria_id = 43
   AND upper(nombre) = 'TESORERO';   -- guard: si el 43 ya es ABOGADO, no borra nada

INSERT INTO categoria (categoria_id, nombre, orden, visible_solicitud_personal, active)
SELECT v.id, v.nombre, v.orden, false, v.activo
FROM (VALUES
    (43, 'ABOGADO',    2001, true),
    (44, 'ALMACENERO', 2002, true),
    (45, 'TESORERÍA',  2003, false),   -- prod la dejó inactiva: la reemplazó la 46
    (46, 'TESORERO',   2004, true)
) AS v(id, nombre, orden, activo)
WHERE NOT EXISTS (SELECT 1 FROM categoria c WHERE c.categoria_id = v.id);

SELECT setval('categoria_categoria_id_seq', GREATEST((SELECT max(categoria_id) FROM categoria), 46), true);

-- El puesto de tesorería queda en la categoría definitiva, como en prod.
UPDATE puesto
   SET categoria_id = 46, updated_date_time = now()
 WHERE state
   AND upper(nombre) IN ('TESORERO', 'TESORERA')
   AND categoria_id <> 46;

-- ── 2. Roles: liberar el 80 y traer los tres de prod ────────────────────────

DELETE FROM role_feature
 WHERE role_id = 80
   AND EXISTS (SELECT 1 FROM role r WHERE r.role_id = 80 AND upper(r.role_description) = 'TESORERO');

DELETE FROM role
 WHERE role_id = 80
   AND upper(role_description) = 'TESORERO';   -- guard: si el 80 ya es PLANEAMIENTO UDP, no borra nada

INSERT INTO role (role_id, role_description, created_user_id, active, state)
SELECT v.id, v.nombre, (SELECT min(user_id) FROM app_user), true, true
FROM (VALUES
    (80, 'PLANEAMIENTO UDP'),
    (81, 'JEFE'),
    (82, 'GERENTE')
) AS v(id, nombre)
WHERE NOT EXISTS (SELECT 1 FROM role r WHERE r.role_id = v.id);

SELECT setval('role_role_id_seq', GREATEST((SELECT max(role_id) FROM role), 82), true);

COMMIT;

-- ============================================================================
-- Verificación
-- ============================================================================
-- SELECT categoria_id, nombre, active FROM categoria WHERE categoria_id >= 42 ORDER BY 1;
-- SELECT puesto_id, nombre, categoria_id FROM puesto WHERE upper(nombre) LIKE '%TESOR%';
-- SELECT role_id, role_description FROM role WHERE role_id >= 79 ORDER BY 1;
-- (el rol TESORERO = 83 lo crea la migración principal, que ya se puede correr en los dos entornos)
