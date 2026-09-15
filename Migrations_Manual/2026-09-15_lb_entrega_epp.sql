-- Entrega de EPP individual (Fase 2, punto 5 de CONTEXT_LOGISTICA.md sección 3).
-- Sin DDL propio en el documento — diseñado ahora reutilizando lb_almacen/lb_stock/lb_movimiento
-- (Fase 1) y respetando la regla ya especificada en sección 4.1: el tipo de vínculo de la persona
-- (lb_tipo_vinculo.requiere_epp) decide si puede recibir EPP, no un código de vínculo hardcodeado.

CREATE TABLE lb_entrega_epp (
    id              BIGSERIAL PRIMARY KEY,
    persona_id      INT NOT NULL REFERENCES lb_persona(id), -- quien RECIBE el EPP (el trabajador)
    almacen_id      INT NOT NULL REFERENCES lb_almacen(id),
    entregado_por_usuario_sistema_id BIGINT NOT NULL REFERENCES lb_usuario_sistema(id),
    observacion     TEXT,
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_entrega_epp_persona ON lb_entrega_epp(persona_id);

CREATE TABLE lb_entrega_epp_item (
    id              BIGSERIAL PRIMARY KEY,
    entrega_id      BIGINT NOT NULL REFERENCES lb_entrega_epp(id),
    producto_id     BIGINT NOT NULL REFERENCES lb_producto(id),
    talla           VARCHAR(10) NOT NULL DEFAULT '',
    cantidad        NUMERIC(12,2) NOT NULL
);
CREATE INDEX idx_entrega_epp_item_entrega ON lb_entrega_epp_item(entrega_id);

INSERT INTO lb_permiso (codigo, descripcion) VALUES
('EPP_ENTREGAR', 'Registrar entrega de EPP a un trabajador (descuenta stock)');

INSERT INTO lb_rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id FROM lb_rol r, lb_permiso p
WHERE p.codigo = 'EPP_ENTREGAR' AND r.codigo IN ('ALMACENERO', 'ADMIN');
