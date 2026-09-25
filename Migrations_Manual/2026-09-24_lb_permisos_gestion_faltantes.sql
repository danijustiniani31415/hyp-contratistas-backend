-- Los módulos Roles y Permisos, Personas, Catálogo Maestro, Tareo y Almacén nunca tuvieron un
-- código de permiso propio: sus controllers solo exigían [Authorize] (estar logueado), sin
-- validar el permiso real vía HasLbPermiso como sí hacen Compras/Pedidos/EPP/Herramientas/Guías.
-- Resultado: cualquier usuario logueado, sin importar su rol, podía crear/editar trabajadores,
-- reasignarse roles y permisos a sí mismo, editar el catálogo, marcar tareo o mover stock
-- manualmente vía API directa. Este script agrega los permisos que faltaban y, por ahora, se los
-- asigna solo a ADMIN (el rol ya usado para "Administrador del Sistema") — desde la propia
-- pantalla de Roles y Permisos se pueden extender a otros roles cuando el negocio lo defina.
-- Idempotente: seguro correr más de una vez.

INSERT INTO lb_permiso (id, codigo, descripcion)
SELECT v.id, v.codigo, v.descripcion
FROM (VALUES
    (17, 'ROLES_GESTIONAR', 'Ver y administrar roles y permisos del sistema'),
    (18, 'PERSONA_GESTIONAR', 'Crear y editar trabajadores, vínculos laborales, planilla y accesos al sistema'),
    (19, 'CATALOGO_GESTIONAR', 'Crear y editar categorías, productos y catálogos de valores (banco, AFP, color, etc.)'),
    (20, 'TAREO_REGISTRAR', 'Registrar y editar el tareo (asistencia diaria) de los trabajadores'),
    (21, 'ALMACEN_AJUSTAR', 'Registrar movimientos manuales de almacén y ajustar umbrales de reposición')
) AS v(id, codigo, descripcion)
WHERE NOT EXISTS (SELECT 1 FROM lb_permiso p WHERE p.codigo = v.codigo);

SELECT setval('lb_permiso_id_seq', (SELECT MAX(id) FROM lb_permiso));

INSERT INTO lb_rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id
FROM lb_rol r
CROSS JOIN lb_permiso p
WHERE r.codigo = 'ADMIN'
  AND p.codigo IN ('ROLES_GESTIONAR', 'PERSONA_GESTIONAR', 'CATALOGO_GESTIONAR', 'TAREO_REGISTRAR', 'ALMACEN_AJUSTAR')
  AND NOT EXISTS (
    SELECT 1 FROM lb_rol_permiso rp WHERE rp.rol_id = r.id AND rp.permiso_id = p.id
  );
