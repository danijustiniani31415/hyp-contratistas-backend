-- Rol CONTADORA (acceso completo, incluye emitir facturas y administrar accesos), permiso para ver el almacén
-- acotado por proyecto (ALMACEN_VER) y permiso OPCIONAL de pagos a proveedores. Idempotente.
-- Los ids se calculan con MAX()+n: las secuencias de lb_rol/lb_permiso pueden ir atrasadas respecto a los datos.

-- 1) Permisos nuevos
INSERT INTO lb_permiso (id, codigo, descripcion)
SELECT (SELECT COALESCE(MAX(id), 0) FROM lb_permiso) + row_number() OVER (ORDER BY v.codigo), v.codigo, v.descripcion
FROM (VALUES
    ('ALMACEN_VER', 'Ver el stock, el kardex y los inventarios del almacén (solo de los proyectos asignados al usuario)'),
    ('PAGO_PROVEEDOR_REGISTRAR', 'Registrar y aprobar pagos a proveedores (opcional: activar solo si la empresa lo requiere)')
) AS v(codigo, descripcion)
WHERE NOT EXISTS (SELECT 1 FROM lb_permiso p WHERE p.codigo = v.codigo);

-- 2) Rol Contadora
INSERT INTO lb_rol (id, codigo, nombre, descripcion, es_global, activo)
SELECT (SELECT COALESCE(MAX(id), 0) FROM lb_rol) + 1, 'CONTADORA', 'Contadora',
       'Acceso completo: factura, administra accesos y ve todos los proyectos', true, true
WHERE NOT EXISTS (SELECT 1 FROM lb_rol WHERE codigo = 'CONTADORA');

-- 3) Contadora: todos los permisos que existen hoy
INSERT INTO lb_rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id FROM lb_rol r CROSS JOIN lb_permiso p
WHERE r.codigo = 'CONTADORA'
  AND NOT EXISTS (SELECT 1 FROM lb_rol_permiso rp WHERE rp.rol_id = r.id AND rp.permiso_id = p.id);

-- 4) Resto de roles
INSERT INTO lb_rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id
FROM lb_rol r
JOIN (VALUES
    ('GERENTE_GENERAL', 'ALMACEN_VER'), ('GERENTE_GENERAL', 'FACTURACION_VER'), ('GERENTE_GENERAL', 'PAGO_PROVEEDOR_REGISTRAR'),
    ('LOGISTICA', 'ALMACEN_VER'), ('COMPRAS', 'ALMACEN_VER'), ('RESIDENTE', 'ALMACEN_VER'), ('ALMACENERO', 'ALMACEN_VER'),
    ('ADMIN', 'ALMACEN_VER'), ('ADMIN', 'PAGO_PROVEEDOR_REGISTRAR'),
    -- Administrador de mina: tareo, planillas, recibe pedidos/compras y ve el almacén de su proyecto.
    ('ADMINISTRATIVO', 'ALMACEN_VER'), ('ADMINISTRATIVO', 'TAREO_REGISTRAR'), ('ADMINISTRATIVO', 'PLANILLA_CALCULAR'),
    ('ADMINISTRATIVO', 'PEDIDO_ENTREGAR'), ('ADMINISTRATIVO', 'COMPRA_RECIBIR')
) AS m(rol, permiso) ON m.rol = r.codigo
JOIN lb_permiso p ON p.codigo = m.permiso
WHERE NOT EXISTS (SELECT 1 FROM lb_rol_permiso rp WHERE rp.rol_id = r.id AND rp.permiso_id = p.id);

SELECT setval('lb_permiso_id_seq', (SELECT MAX(id) FROM lb_permiso));
SELECT setval(pg_get_serial_sequence('lb_rol', 'id'), (SELECT MAX(id) FROM lb_rol));
