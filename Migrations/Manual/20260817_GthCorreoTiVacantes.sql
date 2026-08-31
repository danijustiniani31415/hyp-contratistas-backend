-- ============================================================================
-- Gestión GTH · Aprobaciones — Correo de vacantes aprobadas a TI
-- Fecha: 2026-08-17
--
-- Cuando Gerencia General aprueba una solicitud de personal salía UN correo:
-- el aviso a GTH con las vacantes aprobadas (tipo SOLICITUD). Ahora sale un
-- segundo correo, a TI, con esas mismas vacantes, para que puedan ir previendo
-- equipo, usuario, correo y accesos de cada ingreso.
--
-- Es un correo aparte y no una copia del de GTH a propósito: son dos áreas con
-- necesidades distintas (GTH recluta, TI alista), su cuerpo es distinto y cada
-- una se prende, se apaga y se configura por su cuenta desde
-- /gestion-gth/aprobaciones/configuracion.
--
--   • gth_correo_tipo        → TI_VACANTES (nuevo)
--   • gth_correo_destinatario→ TI_AREA (dinámico, principal, ACTIVO)
--   • area_scope.email       → sistemas@abril.pe en el área de TI
--
-- El correo de TI NO va escrito en el código ni guardado en la fila del
-- destinatario: TI_AREA es un destinatario dinámico que lee area_scope.email
-- del nodo "Tecnología de la Información" al momento de enviar, igual que ya lo
-- hace GTH_AREA con el área de GTH. Si mañana TI cambia de buzón, se cambia en
-- Configuración → Áreas y todos los correos que lo usan lo siguen solos.
--
-- El área se ubica por NOMBRE y no por su area_scope_id (61 en ambas bases) para
-- que el script no dependa de que el id sea el mismo. El patrón del LIKE evita a
-- propósito las vocales acentuadas, así el match no depende de la codificación
-- del cliente con el que se corra.
--
-- ⚠️ Cambio de comportamiento tras correr el script: desde la siguiente
--    aprobación de Gerencia General, TI empieza a recibir este correo. Si se
--    quiere postergar, apagar el correo desde la pantalla de Configuración
--    (sección «Aprobación de Gerencia a TI») — no hace falta tocar la BD.
--
-- Idempotente: se puede correr varias veces sin duplicar ni pisar nada.
-- Aplicar en dev y prod.
-- ============================================================================

BEGIN;

-- ============================================================================
-- 1) Correo del área de Tecnología de la Información
--
--    Solo se escribe si el área todavía no tiene correo cargado: si alguien ya
--    lo configuró desde Configuración → Áreas, esa decisión manda y una segunda
--    corrida de este script no la pisa.
-- ============================================================================
UPDATE area_scope s
   SET email = 'sistemas@abril.pe'
  FROM area_item i
 WHERE i.area_item_id = s.area_item_id
   AND i.state
   AND s.state
   AND i.area_item_name ILIKE 'Tecnolog_a de la Informaci_n'
   AND (s.email IS NULL OR btrim(s.email) = '');

-- ============================================================================
-- 2) Tipo de correo TI_VACANTES
--
--    active = true (default): el correo nace prendido, que es justo lo que se
--    pidió. Se apaga desde la pantalla si hiciera falta.
-- ============================================================================
INSERT INTO gth_correo_tipo (codigo, nombre, descripcion, orden, principal_automatico)
SELECT 'TI_VACANTES',
       'Aprobación de Gerencia a TI',
       'Se envía a TI con las vacantes que Gerencia General aprobó, al mismo tiempo que el aviso a GTH. Es un correo de anticipación: TI lo usa para prever equipo, usuario, correo y accesos de cada ingreso. Se prende y se apaga por su cuenta, sin afectar al correo a GTH.',
       3,
       false
WHERE NOT EXISTS (
    SELECT 1 FROM gth_correo_tipo WHERE codigo = 'TI_VACANTES' AND state
);

-- El nuevo correo sale en el mismo paso del flujo que el de GTH, así que va
-- justo detrás. Se reescriben todos los órdenes con su valor final (y no con un
-- "orden + 1") para que correr el script dos veces deje lo mismo.
UPDATE gth_correo_tipo t
   SET orden             = v.orden,
       updated_date_time = now()
FROM (VALUES
        ('APROBACION_GG',         1),
        ('SOLICITUD',             2),
        ('TI_VACANTES',           3),
        ('LONG_LIST',             4),
        ('LONG_LIST_DECISION',    5),
        ('FINALISTA_DECISION',    6),
        ('FORMULARIO_COMPLETADO', 7),
        ('ENTREVISTA',            8)
     ) AS v(codigo, orden)
WHERE t.codigo = v.codigo
  AND t.state
  AND t.orden IS DISTINCT FROM v.orden;

-- ============================================================================
-- 3) Destinatario dinámico TI_AREA
--
--    Principal (es_copia = false) y ACTIVO: es el destinatario del correo, no
--    un agregado opcional. Sin él el correo no le llegaría a nadie.
--    Va sin email: lo resuelve el backend leyendo area_scope.email del paso 1.
-- ============================================================================
INSERT INTO gth_correo_destinatario
    (gth_correo_tipo_id, codigo, email, nombre, descripcion, es_copia, orden, active)
SELECT t.gth_correo_tipo_id,
       'TI_AREA',
       NULL,
       'Área de Tecnología de la Información',
       'Se toma el correo configurado para el área de TI en Configuración → Áreas.',
       false,
       1,
       true
FROM gth_correo_tipo t
WHERE t.codigo = 'TI_VACANTES' AND t.state
  AND NOT EXISTS (
      SELECT 1 FROM gth_correo_destinatario d
      WHERE d.gth_correo_tipo_id = t.gth_correo_tipo_id
        AND upper(d.codigo) = 'TI_AREA' AND d.state
  );

COMMIT;

-- ============================================================================
-- Verificación
-- ============================================================================
-- Correo que va a resolver TI_AREA (debe decir sistemas@abril.pe):
-- SELECT s.area_scope_id, i.area_item_name, s.email
--   FROM area_scope s JOIN area_item i ON i.area_item_id = s.area_item_id
--  WHERE s.state AND i.area_item_name ILIKE 'Tecnolog_a de la Informaci_n';
--
-- El correo nuevo y sus destinatarios:
-- SELECT t.codigo AS correo, t.nombre, t.active AS correo_activo, t.orden,
--        d.codigo AS destinatario, d.es_copia, d.active
--   FROM gth_correo_tipo t
--   LEFT JOIN gth_correo_destinatario d
--          ON d.gth_correo_tipo_id = t.gth_correo_tipo_id AND d.state
--  WHERE t.state AND t.codigo = 'TI_VACANTES';
--
-- Los correos de la pantalla de Aprobaciones, en el orden en que se muestran:
-- SELECT codigo, nombre, orden, active FROM gth_correo_tipo
--  WHERE state AND codigo IN ('SOLICITUD', 'TI_VACANTES') ORDER BY orden;
