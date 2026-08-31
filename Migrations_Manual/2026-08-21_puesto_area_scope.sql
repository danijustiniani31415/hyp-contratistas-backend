-- ============================================================================
-- puesto_area_scope: a que area(s) pertenece cada puesto
-- ============================================================================
-- Hoy "Solicitud de Personal" (/gestion-gth/solicitud-personal) lista TODOS los
-- puestos vivos del catalogo. GTH pidio que solo salgan los del area del
-- solicitante y los de sus areas hijas, y para eso hace falta la relacion
-- puesto <-> area, que no existia.
--
-- Modelo que queda:
--   * un puesto pertenece a N areas     -> tabla intermedia `puesto_area_scope`
--   * un puesto pertenece a 1 categoria -> `puesto.categoria_id` (ya existia, NOT NULL)
--
-- La data sale del Excel "DATA MAESTRA_RENOVACIONES 2026.xlsx", hoja
-- Data_Renovaciones, columnas PUESTO + "AREA A LA QUE PERTENECE EL PUESTO":
-- 115 pares distintos sobre 112 puestos y 19 areas. El Excel solo cubre
-- personal de OFICINA, asi que los puestos de obra quedan a proposito sin area.
--
-- Correr PASO por PASO en pgAdmin. Cada paso es idempotente.
-- ============================================================================


-- ============================================================================
-- PASO 0 - Diagnostico. No modifica nada: correrlo y leer la salida.
-- ============================================================================

-- 0a. Confirmar que los area_scope_id que usa el PASO 4a son los que se esperan.
--     Tienen que salir 18 filas con exactamente estos nombres:
--       16 Gerencia de Marketing      17 Gerencia de Proyectos
--       41 Unidad de Proyectos        42 Calidad
--       44 SSOMA                      46 Costos y Presupuestos
--       52 Post Venta                 54 Gerencia de Administracion
--       56 Contabilidad               57 Finanzas
--       58 Gestion del Talento Humano 59 Legal
--       60 Logistica                  61 Tecnologia de la Informacion
--       62 Tramites Documentarios     63 Ventas
--       80 Arquitectura Comercial     81 Arquitectura
SELECT s.area_scope_id, ai.area_item_name, at2.area_type_name,
       pai.area_item_name AS cuelga_de
FROM area_scope s
JOIN area_item ai       ON ai.area_item_id = s.area_item_id
JOIN area_type at2      ON at2.area_type_id = ai.area_type_id
LEFT JOIN area_scope ps ON ps.area_scope_id = s.area_scope_parent_id
LEFT JOIN area_item pai ON pai.area_item_id = ps.area_item_id
WHERE s.area_scope_id IN (16,17,41,42,44,46,52,54,56,57,58,59,60,61,62,63,80,81)
ORDER BY s.area_scope_id;

-- 0b. Puestos del Excel que NO existen en el catalogo. Tienen que salir
--     exactamente los 5 que crea el PASO 3. Si sale alguno mas, ese nombre
--     cambio en la BD y su vinculo se perderia en silencio en el PASO 4.
WITH excel(nombre) AS (VALUES
    ('ABOGADO DE GESTIONES INMOBILIARIAS'),
    ('ABOGADO DE GESTIONES INMOBILIARIAS JR'),
    ('ADMINISTRADOR DE OBRA'),
    ('AGENTE DE SEGURIDAD'),
    ('ALMACENERO'),
    ('ANALISTA ADMINISTRATIVO'),
    ('ANALISTA DE CULTURA Y DESARROLLO'),
    ('ANALISTA DE GTH'),
    ('ANALISTA DE PLANEAMIENTO FINANCIERO Y CONTROL DE GESTIÓN'),
    ('ARQUITECTO BIM'),
    ('ARQUITECTO COMERCIAL'),
    ('ARQUITECTO COMERCIAL JUNIOR'),
    ('ARQUITECTO COORDINADOR DE OBRA'),
    ('ARQUITECTO DE CALIDAD'),
    ('ARQUITECTO DE POST VENTA'),
    ('ARQUITECTO DE POST VENTA JR'),
    ('ARQUITECTO DE PRODUCCIÓN'),
    ('ARQUITECTO DE PROYECTOS'),
    ('ASESOR DE VENTAS'),
    ('ASISTENTA SOCIAL'),
    ('ASISTENTE ADMINISTRATIVO'),
    ('ASISTENTE ADMINISTRATIVO DE COBRANZAS'),
    ('ASISTENTE DE ADMINISTRACION'),
    ('ASISTENTE DE CALIDAD'),
    ('ASISTENTE DE CONTABILIDAD'),
    ('ASISTENTE DE COSTOS Y PRESUPUESTOS'),
    ('ASISTENTE DE CUMPLIMIENTO'),
    ('ASISTENTE DE DESARROLLO SSOMA'),
    ('ASISTENTE DE GERENCIA GENERAL'),
    ('ASISTENTE DE GESTIONES ADMINISTRATIVAS'),
    ('ASISTENTE DE GTH'),
    ('ASISTENTE DE LOGISTICA'),
    ('ASISTENTE DE MARKETING'),
    ('ASISTENTE DE OFICINA TECNICA'),
    ('ASISTENTE DE OPERACIONES'),
    ('ASISTENTE DE PRODUCCIÓN'),
    ('ASISTENTE DE TESORERIA'),
    ('ASISTENTE DE TI'),
    ('ASISTENTE DE VENTAS'),
    ('ASISTENTE DIGITAL'),
    ('ASISTENTE LEGAL'),
    ('AUXILIAR ADMINISTRATIVO'),
    ('AUXILIAR DE LIMPIEZA'),
    ('AUXLIAR DE GESTIONES ADMINISTRATIVAS'),
    ('BACK OFFICE INMOBILIARIO'),
    ('BARISTA EJECUTIVO'),
    ('CHOFER'),
    ('CONTENT CREATOR'),
    ('COORDINADOR ADMINISTRATIVO DE OBRA'),
    ('COORDINADOR CONTABLE'),
    ('COORDINADOR DE ARQUITECTURA COMERCIAL'),
    ('COORDINADOR DE ARQUITECTURA DE PROYECTOS'),
    ('COORDINADOR DE COSTOS Y PRESUPUESTOS'),
    ('COORDINADOR DE GESTIONES ADMINISTRATIVAS'),
    ('COORDINADOR DE INTELIGENCIA COMERCIAL'),
    ('COORDINADOR DE LOGISITICA'),
    ('COORDINADOR DE TI'),
    ('COORDINADOR DE TRADE MARKETING'),
    ('COORDINADOR DE VENTAS'),
    ('COORDINADOR ERP'),
    ('COORDINADOR LEGAL'),
    ('COORDINADOR SSOMA'),
    ('DISEÑADOR GRAFICO SENIOR'),
    ('EJECUTIVO DE MARKETING DE CONTENIDO'),
    ('ESPECIALISTA DE GESTIONES VECINALES'),
    ('ESPECIALISTA EN BUSINESS INTELLIGENCE'),
    ('ESPECIALISTA EN TRANSFORMACIÓN DIGITAL'),
    ('GERENTE DE ADMINISTRACION Y FINANZAS'),
    ('GERENTE DE MARKETING'),
    ('GERENTE GENERAL'),
    ('GERENTE INMOBILIARIO'),
    ('GESTOR ADMINISTRATIVO'),
    ('GESTOR DE VENTAS'),
    ('INGENIERO DE CALIDAD'),
    ('INGENIERO DE COSTOS Y PRESUPUESTOS'),
    ('INGENIERO DE INSTALACIONES'),
    ('INGENIERO DE OFICINA TECNICA'),
    ('INGENIERO DE PLANEAMIENTO BIM'),
    ('INGENIERO DE PRODUCCIÓN'),
    ('INGENIERO DE PROYECTOS'),
    ('INGENIERO PRACTICANTE'),
    ('INGENIERO RESIDENTE'),
    ('JEFE DE ADMINISTRACIÓN'),
    ('JEFE DE ALMACÉN'),
    ('JEFE DE ARQUITECTURA'),
    ('JEFE DE ARQUITECTURA COMERCIAL'),
    ('JEFE DE CALIDAD'),
    ('JEFE DE COSTOS Y PRESUPUESTOS'),
    ('JEFE DE GESTIONES ADMINISTRATIVAS'),
    ('JEFE DE GTH'),
    ('JEFE DE LOGISTICA'),
    ('JEFE DE MARKETING'),
    ('JEFE DE MARKETING DIGITAL'),
    ('JEFE DE POST VENTA'),
    ('JEFE DE PROYECTOS'),
    ('JEFE DE SEGURIDAD Y SALUD EN EL TRABAJO'),
    ('JEFE DE TI'),
    ('JEFE DE VENTAS'),
    ('MODELADOR BIM'),
    ('MÉDICO OCUPACIONAL'),
    ('OPERADOR DE CONTACT CENTER'),
    ('PERSONAL DE MANTENIMIENTO'),
    ('PRACTICANTE PRE PROFESIONAL DE ARQUITECTURA'),
    ('PRACTICANTE PROFESIONAL DE LOGISTICA'),
    ('PREVENCIONISTA DE RIESGOS'),
    ('PREVENCIONISTA DE RIESGOS JR'),
    ('PROCURADOR'),
    ('SUPERVISOR DE INSTALACIONES'),
    ('SUPERVISOR DE POST VENTA'),
    ('SUPERVISORA DE LIMPIEZA'),
    ('TESORERA'),
    ('VIGILANTE')
)
SELECT e.nombre
FROM excel e
LEFT JOIN puesto p ON p.nombre = e.nombre AND p.state
WHERE p.puesto_id IS NULL
ORDER BY 1;

-- 0c. Puestos vivos del catalogo (referencia: cuantos quedaran sin area).
SELECT count(*) AS puestos_vivos FROM puesto WHERE state;


-- ============================================================================
-- PASO 1 - Tabla intermedia puesto <-> area_scope
-- ============================================================================
-- Sin columna `active`: para un vinculo no existe el caso "existe pero no
-- aparece". Quitarle un area a un puesto es soft delete (state = false) y
-- volver a ponersela revive la fila; por eso el UNIQUE es parcial sobre las
-- vivas, como en ga_salida_visibilidad_area.

CREATE TABLE IF NOT EXISTS puesto_area_scope (
    puesto_area_scope_id integer     GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    puesto_id            integer     NOT NULL REFERENCES puesto(puesto_id),
    area_scope_id        integer     NOT NULL REFERENCES area_scope(area_scope_id),
    created_date_time    timestamptz NOT NULL DEFAULT now(),
    created_user_id      integer,
    updated_date_time    timestamptz,
    updated_user_id      integer,
    state                boolean     NOT NULL DEFAULT true
);

COMMENT ON TABLE puesto_area_scope IS
    'A que areas pertenece cada puesto (N:N). La usa Solicitud de Personal para listarle al solicitante solo los puestos de su area y de sus areas hijas.';

CREATE UNIQUE INDEX IF NOT EXISTS ux_puesto_area_scope_vivo
    ON puesto_area_scope (puesto_id, area_scope_id) WHERE state;
CREATE INDEX IF NOT EXISTS ix_puesto_area_scope_puesto
    ON puesto_area_scope (puesto_id) WHERE state;
CREATE INDEX IF NOT EXISTS ix_puesto_area_scope_area
    ON puesto_area_scope (area_scope_id) WHERE state;


-- ============================================================================
-- PASO 2 - Nodo "Gerencia General" en el arbol de areas
-- ============================================================================
-- El Excel manda 13 puestos a "Gerencia General". El area_item ya existe
-- (id 3, tipo "Area de Gerencia") pero nunca tuvo nodo en area_scope, asi que
-- no era un area usable. Se crea como RAIZ, hermana de las otras 3 gerencias
-- -- NO como padre de ellas: colgarles las gerencias abajo reordenaria todo el
-- arbol y cambiaria el alcance de cada filtro por area del sistema.
--
-- OJO, dos efectos que hay que tener presentes:
--   * "Gerencia General" pasa a aparecer en TODOS los desplegables de area
--     (salidas, reuniones, lecciones aprendidas, habilitacion...).
--   * Para que Solicitud de Personal le cargue estos 13 puestos a alguien, ese
--     alguien tiene que tener workers.area_scope_id apuntando a este nodo.
--     Hoy nadie lo tiene (la ficha del Gerente General lo tiene NULL).

INSERT INTO area_scope (area_item_id, area_scope_parent_id, display_order, active, state)
SELECT ai.area_item_id, NULL, 2, true, true
FROM area_item ai
WHERE ai.area_item_name = 'Gerencia General'
  AND ai.state
  AND NOT EXISTS (
        SELECT 1 FROM area_scope s
        WHERE s.area_item_id = ai.area_item_id
          AND s.area_scope_parent_id IS NULL
          AND s.state);


-- ============================================================================
-- PASO 3 - Puestos del Excel que faltan en el catalogo
-- ============================================================================
-- Los 5 que reporta el PASO 0b. La categoria se infiere del nombre y es la que
-- decide el nivel de todo trabajador que reciba ese puesto (ver Puesto.cs).
-- COORDINADOR ANALISTA CONTABLE y JEFE DE MARKETING ya existen y son puestos
-- DISTINTOS de estos: aca se crean nuevos, no se renombra nada.

INSERT INTO puesto (nombre, categoria_id, orden, created_date_time, active, state)
SELECT v.nombre,
       c.categoria_id,
       (SELECT COALESCE(max(orden), 0) FROM puesto) + row_number() OVER (ORDER BY v.nombre),
       now(), true, true
FROM (VALUES
    ('ANALISTA DE CULTURA Y DESARROLLO', 'ANALISTA'),
    ('COORDINADOR CONTABLE',             'COORDINADOR'),
    ('JEFE DE CALIDAD',                  'JEFE'),
    ('JEFE DE MARKETING DIGITAL',        'JEFE'),
    ('SUPERVISOR DE POST VENTA',         'SUPERVISOR')
) AS v(nombre, categoria)
JOIN categoria c ON c.nombre = v.categoria AND c.state
WHERE NOT EXISTS (SELECT 1 FROM puesto p WHERE p.nombre = v.nombre AND p.state);


-- ============================================================================
-- PASO 4 - Relacion puesto <-> area (los 115 pares del Excel)
-- ============================================================================

-- 4a. Los 102 pares de las 18 areas que ya existian.
INSERT INTO puesto_area_scope (puesto_id, area_scope_id, created_date_time, state)
SELECT p.puesto_id, v.area_scope_id, now(), true
FROM (VALUES
    (16, 'ASISTENTE DE MARKETING'),
    (16, 'ASISTENTE DIGITAL'),
    (16, 'CONTENT CREATOR'),
    (16, 'COORDINADOR DE TRADE MARKETING'),
    (16, 'DISEÑADOR GRAFICO SENIOR'),
    (16, 'EJECUTIVO DE MARKETING DE CONTENIDO'),
    (16, 'JEFE DE MARKETING'),
    (16, 'JEFE DE MARKETING DIGITAL'),
    (17, 'ARQUITECTO DE PRODUCCIÓN'),
    (17, 'ASISTENTE DE PRODUCCIÓN'),
    (17, 'COORDINADOR ADMINISTRATIVO DE OBRA'),
    (17, 'INGENIERO DE PRODUCCIÓN'),
    (17, 'INGENIERO PRACTICANTE'),
    (17, 'INGENIERO RESIDENTE'),
    (17, 'JEFE DE ARQUITECTURA'),
    (17, 'JEFE DE ARQUITECTURA COMERCIAL'),
    (17, 'JEFE DE CALIDAD'),
    (17, 'JEFE DE COSTOS Y PRESUPUESTOS'),
    (17, 'JEFE DE POST VENTA'),
    (17, 'JEFE DE PROYECTOS'),
    (17, 'JEFE DE SEGURIDAD Y SALUD EN EL TRABAJO'),
    (41, 'ARQUITECTO BIM'),
    (41, 'ARQUITECTO DE PROYECTOS'),
    (41, 'INGENIERO DE INSTALACIONES'),
    (41, 'INGENIERO DE PLANEAMIENTO BIM'),
    (41, 'INGENIERO DE PROYECTOS'),
    (41, 'MODELADOR BIM'),
    (42, 'ARQUITECTO DE CALIDAD'),
    (42, 'ASISTENTE DE CALIDAD'),
    (42, 'INGENIERO DE CALIDAD'),
    (42, 'SUPERVISOR DE INSTALACIONES'),
    (44, 'COORDINADOR SSOMA'),
    (44, 'MÉDICO OCUPACIONAL'),
    (44, 'PREVENCIONISTA DE RIESGOS'),
    (46, 'ASISTENTE DE COSTOS Y PRESUPUESTOS'),
    (46, 'ASISTENTE DE OFICINA TECNICA'),
    (46, 'COORDINADOR DE COSTOS Y PRESUPUESTOS'),
    (46, 'INGENIERO DE COSTOS Y PRESUPUESTOS'),
    (46, 'INGENIERO DE OFICINA TECNICA'),
    (52, 'ARQUITECTO DE POST VENTA'),
    (52, 'ARQUITECTO DE POST VENTA JR'),
    (52, 'ESPECIALISTA DE GESTIONES VECINALES'),
    (52, 'PREVENCIONISTA DE RIESGOS JR'),
    (52, 'SUPERVISOR DE POST VENTA'),
    (54, 'AGENTE DE SEGURIDAD'),
    (54, 'COORDINADOR CONTABLE'),
    (54, 'COORDINADOR LEGAL'),
    (54, 'JEFE DE ADMINISTRACIÓN'),
    (54, 'JEFE DE GESTIONES ADMINISTRATIVAS'),
    (54, 'JEFE DE LOGISTICA'),
    (54, 'JEFE DE TI'),
    (54, 'SUPERVISORA DE LIMPIEZA'),
    (56, 'ASISTENTE DE CONTABILIDAD'),
    (56, 'ASISTENTE DE TESORERIA'),
    (56, 'AUXILIAR ADMINISTRATIVO'),
    (56, 'TESORERA'),
    (57, 'ANALISTA DE PLANEAMIENTO FINANCIERO Y CONTROL DE GESTIÓN'),
    (58, 'ADMINISTRADOR DE OBRA'),
    (58, 'ANALISTA DE CULTURA Y DESARROLLO'),
    (58, 'ANALISTA DE GTH'),
    (58, 'ASISTENTA SOCIAL'),
    (58, 'ASISTENTE DE GTH'),
    (59, 'ABOGADO DE GESTIONES INMOBILIARIAS'),
    (59, 'ABOGADO DE GESTIONES INMOBILIARIAS JR'),
    (59, 'ANALISTA ADMINISTRATIVO'),
    (59, 'ASISTENTE ADMINISTRATIVO'),
    (59, 'ASISTENTE ADMINISTRATIVO DE COBRANZAS'),
    (59, 'ASISTENTE DE ADMINISTRACION'),
    (59, 'ASISTENTE DE CUMPLIMIENTO'),
    (59, 'ASISTENTE LEGAL'),
    (59, 'AUXILIAR DE LIMPIEZA'),
    (59, 'GESTOR ADMINISTRATIVO'),
    (59, 'PROCURADOR'),
    (60, 'ALMACENERO'),
    (60, 'ASISTENTE DE LOGISTICA'),
    (60, 'CHOFER'),
    (60, 'COORDINADOR DE LOGISITICA'),
    (60, 'JEFE DE ALMACÉN'),
    (60, 'PRACTICANTE PROFESIONAL DE LOGISTICA'),
    (61, 'ASISTENTE DE DESARROLLO SSOMA'),
    (61, 'ASISTENTE DE TI'),
    (61, 'COORDINADOR DE TI'),
    (61, 'COORDINADOR ERP'),
    (61, 'ESPECIALISTA EN BUSINESS INTELLIGENCE'),
    (61, 'ESPECIALISTA EN TRANSFORMACIÓN DIGITAL'),
    (62, 'ASISTENTE DE GESTIONES ADMINISTRATIVAS'),
    (62, 'AUXLIAR DE GESTIONES ADMINISTRATIVAS'),
    (62, 'COORDINADOR DE GESTIONES ADMINISTRATIVAS'),
    (63, 'ASESOR DE VENTAS'),
    (63, 'ASISTENTE DE VENTAS'),
    (63, 'BACK OFFICE INMOBILIARIO'),
    (63, 'COORDINADOR DE INTELIGENCIA COMERCIAL'),
    (63, 'COORDINADOR DE VENTAS'),
    (63, 'GESTOR DE VENTAS'),
    (63, 'OPERADOR DE CONTACT CENTER'),
    (80, 'ARQUITECTO COMERCIAL'),
    (80, 'ARQUITECTO COMERCIAL JUNIOR'),
    (80, 'COORDINADOR DE ARQUITECTURA COMERCIAL'),
    (81, 'ARQUITECTO COORDINADOR DE OBRA'),
    (81, 'ARQUITECTO DE PROYECTOS'),
    (81, 'COORDINADOR DE ARQUITECTURA DE PROYECTOS'),
    (81, 'PRACTICANTE PRE PROFESIONAL DE ARQUITECTURA')
) AS v(area_scope_id, puesto)
JOIN puesto p ON p.nombre = v.puesto AND p.state
ON CONFLICT DO NOTHING;

-- 4b. Los 13 pares de Gerencia General (el nodo lo creo el PASO 2).
INSERT INTO puesto_area_scope (puesto_id, area_scope_id, created_date_time, state)
SELECT p.puesto_id, gg.area_scope_id, now(), true
FROM (VALUES
    ('ASISTENTE ADMINISTRATIVO'),
    ('ASISTENTE DE GERENCIA GENERAL'),
    ('ASISTENTE DE OPERACIONES'),
    ('BARISTA EJECUTIVO'),
    ('CHOFER'),
    ('GERENTE DE ADMINISTRACION Y FINANZAS'),
    ('GERENTE DE MARKETING'),
    ('GERENTE GENERAL'),
    ('GERENTE INMOBILIARIO'),
    ('JEFE DE GTH'),
    ('JEFE DE VENTAS'),
    ('PERSONAL DE MANTENIMIENTO'),
    ('VIGILANTE')
) AS v(puesto)
JOIN puesto p ON p.nombre = v.puesto AND p.state
CROSS JOIN LATERAL (
    SELECT s.area_scope_id
    FROM area_scope s
    JOIN area_item ai ON ai.area_item_id = s.area_item_id
    WHERE ai.area_item_name = 'Gerencia General'
      AND s.area_scope_parent_id IS NULL
      AND s.state AND ai.state
    ORDER BY s.area_scope_id
    LIMIT 1
) gg
ON CONFLICT DO NOTHING;


-- ============================================================================
-- PASO 5 - Verificacion
-- ============================================================================

-- 5a. Tienen que salir 115 vinculos vivos sobre 112 puestos y 19 areas.
SELECT count(*)                      AS vinculos,
       count(DISTINCT puesto_id)     AS puestos_con_area,
       count(DISTINCT area_scope_id) AS areas_con_puestos
FROM puesto_area_scope WHERE state;

-- 5b. Cuantos puestos quedaron por area.
SELECT ai.area_item_name AS area, count(*) AS puestos
FROM puesto_area_scope pas
JOIN area_scope s ON s.area_scope_id = pas.area_scope_id
JOIN area_item ai ON ai.area_item_id = s.area_item_id
WHERE pas.state
GROUP BY ai.area_item_name
ORDER BY 1;

-- 5c. Puestos que quedaron en mas de un area. Esperado: 3 --
--     ARQUITECTO DE PROYECTOS (Unidad de Proyectos + Arquitectura),
--     ASISTENTE ADMINISTRATIVO (Legal + Gerencia General),
--     CHOFER (Logistica + Gerencia General).
SELECT p.nombre, string_agg(ai.area_item_name, ' + ' ORDER BY ai.area_item_name) AS areas
FROM puesto_area_scope pas
JOIN puesto p     ON p.puesto_id = pas.puesto_id
JOIN area_scope s ON s.area_scope_id = pas.area_scope_id
JOIN area_item ai ON ai.area_item_id = s.area_item_id
WHERE pas.state
GROUP BY p.puesto_id, p.nombre
HAVING count(*) > 1
ORDER BY 1;


-- ============================================================================
-- PASO 6 - REVISAR: quien se queda sin poder pedir personal
-- ============================================================================
-- El Excel mapeo los puestos a la GERENCIA o a un area estandar, pero varias
-- areas HOJA del arbol quedaron sin ningun puesto. Un usuario cuyo
-- workers.area_scope_id apunte a una de esas ve el desplegable "Puesto" VACIO y
-- no puede registrar la solicitud (la pantalla se lo avisa, pero igual queda
-- trabado). Al 2026-08-21 son 48 usuarios en prod, repartidos en 7 areas:
-- Produccion (16), Administracion de Obra (8), Marketing (8), Residencia (7),
-- Planeamiento BIM (4), Administracion (3), Ingenieria BIM (2).
--
-- Se arregla con DATA, no con codigo: desde Gestion GTH > Configuracion >
-- Categorias y Puestos, editando cada puesto y agregandole esas areas.
-- Esta consulta dice exactamente cuales faltan.

WITH RECURSIVE sub AS (
    SELECT s.area_scope_id AS raiz, s.area_scope_id AS nodo
    FROM area_scope s WHERE s.state
    UNION ALL
    SELECT sub.raiz, c.area_scope_id
    FROM sub JOIN area_scope c ON c.area_scope_parent_id = sub.nodo AND c.state
)
SELECT ai.area_item_name AS area,
       s.area_scope_id,
       count(DISTINCT p.puesto_id) AS puestos_que_vera,
       (SELECT count(*)
        FROM workers w
        JOIN person pe ON pe.person_id = w.person_id AND pe.user_id IS NOT NULL
        WHERE w.area_scope_id = s.area_scope_id) AS usuarios_en_el_area
FROM area_scope s
JOIN area_item ai ON ai.area_item_id = s.area_item_id
JOIN sub ON sub.raiz = s.area_scope_id
LEFT JOIN puesto_area_scope pas ON pas.area_scope_id = sub.nodo AND pas.state
LEFT JOIN puesto p ON p.puesto_id = pas.puesto_id AND p.state AND p.active
WHERE s.state
GROUP BY s.area_scope_id, ai.area_item_name
HAVING count(DISTINCT p.puesto_id) = 0
ORDER BY 4 DESC, 1;
