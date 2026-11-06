-- Fase C (control de costos): presupuesto por partida + moneda, y trazabilidad de a qué partida
-- se cargó cada movimiento de kardex (salida de stock), para poder calcular consumido vs
-- presupuestado. Ver CONTEXT_LOGISTICA.md — este catálogo lb_moneda es propio de Las Bravas
-- (lb_*), no reutiliza la tabla "currency" heredada del fork de Abril (otro dominio, sin
-- prefijo lb_, no seguía la convención de este proyecto).

CREATE TABLE lb_moneda (
    id      SMALLSERIAL PRIMARY KEY,
    codigo  VARCHAR(5) NOT NULL UNIQUE,
    nombre  VARCHAR(40) NOT NULL
);
INSERT INTO lb_moneda (codigo, nombre) VALUES ('PEN', 'Soles'), ('USD', 'Dólares');

ALTER TABLE lb_partida ADD COLUMN moneda_id SMALLINT REFERENCES lb_moneda(id);
ALTER TABLE lb_partida ADD COLUMN monto_presupuestado NUMERIC(14,2);

ALTER TABLE lb_movimiento ADD COLUMN partida_id INT REFERENCES lb_partida(id);
CREATE INDEX idx_movimiento_partida ON lb_movimiento(partida_id) WHERE partida_id IS NOT NULL;
