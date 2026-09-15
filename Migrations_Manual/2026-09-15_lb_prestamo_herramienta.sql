-- Préstamo de Herramientas y Equipos (Fase 2, punto 6 de CONTEXT_LOGISTICA.md sección 3).
-- Diferencia real con Pedidos/EPP: el producto vuelve. Se maneja con el mismo lb_movimiento —
-- SALIDA al prestar, INGRESO al devolver — en vez de inventar un concepto de stock paralelo.
-- El estado vive por ITEM (no en el header): puedes devolver el taladro hoy y la escalera la
-- semana que viene sin que el préstamo completo quede en un estado ambiguo.

CREATE TABLE lb_prestamo (
    id              BIGSERIAL PRIMARY KEY,
    codigo          VARCHAR(20) NOT NULL UNIQUE, -- PRES-2026-000001, la "hoja de ruta de retiro"
    almacen_id      INT NOT NULL REFERENCES lb_almacen(id),
    persona_id      INT NOT NULL REFERENCES lb_persona(id), -- quien se lleva la herramienta
    proyecto_id     INT REFERENCES lb_proyecto(id), -- opcional: a que proyecto/frente va
    prestado_por_usuario_sistema_id BIGINT NOT NULL REFERENCES lb_usuario_sistema(id),
    fecha_devolucion_estimada DATE,
    observacion     TEXT,
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_prestamo_persona ON lb_prestamo(persona_id);

CREATE TABLE lb_prestamo_item (
    id              BIGSERIAL PRIMARY KEY,
    prestamo_id     BIGINT NOT NULL REFERENCES lb_prestamo(id),
    producto_id     BIGINT NOT NULL REFERENCES lb_producto(id), -- debe ser es_retornable = true
    talla           VARCHAR(10) NOT NULL DEFAULT '',
    cantidad        NUMERIC(12,2) NOT NULL,
    estado          VARCHAR(20) NOT NULL DEFAULT 'PRESTADO', -- PRESTADO, DEVUELTO, PERDIDO, DANADO
    devuelto_por_usuario_sistema_id BIGINT REFERENCES lb_usuario_sistema(id),
    fecha_devolucion TIMESTAMPTZ,
    observacion_devolucion TEXT
);
CREATE INDEX idx_prestamo_item_prestamo ON lb_prestamo_item(prestamo_id);
CREATE INDEX idx_prestamo_item_estado ON lb_prestamo_item(estado) WHERE estado = 'PRESTADO';

CREATE SEQUENCE lb_prestamo_correlativo START 1;

INSERT INTO lb_permiso (codigo, descripcion) VALUES
('HERRAMIENTA_PRESTAR',  'Registrar préstamo de herramienta/equipo (descuenta stock)'),
('HERRAMIENTA_DEVOLVER', 'Registrar devolución de herramienta/equipo (repone stock, o marca pérdida/daño)');

INSERT INTO lb_rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id FROM lb_rol r, lb_permiso p
WHERE p.codigo IN ('HERRAMIENTA_PRESTAR', 'HERRAMIENTA_DEVOLVER')
  AND r.codigo IN ('ALMACENERO', 'ADMIN');
