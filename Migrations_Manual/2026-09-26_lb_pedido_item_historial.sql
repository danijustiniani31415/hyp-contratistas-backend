-- Fase E (auditoría fina): con aprobación por ítem, Pedido solo guarda el último
-- aprobador/fecha a nivel de cabecera — no alcanza para responder "quién redujo esta línea
-- puntual, o quién rechazó cuál ítem y por qué" cuando ya pasó por dos etapas (Visar/Aprobar).
-- Esta tabla es bitácora inmutable: una fila por cada decisión que se tomó sobre un ítem.

CREATE TABLE lb_pedido_item_historial (
    id                  BIGSERIAL PRIMARY KEY,
    pedido_item_id      BIGINT NOT NULL REFERENCES lb_pedido_item(id),
    etapa               VARCHAR(20) NOT NULL,           -- VISADO, APROBACION
    usuario_sistema_id  BIGINT NOT NULL REFERENCES lb_usuario_sistema(id),
    estado_resultante   VARCHAR(20) NOT NULL,           -- VISADO, APROBADO, RECHAZADO
    cantidad_solicitada NUMERIC(12,2) NOT NULL,          -- snapshot del tope vigente en ese momento
    cantidad_aprobada   NUMERIC(12,2),
    motivo_rechazo      VARCHAR(200),
    creado_en           TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_pedido_item_historial_item ON lb_pedido_item_historial(pedido_item_id);
