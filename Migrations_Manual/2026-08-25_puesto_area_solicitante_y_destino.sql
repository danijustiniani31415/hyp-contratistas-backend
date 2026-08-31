-- ═══════════════════════════════════════════════════════════════════════════
-- Configuración GTH · El puesto dice QUIÉN lo puede pedir y A DÓNDE va el postulante
-- ═══════════════════════════════════════════════════════════════════════════
--
-- Hoy `puesto.area_scope_id` (migración 2026-08-25_puesto_una_sola_area.sql)
-- hace DOS trabajos que no son el mismo:
--
--   1. filtra el desplegable de Solicitud de Personal — al solicitante solo se
--      le ofrecen los puestos de su área y de sus áreas hijas;
--   2. decide a qué área entra el postulante cuando lo aprueban como finalista
--      (`workers.area_scope_id` de su ficha de pre-ingreso).
--
-- Confundirlos obliga a elegir mal. INGENIERO RESIDENTE lo pide la Gerencia
-- Inmobiliaria, pero el residente NO trabaja en la gerencia: trabaja en
-- Residencia. Con una sola columna, ponerla en «Gerencia Inmobiliaria» manda al
-- residente a la gerencia, y ponerla en «Residencia» le esconde el puesto a
-- quien tiene que pedirlo.
--
-- Este script parte la columna en dos:
--
--   * `area_solicitante_scope_id` — el área en la que tiene que estar el
--     solicitante para poder pedir este puesto. Es EXACTAMENTE lo que hace hoy
--     `area_scope_id`, así que hereda su valor tal cual.
--   * `area_destino_scope_id`     — el área a la que entra el postulante si lo
--     eligen. Nueva.
--
-- Las dos son nullable a propósito: los ~190 puestos de obra no tienen ninguna
-- (el padrón de GTH solo cubre oficina) y salen como «Sin área» en pantalla.
-- Sin destino, la ficha del finalista se sigue cayendo al área del solicitante,
-- que es el comportamiento de siempre.
--
-- ⚠ ORDEN DE EJECUCIÓN (los PASOS 1-4 se corren HOY; el PASO 5 NO)
--
--   `area_scope_id` NO se renombra de una: el backend en producción la mapea en
--   `Shared/Models/Puesto.cs` y un rename la tumbaría entera (42703) hasta que
--   termine el deploy. Se agregan las columnas nuevas, se dejan sincronizadas
--   con un trigger (PASO 3) y recién DESPUÉS DEL DEPLOY se baja la vieja
--   (PASO 5). Mismo patrón que usó 2026-08-25_puesto_una_sola_area.sql con
--   `puesto_area_scope`.
--
-- Cada paso es idempotente. Correr paso por paso en pgAdmin y leer la salida.
-- ═══════════════════════════════════════════════════════════════════════════


-- ═══════════════════════════════════════════════════════════════════════════
-- PASO 0 — Diagnóstico. No modifica nada.
-- ═══════════════════════════════════════════════════════════════════════════

-- 0a. Cuántos puestos vivos tienen área hoy y cuántos son de obra (sin área).
SELECT count(*)                                        AS puestos_vivos,
       count(*) FILTER (WHERE area_scope_id IS NOT NULL) AS con_area,
       count(*) FILTER (WHERE area_scope_id IS NULL)     AS sin_area
FROM puesto
WHERE state;

-- 0b. Los puestos en los que el padrón de GTH separa pedir de ir: son los
--     únicos en los que las dos columnas nuevas van a quedar distintas.
--     (ADMINISTRADOR DE OBRA, ARQUITECTO/ASISTENTE/INGENIERO DE PRODUCCIÓN,
--      COORDINADOR ADMINISTRATIVO DE OBRA e INGENIERO RESIDENTE.)
SELECT p.puesto_id, p.nombre,
       ai.area_item_name AS area_hoy,
       (SELECT count(*) FROM workers w WHERE w.puesto_id = p.puesto_id AND w.state) AS fichas
FROM puesto p
LEFT JOIN area_scope s ON s.area_scope_id = p.area_scope_id
LEFT JOIN area_item ai ON ai.area_item_id = s.area_item_id
WHERE p.state AND p.puesto_id IN (9, 31, 57, 103, 163, 170)
ORDER BY p.nombre;


-- ═══════════════════════════════════════════════════════════════════════════
-- PASO 1 — Las dos columnas nuevas.
--   `area_solicitante_scope_id` nace copiando `area_scope_id`: significa lo
--   mismo, así que el dato de hoy ya es el correcto.
-- ═══════════════════════════════════════════════════════════════════════════

ALTER TABLE puesto ADD COLUMN IF NOT EXISTS area_solicitante_scope_id integer;
ALTER TABLE puesto ADD COLUMN IF NOT EXISTS area_destino_scope_id     integer;

UPDATE puesto
   SET area_solicitante_scope_id = area_scope_id
 WHERE area_solicitante_scope_id IS DISTINCT FROM area_scope_id;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint
                    WHERE conname = 'fk_puesto_area_solicitante_scope') THEN
        ALTER TABLE puesto
            ADD CONSTRAINT fk_puesto_area_solicitante_scope
            FOREIGN KEY (area_solicitante_scope_id) REFERENCES area_scope (area_scope_id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint
                    WHERE conname = 'fk_puesto_area_destino_scope') THEN
        ALTER TABLE puesto
            ADD CONSTRAINT fk_puesto_area_destino_scope
            FOREIGN KEY (area_destino_scope_id) REFERENCES area_scope (area_scope_id);
    END IF;
END $$;

COMMENT ON COLUMN puesto.area_solicitante_scope_id IS
    'Área en la que tiene que estar el solicitante para poder pedir este puesto en Solicitud de Personal (su área y sus áreas hijas lo ven). NULL = puesto de obra, fuera del padrón de GTH.';
COMMENT ON COLUMN puesto.area_destino_scope_id IS
    'Área a la que entra el postulante si lo aprueban como finalista (queda en workers.area_scope_id de su ficha de pre-ingreso). NULL = se cae al área del solicitante.';

CREATE INDEX IF NOT EXISTS ix_puesto_area_solicitante_scope
    ON puesto (area_solicitante_scope_id) WHERE state;
CREATE INDEX IF NOT EXISTS ix_puesto_area_destino_scope
    ON puesto (area_destino_scope_id) WHERE state;


-- ═══════════════════════════════════════════════════════════════════════════
-- PASO 2 — El padrón de GTH («Grupo Ocupacional - Abril.xlsx», hoja única:
--          Cargo | Área que puede pedir | Área a la que irá).
--
--   Son 112 puestos. El nombre viaja en el VALUES solo para verificar: si a un
--   puesto lo renombraron después de armar el padrón, esa fila NO se aplica y
--   sale listada en el PASO 4c en vez de pisar el puesto equivocado.
--
--   Los 6 puestos en los que pedir ≠ ir son los de obra que dependen de la
--   Gerencia Inmobiliaria: la gerencia los pide, pero el trabajador entra a
--   Producción, Residencia o Administración de Obra.
-- ═══════════════════════════════════════════════════════════════════════════

WITH padron (puesto_id, solicitante, destino) AS (
  VALUES
    (   2, 59, 59),  -- ABOGADO DE GESTIONES INMOBILIARIAS
    (   3, 59, 59),  -- ABOGADO DE GESTIONES INMOBILIARIAS JR
    (   9, 17, 83),  -- ADMINISTRADOR DE OBRA
    (  10, 88, 88),  -- AGENTE DE SEGURIDAD
    (  14, 60, 60),  -- ALMACENERO
    (  15, 55, 55),  -- ANALISTA ADMINISTRATIVO
    ( 340, 58, 58),  -- ANALISTA DE CULTURA Y DESARROLLO
    (  17, 58, 58),  -- ANALISTA DE GTH
    (  18, 57, 57),  -- ANALISTA DE PLANEAMIENTO FINANCIERO Y CONTROL DE GESTIÓN
    (  23, 41, 41),  -- ARQUITECTO BIM
    (  24, 80, 80),  -- ARQUITECTO COMERCIAL
    (  26, 80, 80),  -- ARQUITECTO COMERCIAL JUNIOR
    (  27, 81, 81),  -- ARQUITECTO COORDINADOR DE OBRA
    (  28, 42, 42),  -- ARQUITECTO DE CALIDAD
    (  29, 52, 52),  -- ARQUITECTO DE POST VENTA
    (  30, 52, 52),  -- ARQUITECTO DE POST VENTA JR
    (  31, 17, 76),  -- ARQUITECTO DE PRODUCCIÓN
    (  32, 81, 81),  -- ARQUITECTO DE PROYECTOS
    ( 347, 41, 41),  -- ARQUITECTO DE PROYECTOS
    (  35, 63, 63),  -- ASESOR DE VENTAS
    (  36, 58, 58),  -- ASISTENTA SOCIAL
    (  37, 55, 55),  -- ASISTENTE ADMINISTRATIVO
    ( 348, 88, 88),  -- ASISTENTE ADMINISTRATIVO
    (  38, 57, 57),  -- ASISTENTE ADMINISTRATIVO DE COBRANZAS
    (  40, 55, 55),  -- ASISTENTE DE ADMINISTRACION
    (  43, 42, 42),  -- ASISTENTE DE CALIDAD
    (  45, 56, 56),  -- ASISTENTE DE CONTABILIDAD
    (  46, 46, 46),  -- ASISTENTE DE COSTOS Y PRESUPUESTOS
    (  47, 59, 59),  -- ASISTENTE DE CUMPLIMIENTO
    (  49, 88, 88),  -- ASISTENTE DE GERENCIA GENERAL
    (  50, 62, 62),  -- ASISTENTE DE GESTIONES ADMINISTRATIVAS
    (  51, 58, 58),  -- ASISTENTE DE GTH
    (  52, 60, 60),  -- ASISTENTE DE LOGISTICA
    (  53, 75, 75),  -- ASISTENTE DE MARKETING
    (  55, 46, 46),  -- ASISTENTE DE OFICINA TECNICA
    (  56, 88, 88),  -- ASISTENTE DE OPERACIONES
    (  57, 17, 76),  -- ASISTENTE DE PRODUCCIÓN
    (  58, 57, 57),  -- ASISTENTE DE TESORERIA
    (  59, 61, 61),  -- ASISTENTE DE TI
    (  60, 63, 63),  -- ASISTENTE DE VENTAS
    (  61, 75, 75),  -- ASISTENTE DIGITAL
    (  63, 59, 59),  -- ASISTENTE LEGAL
    (  66, 55, 55),  -- AUXILIAR ADMINISTRATIVO
    (  67, 55, 55),  -- AUXILIAR DE LIMPIEZA
    (  69, 62, 62),  -- AUXLIAR DE GESTIONES ADMINISTRATIVAS
    (  87, 63, 63),  -- BACK OFFICE INMOBILIARIO
    (  88, 17, 17),  -- BARISTA EJECUTIVO
    ( 349, 88, 88),  -- CHOFER
    (  97, 60, 60),  -- CHOFER
    ( 102, 75, 75),  -- CONTENT CREATOR
    ( 103, 17, 83),  -- COORDINADOR ADMINISTRATIVO DE OBRA
    ( 341, 56, 56),  -- COORDINADOR CONTABLE
    ( 105, 80, 80),  -- COORDINADOR DE ARQUITECTURA COMERCIAL
    ( 106, 81, 81),  -- COORDINADOR DE ARQUITECTURA DE PROYECTOS
    ( 107, 46, 46),  -- COORDINADOR DE COSTOS Y PRESUPUESTOS
    ( 108, 62, 62),  -- COORDINADOR DE GESTIONES ADMINISTRATIVAS
    ( 109, 63, 63),  -- COORDINADOR DE INTELIGENCIA COMERCIAL
    ( 110, 60, 60),  -- COORDINADOR DE LOGISITICA
    ( 114, 61, 61),  -- COORDINADOR DE TI
    ( 115, 75, 75),  -- COORDINADOR DE TRADE MARKETING
    ( 116, 63, 63),  -- COORDINADOR DE VENTAS
    ( 117, 61, 61),  -- COORDINADOR ERP
    ( 118, 59, 59),  -- COORDINADOR LEGAL
    ( 119, 44, 44),  -- COORDINADOR SSOMA
    ( 346, 61, 61),  -- DESARROLLADOR FULLSTACK (PRUEBA)
    ( 124, 75, 75),  -- DISEÑADOR GRAFICO SENIOR
    ( 128, 75, 75),  -- EJECUTIVO DE MARKETING DE CONTENIDO
    ( 133, 52, 52),  -- ESPECIALISTA DE GESTIONES VECINALES
    ( 134, 61, 61),  -- ESPECIALISTA EN BUSINESS INTELLIGENCE
    ( 135, 61, 61),  -- ESPECIALISTA EN TRANSFORMACIÓN DIGITAL
    ( 141, 54, 54),  -- GERENTE DE ADMINISTRACION Y FINANZAS
    ( 142, 16, 16),  -- GERENTE DE MARKETING
    ( 144, 88, 88),  -- GERENTE GENERAL
    ( 145, 17, 17),  -- GERENTE INMOBILIARIO
    ( 148, 55, 55),  -- GESTOR ADMINISTRATIVO
    ( 151, 63, 63),  -- GESTOR DE VENTAS
    ( 158, 42, 42),  -- INGENIERO DE CALIDAD
    ( 159, 46, 46),  -- INGENIERO DE COSTOS Y PRESUPUESTOS
    ( 160, 41, 41),  -- INGENIERO DE INSTALACIONES
    ( 161, 46, 46),  -- INGENIERO DE OFICINA TECNICA
    ( 162, 41, 41),  -- INGENIERO DE PLANEAMIENTO BIM
    ( 163, 17, 76),  -- INGENIERO DE PRODUCCIÓN
    ( 164, 41, 41),  -- INGENIERO DE PROYECTOS
    ( 170, 17, 78),  -- INGENIERO RESIDENTE
    ( 176, 55, 55),  -- JEFE DE ADMINISTRACIÓN
    ( 177, 60, 60),  -- JEFE DE ALMACÉN
    ( 178, 81, 81),  -- JEFE DE ARQUITECTURA
    ( 179, 80, 80),  -- JEFE DE ARQUITECTURA COMERCIAL
    ( 342, 42, 42),  -- JEFE DE CALIDAD
    ( 181, 46, 46),  -- JEFE DE COSTOS Y PRESUPUESTOS
    ( 182, 62, 62),  -- JEFE DE GESTIONES ADMINISTRATIVAS
    ( 183, 58, 58),  -- JEFE DE GTH
    ( 184, 60, 60),  -- JEFE DE LOGISTICA
    ( 185, 75, 75),  -- JEFE DE MARKETING
    ( 343, 75, 75),  -- JEFE DE MARKETING DIGITAL
    ( 187, 52, 52),  -- JEFE DE POST VENTA
    ( 188, 41, 41),  -- JEFE DE PROYECTOS
    ( 189, 44, 44),  -- JEFE DE SEGURIDAD Y SALUD EN EL TRABAJO
    ( 190, 61, 61),  -- JEFE DE TI
    ( 191, 63, 63),  -- JEFE DE VENTAS
    ( 201, 41, 41),  -- MODELADOR BIM
    ( 200, 44, 44),  -- MÉDICO OCUPACIONAL
    ( 225, 75, 75),  -- OPERADOR DE CONTACT CENTER
    ( 270, 88, 88),  -- PERSONAL DE MANTENIMIENTO
    ( 281, 44, 44),  -- PREVENCIONISTA DE RIESGOS
    ( 282, 44, 44),  -- PREVENCIONISTA DE RIESGOS JR
    ( 286, 55, 55),  -- PROCURADOR
    ( 301, 42, 42),  -- SUPERVISOR DE INSTALACIONES
    ( 344, 52, 52),  -- SUPERVISOR DE POST VENTA
    ( 307, 55, 55),  -- SUPERVISORA DE LIMPIEZA
    ( 324, 57, 57),  -- TESORERA
    ( 333, 88, 88)   -- VIGILANTE
)
UPDATE puesto p
   SET area_solicitante_scope_id = pa.solicitante,
       area_destino_scope_id     = pa.destino,
       -- La columna vieja sigue siendo la que lee el backend en producción
       -- hasta el deploy: se mueve junto con la de solicitante, que es su
       -- mismo significado.
       area_scope_id             = pa.solicitante,
       updated_date_time         = now()
  FROM padron pa
 WHERE p.puesto_id = pa.puesto_id
   AND p.state
   AND (p.area_solicitante_scope_id IS DISTINCT FROM pa.solicitante
     OR p.area_destino_scope_id     IS DISTINCT FROM pa.destino);

-- 2b. Los puestos con área que el padrón no nombra (copias del corte del
--     25-ago, puestos creados a mano después): conservan su área solicitante y
--     se llevan esa misma como destino, que es exactamente lo que hace hoy el
--     sistema con la columna única. Así ninguno cambia de comportamiento.
UPDATE puesto
   SET area_destino_scope_id = area_solicitante_scope_id,
       updated_date_time     = now()
 WHERE state
   AND area_solicitante_scope_id IS NOT NULL
   AND area_destino_scope_id IS NULL;


-- ═══════════════════════════════════════════════════════════════════════════
-- PASO 3 — Índice del nombre único + espejo con la columna vieja.
--
--   `ux_puesto_nombre_area_vivo` (nombre, area_scope_id) sigue vivo hasta el
--   PASO 5. El nuevo repite la regla sobre la columna nueva: el nombre del
--   puesto es único DENTRO de un área SOLICITANTE — CHOFER puede existir en
--   Gerencia General y en Logística, pero no dos veces en Logística.
--   `NULLS NOT DISTINCT` (PG 15+) es lo que protege también el bolsón «Sin
--   área»: sin él, cada NULL cuenta como distinto y entrarían diez ALMACENERO
--   sueltos.
--
--   El trigger mantiene las dos columnas iguales mientras convivan: el backend
--   en producción escribe `area_scope_id` y el nuevo escribirá
--   `area_solicitante_scope_id`. Sin él, un puesto creado desde la pantalla
--   entre este script y el deploy nacería con una sola de las dos llena.
-- ═══════════════════════════════════════════════════════════════════════════

CREATE UNIQUE INDEX IF NOT EXISTS ux_puesto_nombre_area_solicitante_vivo
    ON puesto (nombre, area_solicitante_scope_id) NULLS NOT DISTINCT
 WHERE state;

CREATE OR REPLACE FUNCTION puesto_sync_area_solicitante() RETURNS trigger AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        IF NEW.area_solicitante_scope_id IS NULL THEN
            NEW.area_solicitante_scope_id := NEW.area_scope_id;
        ELSIF NEW.area_scope_id IS NULL THEN
            NEW.area_scope_id := NEW.area_solicitante_scope_id;
        END IF;
    ELSE
        -- Gana la que cambió en esta sentencia; si cambiaron las dos (el propio
        -- PASO 2) ya vienen iguales y no hay nada que decidir.
        IF NEW.area_scope_id IS DISTINCT FROM OLD.area_scope_id THEN
            NEW.area_solicitante_scope_id := NEW.area_scope_id;
        ELSIF NEW.area_solicitante_scope_id IS DISTINCT FROM OLD.area_solicitante_scope_id THEN
            NEW.area_scope_id := NEW.area_solicitante_scope_id;
        END IF;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_puesto_sync_area_solicitante ON puesto;
CREATE TRIGGER trg_puesto_sync_area_solicitante
    BEFORE INSERT OR UPDATE ON puesto
    FOR EACH ROW EXECUTE FUNCTION puesto_sync_area_solicitante();


-- ═══════════════════════════════════════════════════════════════════════════
-- PASO 4 — Verificación. Solo lectura.
-- ═══════════════════════════════════════════════════════════════════════════

-- 4a. Resumen: cuántos puestos quedaron con cada cosa.
SELECT count(*)                                                      AS puestos_vivos,
       count(*) FILTER (WHERE area_solicitante_scope_id IS NOT NULL) AS con_solicitante,
       count(*) FILTER (WHERE area_destino_scope_id     IS NOT NULL) AS con_destino,
       count(*) FILTER (WHERE area_solicitante_scope_id IS DISTINCT FROM area_destino_scope_id)
                                                                     AS pedir_distinto_de_ir,
       count(*) FILTER (WHERE area_scope_id IS DISTINCT FROM area_solicitante_scope_id)
                                                                     AS espejo_roto
FROM puesto
WHERE state;
-- Se espera: con_solicitante = con_destino, pedir_distinto_de_ir = 6, espejo_roto = 0.

-- 4b. Los puestos donde pedir ≠ ir, con nombres. Esta lista se le enseña a GTH.
SELECT p.puesto_id, p.nombre,
       so.area_item_name AS lo_pide,
       de.area_item_name AS va_a,
       (SELECT count(*) FROM workers w WHERE w.puesto_id = p.puesto_id AND w.state) AS fichas
FROM puesto p
JOIN area_scope ss ON ss.area_scope_id = p.area_solicitante_scope_id
JOIN area_item  so ON so.area_item_id  = ss.area_item_id
JOIN area_scope ds ON ds.area_scope_id = p.area_destino_scope_id
JOIN area_item  de ON de.area_item_id  = ds.area_item_id
WHERE p.state AND p.area_solicitante_scope_id IS DISTINCT FROM p.area_destino_scope_id
ORDER BY p.nombre;

-- 4c. Filas del padrón que NO se aplicaron (el puesto_id no existe vivo).
--     Si sale alguna, el puesto se eliminó o se renombró: hay que resolverla a
--     mano desde Configuración → Categorías y Puestos.
WITH padron (puesto_id, solicitante, destino) AS (
  VALUES
    (   2, 59, 59),  -- ABOGADO DE GESTIONES INMOBILIARIAS
    (   3, 59, 59),  -- ABOGADO DE GESTIONES INMOBILIARIAS JR
    (   9, 17, 83),  -- ADMINISTRADOR DE OBRA
    (  10, 88, 88),  -- AGENTE DE SEGURIDAD
    (  14, 60, 60),  -- ALMACENERO
    (  15, 55, 55),  -- ANALISTA ADMINISTRATIVO
    ( 340, 58, 58),  -- ANALISTA DE CULTURA Y DESARROLLO
    (  17, 58, 58),  -- ANALISTA DE GTH
    (  18, 57, 57),  -- ANALISTA DE PLANEAMIENTO FINANCIERO Y CONTROL DE GESTIÓN
    (  23, 41, 41),  -- ARQUITECTO BIM
    (  24, 80, 80),  -- ARQUITECTO COMERCIAL
    (  26, 80, 80),  -- ARQUITECTO COMERCIAL JUNIOR
    (  27, 81, 81),  -- ARQUITECTO COORDINADOR DE OBRA
    (  28, 42, 42),  -- ARQUITECTO DE CALIDAD
    (  29, 52, 52),  -- ARQUITECTO DE POST VENTA
    (  30, 52, 52),  -- ARQUITECTO DE POST VENTA JR
    (  31, 17, 76),  -- ARQUITECTO DE PRODUCCIÓN
    (  32, 81, 81),  -- ARQUITECTO DE PROYECTOS
    ( 347, 41, 41),  -- ARQUITECTO DE PROYECTOS
    (  35, 63, 63),  -- ASESOR DE VENTAS
    (  36, 58, 58),  -- ASISTENTA SOCIAL
    (  37, 55, 55),  -- ASISTENTE ADMINISTRATIVO
    ( 348, 88, 88),  -- ASISTENTE ADMINISTRATIVO
    (  38, 57, 57),  -- ASISTENTE ADMINISTRATIVO DE COBRANZAS
    (  40, 55, 55),  -- ASISTENTE DE ADMINISTRACION
    (  43, 42, 42),  -- ASISTENTE DE CALIDAD
    (  45, 56, 56),  -- ASISTENTE DE CONTABILIDAD
    (  46, 46, 46),  -- ASISTENTE DE COSTOS Y PRESUPUESTOS
    (  47, 59, 59),  -- ASISTENTE DE CUMPLIMIENTO
    (  49, 88, 88),  -- ASISTENTE DE GERENCIA GENERAL
    (  50, 62, 62),  -- ASISTENTE DE GESTIONES ADMINISTRATIVAS
    (  51, 58, 58),  -- ASISTENTE DE GTH
    (  52, 60, 60),  -- ASISTENTE DE LOGISTICA
    (  53, 75, 75),  -- ASISTENTE DE MARKETING
    (  55, 46, 46),  -- ASISTENTE DE OFICINA TECNICA
    (  56, 88, 88),  -- ASISTENTE DE OPERACIONES
    (  57, 17, 76),  -- ASISTENTE DE PRODUCCIÓN
    (  58, 57, 57),  -- ASISTENTE DE TESORERIA
    (  59, 61, 61),  -- ASISTENTE DE TI
    (  60, 63, 63),  -- ASISTENTE DE VENTAS
    (  61, 75, 75),  -- ASISTENTE DIGITAL
    (  63, 59, 59),  -- ASISTENTE LEGAL
    (  66, 55, 55),  -- AUXILIAR ADMINISTRATIVO
    (  67, 55, 55),  -- AUXILIAR DE LIMPIEZA
    (  69, 62, 62),  -- AUXLIAR DE GESTIONES ADMINISTRATIVAS
    (  87, 63, 63),  -- BACK OFFICE INMOBILIARIO
    (  88, 17, 17),  -- BARISTA EJECUTIVO
    ( 349, 88, 88),  -- CHOFER
    (  97, 60, 60),  -- CHOFER
    ( 102, 75, 75),  -- CONTENT CREATOR
    ( 103, 17, 83),  -- COORDINADOR ADMINISTRATIVO DE OBRA
    ( 341, 56, 56),  -- COORDINADOR CONTABLE
    ( 105, 80, 80),  -- COORDINADOR DE ARQUITECTURA COMERCIAL
    ( 106, 81, 81),  -- COORDINADOR DE ARQUITECTURA DE PROYECTOS
    ( 107, 46, 46),  -- COORDINADOR DE COSTOS Y PRESUPUESTOS
    ( 108, 62, 62),  -- COORDINADOR DE GESTIONES ADMINISTRATIVAS
    ( 109, 63, 63),  -- COORDINADOR DE INTELIGENCIA COMERCIAL
    ( 110, 60, 60),  -- COORDINADOR DE LOGISITICA
    ( 114, 61, 61),  -- COORDINADOR DE TI
    ( 115, 75, 75),  -- COORDINADOR DE TRADE MARKETING
    ( 116, 63, 63),  -- COORDINADOR DE VENTAS
    ( 117, 61, 61),  -- COORDINADOR ERP
    ( 118, 59, 59),  -- COORDINADOR LEGAL
    ( 119, 44, 44),  -- COORDINADOR SSOMA
    ( 346, 61, 61),  -- DESARROLLADOR FULLSTACK (PRUEBA)
    ( 124, 75, 75),  -- DISEÑADOR GRAFICO SENIOR
    ( 128, 75, 75),  -- EJECUTIVO DE MARKETING DE CONTENIDO
    ( 133, 52, 52),  -- ESPECIALISTA DE GESTIONES VECINALES
    ( 134, 61, 61),  -- ESPECIALISTA EN BUSINESS INTELLIGENCE
    ( 135, 61, 61),  -- ESPECIALISTA EN TRANSFORMACIÓN DIGITAL
    ( 141, 54, 54),  -- GERENTE DE ADMINISTRACION Y FINANZAS
    ( 142, 16, 16),  -- GERENTE DE MARKETING
    ( 144, 88, 88),  -- GERENTE GENERAL
    ( 145, 17, 17),  -- GERENTE INMOBILIARIO
    ( 148, 55, 55),  -- GESTOR ADMINISTRATIVO
    ( 151, 63, 63),  -- GESTOR DE VENTAS
    ( 158, 42, 42),  -- INGENIERO DE CALIDAD
    ( 159, 46, 46),  -- INGENIERO DE COSTOS Y PRESUPUESTOS
    ( 160, 41, 41),  -- INGENIERO DE INSTALACIONES
    ( 161, 46, 46),  -- INGENIERO DE OFICINA TECNICA
    ( 162, 41, 41),  -- INGENIERO DE PLANEAMIENTO BIM
    ( 163, 17, 76),  -- INGENIERO DE PRODUCCIÓN
    ( 164, 41, 41),  -- INGENIERO DE PROYECTOS
    ( 170, 17, 78),  -- INGENIERO RESIDENTE
    ( 176, 55, 55),  -- JEFE DE ADMINISTRACIÓN
    ( 177, 60, 60),  -- JEFE DE ALMACÉN
    ( 178, 81, 81),  -- JEFE DE ARQUITECTURA
    ( 179, 80, 80),  -- JEFE DE ARQUITECTURA COMERCIAL
    ( 342, 42, 42),  -- JEFE DE CALIDAD
    ( 181, 46, 46),  -- JEFE DE COSTOS Y PRESUPUESTOS
    ( 182, 62, 62),  -- JEFE DE GESTIONES ADMINISTRATIVAS
    ( 183, 58, 58),  -- JEFE DE GTH
    ( 184, 60, 60),  -- JEFE DE LOGISTICA
    ( 185, 75, 75),  -- JEFE DE MARKETING
    ( 343, 75, 75),  -- JEFE DE MARKETING DIGITAL
    ( 187, 52, 52),  -- JEFE DE POST VENTA
    ( 188, 41, 41),  -- JEFE DE PROYECTOS
    ( 189, 44, 44),  -- JEFE DE SEGURIDAD Y SALUD EN EL TRABAJO
    ( 190, 61, 61),  -- JEFE DE TI
    ( 191, 63, 63),  -- JEFE DE VENTAS
    ( 201, 41, 41),  -- MODELADOR BIM
    ( 200, 44, 44),  -- MÉDICO OCUPACIONAL
    ( 225, 75, 75),  -- OPERADOR DE CONTACT CENTER
    ( 270, 88, 88),  -- PERSONAL DE MANTENIMIENTO
    ( 281, 44, 44),  -- PREVENCIONISTA DE RIESGOS
    ( 282, 44, 44),  -- PREVENCIONISTA DE RIESGOS JR
    ( 286, 55, 55),  -- PROCURADOR
    ( 301, 42, 42),  -- SUPERVISOR DE INSTALACIONES
    ( 344, 52, 52),  -- SUPERVISOR DE POST VENTA
    ( 307, 55, 55),  -- SUPERVISORA DE LIMPIEZA
    ( 324, 57, 57),  -- TESORERA
    ( 333, 88, 88)   -- VIGILANTE
)
SELECT pa.puesto_id, pa.solicitante, pa.destino
FROM padron pa
LEFT JOIN puesto p ON p.puesto_id = pa.puesto_id AND p.state
WHERE p.puesto_id IS NULL;

-- 4d. Puestos CON fichas de trabajadores que quedaron sin área de destino: son
--     los de obra. Los finalistas de esos puestos siguen cayendo al área del
--     solicitante. Se listan los 15 con más gente para revisar de un vistazo.
SELECT p.puesto_id, p.nombre,
       (SELECT count(*) FROM workers w WHERE w.puesto_id = p.puesto_id AND w.state) AS fichas
FROM puesto p
WHERE p.state AND p.area_destino_scope_id IS NULL
ORDER BY fichas DESC, p.nombre
LIMIT 15;


-- ═══════════════════════════════════════════════════════════════════════════
-- PASO 5 — ⛔ SOLO DESPUÉS DE QUE EL DEPLOY ESTÉ ARRIBA.
--
--   Correrlo junto con los pasos anteriores tumba producción: el backend que
--   está corriendo AHORA mapea `puesto.area_scope_id` y sin ella todo lo que
--   toque puestos (Configuración, Solicitud de Personal, Reclutamiento, la
--   ficha del trabajador) responde 42703.
-- ═══════════════════════════════════════════════════════════════════════════

-- DROP TRIGGER IF EXISTS trg_puesto_sync_area_solicitante ON puesto;
-- DROP FUNCTION IF EXISTS puesto_sync_area_solicitante();
-- DROP INDEX IF EXISTS ux_puesto_nombre_area_vivo;
-- ALTER TABLE puesto DROP COLUMN IF EXISTS area_scope_id;
