-- Guías de remisión: el stock y el avance del pedido se mueven recién cuando SUNAT ACEPTA la guía,
-- no al crear el borrador. despacho_aplicado marca si ese movimiento ya ocurrió.

-- Las guías existentes movieron stock y pedido al crearse (modelo anterior): se marcan TRUE para que
-- eliminarlas siga revirtiendo todo. Las nuevas nacen en FALSE.
ALTER TABLE lb_guia_remision ADD COLUMN IF NOT EXISTS despacho_aplicado BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE lb_guia_remision ALTER COLUMN despacho_aplicado SET DEFAULT FALSE;
