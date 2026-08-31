-- ═══════════════════════════════════════════════════════════════════════════
-- Gestión GTH · Correos: aviso al gerente del área, reemplazo secuencial y
--                        dos avisos nuevos al solicitante en Reclutamiento
-- ═══════════════════════════════════════════════════════════════════════════
--
-- Cuatro tipos de correo nuevos y un cambio de destinatarios en uno existente.
-- Nada de esto es un cambio de esquema: la configuración de correos es data
-- (`gth_correo_tipo` + `gth_correo_destinatario`) y la pantalla de
-- Configuración se arma sola con lo que haya acá.
--
-- 1) AVISO_GERENTE_AREA — «Solicitud de vacantes nuevas (aviso al área)»
--    Sale junto con APROBACION_GG y con las MISMAS vacantes, pero al gerente
--    del área del solicitante y sin botón de aprobar: las vacantes nuevas las
--    decide Gerencia General, el gerente del área solo tiene que enterarse.
--    Se configura desde Solicitud de Personal.
--
-- 2) APROBACION_REEMPLAZO_GTH — «Aprobación de reemplazos (a GTH)»
--    El reemplazo pasa de aprobarse en PARALELO a aprobarse en SECUENCIA:
--
--       antes:  registrar solicitud ──┬─→ gerente del área ─┐
--                                     └─→ GTH ──────────────┴─→ las dos firmas
--
--       ahora:  registrar solicitud ──→ gerente del área ──→ GTH ──→ las dos
--
--    Hasta que el gerente del área no aprueba, GTH no ve esas vacantes en
--    «Aprobaciones» ni recibe ningún correo. Su firma es la que dispara ESTE
--    correo, que lleva solo las vacantes que el área aprobó. Se configura desde
--    Aprobaciones, que es donde se registra esa firma.
--
--    Por eso mismo, GTH_AREA sale de los destinatarios de APROBACION_REEMPLAZO
--    (el correo del registro de la solicitud): ese correo ahora es solo para el
--    gerente del área.
--
-- 3) ENTREVISTA_CONFIRMADA_SOLICITANTE — «Entrevista confirmada (al solicitante)»
--    Lo dispara el mismo botón «Confirmar» del correo de invitación que ya
--    avisaba a GTH, pero le habla al solicitante y solo cuando el candidato
--    CONFIRMA: es una cita a la que él tiene que ir, así que lleva día, hora y
--    lugar. Se configura desde Reclutamiento.
--
-- 4) CANDIDATO_RETOMADO — «Candidato retomado (al solicitante)»
--    Sale cuando GTH elige a un rechazado para continuar el proceso tras un EMO
--    de ingreso No Apto. Se configura desde Reclutamiento.
--
-- ── Qué hace concretamente en PRODUCCIÓN (verificado el 2026-08-28) ────────
-- Los destinatarios se copian del correo gemelo en vez de escribirse acá (ver el
-- paso 3 para el porqué), así que el resultado es:
--
--   AVISO_GERENTE_AREA        → calvarez@abril.pe (activo, copiado de
--                               APROBACION_GG) + GERENTE_AREA apagado.
--   APROBACION_REEMPLAZO_GTH  → vrabanal@abril.pe (activo, copiado de
--                               REEMPLAZO_APROBADO) + GTH_AREA apagado.
--   APROBACION_REEMPLAZO      → pierde GTH_AREA (baja lógica) y se le APAGA
--                               vrabanal@abril.pe, que es el buzón de GTH: con
--                               las firmas en secuencia, GTH tiene que enterarse
--                               del reemplazo recién cuando el gerente del área
--                               lo aprueba, y ese buzón ya quedó en el correo de
--                               arriba. calvarez@abril.pe se queda como está.
--   Los dos correos al solicitante no llevan destinatarios: su principal lo pone
--   el sistema.
--
-- Ojo con una consecuencia: **GERENTE_AREA queda apagado**, igual que en todos
-- los demás correos de esta base. O sea que el aviso al gerente del área existe
-- y funciona, pero todavía no le llega a ningún gerente de verdad — sale al
-- mismo buzón al que hoy sale el correo de aprobación. Prenderlo desde
-- Configuración es lo que lo pone en marcha, y es una decisión de cuándo, no una
-- parte de este script.
--
-- Idempotente: se puede correr varias veces sin duplicar ni pisar nada.
-- ═══════════════════════════════════════════════════════════════════════════

BEGIN;

SET client_encoding TO 'UTF8';

-- ───────────────────────────────────────────────────────────────────────────
-- 1) Los cuatro tipos nuevos
-- ───────────────────────────────────────────────────────────────────────────
INSERT INTO gth_correo_tipo (codigo, nombre, descripcion, orden,
                             principal_automatico, principal_automatico_active,
                             principal_automatico_nombre, active, state, created_date_time)
SELECT v.codigo, v.nombre, v.descripcion, v.orden,
       v.principal_automatico, true, v.principal_nombre, true, true, now()
FROM  (VALUES
    ('AVISO_GERENTE_AREA',
     'Aviso al gerente del área (requerimientos nuevos)',
     'Le avisa al gerente del área del solicitante las vacantes nuevas que se acaban de pedir. Es informativo: no las aprueba él, así que el correo sale sin el botón de aprobación.',
     2, false, NULL),

    ('APROBACION_REEMPLAZO_GTH',
     'Aprobación de reemplazos (a GTH)',
     'Le pide a GTH su aprobación de los reemplazos que el gerente del área ya aprobó. Es la segunda de las dos firmas: hasta que el área no decide, GTH no ve esas vacantes.',
     6, false, NULL),

    ('ENTREVISTA_CONFIRMADA_SOLICITANTE',
     'Entrevista confirmada (al solicitante)',
     'Le avisa al solicitante el día, la hora y el lugar de la entrevista que el candidato confirmó. Solo sale con la confirmación; el rechazo lo atiende GTH.',
     18, true, 'Solicitante del requerimiento'),

    ('CANDIDATO_RETOMADO',
     'Candidato retomado (al solicitante)',
     'Le avisa al solicitante que el proceso continúa con un candidato del historial de rechazados, y desde qué etapa se retoma.',
     21, true, 'Solicitante del requerimiento')
) AS v(codigo, nombre, descripcion, orden, principal_automatico, principal_nombre)
WHERE NOT EXISTS (
    SELECT 1 FROM gth_correo_tipo t WHERE t.codigo = v.codigo AND t.state = true
);

-- Nombre, descripción y principal automático también en el rerun (por si el
-- texto se afinó después de la primera corrida).
UPDATE gth_correo_tipo t
SET    nombre                      = v.nombre,
       descripcion                 = v.descripcion,
       principal_automatico        = v.principal_automatico,
       principal_automatico_nombre = v.principal_nombre,
       updated_date_time           = now()
FROM  (VALUES
    ('AVISO_GERENTE_AREA',
     'Aviso al gerente del área (requerimientos nuevos)',
     'Le avisa al gerente del área del solicitante las vacantes nuevas que se acaban de pedir. Es informativo: no las aprueba él, así que el correo sale sin el botón de aprobación.',
     false, NULL::text),
    ('APROBACION_REEMPLAZO_GTH',
     'Aprobación de reemplazos (a GTH)',
     'Le pide a GTH su aprobación de los reemplazos que el gerente del área ya aprobó. Es la segunda de las dos firmas: hasta que el área no decide, GTH no ve esas vacantes.',
     false, NULL),
    ('ENTREVISTA_CONFIRMADA_SOLICITANTE',
     'Entrevista confirmada (al solicitante)',
     'Le avisa al solicitante el día, la hora y el lugar de la entrevista que el candidato confirmó. Solo sale con la confirmación; el rechazo lo atiende GTH.',
     true, 'Solicitante del requerimiento'),
    ('CANDIDATO_RETOMADO',
     'Candidato retomado (al solicitante)',
     'Le avisa al solicitante que el proceso continúa con un candidato del historial de rechazados, y desde qué etapa se retoma.',
     true, 'Solicitante del requerimiento')
) AS v(codigo, nombre, descripcion, principal_automatico, principal_nombre)
WHERE t.codigo = v.codigo AND t.state = true;

-- ───────────────────────────────────────────────────────────────────────────
-- 2) El correo de reemplazo del registro deja de ir a GTH
--    Baja lógica del destinatario (state = false, como el borrado desde la
--    pantalla): ese buzón ya no pertenece a este correo. Su lugar es el tipo
--    nuevo APROBACION_REEMPLAZO_GTH, que sale después.
-- ───────────────────────────────────────────────────────────────────────────
UPDATE gth_correo_destinatario d
SET    state             = false,
       updated_date_time = now()
FROM   gth_correo_tipo t
WHERE  t.gth_correo_tipo_id = d.gth_correo_tipo_id
  AND  t.codigo = 'APROBACION_REEMPLAZO' AND t.state = true
  AND  upper(d.codigo) = 'GTH_AREA'
  AND  d.state = true;

UPDATE gth_correo_tipo
SET    nombre            = 'Aprobación del gerente del área (reemplazos)',
       descripcion       = 'Le pide al gerente del área del solicitante la aprobación de las vacantes de reemplazo recién registradas. Es la primera de las dos firmas: con su visto bueno, las vacantes pasan a GTH.',
       updated_date_time = now()
WHERE  codigo = 'APROBACION_REEMPLAZO' AND state = true;

UPDATE gth_correo_tipo
SET    descripcion       = 'Avisa a GTH los reemplazos que ya tienen las dos firmas (la del gerente del área y la suya) y que hay que empezar a reclutar.',
       updated_date_time = now()
WHERE  codigo = 'REEMPLAZO_APROBADO' AND state = true;

-- ───────────────────────────────────────────────────────────────────────────
-- 3) Destinatarios de los dos correos que no tienen principal automático. Los
--    otros dos no llevan fila: su principal es el solicitante, que lo pone el
--    sistema (principal_automatico).
--
--    Cada uno se arma copiando al GEMELO que ya existe, en vez de con una lista
--    escrita acá. La razón es concreta: en producción NINGÚN destinatario
--    dinámico está prendido salvo TI_AREA — el despliegue está en marcha y los
--    correos se están enviando a buzones concretos (calvarez@ para lo de
--    aprobación, vrabanal@ para lo de GTH) en vez de a los gerentes de verdad.
--    Una lista fija acá haría una de dos cosas malas: prender un dinámico y
--    empezar a escribirle a gerentes que todavía no debían recibir nada, o dejar
--    el correo nuevo sin ningún destinatario y que no salga.
--
--    Copiando los correos a mano del gemelo, cada correo nuevo aterriza donde ya
--    aterriza el suyo, con los mismos interruptores, en cualquier base:
--      • AVISO_GERENTE_AREA       ← los manuales de APROBACION_GG (mismo
--        disparo, mismas vacantes, otro público) + la fila dinámica
--        GERENTE_AREA con el mismo `active` que tenga en APROBACION_REEMPLAZO,
--        que es el otro correo dirigido al gerente del área.
--      • APROBACION_REEMPLAZO_GTH ← los manuales de REEMPLAZO_APROBADO (mismo
--        público, misma ruta) + la fila dinámica GTH_AREA, igual que allá.
--
--    Después de correr esto, revisar en Configuración que sea lo que se quiere.
--    Concretamente: PRENDER GERENTE_AREA en AVISO_GERENTE_AREA el día que los
--    gerentes de área deban empezar a recibirlo de verdad.
-- ───────────────────────────────────────────────────────────────────────────

-- 3.a) Los correos escritos a mano del gemelo, tal cual y con su `active`. Solo
--      los manuales: los dinámicos del gemelo son de SU público (el del correo
--      de aprobación es el Gerente General, y acá el público es el gerente del
--      área), así que el dinámico que corresponde lo pone el paso 3.b.
INSERT INTO gth_correo_destinatario (gth_correo_tipo_id, nombre, descripcion,
                                     email, es_copia, orden, active, state, created_date_time)
SELECT destino.gth_correo_tipo_id, d.nombre, d.descripcion,
       d.email, d.es_copia, d.orden, d.active, true, now()
FROM  (VALUES
    ('AVISO_GERENTE_AREA',       'APROBACION_GG'),
    ('APROBACION_REEMPLAZO_GTH', 'REEMPLAZO_APROBADO')
) AS v(destino_codigo, gemelo_codigo)
JOIN  gth_correo_tipo destino ON destino.codigo = v.destino_codigo AND destino.state = true
JOIN  gth_correo_tipo gemelo  ON gemelo.codigo  = v.gemelo_codigo  AND gemelo.state  = true
JOIN  gth_correo_destinatario d
       ON d.gth_correo_tipo_id = gemelo.gth_correo_tipo_id AND d.state = true
-- Solo los que hoy están PRENDIDOS: los apagados del gemelo son restos de una
-- configuración anterior y copiarlos solo ensuciaría la sección nueva.
WHERE d.codigo IS NULL AND d.email IS NOT NULL AND d.active = true
  AND NOT EXISTS (
    SELECT 1 FROM gth_correo_destinatario x
    WHERE  x.gth_correo_tipo_id = destino.gth_correo_tipo_id
      AND  x.state = true AND x.codigo IS NULL
      AND  lower(x.email) = lower(d.email));

-- 3.b) La fila dinámica propia de cada correo, si el gemelo no la trajo. El
--      `active` se toma del correo donde ese mismo destinatario ya está
--      configurado, para no prender por nuestra cuenta un buzón que la
--      organización todavía tiene apagado.
INSERT INTO gth_correo_destinatario (gth_correo_tipo_id, codigo, nombre, es_copia,
                                     orden, active, state, created_date_time)
SELECT destino.gth_correo_tipo_id, v.codigo, v.nombre, false, 1,
       coalesce((SELECT r.active
                   FROM gth_correo_destinatario r
                   JOIN gth_correo_tipo rt ON rt.gth_correo_tipo_id = r.gth_correo_tipo_id
                  WHERE rt.codigo = v.referencia_codigo AND rt.state = true
                    AND upper(r.codigo) = v.codigo AND r.state = true
                  LIMIT 1), true),
       true, now()
FROM  (VALUES
    ('AVISO_GERENTE_AREA',       'GERENTE_AREA', 'Gerente del área solicitante',       'APROBACION_REEMPLAZO'),
    ('APROBACION_REEMPLAZO_GTH', 'GTH_AREA',     'Área de Gestión del Talento Humano', 'REEMPLAZO_APROBADO')
) AS v(destino_codigo, codigo, nombre, referencia_codigo)
JOIN  gth_correo_tipo destino ON destino.codigo = v.destino_codigo AND destino.state = true
WHERE NOT EXISTS (
    SELECT 1 FROM gth_correo_destinatario d
    WHERE  d.gth_correo_tipo_id = destino.gth_correo_tipo_id
      AND  upper(d.codigo) = v.codigo
      AND  d.state = true
);

-- 3.c) El buzón de GTH sale del correo del REGISTRO de la solicitud.
--      Con las firmas en secuencia, GTH tiene que enterarse del reemplazo recién
--      cuando el gerente del área lo aprueba. El paso 2 ya le quitó el
--      destinatario dinámico GTH_AREA, pero en producción ese correo además
--      lleva el buzón de GTH escrito a mano, y ese también hay que correrlo de
--      lugar o el corte no existe.
--
--      Se identifica sin cablear ninguna dirección, con dos condiciones que solo
--      cumple un buzón de GTH:
--        • que YA esté recibiendo el correo nuevo APROBACION_REEMPLAZO_GTH (lo
--          acaba de dejar ahí el paso 3.a). Así nunca se le quita el correo a
--          alguien que no lo tenga en su nuevo lugar.
--        • que NO reciba APROBACION_GG, o sea que no sea un buzón de aprobación
--          ni el de quien está probando el flujo.
--      En producción eso da exactamente vrabanal@abril.pe; en desarrollo no da
--      ninguno, porque el buzón de pruebas está en los tres correos.
--
--      Se APAGA, no se borra: la fila sigue a la vista en Configuración y se
--      vuelve a prender con un clic si esto no era lo que se quería.
UPDATE gth_correo_destinatario d
SET    active            = false,
       updated_date_time = now()
FROM   gth_correo_tipo t
WHERE  t.gth_correo_tipo_id = d.gth_correo_tipo_id
  AND  t.codigo = 'APROBACION_REEMPLAZO' AND t.state = true
  AND  d.state = true AND d.active = true
  AND  d.codigo IS NULL AND d.email IS NOT NULL
  AND  EXISTS (
        SELECT 1 FROM gth_correo_destinatario g
        JOIN   gth_correo_tipo gt ON gt.gth_correo_tipo_id = g.gth_correo_tipo_id
        WHERE  gt.codigo = 'APROBACION_REEMPLAZO_GTH' AND gt.state = true
          AND  g.state = true AND g.active = true AND lower(g.email) = lower(d.email))
  AND  NOT EXISTS (
        SELECT 1 FROM gth_correo_destinatario a
        JOIN   gth_correo_tipo at2 ON at2.gth_correo_tipo_id = a.gth_correo_tipo_id
        WHERE  at2.codigo = 'APROBACION_GG' AND at2.state = true
          AND  a.state = true AND lower(a.email) = lower(d.email));

-- ───────────────────────────────────────────────────────────────────────────
-- 4) Orden del flujo completo (solo afecta el orden de las pestañas)
-- ───────────────────────────────────────────────────────────────────────────
UPDATE gth_correo_tipo t
SET    orden = v.orden, updated_date_time = now()
FROM  (VALUES
    ('APROBACION_GG',                      1),
    ('AVISO_GERENTE_AREA',                 2),
    ('APROBACION_REEMPLAZO',               3),
    ('FFT_SOLICITUD_GG',                   4),
    ('SOLICITUD',                          5),
    ('APROBACION_REEMPLAZO_GTH',           6),
    ('REEMPLAZO_APROBADO',                 7),
    ('TI_VACANTES',                        8),
    ('FFT_APROBACION_GG',                  9),
    ('LONG_LIST',                         10),
    ('LONG_LIST_DECISION',                11),
    ('FORMULARIO_ENVIO',                  12),
    ('FORMULARIO_COMPLETADO',             13),
    ('FORMULARIO_CORRECCION',             14),
    ('FFT_EMO',                           15),
    ('ENTREVISTA',                        16),
    ('ENTREVISTA_RESPUESTA',              17),
    ('ENTREVISTA_CONFIRMADA_SOLICITANTE', 18),
    ('FINALISTA_ENVIO',                   19),
    ('FINALISTA_DECISION',                20),
    ('CANDIDATO_RETOMADO',                21),
    ('AGRADECIMIENTO',                    22)
) AS v(codigo, orden)
WHERE t.codigo = v.codigo AND t.state = true AND t.orden <> v.orden;

COMMIT;

-- ───────────────────────────────────────────────────────────────────────────
-- Verificación
-- ───────────────────────────────────────────────────────────────────────────
-- SELECT t.orden, t.codigo, t.nombre, t.active,
--        t.principal_automatico, t.principal_automatico_nombre,
--        coalesce(string_agg(coalesce(d.codigo, d.email) ||
--                            case when d.es_copia then ' (CC)' else '' end ||
--                            case when d.active then '' else ' [off]' end,
--                            ', ' ORDER BY d.orden), '—') AS destinatarios
--   FROM gth_correo_tipo t
--   LEFT JOIN gth_correo_destinatario d
--          ON d.gth_correo_tipo_id = t.gth_correo_tipo_id AND d.state
--  WHERE t.state
--  GROUP BY t.orden, t.codigo, t.nombre, t.active,
--           t.principal_automatico, t.principal_automatico_nombre
--  ORDER BY t.orden;
