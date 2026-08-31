-- Clasificación de riesgo (Obra/Staff/Oficina Central) por vinculación, no solo en workers.
-- Permite saber qué clasificación tenía el trabajador en cada obra/empresa pasada — necesario
-- para evaluar convalidaciones de EMO con precisión histórica, y para detectar cambios de
-- riesgo (Oficina Central → Staff/Obra) al momento de cada cambio de obra/puesto.
-- Ejecutar manualmente en pgAdmin contra la base de datos objetivo.

ALTER TABLE worker_vinculaciones
  ADD COLUMN IF NOT EXISTS obra_oficina_staff_id integer
    REFERENCES workers_obra_oficina_staff(workers_obra_oficina_staff_id);
