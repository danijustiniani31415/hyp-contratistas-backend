-- Ajuste del flujo real de Pedidos (2026-09-22): Administrador/Asistente Administrativa de mina
-- CREA el pedido -> Residente O Gerente de Operaciones VISA (primer nivel) -> Gerente General
-- APRUEBA (segundo nivel, definitivo) -> el Logistico de Lima recibe para atender/despachar.
-- Antes RESIDENTE tenia PEDIDO_CREAR ademas de PEDIDO_VISAR -- se deja asi (no molesta que
-- tambien pueda crear), pero el creador real de mina es un rol nuevo, separado, que NO visa.
-- Idempotente: seguro correr mas de una vez.

INSERT INTO lb_rol (codigo, nombre, es_global)
SELECT 'ADMINISTRATIVO', 'Administrativo de Mina', false
WHERE NOT EXISTS (SELECT 1 FROM lb_rol WHERE codigo = 'ADMINISTRATIVO');

INSERT INTO lb_rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id FROM lb_rol r, lb_permiso p
WHERE ((r.codigo = 'ADMINISTRATIVO' AND p.codigo = 'PEDIDO_CREAR')
    -- Gerente de Operaciones tambien puede visar, igual que Residente -- se le da el mismo
    -- permiso vía el rol RESIDENTE (los roles del sistema no estan atados 1 a 1 al cargo real
    -- de la persona; se le puede asignar el rol RESIDENTE a alguien con cargo "Gerente de
    -- Operaciones" sin problema, es solo la etiqueta de permisos).
    OR (r.codigo = 'LOGISTICA' AND p.codigo = 'PEDIDO_ENTREGAR'))
  AND NOT EXISTS (SELECT 1 FROM lb_rol_permiso rp WHERE rp.rol_id = r.id AND rp.permiso_id = p.id);
