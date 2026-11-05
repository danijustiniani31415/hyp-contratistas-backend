-- Guías de remisión: DNI del conductor. SUNAT lo exige como documento de identidad en el XML;
-- la licencia de conducir va como referencia aparte.
ALTER TABLE lb_guia_remision ADD COLUMN IF NOT EXISTS conductor_dni VARCHAR(15);
