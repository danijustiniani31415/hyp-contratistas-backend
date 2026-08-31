-- ============================================================================
-- Gestión GTH · Reclutamiento — Destinatarios dinámicos y switches de correo
--
-- La configuración de correos de "Solicitud de Personal" pasa de ser un modal
-- con dos cajas de texto (Para / CC, todo escrito a mano) a una pantalla propia
-- con una sección por correo, al estilo de la Configuración de EMOs:
--
--   • Cada correo (gth_correo_tipo) se puede APAGAR entero con un interruptor
--     (gth_correo_tipo.active, que hasta ahora nadie leía). Apagado = no se
--     envía, sin tener que borrarle los destinatarios.
--   • Cada destinatario (gth_correo_destinatario) se prende/apaga por su cuenta
--     (gth_correo_destinatario.active, que ya existía pero solo lo escribía el
--     "replace" masivo del modal viejo).
--   • Aparecen los destinatarios DINÁMICOS: filas cuyo correo NO está guardado
--     sino que se resuelve al momento de enviar. Se identifican por `codigo`:
--       - GERENTE_GENERAL → workers.email_corporativo del trabajador ACTIVO
--                           cuya CATEGORÍA es 'GERENTE GENERAL'.
--       - GERENTE_AREA    → gerente del área del solicitante (categoría GERENTE,
--                           subiendo por el árbol de area_scope). Antes estaba
--                           cableado en el código y siempre encendido; ahora es
--                           una fila más que se puede apagar.
--       - GTH_AREA        → area_scope.email del área "Gestión del Talento
--                           Humano" (id 58).
--     Por eso `email` pasa a ser NULL-able: un destinatario dinámico no tiene
--     correo propio. El CHECK obliga a que la fila tenga código o correo.
--
-- Los destinatarios escritos a mano que ya existen NO se tocan: siguen vivos y
-- activos como "correos adicionales" y conviven con los dinámicos en la misma
-- sección. Si un correo de pruebas quedó configurado, hay que apagarlo desde la
-- pantalla — este script no decide eso por nadie.
--
-- ⚠️ Cambio de comportamiento tras correr el script:
--    • El correo de aprobación (APROBACION_GG) empieza a salirle al Gerente
--      General real (puesto 'GERENTE GENERAL'), además de a los correos que ya
--      estuvieran configurados a mano.
--    • GTH_AREA se siembra APAGADO en los tres correos que van a GTH, para no
--      cambiar a quién le llega hoy. Se prende desde la pantalla cuando se
--      quiera.
--
-- Idempotente: se puede correr múltiples veces sin duplicar ni romper nada.
-- ============================================================================

BEGIN;

-- ============================================================================
-- 0) Categoría 'GERENTE GENERAL' y su asignación
--
--    El destinatario dinámico GERENTE_GENERAL se resuelve por CATEGORÍA, no por
--    puesto: `puesto` es el campo de presentación y no debe decidir negocio.
--    La categoría ya existe en ambas bases (id 39), pero NINGÚN trabajador la
--    tiene asignada: el Gerente General sigue en la categoría genérica 'GERENTE',
--    así que sin este paso el destinatario no resolvería a nadie.
--
--    La asignación se deriva UNA sola vez desde el puesto, que es el dato que hoy
--    identifica al Gerente General. Es un bootstrap de datos, no una regla de
--    negocio: de acá en adelante manda la categoría. Solo toca a quien siga en
--    'GERENTE', así que no pisa una asignación hecha a mano ni se repite.
--
--    Verificado que el cambio no arrastra efectos colaterales: los otros lugares
--    que filtran por categoría 'GERENTE' (visibilidad y aprobación de salidas,
--    aprobador de solicitudes, gerente del área, recordatorios de evaluaciones)
--    exigen area_scope_id, subarea o un usuario del sistema, y la ficha del
--    Gerente General no tiene ninguno de los tres.
-- ============================================================================
INSERT INTO categoria (nombre, orden, created_date_time, active, state)
SELECT 'GERENTE GENERAL', 2000, now(), true, true
WHERE NOT EXISTS (
    SELECT 1 FROM categoria WHERE upper(nombre) = 'GERENTE GENERAL' AND state = true
);

UPDATE workers w
   SET categoria_id = cgg.categoria_id,
       updated_at   = now()
FROM categoria cgg, puesto pu, categoria cact
WHERE upper(cgg.nombre) = 'GERENTE GENERAL' AND cgg.state = true
  AND pu.puesto_id = w.puesto_id AND pu.state = true
  AND upper(pu.nombre) = 'GERENTE GENERAL'
  AND cact.categoria_id = w.categoria_id
  AND upper(cact.nombre) = 'GERENTE'      -- solo el que sigue en la categoría genérica
  AND w.estado = 'ACTIVO';

-- ============================================================================
-- 1) gth_correo_tipo: descripción de cada correo
--    El texto que explica a quién se le manda cada correo vivía hardcodeado en
--    el frontend (un array de `intro` por pestaña). Se baja a la BD para que la
--    pantalla se arme sola con lo que haya en el catálogo.
-- ============================================================================
ALTER TABLE gth_correo_tipo
    ADD COLUMN IF NOT EXISTS descripcion text NULL;

UPDATE gth_correo_tipo SET descripcion =
    'Primer paso del flujo: al registrar una solicitud de personal se envía este correo para que la aprueben. '
    'Hasta que no lo aprueben, GTH no recibe nada. Sin ningún destinatario principal activo la solicitud queda '
    'esperando y hay que reenviar el correo desde la tabla.'
WHERE codigo = 'APROBACION_GG' AND state = true AND descripcion IS DISTINCT FROM
    'Primer paso del flujo: al registrar una solicitud de personal se envía este correo para que la aprueben. '
    'Hasta que no lo aprueben, GTH no recibe nada. Sin ningún destinatario principal activo la solicitud queda '
    'esperando y hay que reenviar el correo desde la tabla.';

UPDATE gth_correo_tipo SET descripcion =
    'Se envía a GTH con las vacantes que Gerencia General aprobó. Sale recién después de la aprobación y solo '
    'con lo aprobado.'
WHERE codigo = 'SOLICITUD' AND state = true AND descripcion IS DISTINCT FROM
    'Se envía a GTH con las vacantes que Gerencia General aprobó. Sale recién después de la aprobación y solo '
    'con lo aprobado.';

UPDATE gth_correo_tipo SET descripcion =
    'Se envía a GTH cuando el solicitante manda su decisión sobre la long list (candidatos aprobados y '
    'rechazados). Es independiente del correo de nueva solicitud.'
WHERE codigo = 'LONG_LIST_DECISION' AND state = true AND descripcion IS DISTINCT FROM
    'Se envía a GTH cuando el solicitante manda su decisión sobre la long list (candidatos aprobados y '
    'rechazados). Es independiente del correo de nueva solicitud.';

UPDATE gth_correo_tipo SET descripcion =
    'Se envía a GTH cuando el solicitante aprueba o rechaza a un finalista. Es independiente de los otros correos.'
WHERE codigo = 'FINALISTA_DECISION' AND state = true AND descripcion IS DISTINCT FROM
    'Se envía a GTH cuando el solicitante aprueba o rechaza a un finalista. Es independiente de los otros correos.';

UPDATE gth_correo_tipo SET descripcion =
    'Se envía al solicitante cuando GTH le manda la long list de CVs. El destinatario principal es SIEMPRE el '
    'solicitante que registró la solicitud; acá solo se definen principales adicionales y copias.'
WHERE codigo = 'LONG_LIST' AND state = true AND descripcion IS DISTINCT FROM
    'Se envía al solicitante cuando GTH le manda la long list de CVs. El destinatario principal es SIEMPRE el '
    'solicitante que registró la solicitud; acá solo se definen principales adicionales y copias.';

-- ============================================================================
-- 2) gth_correo_destinatario: código dinámico, nombre, descripción y orden
-- ============================================================================
ALTER TABLE gth_correo_destinatario
    ADD COLUMN IF NOT EXISTS codigo      text    NULL,
    ADD COLUMN IF NOT EXISTS nombre      text    NULL,
    ADD COLUMN IF NOT EXISTS descripcion text    NULL,
    ADD COLUMN IF NOT EXISTS orden       integer NOT NULL DEFAULT 0;

-- Un destinatario dinámico no guarda correo: se resuelve al enviar.
ALTER TABLE gth_correo_destinatario
    ALTER COLUMN email DROP NOT NULL;

-- ...pero una fila sin código Y sin correo no le llega a nadie.
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_gth_correo_destinatario_codigo_o_email'
    ) THEN
        ALTER TABLE gth_correo_destinatario
            ADD CONSTRAINT ck_gth_correo_destinatario_codigo_o_email
            CHECK (codigo IS NOT NULL OR email IS NOT NULL);
    END IF;
END $$;

-- El índice viejo (tipo, email) WHERE state se cae con varias filas dinámicas
-- (email NULL no choca en Postgres, pero sí queremos separar los dos mundos):
--   • los correos adicionales son únicos por (tipo, correo)
--   • los dinámicos son únicos por (tipo, código)
DROP INDEX IF EXISTS ix_gth_correo_destinatario_tipo_email_vivo;

CREATE UNIQUE INDEX IF NOT EXISTS ix_gth_correo_destinatario_tipo_email_vivo
    ON gth_correo_destinatario (gth_correo_tipo_id, lower(email))
    WHERE state = true AND codigo IS NULL AND email IS NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ix_gth_correo_destinatario_tipo_codigo_vivo
    ON gth_correo_destinatario (gth_correo_tipo_id, upper(codigo))
    WHERE state = true AND codigo IS NOT NULL;

-- Los adicionales que ya existían quedan después de los dinámicos del catálogo.
UPDATE gth_correo_destinatario
   SET orden = 100
 WHERE state = true AND codigo IS NULL AND orden = 0;

-- ============================================================================
-- 3) Siembra de los destinatarios dinámicos
--
--    APROBACION_GG → Gerente General + Gerente del área (ACTIVOS: es el
--                    comportamiento que se pidió; el gerente del área ya salía
--                    hoy, solo que cableado en el código).
--    SOLICITUD / LONG_LIST_DECISION / FINALISTA_DECISION → Área de GTH
--                    (APAGADO: nadie lo recibía antes, se prende desde la UI).
-- ============================================================================
INSERT INTO gth_correo_destinatario
    (gth_correo_tipo_id, codigo, email, nombre, descripcion, es_copia, orden, active, state)
SELECT t.gth_correo_tipo_id, v.codigo, NULL, v.nombre, v.descripcion, false, v.orden, v.activo, true
FROM (VALUES
        ('APROBACION_GG',      'GERENTE_GENERAL', 'Gerente General',
         'Se toma el correo corporativo del trabajador activo cuya categoría es «Gerente General».', 1, true),
        ('APROBACION_GG',      'GERENTE_AREA',    'Gerente del área solicitante',
         'Se busca al trabajador de categoría «Gerente» del área del solicitante, subiendo por el árbol de áreas.', 2, true),
        ('SOLICITUD',          'GTH_AREA',        'Área de Gestión del Talento Humano',
         'Se toma el correo configurado para el área de GTH en Configuración → Áreas.', 1, false),
        ('LONG_LIST_DECISION', 'GTH_AREA',        'Área de Gestión del Talento Humano',
         'Se toma el correo configurado para el área de GTH en Configuración → Áreas.', 1, false),
        ('FINALISTA_DECISION', 'GTH_AREA',        'Área de Gestión del Talento Humano',
         'Se toma el correo configurado para el área de GTH en Configuración → Áreas.', 1, false)
     ) AS v(tipo_codigo, codigo, nombre, descripcion, orden, activo)
JOIN gth_correo_tipo t ON t.codigo = v.tipo_codigo AND t.state = true
WHERE NOT EXISTS (
    SELECT 1 FROM gth_correo_destinatario d
    WHERE d.gth_correo_tipo_id = t.gth_correo_tipo_id
      AND d.state = true
      AND upper(d.codigo) = upper(v.codigo)
);

-- Refresca nombre/descripción/orden de los dinámicos si el texto cambió en una
-- corrida posterior (el `active` NO se toca: es decisión del usuario).
UPDATE gth_correo_destinatario d
   SET nombre      = v.nombre,
       descripcion = v.descripcion,
       orden       = v.orden,
       updated_date_time = now()
FROM (VALUES
        ('GERENTE_GENERAL', 'Gerente General',
         'Se toma el correo corporativo del trabajador activo cuya categoría es «Gerente General».', 1),
        ('GERENTE_AREA',    'Gerente del área solicitante',
         'Se busca al trabajador de categoría «Gerente» del área del solicitante, subiendo por el árbol de áreas.', 2),
        ('GTH_AREA',        'Área de Gestión del Talento Humano',
         'Se toma el correo configurado para el área de GTH en Configuración → Áreas.', 1)
     ) AS v(codigo, nombre, descripcion, orden)
WHERE d.state = true
  AND upper(d.codigo) = upper(v.codigo)
  AND (d.nombre IS DISTINCT FROM v.nombre
       OR d.descripcion IS DISTINCT FROM v.descripcion
       OR d.orden IS DISTINCT FROM v.orden);

COMMIT;

-- ============================================================================
-- Verificación
-- ============================================================================
-- SELECT t.codigo AS correo, t.active AS correo_activo,
--        coalesce(d.codigo, d.email) AS destinatario,
--        d.es_copia, d.active, d.orden
--   FROM gth_correo_tipo t
--   LEFT JOIN gth_correo_destinatario d
--          ON d.gth_correo_tipo_id = t.gth_correo_tipo_id AND d.state = true
--  WHERE t.state = true
--  ORDER BY t.orden, d.orden, d.gth_correo_destinatario_id;
--
-- Gerente General que se va a resolver:
-- SELECT w.id, w.email_corporativo, c.nombre AS categoria
--   FROM workers w JOIN categoria c ON c.categoria_id = w.categoria_id
--  WHERE upper(c.nombre) = 'GERENTE GENERAL' AND c.state AND w.estado = 'ACTIVO'
--    AND w.email_corporativo LIKE '%@%';
