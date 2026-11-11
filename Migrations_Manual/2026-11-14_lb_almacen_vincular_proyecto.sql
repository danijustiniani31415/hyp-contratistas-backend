-- Los almacenes tipo PROYECTO estaban sin proyecto_id, así que el alcance por proyecto (AlmacenAlcance) dejaba a
-- residentes/administradores de obra sin ver NINGÚN almacén. Hoy existe un solo proyecto real (LASBRAVAS, en la Unidad
-- Minera Esperanza), así que todo almacén de proyecto sin vincular se asocia a él. El almacén CENTRAL queda sin proyecto.
-- Idempotente.
UPDATE lb_almacen
SET proyecto_id = (SELECT id FROM lb_proyecto WHERE codigo = 'LASBRAVAS')
WHERE tipo = 'PROYECTO' AND proyecto_id IS NULL;
