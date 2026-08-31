-- Convalidaciones: datos de cambio de puesto (origen/destino) y análisis de riesgo.
-- Riesgo: Oficina Central (id 3) = Bajo; Staff (id 2) y Obra (id 1) = Alto.
-- Ejecutar manualmente en pgAdmin contra la base de datos objetivo.

ALTER TABLE worker_emo_convalidaciones
  ADD COLUMN IF NOT EXISTS puesto_origen varchar(150),
  ADD COLUMN IF NOT EXISTS puesto_destino varchar(150),
  ADD COLUMN IF NOT EXISTS obra_oficina_staff_origen_id integer
    REFERENCES workers_obra_oficina_staff(workers_obra_oficina_staff_id),
  ADD COLUMN IF NOT EXISTS obra_oficina_staff_destino_id integer
    REFERENCES workers_obra_oficina_staff(workers_obra_oficina_staff_id),
  ADD COLUMN IF NOT EXISTS cambio_riesgo boolean NOT NULL DEFAULT false;
