-- ============================================================================
-- workers.categoria_id se baja: la categoria del trabajador sale de su puesto
-- ============================================================================
-- Modelo que queda:
--   puesto    -> nombre visible, "maleable", se puede poner cualquier cosa.
--                NUNCA se usa para filtrar ni decidir nada.
--   categoria -> el campo de LOGICA (filtros, permisos, reglas). Ya no vive en
--                `workers`: se llega por `workers.puesto_id -> puesto.categoria_id`.
--
-- Antes `workers` tenia las dos FK y podian contradecirse. En prod 894 de 3.376
-- fichas (26%) tienen una categoria distinta a la de su puesto (verificado el
-- 2026-08-21 contra prod). Al bajar la columna, esas 894 pasan a la del puesto.
--
-- Eso NO es perdida de informacion arbitraria: `puesto.categoria_id` se derivo de
-- la categoria mas frecuente entre quienes ejercen ese puesto (ver Shared/Models/
-- Puesto.cs), asi que la minoria se alinea con la mayoria de su mismo puesto.
-- Casi todas son categorias de obra (PEON / OFICIAL / OPERARIO sobre ALBANIL,
-- CARPINTERO, TODISTA...), que solo se muestran y se filtran desde la UI.
--
-- REVISAR: el PASO 0b lista 131 fichas cuya categoria cambia Y toca alguna
-- categoria con logica detras. No hay que revisarlas una por una: 122 de ellas
-- solo mueven reglas de DATA de SSOMA / Habilitacion (que entregables lleva,
-- que charlas le tocan, que EMO le corresponde) y son CAPATAZ, SUPERVISOR,
-- PREVENCIONISTA, RIGGER, OPERADOR, VIGIA y PRACTICANTE mezclados entre si
-- sobre puestos de obra. Ahi el puesto es la mejor lectura de que hace cada uno.
--
-- Las que importan son las 9 que cambian PERMISOS -- quien ve y quien aprueba:
--   worker 12128  GERENTE            -> PEON              (puesto ACI)
--   worker 12621  GERENTE            -> SUPERVISOR        (puesto SUPERVISOR)
--   worker 12831  GERENTE            -> SUPERVISOR        (puesto INGENIERO DE PROYECTOS)
--   worker 12153  JEFE               -> OPERARIO          (puesto CARPINTERO)
--   worker 13017  JEFE               -> PREVENCIONISTA    (puesto SSOMA)
--   worker 12341  COORDINADOR        -> ASISTENTE         (puesto ASISTENTE DE LOGISTICA)
--   worker 13901  COORDINADOR SSOMA  -> PREVENCIONISTA    (puesto INGENIERO DE SSOMA)
--   worker 13385  INGENIERO          -> COORDINADOR       (gana nivel)
--   worker 15132  EMPLEADO           -> COORDINADOR SSOMA (gana nivel)
-- Los tres GERENTE y los dos JEFE pierden lo que hoy pueden hacer en Aprobaciones
-- GTH, Gestion de Salidas, Revisores de Areas y Lecciones Aprendidas. Si alguno
-- debe seguir siendo gerente o jefe, corregirle el PUESTO antes del PASO 5.
-- El PASO 0 los vuelve a listar para confirmar sobre datos frescos.
--
-- Correr PASO por PASO en pgAdmin. Cada paso es idempotente.
-- ============================================================================


-- ============================================================================
-- PASO 0 - Diagnostico. No modifica nada: correrlo y leer la salida.
-- ============================================================================

-- 0a. Cuanta gente cambia de categoria.
SELECT count(*)                                                             AS fichas_totales,
       count(*) FILTER (WHERE w.puesto_id IS NULL)                          AS sin_puesto,
       count(*) FILTER (WHERE w.puesto_id IS NOT NULL
                          AND w.categoria_id IS DISTINCT FROM p.categoria_id) AS cambian_de_categoria
FROM workers w
LEFT JOIN puesto p ON p.puesto_id = w.puesto_id;

-- 0b. Las fichas que tocan categorias con logica detras (las que hay que revisar).
--     4=PRACTICANTE 8=RESIDENTE 11=GERENTE 17=JEFE 22=COORDINADOR 26=MEDICO
--     29=SUB GERENTE 37=SUPERVISOR 39=GERENTE GENERAL 40=GERENTE ADM Y FIN
--     41=COORDINADOR SSOMA, mas 10=RIGGER 21=OPERADOR 31=VIGIA 35=PREVENCIONISTA
--     36=CAPATAZ (reglas de Habilitacion guardadas en data).
SELECT w.id AS worker_id, w.area,
       wc.nombre AS categoria_hoy, p.nombre AS puesto, pc.nombre AS categoria_al_migrar
FROM workers w
JOIN puesto p          ON p.puesto_id = w.puesto_id
LEFT JOIN categoria wc ON wc.categoria_id = w.categoria_id
LEFT JOIN categoria pc ON pc.categoria_id = p.categoria_id
WHERE w.categoria_id IS DISTINCT FROM p.categoria_id
  AND (w.categoria_id IN (4,8,10,11,17,21,22,26,29,31,35,36,37,39,40,41)
    OR p.categoria_id IN (4,8,10,11,17,21,22,26,29,31,35,36,37,39,40,41))
ORDER BY w.id;

-- 0c. Puestos sin categoria: al terminar la migracion no puede quedar ninguno.
SELECT puesto_id, nombre, active, state FROM puesto WHERE categoria_id IS NULL ORDER BY puesto_id;


-- ============================================================================
-- PASO 1 - puesto.categoria_id deja de poder ser NULL.
-- Es la pieza que sostiene el modelo nuevo: un puesto sin categoria seria un
-- trabajador sin categoria, o sea invisible para todo filtro y toda regla.
--
-- REVISAR: los 8 puestos que hoy estan en NULL son justamente los que no tenian
-- senal suficiente para deducirles la categoria. Estos valores son la mejor
-- lectura de los datos, no un dato duro - cambiarlos aca si GTH sabe otra cosa:
--   270 PERSONAL DE MANTENIMIENTO -> TECNICO (2 de sus 5 fichas ya son TECNICO)
--   333 VIGILANTE                 -> AGENTE DE VIGILANCIA
--   324 TESORERA                  -> EMPLEADO (planilla sin categoria propia)
--    99 COCINERO, 127 ECONOMISTA, 174 INTELIGENCIA COMERCIAL, 196 LLAVERO,
--   328 TRADE MARKETING           -> EMPLEADO (los 5 tienen state=false, o sea
--                                    eliminados: el valor solo cumple el NOT NULL)
-- ============================================================================
BEGIN;

UPDATE puesto SET categoria_id = 20, updated_date_time = now()
WHERE categoria_id IS NULL AND nombre = 'PERSONAL DE MANTENIMIENTO';

UPDATE puesto SET categoria_id = 32, updated_date_time = now()
WHERE categoria_id IS NULL AND nombre = 'VIGILANTE';

-- El resto (TESORERA y los cinco soft-delete) cae en EMPLEADO.
UPDATE puesto SET categoria_id = 42, updated_date_time = now()
WHERE categoria_id IS NULL;

ALTER TABLE puesto ALTER COLUMN categoria_id SET NOT NULL;

COMMIT;


-- ============================================================================
-- PASO 2 - Guardar la categoria divergente antes de perderla.
-- `worker_vinculaciones.categoria_id` ya es el historico de categoria por
-- vinculacion y en prod cubre 878 de las 894 divergencias. Este paso completa las
-- 16 que faltan, para que ninguna categoria que existio hoy quede sin rastro.
--
-- Correr los PASOS 1-5 seguidos, apenas termine el deploy. El backend nuevo ya no
-- escribe `workers.categoria_id` (la deja como este), asi que a un trabajador al
-- que le cambien el puesto en esa ventana se le guardaria aca la categoria vieja
-- como si fuera la vigente. La ventana es de minutos si no se deja a medias.
-- ============================================================================
BEGIN;

WITH ultima AS (
    SELECT DISTINCT ON (v.worker_id) v.id, v.worker_id
    FROM worker_vinculaciones v
    ORDER BY v.worker_id, v.fecha_inicio DESC NULLS LAST, v.id DESC
)
UPDATE worker_vinculaciones v
SET categoria_id = w.categoria_id
FROM ultima u
JOIN workers w ON w.id = u.worker_id
JOIN puesto  p ON p.puesto_id = w.puesto_id
WHERE v.id = u.id
  AND w.categoria_id IS NOT NULL
  AND w.categoria_id IS DISTINCT FROM p.categoria_id
  AND NOT EXISTS (
        SELECT 1 FROM worker_vinculaciones v2
        WHERE v2.worker_id = w.id AND v2.categoria_id = w.categoria_id
      );

COMMIT;


-- ============================================================================
-- PASO 3 - Fichas con categoria pero sin puesto: se les da un puesto para que no
-- queden sin categoria. Se reusa el puesto que ya se llama igual que su categoria
-- si existe; si no, se crea uno con ese nombre y `active = false` (existe y es
-- asignable, pero no ensucia los desplegables de nadie).
--
-- Nota: si el puesto homonimo existe con OTRA categoria (en prod pasa con
-- "MAESTRO DE OBRA", que apunta a OPERARIO porque 63 de sus 67 fichas son
-- OPERARIO), la ficha queda con la categoria de ese puesto, igual que sus pares.
-- ============================================================================
BEGIN;

INSERT INTO puesto (nombre, categoria_id, orden, active, state)
SELECT DISTINCT c.nombre, c.categoria_id, 0, false, true
FROM workers w
JOIN categoria c ON c.categoria_id = w.categoria_id
WHERE w.puesto_id IS NULL
  AND NOT EXISTS (SELECT 1 FROM puesto p WHERE p.nombre = c.nombre AND p.state);

UPDATE workers w
SET puesto_id = p.puesto_id, updated_at = now()
FROM categoria c
JOIN puesto p ON p.nombre = c.nombre AND p.state
WHERE w.puesto_id IS NULL
  AND w.categoria_id = c.categoria_id;

COMMIT;


-- ============================================================================
-- PASO 4 - Ultimo control antes del punto sin retorno.
-- Tiene que devolver 0 filas. Si devuelve algo es un puesto sin categoria o una
-- ficha que se quedaria sin categoria: resolverlo antes de seguir.
-- ============================================================================
SELECT 'puesto sin categoria' AS problema, p.puesto_id::text AS id, p.nombre AS detalle
FROM puesto p WHERE p.categoria_id IS NULL
UNION ALL
SELECT 'ficha con categoria y sin puesto', w.id::text, w.apellido_nombre
FROM workers w WHERE w.puesto_id IS NULL AND w.categoria_id IS NOT NULL;


-- ============================================================================
-- PASO 5 - Bajar workers.categoria_id.
-- Se borra la columna, no se congela: el dato deja de capturarse y la copia
-- historica vive en worker_vinculaciones.categoria_id (PASO 2). Una columna
-- congelada aca seria peor que borrarla - el codigo compila, corre y devuelve
-- vacio sin que nadie se entere.
--
-- Correr DESPUES de desplegar el backend nuevo: el codigo viejo lee esta columna.
-- ============================================================================
BEGIN;

DROP INDEX IF EXISTS ix_workers_categoria_id;

ALTER TABLE workers DROP CONSTRAINT IF EXISTS workers_categoria_id_fkey;
ALTER TABLE workers DROP COLUMN IF EXISTS categoria_id;

COMMIT;


-- ============================================================================
-- PASO 6 - Verificacion.
-- ============================================================================

-- 6a. La columna ya no existe.
SELECT count(*) AS debe_ser_0
FROM information_schema.columns
WHERE table_name = 'workers' AND column_name = 'categoria_id';

-- 6b. Reparto de categorias (asi lo ve la app de ahora en adelante).
SELECT c.nombre AS categoria, count(*) AS fichas
FROM workers w
JOIN puesto p    ON p.puesto_id = w.puesto_id
JOIN categoria c ON c.categoria_id = p.categoria_id
GROUP BY c.nombre ORDER BY fichas DESC;

-- 6c. Fichas que quedaron sin categoria (= sin puesto).
-- En prod esto NO debe dar 0: da 8. Son las fichas que hoy ya estan sin puesto Y
-- sin categoria (de las 23 sin puesto, 15 tienen categoria y el PASO 3 se las
-- resuelve; las otras 8 no tienen ninguna de las dos). No es una regresion: hoy
-- tampoco tienen categoria. Quedan invisibles para filtros y reglas hasta que
-- alguien les asigne un puesto desde la ficha del trabajador.
SELECT count(*) AS fichas_sin_categoria FROM workers WHERE puesto_id IS NULL;
