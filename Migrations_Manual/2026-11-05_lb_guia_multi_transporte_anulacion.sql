-- Guías de remisión: varios conductores y vehículos, y anulación con rastro auditable.

-- Vehículo principal: tarjeta de circulación. Secundarios: copia congelada en JSON (como el principal).
ALTER TABLE lb_guia_remision ADD COLUMN IF NOT EXISTS vehiculo_nro_circulacion VARCHAR(30);
ALTER TABLE lb_guia_remision ADD COLUMN IF NOT EXISTS conductores_secundarios_json TEXT;
ALTER TABLE lb_guia_remision ADD COLUMN IF NOT EXISTS vehiculos_secundarios_json TEXT;

-- Anulación: SUNAT solo permite la baja desde SOL, así que se exige y guarda el motivo y la constancia.
ALTER TABLE lb_guia_remision ADD COLUMN IF NOT EXISTS anulacion_motivo VARCHAR(300);
ALTER TABLE lb_guia_remision ADD COLUMN IF NOT EXISTS anulacion_constancia VARCHAR(60);
ALTER TABLE lb_guia_remision ADD COLUMN IF NOT EXISTS anulada_por_usuario_sistema_id BIGINT;
ALTER TABLE lb_guia_remision ADD COLUMN IF NOT EXISTS anulada_en TIMESTAMPTZ;
