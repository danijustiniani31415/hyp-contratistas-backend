-- Consolidación del catálogo ss_tipo_equipo (nació de texto libre, quedó con muchas
-- variantes de mayúsculas/tildes/typos para el mismo equipo) + asignación de
-- entregables específicos por tipo. Ejecutar manualmente en pgAdmin, en orden.

-- ══════════════════════════════════════════════════════════════════
-- PARTE 1: Consolidar duplicados
-- Patrón por grupo: repuntar los equipos existentes al id "canónico",
-- renombrar ese id con el nombre limpio, y desactivar los duplicados
-- (no se borran: quedan por si algo histórico los referencia).
-- ══════════════════════════════════════════════════════════════════

-- Amoladora (6) — ya está limpio, sin duplicados.

-- Andamio Colgante Eléctrico: 41, 13 -> 25 (25 ya tenía tildes correctas)
UPDATE ss_equipo SET tipo_equipo_id = 25 WHERE tipo_equipo_id IN (41, 13);
UPDATE ss_tipo_equipo SET nombre = 'Andamio Colgante Eléctrico', updated_at = NOW() WHERE id = 25;
UPDATE ss_tipo_equipo SET activo = false, updated_at = NOW() WHERE id IN (41, 13);

-- Andamio Colgante (57), Andamio Colgante Eléctrico - Prensa (7),
-- - Sistema de Anclaje (46), - Contrapeso (35): quedan como tipos propios (no se fusionan).
UPDATE ss_tipo_equipo SET nombre = 'Andamio Colgante Eléctrico - Prensa', updated_at = NOW() WHERE id = 7;
UPDATE ss_tipo_equipo SET nombre = 'Andamio Colgante Eléctrico - Sistema de Anclaje', updated_at = NOW() WHERE id = 46;
UPDATE ss_tipo_equipo SET nombre = 'Andamio Colgante Eléctrico - Contrapeso', updated_at = NOW() WHERE id = 35;

-- Camión Bomba Hormigonera: 59, 28, 58 -> 5 (los números de placa son datos del equipo, no del tipo)
UPDATE ss_equipo SET tipo_equipo_id = 5 WHERE tipo_equipo_id IN (59, 28, 58);
UPDATE ss_tipo_equipo SET nombre = 'Camión Bomba Hormigonera', updated_at = NOW() WHERE id = 5;
UPDATE ss_tipo_equipo SET activo = false, updated_at = NOW() WHERE id IN (59, 28, 58);

-- Camión Grúa: 22, 55, 38 -> 8
UPDATE ss_equipo SET tipo_equipo_id = 8 WHERE tipo_equipo_id IN (22, 55, 38);
UPDATE ss_tipo_equipo SET nombre = 'Camión Grúa', updated_at = NOW() WHERE id = 8;
UPDATE ss_tipo_equipo SET activo = false, updated_at = NOW() WHERE id IN (22, 55, 38);

-- Compresora de Aire Portátil: 51, 54, 48 -> 14
UPDATE ss_equipo SET tipo_equipo_id = 14 WHERE tipo_equipo_id IN (51, 54, 48);
UPDATE ss_tipo_equipo SET nombre = 'Compresora de Aire Portátil', updated_at = NOW() WHERE id = 14;
UPDATE ss_tipo_equipo SET activo = false, updated_at = NOW() WHERE id IN (51, 54, 48);

-- Esmeril (1) — ya está limpio.

-- Excavadora: 11, 31 -> 17
UPDATE ss_equipo SET tipo_equipo_id = 17 WHERE tipo_equipo_id IN (11, 31);
UPDATE ss_tipo_equipo SET nombre = 'Excavadora', updated_at = NOW() WHERE id = 17;
UPDATE ss_tipo_equipo SET activo = false, updated_at = NOW() WHERE id IN (11, 31);

-- Excavadora sobre Oruga: 60 -> 42
UPDATE ss_equipo SET tipo_equipo_id = 42 WHERE tipo_equipo_id IN (60);
UPDATE ss_tipo_equipo SET nombre = 'Excavadora sobre Oruga', updated_at = NOW() WHERE id = 42;
UPDATE ss_tipo_equipo SET activo = false, updated_at = NOW() WHERE id IN (60);

-- Excavadora Neumática: 9, 52 -> 45
UPDATE ss_equipo SET tipo_equipo_id = 45 WHERE tipo_equipo_id IN (9, 52);
UPDATE ss_tipo_equipo SET nombre = 'Excavadora Neumática', updated_at = NOW() WHERE id = 45;
UPDATE ss_tipo_equipo SET activo = false, updated_at = NOW() WHERE id IN (9, 52);

-- Elevador de Plataforma Tijera (39) y Elevador de Plataforma Aérea (62): tipos distintos, no se fusionan.
UPDATE ss_tipo_equipo SET nombre = 'Elevador de Plataforma Aérea', updated_at = NOW() WHERE id = 62;

-- Manlift (15): equipo distinto al elevador de plataforma aérea, no se fusiona.

-- Freno para Cuerda (44) — normalizar nombre.
UPDATE ss_tipo_equipo SET nombre = 'Freno para Cuerda', updated_at = NOW() WHERE id = 44;

-- Grupo Electrógeno (3), Inyectora (27) — ya están limpios.

-- Grúa Torre (2): queda sola, entregables de montaje propios (ver PARTE 2).

-- Grúa Móvil: 20 -> 29
UPDATE ss_equipo SET tipo_equipo_id = 29 WHERE tipo_equipo_id IN (20);
UPDATE ss_tipo_equipo SET nombre = 'Grúa Móvil', updated_at = NOW() WHERE id = 29;
UPDATE ss_tipo_equipo SET activo = false, updated_at = NOW() WHERE id IN (20);

-- Grúa Telescópica: 50, 10 -> 21
UPDATE ss_equipo SET tipo_equipo_id = 21 WHERE tipo_equipo_id IN (50, 10);
UPDATE ss_tipo_equipo SET nombre = 'Grúa Telescópica', updated_at = NOW() WHERE id = 21;
UPDATE ss_tipo_equipo SET activo = false, updated_at = NOW() WHERE id IN (50, 10);

-- Grúa (genérico / comodín para lo no especificado): 12, 33 -> 40
UPDATE ss_equipo SET tipo_equipo_id = 40 WHERE tipo_equipo_id IN (12, 33);
UPDATE ss_tipo_equipo SET nombre = 'Grúa', updated_at = NOW() WHERE id = 40;
UPDATE ss_tipo_equipo SET activo = false, updated_at = NOW() WHERE id IN (12, 33);

-- Máquina de Soldar (32) — agregar tilde.
UPDATE ss_tipo_equipo SET nombre = 'Máquina de Soldar', updated_at = NOW() WHERE id = 32;

-- Minicargador: 26 -> 61
UPDATE ss_equipo SET tipo_equipo_id = 61 WHERE tipo_equipo_id IN (26);
UPDATE ss_tipo_equipo SET nombre = 'Minicargador', updated_at = NOW() WHERE id = 61;
UPDATE ss_tipo_equipo SET activo = false, updated_at = NOW() WHERE id IN (26);

-- Montacarga: 4, 24, 36 -> 16
UPDATE ss_equipo SET tipo_equipo_id = 16 WHERE tipo_equipo_id IN (4, 24, 36);
UPDATE ss_tipo_equipo SET nombre = 'Montacarga', updated_at = NOW() WHERE id = 16;
UPDATE ss_tipo_equipo SET activo = false, updated_at = NOW() WHERE id IN (4, 24, 36);

-- Perforadora Hidráulica (49) — ya está limpio, distinto de "sobre Orugas".
-- Perforadora sobre Orugas: 56 -> 30
UPDATE ss_equipo SET tipo_equipo_id = 30 WHERE tipo_equipo_id IN (56);
UPDATE ss_tipo_equipo SET nombre = 'Perforadora sobre Orugas', updated_at = NOW() WHERE id = 30;
UPDATE ss_tipo_equipo SET activo = false, updated_at = NOW() WHERE id IN (56);

-- Placing Boom (47), Rectificadora (37), Rodillo (23), Rotomartillo (53),
-- Sopladora (34), Tecle Eléctrico (19) — ya están limpios, sin duplicados.

-- Trípode (43) — agregar tilde.
UPDATE ss_tipo_equipo SET nombre = 'Trípode', updated_at = NOW() WHERE id = 43;

-- Volquete (18) — ya está limpio, sin cambios.

-- Verificación: no debe quedar ningún equipo apuntando a un tipo desactivado.
-- SELECT e.id, e.tipo_equipo_id, t.nombre, t.activo
-- FROM ss_equipo e JOIN ss_tipo_equipo t ON t.id = e.tipo_equipo_id
-- WHERE t.activo = false;

-- ══════════════════════════════════════════════════════════════════
-- PARTE 2: Entregables específicos de Grúa Torre (id 2)
-- Ítems de montaje/izaje que hoy son genéricos (se le piden a TODOS los
-- equipos, incluido el volquete). Pasan a exigirse solo a Grúa Torre.
-- ══════════════════════════════════════════════════════════════════

UPDATE ss_item_equipo
SET tipo_equipo_id = 2
WHERE nombre IN ('Memoria de Calculo', 'Procedimiento de Montaje', 'Certificado de Instalacion', 'Certificado de cables');

-- ══════════════════════════════════════════════════════════════════
-- PARTE 3: Entregables específicos de Volquete (id 18)
-- Confirmados: SOAT, Tarjeta de Propiedad, Revisión Técnica.
-- Sugeridos además (típicos en obra de construcción en Perú) — revisa y
-- borra las líneas que no apliquen a tu operación antes de correr esto:
--   - Certificado de Alarma de Retroceso
--   - Certificado de Extintor Vehicular
--   - Constancia de Botiquín de Primeros Auxilios
-- ══════════════════════════════════════════════════════════════════

INSERT INTO ss_item_equipo (nombre, requiere_vigencia, orden, activo, tipo_equipo_id) VALUES
    ('SOAT', true, 12, true, 18),
    ('Tarjeta de Propiedad', false, 13, true, 18),
    ('Revisión Técnica', true, 14, true, 18),
    ('Certificado de Alarma de Retroceso', true, 15, true, 18),
    ('Certificado de Extintor Vehicular', true, 16, true, 18),
    ('Constancia de Botiquín de Primeros Auxilios', false, 17, true, 18);
