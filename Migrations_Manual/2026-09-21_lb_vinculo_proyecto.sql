-- Sede/proyecto de la persona (a qué unidad pertenece: Central Lima, Las Bravas, ...) — pedido
-- explícito del usuario 2026-09-21 para poder filtrar accesos por sede.
-- No confundir con lb_usuario_asignacion.proyecto_id (el scope de PERMISOS del usuario de
-- sistema) — normalmente van a coincidir, pero son conceptos distintos (uno es dato de RR.HH.,
-- el otro es seguridad).

ALTER TABLE lb_vinculo_laboral ADD COLUMN IF NOT EXISTS proyecto_id INT REFERENCES lb_proyecto(id);

-- lb_proyecto estaba vacía (sin seed, sin CRUD todavía) — sembramos las 2 sedes que hay hoy.
INSERT INTO lb_proyecto (codigo, nombre, ubicacion, estado)
SELECT v.codigo, v.nombre, v.ubicacion, 'ACTIVO' FROM (VALUES
    ('CENTRAL', 'Central Lima', 'Lima'),
    ('LASBRAVAS', 'Las Bravas', NULL)
) AS v(codigo, nombre, ubicacion)
WHERE NOT EXISTS (SELECT 1 FROM lb_proyecto p WHERE p.codigo = v.codigo);
