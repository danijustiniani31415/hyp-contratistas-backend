-- ============================================================================
-- Personal Externo en workers_obra_oficina_staff  +  correccion de Victor Colonio
-- Fuente de datos: "DATA MAESTRA_RENOVACIONES 2026", hoja Data_Maestra
--                  (columna UBICACION = "PERSONAL EXTERNO")
--
-- Contexto: el calculo de cupos por razon social de Reclutamiento (GTH) cuenta
-- solo al personal que NO es de Obra. Los trabajadores cuya UBICACION en la
-- Data Maestra es "PERSONAL EXTERNO" (vigilantes, mantenimiento, chofer) se
-- habian metido provisionalmente como Staff; ahora tienen su propio valor de
-- catalogo y siguen consumiendo cupo de su razon social.
--
-- EJECUCION: sentencia por sentencia, SIN transaccion envolvente y SIN tablas
-- temporales (la VPS tiene poca RAM). Todo es idempotente.
--
-- Backend que acompana a este cambio:
--   Shared/Constants/ObraOficinaStaffIds.cs -> PersonalExterno = 4 y la lista
--   ConsumenCupoRazonSocial = { Staff, OficinaCentral, PersonalExterno },
--   usada por ReclutamientoRepository.GetDetalleGth.
--   OJO: StaffUOficinaCentral NO cambia — la usan SCTR/Vida Ley, Habilitacion,
--   Control de Acceso y Charlas con otro significado ("personal de escritorio").
-- ============================================================================


-- ---------------------------------------------------------------------------
-- 1. Catalogo: cuarto valor con id explicito, para que dev y prod coincidan.
--    El doble NOT EXISTS lo hace idempotente y respeta el indice unico
--    ux_workers_obra_oficina_staff_name_alive (un solo vivo por nombre).
-- ---------------------------------------------------------------------------
INSERT INTO workers_obra_oficina_staff
       (workers_obra_oficina_staff_id, name, display_order, active, state)
SELECT 4, 'Personal Externo', 4, true, true
WHERE NOT EXISTS (SELECT 1 FROM workers_obra_oficina_staff
                  WHERE workers_obra_oficina_staff_id = 4)
  AND NOT EXISTS (SELECT 1 FROM workers_obra_oficina_staff
                  WHERE lower(btrim(name)) = 'personal externo' AND state);

-- La PK tiene secuencia y el id se inserto a mano: hay que alinearla o el
-- proximo INSERT sin id explicito chocaria con la PK.
SELECT setval('workers_obra_oficina_staff_workers_obra_oficina_staff_id_seq',
              (SELECT max(workers_obra_oficina_staff_id)
               FROM workers_obra_oficina_staff),
              true);


-- ---------------------------------------------------------------------------
-- 2. Los 7 trabajadores con UBICACION = "PERSONAL EXTERNO" pasan de Staff (2)
--    a Personal Externo (4). Siguen consumiendo cupo de su razon social.
--
--    21119010 Ledesma Rosales Freddy          Benevento    Asistente de operaciones
--    22309907 Bendezu Taype Enrique Santos    Florencia    Vigilante
--    70508900 Pichi Cochachin Analy Estefany  Florencia    Personal de mantenimiento
--    40667405 Rodriguez Benancio Bendezu      Florencia    Chofer
--    73381463 Sanchez Casancho Erika          Florencia    Personal de mantenimiento
--    44002555 Campos Quinonez Ever Eliberto   Neo          Vigilante
--    71651478 Calderon Moreno Marimar         Salerno      Personal de mantenimiento
--
--    NO se tocan Caro Nino (73903392) ni Rodriguez Nino (74238911): su
--    UBICACION es el proyecto CEDRO 33, o sea Staff de verdad.
-- ---------------------------------------------------------------------------
UPDATE workers w
SET    obra_oficina_staff_id = 4,
       updated_at            = now()
FROM   person p
WHERE  w.person_id = p.person_id
  AND  ltrim(p.document_identity_code, '0') IN (
       '21119010', '22309907', '40667405', '44002555', '70508900', '71651478',
       '73381463')
  AND  w.estado IS DISTINCT FROM 'RETIRADO'
  AND  w.obra_oficina_staff_id IS DISTINCT FROM 4
  AND  w.id = (SELECT max(w2.id) FROM workers w2
               WHERE w2.person_id = w.person_id
                 AND w2.estado IS DISTINCT FROM 'RETIRADO');


-- ---------------------------------------------------------------------------
-- 3. Victor Alejandro Colonio Barrueto: DNI 76841265 -> 76841365 y
--    Thabit -> Seshat.
--
--    Reemplaza al intento anterior, que devolvio UPDATE 0 en produccion: la
--    guarda comparaba full_name con 'COLONIO BARRUETO VICTOR ALEJANDRO' y en
--    prod el nombre esta guardado como 'VICTOR ALEJANDRO COLONIO BARRUETO'.
--    Ahora la guarda es por apellidos sueltos, sin depender del orden.
-- ---------------------------------------------------------------------------

-- 3a. Previo (solo lectura): debe salir la ficha con el DNI viejo y ninguna
--     con el nuevo (person.document_identity_code tiene indice unico).
SELECT p.person_id, p.document_identity_code, p.full_name,
       w.id AS worker_id, w.estado, c.contributor_name, w.obra_oficina_staff_id
FROM person p
LEFT JOIN workers w ON w.person_id = p.person_id
LEFT JOIN contributor c ON c.contributor_id = w.contributor_id
WHERE ltrim(p.document_identity_code, '0') IN ('76841265', '76841365');

-- 3b. Corregir el DNI.
UPDATE person p
SET    document_identity_code = '76841365',
       updated_date_time      = now()
WHERE  p.document_identity_code = '76841265'
  AND  upper(p.full_name) LIKE '%COLONIO%'
  AND  upper(p.full_name) LIKE '%BARRUETO%'
  AND  NOT EXISTS (SELECT 1 FROM person p2
                   WHERE p2.document_identity_code = '76841365');

-- 3c. Moverlo de Thabit a Seshat.
UPDATE workers w
SET    contributor_id = c.contributor_id,
       updated_at     = now()
FROM   person p, contributor c
WHERE  w.person_id = p.person_id
  AND  p.document_identity_code = '76841365'
  AND  c.state AND c.active AND c.operativo
  AND  upper(translate(replace(replace(c.contributor_name, '.', ''), ' ', ''),
             'ÁÉÍÓÚÑáéíóúñ', 'AEIOUNAEIOUN')) = 'SESHATINMOBILIARIASAC'
  AND  w.estado IS DISTINCT FROM 'RETIRADO'
  AND  w.contributor_id IS DISTINCT FROM c.contributor_id
  AND  w.id = (SELECT max(w2.id) FROM workers w2
               WHERE w2.person_id = w.person_id
                 AND w2.estado IS DISTINCT FROM 'RETIRADO');


-- ---------------------------------------------------------------------------
-- 4. Verificacion (solo lectura).
-- ---------------------------------------------------------------------------

-- 4a. Catalogo.
SELECT * FROM workers_obra_oficina_staff ORDER BY display_order;

-- 4b. Las fichas tocadas.
SELECT ltrim(p.document_identity_code, '0') AS doc,
       COALESCE(p.full_name, w.apellido_nombre) AS trabajador,
       c.contributor_name AS razon_social,
       COALESCE(oo.name, '(sin valor)') AS obra_oficina, w.estado
FROM workers w
LEFT JOIN person p ON p.person_id = w.person_id
LEFT JOIN contributor c ON c.contributor_id = w.contributor_id
LEFT JOIN workers_obra_oficina_staff oo
       ON oo.workers_obra_oficina_staff_id = w.obra_oficina_staff_id
WHERE ltrim(p.document_identity_code, '0') IN (
      '21119010', '22309907', '40667405', '44002555', '70508900', '71651478',
      '73381463', '73903392', '74238911', '76841365', '76841265')
ORDER BY 4, 3, 2;

-- 4c. Cupos por razon social con el criterio nuevo del modal de reclutamiento:
--     Staff (2), Oficina Central (3) o Personal Externo (4), no retirados,
--     sin practicantes; tope 20.
SELECT c.contributor_name AS razon_social,
       count(w.id)        AS ocupan_cupo,
       greatest(0, 20 - count(w.id)) AS cupos_disponibles
FROM contributor c
LEFT JOIN workers w
       ON w.contributor_id = c.contributor_id
      AND w.estado IS DISTINCT FROM 'RETIRADO'
      AND w.obra_oficina_staff_id IN (2, 3, 4)
      AND (w.categoria IS NULL OR lower(btrim(w.categoria)) <> 'practicante')
WHERE c.state AND c.active AND c.operativo
GROUP BY 1 ORDER BY 2 DESC, 1;
