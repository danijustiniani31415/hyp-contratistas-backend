-- Complemento a 2026-09-24_lb_variante_color.sql: la entrega de EPP a un trabajador también debe
-- poder capturar el color (chalecos, etc.), no solo la talla.
ALTER TABLE lb_entrega_epp_item ADD COLUMN IF NOT EXISTS color VARCHAR(30) NOT NULL DEFAULT '';
