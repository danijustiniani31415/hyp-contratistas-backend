-- Conductores: vínculo opcional con Personas (trabajadores). Se completa solo, comparando el DNI:
-- si el DNI es de un trabajador queda enlazado; si no, es un conductor externo (persona_id NULL).
ALTER TABLE lb_guia_conductor ADD COLUMN IF NOT EXISTS persona_id INTEGER;

-- Enlaza los conductores que ya existían cuyo DNI coincide con un trabajador.
UPDATE lb_guia_conductor c
SET persona_id = p.id
FROM lb_persona p
WHERE c.persona_id IS NULL AND p.tipo_documento = 'DNI' AND p.numero_documento = c.num_doc;
