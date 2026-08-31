-- ═══════════════════════════════════════════════════════════════════════════
-- Gestión GTH · La aprobación se decide por TIPO DE REQUERIMIENTO
-- ═══════════════════════════════════════════════════════════════════════════
--
-- Hasta ahora toda solicitud de personal iba al mismo sitio: un correo a
-- Gerencia General y al gerente del área, donde el GG decidía (obligatorio) y el
-- gerente del área dejaba un visto bueno que no movía nada.
--
-- Desde ahora la ruta la decide cada VACANTE, por su tipo de requerimiento:
--
--   • NUEVO (y cualquier vacante FFT) → la aprueba SOLO Gerencia General.
--     El gerente del área sale del circuito: ni correo ni visto bueno.
--
--   • REEMPLAZO (que no sea FFT) → la aprueban el GERENTE DEL ÁREA del
--     solicitante y GTH, los dos. Gerencia General sale del circuito.
--
-- Una misma solicitud puede traer vacantes de los dos tipos: cada una sigue su
-- ruta y salen los dos correos, cada uno listando solo sus vacantes.
--
-- Qué agrega:
--   1) La tercera casilla de decisión (GTH) en gth_aprobacion_gg y su decisión
--      por vacante en gth_aprobacion_gg_detalle. Las dos que ya existían
--      (Gerencia General y gerente del área) no cambian de forma; lo que cambia
--      es CUÁNDO cuenta cada una, y eso vive en el código.
--   2) El correo APROBACION_REEMPLAZO, con el gerente del área y GTH como
--      destinatarios, y el reencuadre de APROBACION_GG a "solo requerimientos
--      nuevos" (se le da de baja su fila de GERENTE_AREA, que ya no aplica).
--
-- Idempotente: se puede correr más de una vez sin duplicar nada.
-- ═══════════════════════════════════════════════════════════════════════════

BEGIN;

SET client_encoding TO 'UTF8';

-- ───────────────────────────────────────────────────────────────────────────
-- 1) La casilla de GTH, en la cabecera y por vacante
-- ───────────────────────────────────────────────────────────────────────────
-- Nace apuntando a PENDIENTE, igual que las otras dos: una aprobación que ya
-- existía y que no tiene reemplazos simplemente nunca la va a usar.
ALTER TABLE gth_aprobacion_gg
    ADD COLUMN IF NOT EXISTS estado_gth_id          integer,
    ADD COLUMN IF NOT EXISTS gth_decidido_date_time timestamptz,
    ADD COLUMN IF NOT EXISTS gth_decidido_user_id   integer,
    ADD COLUMN IF NOT EXISTS gth_comentario         text;

UPDATE gth_aprobacion_gg
   SET estado_gth_id = (SELECT e.gth_aprobacion_gg_estado_id
                          FROM gth_aprobacion_gg_estado e
                         WHERE e.codigo = 'PENDIENTE' AND e.state LIMIT 1)
 WHERE estado_gth_id IS NULL;

DO $nn$
DECLARE pendiente_id int;
BEGIN
    SELECT e.gth_aprobacion_gg_estado_id INTO pendiente_id
      FROM gth_aprobacion_gg_estado e
     WHERE e.codigo = 'PENDIENTE' AND e.state
     LIMIT 1;

    IF pendiente_id IS NULL THEN
        RAISE EXCEPTION 'No esta sembrado el estado PENDIENTE de gth_aprobacion_gg_estado.';
    END IF;

    -- DEFAULT además de NOT NULL, y esto NO es decorativo: es lo que hace que la
    -- migración se pueda correr ANTES del deploy sin romper nada. El backend
    -- viejo inserta en gth_aprobacion_gg sin nombrar esta columna, así que con
    -- NOT NULL a secas toda solicitud registrada entre la migración y el deploy
    -- moriría con "null value in column estado_gth_id". Con el DEFAULT, el
    -- backend viejo escribe PENDIENTE —que es exactamente lo que corresponde— y
    -- el nuevo lo manda explícito.
    EXECUTE format('ALTER TABLE gth_aprobacion_gg ALTER COLUMN estado_gth_id SET DEFAULT %s', pendiente_id);

    -- NOT NULL solo cuando ya no queda ninguna fila sin valor (si el catálogo no
    -- estuviera sembrado, el UPDATE de arriba no habría llenado nada).
    IF NOT EXISTS (SELECT 1 FROM gth_aprobacion_gg WHERE estado_gth_id IS NULL) THEN
        ALTER TABLE gth_aprobacion_gg ALTER COLUMN estado_gth_id SET NOT NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_gth_aprobacion_gg_estado_gth') THEN
        ALTER TABLE gth_aprobacion_gg
            ADD CONSTRAINT fk_gth_aprobacion_gg_estado_gth
            FOREIGN KEY (estado_gth_id) REFERENCES gth_aprobacion_gg_estado (gth_aprobacion_gg_estado_id);
    END IF;
END
$nn$;

COMMENT ON COLUMN gth_aprobacion_gg.estado_gth_id IS
    'Casilla de decision de GTH sobre la solicitud. Solo aplica a las vacantes de tipo REEMPLAZO no-FFT.';

ALTER TABLE gth_aprobacion_gg_detalle
    ADD COLUMN IF NOT EXISTS aprobado_gth           boolean,
    ADD COLUMN IF NOT EXISTS gth_decidido_date_time timestamptz;

COMMENT ON COLUMN gth_aprobacion_gg_detalle.aprobado_gth IS
    'Decision de GTH sobre ESTA vacante: true aprobada, false rechazada, null sin decidir. Solo aplica a REEMPLAZO no-FFT, donde avanza recien cuando GTH y el gerente del area la aprueban.';

-- ───────────────────────────────────────────────────────────────────────────
-- 2) Los dos correos de aprobación
-- ───────────────────────────────────────────────────────────────────────────
-- 2.a) El nuevo: reemplazos, al gerente del área y a GTH.
INSERT INTO gth_correo_tipo (codigo, nombre, descripcion, orden,
                             principal_automatico, principal_automatico_active,
                             principal_automatico_nombre, active, state)
SELECT 'APROBACION_REEMPLAZO',
       'Aprobación de gerencia y GTH (reemplazos)',
       'Pide la aprobación de las vacantes de reemplazo al gerente del área del solicitante y a GTH.',
       2, false, true, NULL, true, true
WHERE NOT EXISTS (
    SELECT 1 FROM gth_correo_tipo t WHERE t.codigo = 'APROBACION_REEMPLAZO' AND t.state = true);

-- 2.b) El de siempre, reencuadrado: ahora solo cubre los requerimientos nuevos.
UPDATE gth_correo_tipo
   SET nombre      = 'Aprobación de Gerencia General (requerimientos nuevos)',
       descripcion = 'Pide a Gerencia General la aprobación de las vacantes nuevas de una solicitud recién registrada.'
 WHERE codigo = 'APROBACION_GG' AND state = true;

-- 2.c) Destinatarios del correo de reemplazos: los dos ACTIVOS de entrada. A
--      diferencia del resto de los correos del módulo —que se siembran apagados
--      para no escribirle a nadie por sorpresa— acá el correo no tiene ningún
--      sentido sin ellos: son justamente quienes tienen que aprobar, y dejarlo
--      apagado sería registrar reemplazos que nadie se entera de que existen.
INSERT INTO gth_correo_destinatario (gth_correo_tipo_id, codigo, nombre, descripcion,
                                     es_copia, orden, active, state)
SELECT t.gth_correo_tipo_id, v.codigo, v.nombre, v.descripcion, false, v.orden, true, true
FROM   gth_correo_tipo t
CROSS  JOIN (VALUES
    ('GERENTE_AREA', 'Gerente del área solicitante', 'Según el área del solicitante', 1),
    ('GTH_AREA',     'Área de Gestión del Talento Humano', NULL, 2)
) AS v(codigo, nombre, descripcion, orden)
WHERE  t.codigo = 'APROBACION_REEMPLAZO' AND t.state = true
  AND  NOT EXISTS (
        SELECT 1 FROM gth_correo_destinatario d
        WHERE  d.gth_correo_tipo_id = t.gth_correo_tipo_id
          AND  d.state = true
          AND  upper(d.codigo) = v.codigo);

-- 2.d) El gerente del área sale del correo de Gerencia General: los nuevos ya no
--      pasan por él. Baja lógica (state = false), no `active = false`: no es que
--      esté apagado y se pueda volver a prender desde la pantalla — es que ese
--      destinatario dejó de existir para este correo.
UPDATE gth_correo_destinatario d
   SET state             = false,
       updated_date_time = now()
  FROM gth_correo_tipo t
 WHERE t.gth_correo_tipo_id = d.gth_correo_tipo_id
   AND t.codigo = 'APROBACION_GG'
   AND d.state = true
   AND upper(d.codigo) = 'GERENTE_AREA';

-- 2.e) Orden del flujo completo, con el nuevo en su lugar (arranca el proceso,
--      igual que APROBACION_GG, así que va justo al lado).
UPDATE gth_correo_tipo t
SET    orden = v.orden
FROM  (VALUES
    ('APROBACION_GG',          1),
    ('APROBACION_REEMPLAZO',   2),
    ('FFT_SOLICITUD_GG',       3),
    ('SOLICITUD',              4),
    ('TI_VACANTES',            5),
    ('FFT_APROBACION_GG',      6),
    ('LONG_LIST',              7),
    ('LONG_LIST_DECISION',     8),
    ('FORMULARIO_ENVIO',       9),
    ('FORMULARIO_COMPLETADO', 10),
    ('FORMULARIO_CORRECCION', 11),
    ('FFT_EMO',               12),
    ('ENTREVISTA',            13),
    ('ENTREVISTA_RESPUESTA',  14),
    ('FINALISTA_ENVIO',       15),
    ('FINALISTA_DECISION',    16),
    ('AGRADECIMIENTO',        17)
) AS v(codigo, orden)
WHERE t.codigo = v.codigo
  AND t.state = true
  AND t.orden IS DISTINCT FROM v.orden;

COMMIT;

-- Verificación
-- SELECT t.codigo, t.nombre, t.orden, t.active,
--        (SELECT string_agg(d.codigo || CASE WHEN d.active THEN '' ELSE ' (apagado)' END, ', ' ORDER BY d.orden)
--           FROM gth_correo_destinatario d
--          WHERE d.gth_correo_tipo_id = t.gth_correo_tipo_id AND d.state) AS destinatarios
-- FROM gth_correo_tipo t WHERE t.state AND t.codigo LIKE 'APROBACION%' ORDER BY t.orden;
