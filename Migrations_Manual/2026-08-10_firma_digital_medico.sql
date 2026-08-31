-- Firma digital del médico (imagen PNG, idealmente sin fondo) que se imprime junto a un
-- recuadro para la firma manuscrita de comparación en el SSO-FO-149.
-- Ejecutar manualmente en pgAdmin.

ALTER TABLE ss_medicos_ocupacionales
  ADD COLUMN IF NOT EXISTS firma_digital_url varchar(500);
