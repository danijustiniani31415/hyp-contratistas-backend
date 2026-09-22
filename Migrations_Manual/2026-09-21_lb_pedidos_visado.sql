-- Flujo de Pedidos pasa de un solo nivel de aprobacion a dos: el Almacenero de mina solicita, el
-- Residente visa (primer nivel), Gerencia/Logistica aprueba (segundo nivel, definitivo), y recien
-- ahi el Almacenero (normalmente el de otro almacen/central) entrega. Pedido explicito del usuario
-- 2026-09-21: "el almacenero debe generar el pedido y el residente lo aprueba primero".
-- Idempotente: seguro correr mas de una vez.

ALTER TABLE lb_pedido ADD COLUMN IF NOT EXISTS visado_por_usuario_sistema_id BIGINT;
ALTER TABLE lb_pedido ADD COLUMN IF NOT EXISTS visado_en TIMESTAMPTZ;

INSERT INTO lb_permiso (codigo, descripcion)
SELECT 'PEDIDO_VISAR', 'Visar pedidos de mina antes de que pasen a Gerencia (primer nivel de aprobacion)'
WHERE NOT EXISTS (SELECT 1 FROM lb_permiso WHERE codigo = 'PEDIDO_VISAR');

-- RESIDENTE visa (nuevo); ALMACENERO pasa a poder crear pedidos (antes solo entregaba, ahora es
-- quien solicita reposicion desde la mina).
INSERT INTO lb_rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id FROM lb_rol r, lb_permiso p
WHERE ((r.codigo = 'RESIDENTE' AND p.codigo = 'PEDIDO_VISAR')
    OR (r.codigo = 'ALMACENERO' AND p.codigo = 'PEDIDO_CREAR')
    OR (r.codigo = 'ADMIN' AND p.codigo = 'PEDIDO_VISAR'))
  AND NOT EXISTS (SELECT 1 FROM lb_rol_permiso rp WHERE rp.rol_id = r.id AND rp.permiso_id = p.id);
