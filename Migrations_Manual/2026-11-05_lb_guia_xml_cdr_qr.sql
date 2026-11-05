-- Guías de remisión: se conservan el XML firmado y el CDR de SUNAT (respaldo legal) y el enlace del QR oficial.
-- SUNAT emite el QR al aceptar la guía; si lo entrega en su respuesta o en el CDR queda en qr_url.
ALTER TABLE lb_guia_remision ADD COLUMN IF NOT EXISTS xml_firmado TEXT;
ALTER TABLE lb_guia_remision ADD COLUMN IF NOT EXISTS cdr_xml TEXT;
ALTER TABLE lb_guia_remision ADD COLUMN IF NOT EXISTS qr_url TEXT;
