-- Rol GERENTE_OPERACIONES (supervisa todos los proyectos: ve pedidos/almacenes/guías, visa y aprueba pedidos, planillas)
-- y ajuste de LOGISTICA: compra, recibe compras y registra pagos a proveedores (decisión del cliente 2026-11-10:
-- Efrain, de logística, hace pagos a proveedores). Idempotente. Ids con MAX()+n (las secuencias pueden ir atrasadas).

INSERT INTO lb_rol (id, codigo, nombre, descripcion, es_global, activo)
SELECT (SELECT COALESCE(MAX(id), 0) FROM lb_rol) + 1, 'GERENTE_OPERACIONES', 'Gerente de Operaciones',
       'Supervisa todos los proyectos: ve almacenes, pedidos y guías; visa y aprueba pedidos; revisa planillas', true, true
WHERE NOT EXISTS (SELECT 1 FROM lb_rol WHERE codigo = 'GERENTE_OPERACIONES');

INSERT INTO lb_rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id
FROM lb_rol r
JOIN (VALUES
    ('GERENTE_OPERACIONES', 'ALMACEN_VER'), ('GERENTE_OPERACIONES', 'PEDIDO_VER_TODOS'),
    ('GERENTE_OPERACIONES', 'PEDIDO_VISAR'), ('GERENTE_OPERACIONES', 'PEDIDO_APROBAR'),
    ('GERENTE_OPERACIONES', 'GUIA_REMISION_VER'), ('GERENTE_OPERACIONES', 'PLANILLA_CALCULAR'),
    ('LOGISTICA', 'COMPRA_CREAR'), ('LOGISTICA', 'COMPRA_RECIBIR'), ('LOGISTICA', 'PAGO_PROVEEDOR_REGISTRAR')
) AS m(rol, permiso) ON m.rol = r.codigo
JOIN lb_permiso p ON p.codigo = m.permiso
WHERE NOT EXISTS (SELECT 1 FROM lb_rol_permiso rp WHERE rp.rol_id = r.id AND rp.permiso_id = p.id);

SELECT setval(pg_get_serial_sequence('lb_rol', 'id'), (SELECT MAX(id) FROM lb_rol));
