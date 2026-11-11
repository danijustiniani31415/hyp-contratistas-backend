-- Pagos a proveedores (cuentas por pagar): cada pago se registra contra un comprobante de compra; el saldo es
-- total - pagos vigentes. Un pago anulado deja de contar pero queda en el historial. Idempotente.

CREATE TABLE IF NOT EXISTS lb_pago_proveedor (
    id                       bigserial PRIMARY KEY,
    comprobante_compra_id    bigint        NOT NULL REFERENCES lb_comprobante_compra(id),
    fecha_pago               date          NOT NULL,
    monto                    numeric(14,2) NOT NULL CHECK (monto > 0),
    medio                    varchar(20)   NOT NULL,
    referencia               varchar(80)   NULL,
    observacion              text          NULL,
    estado                   varchar(12)   NOT NULL DEFAULT 'VIGENTE',
    registrado_por           bigint        NOT NULL,
    registrado_en            timestamptz   NOT NULL DEFAULT now(),
    anulado_por              bigint        NULL,
    anulado_en               timestamptz   NULL,
    motivo_anulacion         text          NULL
);

CREATE INDEX IF NOT EXISTS ix_lb_pago_proveedor_comprobante ON lb_pago_proveedor (comprobante_compra_id);
