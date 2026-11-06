-- Motor de Notificaciones transversal — CONTEXT_LOGISTICA.md sección 5, Fase 3 punto 9
-- (hasta ahora ⬜ no empezado). Se resuelve por evento + regla de scope, nunca por email fijo en
-- código. No reemplaza los correos que Pedidos ya manda directo (PedidoService.NotificarEvento) —
-- esos siguen igual para no arriesgar un flujo que ya funciona en producción. Este motor es
-- aditivo: lo usan casos nuevos, empezando por la alerta de pedidos estancados (Fase F).

CREATE TABLE lb_notif_regla (
    id              SERIAL PRIMARY KEY,
    evento          VARCHAR(50) NOT NULL, -- PEDIDO_CREADO, PEDIDO_APROBADO, DESPACHO_ENVIADO, STOCK_MINIMO, HERRAMIENTA_VENCIDA, PEDIDO_ESTANCADO
    rol_destino_id  SMALLINT REFERENCES lb_rol(id),
    scope_type      VARCHAR(20) NOT NULL DEFAULT 'MISMO_PROYECTO', -- GLOBAL, MISMO_PROYECTO, SOLICITANTE
    canal           VARCHAR(20) NOT NULL DEFAULT 'EMAIL',
    activo          BOOLEAN NOT NULL DEFAULT true
);

CREATE TABLE lb_notif_destinatario_extra (
    id              SERIAL PRIMARY KEY,
    notif_regla_id  INT NOT NULL REFERENCES lb_notif_regla(id),
    email           VARCHAR(150) NOT NULL,
    descripcion     VARCHAR(100)
);

CREATE TABLE lb_notif_envio (
    id                  BIGSERIAL PRIMARY KEY,
    notif_regla_id      INT NOT NULL REFERENCES lb_notif_regla(id),
    usuario_sistema_id  BIGINT REFERENCES lb_usuario_sistema(id),
    email_destino       VARCHAR(150) NOT NULL,
    canal               VARCHAR(20) NOT NULL,
    estado              VARCHAR(20) NOT NULL DEFAULT 'PENDIENTE', -- PENDIENTE, ENVIADO, FALLIDO
    error               TEXT,
    referencia_tipo     VARCHAR(30),
    referencia_id       BIGINT,
    creado_en           TIMESTAMPTZ NOT NULL DEFAULT now(),
    enviado_en          TIMESTAMPTZ
);
CREATE INDEX idx_notif_envio_estado ON lb_notif_envio(estado) WHERE estado != 'ENVIADO';
-- Para el chequeo de "ya se avisó este pedido hoy" del job de SLA (evita reenviar cada 15 min).
CREATE INDEX idx_notif_envio_referencia ON lb_notif_envio(referencia_tipo, referencia_id, creado_en);

-- Semilla de reglas (tabla de CONTEXT_LOGISTICA.md sección 5.1) usando los códigos de rol reales
-- ya sembrados en Fase 1 (lb_rol.codigo).
INSERT INTO lb_notif_regla (evento, rol_destino_id, scope_type)
SELECT 'PEDIDO_CREADO', id, 'GLOBAL' FROM lb_rol WHERE codigo = 'GERENTE_GENERAL'
UNION ALL
SELECT 'PEDIDO_APROBADO', id, 'GLOBAL' FROM lb_rol WHERE codigo = 'LOGISTICA'
UNION ALL
SELECT 'PEDIDO_APROBADO', id, 'MISMO_PROYECTO' FROM lb_rol WHERE codigo = 'ALMACENERO'
UNION ALL
SELECT 'DESPACHO_ENVIADO', id, 'SOLICITANTE' FROM lb_rol WHERE codigo = 'RESIDENTE'
UNION ALL
SELECT 'STOCK_MINIMO', id, 'MISMO_PROYECTO' FROM lb_rol WHERE codigo = 'ALMACENERO'
UNION ALL
SELECT 'STOCK_MINIMO', id, 'GLOBAL' FROM lb_rol WHERE codigo = 'LOGISTICA'
UNION ALL
SELECT 'HERRAMIENTA_VENCIDA', id, 'MISMO_PROYECTO' FROM lb_rol WHERE codigo = 'ALMACENERO';

-- Fase F: pedido estancado en PENDIENTE o PENDIENTE_GERENTE más de N días — avisa a quien debe
-- revisarlo en ESE nivel (Residente para PENDIENTE, Gerencia para PENDIENTE_GERENTE) más a
-- Logística como respaldo global.
INSERT INTO lb_notif_regla (evento, rol_destino_id, scope_type)
SELECT 'PEDIDO_ESTANCADO_VISADO', id, 'MISMO_PROYECTO' FROM lb_rol WHERE codigo = 'RESIDENTE'
UNION ALL
SELECT 'PEDIDO_ESTANCADO_APROBACION', id, 'GLOBAL' FROM lb_rol WHERE codigo = 'GERENTE_GENERAL'
UNION ALL
SELECT 'PEDIDO_ESTANCADO_VISADO', id, 'GLOBAL' FROM lb_rol WHERE codigo = 'LOGISTICA'
UNION ALL
SELECT 'PEDIDO_ESTANCADO_APROBACION', id, 'GLOBAL' FROM lb_rol WHERE codigo = 'LOGISTICA';
