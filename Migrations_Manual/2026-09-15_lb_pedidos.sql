-- Pedidos (Fase 2, punto 4 de CONTEXT_LOGISTICA.md sección 3) — no tenia DDL en el documento,
-- diseñado ahora siguiendo los mismos patrones de Fase 1: historial de estado en la propia fila
-- (igual que lb_vinculo_laboral), trazabilidad de quien aprueba/entrega (igual que
-- lb_usuario_asignacion.otorgado_por), y permisos en vez de codigo de rol hardcodeado
-- (lb_rol_permiso, ya sembrado en Fase 1 seccion 4.3 precisamente para este momento).

CREATE TABLE lb_pedido (
    id              BIGSERIAL PRIMARY KEY,
    codigo          VARCHAR(20) NOT NULL UNIQUE, -- PED-2026-000001, correlativo legible
    proyecto_id     INT NOT NULL REFERENCES lb_proyecto(id),
    almacen_id      INT NOT NULL REFERENCES lb_almacen(id), -- de que almacen se despacha
    solicitante_usuario_sistema_id BIGINT NOT NULL REFERENCES lb_usuario_sistema(id),
    estado          VARCHAR(20) NOT NULL DEFAULT 'PENDIENTE', -- PENDIENTE, APROBADO, RECHAZADO, ENTREGADO, CANCELADO
    observacion     TEXT, -- comentario del solicitante al crear
    motivo_rechazo  TEXT,
    aprobado_por_usuario_sistema_id BIGINT REFERENCES lb_usuario_sistema(id),
    aprobado_en     TIMESTAMPTZ,
    entregado_por_usuario_sistema_id BIGINT REFERENCES lb_usuario_sistema(id),
    entregado_en    TIMESTAMPTZ,
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_pedido_estado ON lb_pedido(estado);
CREATE INDEX idx_pedido_proyecto ON lb_pedido(proyecto_id);

CREATE TABLE lb_pedido_item (
    id                  BIGSERIAL PRIMARY KEY,
    pedido_id           BIGINT NOT NULL REFERENCES lb_pedido(id),
    producto_id         BIGINT NOT NULL REFERENCES lb_producto(id),
    talla               VARCHAR(10) NOT NULL DEFAULT '',
    cantidad_solicitada NUMERIC(12,2) NOT NULL,
    -- NULL hasta que se entrega; hoy siempre = cantidad_solicitada (se valida stock completo
    -- antes de entregar) pero se deja separado para permitir entrega parcial en el futuro sin
    -- otra migracion.
    cantidad_entregada  NUMERIC(12,2)
);
CREATE INDEX idx_pedido_item_pedido ON lb_pedido_item(pedido_id);

-- Correlativo del codigo del pedido (PED-2026-000001, PED-2026-000002, ...) — una secuencia por
-- año evita que el numero se reinicie a mano cada enero y evita colisiones entre transacciones
-- concurrentes (a diferencia de "SELECT MAX(id)+1").
CREATE SEQUENCE lb_pedido_correlativo START 1;

-- Nuevos permisos del modulo Pedidos — se agregan a lb_rol_permiso, no se hardcodea el codigo
-- del rol en el backend (mismo criterio de CONTEXT_LOGISTICA.md seccion 4.3).
INSERT INTO lb_permiso (codigo, descripcion) VALUES
('PEDIDO_CREAR',    'Crear pedidos de su proyecto'),
('PEDIDO_APROBAR',  'Aprobar o rechazar pedidos'),
('PEDIDO_ENTREGAR', 'Marcar un pedido como entregado (descuenta stock)'),
('PEDIDO_VER_TODOS','Ver pedidos de todos los proyectos, no solo el propio');

INSERT INTO lb_rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id FROM lb_rol r, lb_permiso p
WHERE (r.codigo = 'RESIDENTE'      AND p.codigo = 'PEDIDO_CREAR')
   OR (r.codigo = 'GERENTE_GENERAL' AND p.codigo IN ('PEDIDO_APROBAR', 'PEDIDO_VER_TODOS'))
   OR (r.codigo = 'ALMACENERO'     AND p.codigo = 'PEDIDO_ENTREGAR')
   OR (r.codigo = 'LOGISTICA'      AND p.codigo IN ('PEDIDO_VER_TODOS', 'PEDIDO_APROBAR'))
   OR (r.codigo = 'ADMIN'          AND p.codigo IN ('PEDIDO_CREAR', 'PEDIDO_APROBAR', 'PEDIDO_ENTREGAR', 'PEDIDO_VER_TODOS'));
