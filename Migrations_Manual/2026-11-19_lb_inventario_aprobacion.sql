-- Control del inventario físico: si el valor de las diferencias supera un monto configurable, el cierre queda pendiente de
-- aprobación de gerencia y el stock no cambia hasta que se apruebe. El monto lo edita quien tenga INVENTARIO_APROBAR.
-- Idempotente. Ids de permiso con MAX()+n (las secuencias pueden ir atrasadas).

CREATE TABLE IF NOT EXISTS lb_parametro (
    clave           varchar(60)  PRIMARY KEY,
    valor           text         NOT NULL,
    actualizado_por bigint       NULL,
    actualizado_en  timestamptz  NOT NULL DEFAULT now()
);
INSERT INTO lb_parametro (clave, valor) VALUES ('INVENTARIO_UMBRAL_APROBACION', '500') ON CONFLICT (clave) DO NOTHING;

ALTER TABLE lb_inventario ADD COLUMN IF NOT EXISTS valor_faltante     numeric(14,2) NOT NULL DEFAULT 0;
ALTER TABLE lb_inventario ADD COLUMN IF NOT EXISTS valor_sobrante     numeric(14,2) NOT NULL DEFAULT 0;
ALTER TABLE lb_inventario ADD COLUMN IF NOT EXISTS umbral_aplicado    numeric(14,2) NULL;
ALTER TABLE lb_inventario ADD COLUMN IF NOT EXISTS solicitado_por     bigint        NULL;
ALTER TABLE lb_inventario ADD COLUMN IF NOT EXISTS solicitado_en      timestamptz   NULL;
ALTER TABLE lb_inventario ADD COLUMN IF NOT EXISTS decidido_por       bigint        NULL;
ALTER TABLE lb_inventario ADD COLUMN IF NOT EXISTS decidido_en        timestamptz   NULL;
ALTER TABLE lb_inventario ADD COLUMN IF NOT EXISTS motivo_decision    text          NULL;

INSERT INTO lb_permiso (id, codigo, descripcion)
SELECT (SELECT COALESCE(MAX(id), 0) FROM lb_permiso) + 1, 'INVENTARIO_APROBAR',
       'Aprobar o rechazar ajustes de inventario físico grandes y definir desde qué monto se exige aprobación'
WHERE NOT EXISTS (SELECT 1 FROM lb_permiso WHERE codigo = 'INVENTARIO_APROBAR');

INSERT INTO lb_rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id
FROM lb_rol r
JOIN (VALUES ('ADMIN', 'INVENTARIO_APROBAR'), ('GERENTE_GENERAL', 'INVENTARIO_APROBAR'), ('GERENTE_OPERACIONES', 'INVENTARIO_APROBAR')) AS m(rol, permiso) ON m.rol = r.codigo
JOIN lb_permiso p ON p.codigo = m.permiso
WHERE NOT EXISTS (SELECT 1 FROM lb_rol_permiso rp WHERE rp.rol_id = r.id AND rp.permiso_id = p.id);

SELECT setval(pg_get_serial_sequence('lb_permiso', 'id'), (SELECT MAX(id) FROM lb_permiso));

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'hyp_local') THEN
        GRANT ALL ON lb_parametro, lb_inventario TO hyp_local;
    END IF;
END $$;
