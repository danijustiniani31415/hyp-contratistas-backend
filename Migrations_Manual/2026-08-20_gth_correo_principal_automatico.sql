-- ═══════════════════════════════════════════════════════════════════════════
-- Gestión GTH · Configuración de correos: el destinatario que asigna el
-- sistema también se prende y se apaga
-- ═══════════════════════════════════════════════════════════════════════════
--
-- Hasta ahora, los correos con "principal automático" (la long list al
-- solicitante, el formulario al postulante, la citación, el fin de proceso al
-- candidato…) le llegaban SIEMPRE a ese destinatario y la pantalla solo lo
-- explicaba con un aviso. Ahora es una fila más de la tabla de destinatarios,
-- con su propio interruptor.
--
-- Qué agrega:
--   1) gth_correo_tipo.principal_automatico_active — el interruptor. Va aparte
--      de `active` porque son dos decisiones distintas: apagar el correo
--      entero o dejar de mandárselo a quien lo pone el sistema. Arranca en
--      true = como venía funcionando.
--   2) gth_correo_tipo.principal_automatico_nombre — cómo se llama ese
--      destinatario en la pantalla ("Postulante", "Candidato"…). Es un dato
--      del tipo y no un texto en el frontend: cada correo se lo manda a
--      alguien distinto.
--   3) Descripciones cortas. Las de antes eran párrafos que repetían lo que
--      la propia pantalla ya muestra; se dejan en una línea.
--   4) Los destinatarios del catálogo pierden su `descripcion`: la fila ya
--      muestra el nombre y el correo al que resuelve hoy, así que el texto de
--      abajo solo agregaba ruido.
--
-- Idempotente: se puede correr más de una vez sin duplicar nada.
-- ═══════════════════════════════════════════════════════════════════════════

BEGIN;

SET client_encoding TO 'UTF8';

-- 1 y 2) Interruptor y etiqueta del destinatario que pone el sistema.
ALTER TABLE gth_correo_tipo
    ADD COLUMN IF NOT EXISTS principal_automatico_active boolean NOT NULL DEFAULT true,
    ADD COLUMN IF NOT EXISTS principal_automatico_nombre text;

UPDATE gth_correo_tipo t
SET    principal_automatico_nombre = v.nombre
FROM  (VALUES
    ('LONG_LIST',             'Solicitante del requerimiento'),
    ('FORMULARIO_ENVIO',      'Postulante'),
    ('FORMULARIO_CORRECCION', 'Postulante'),
    ('ENTREVISTA',            'Postulante citado'),
    ('ENTREVISTA_RESPUESTA',  'Área de Gestión del Talento Humano'),
    ('FINALISTA_ENVIO',       'Solicitante del requerimiento'),
    ('AGRADECIMIENTO',        'Candidato')
) AS v(codigo, nombre)
WHERE t.codigo = v.codigo
  AND t.state = true
  AND t.principal_automatico_nombre IS DISTINCT FROM v.nombre;

-- 3) Descripciones de una línea (la pantalla ya dice a quién le llega).
UPDATE gth_correo_tipo t
SET    descripcion = v.descripcion
FROM  (VALUES
    ('APROBACION_GG',         'Pide a Gerencia la aprobación de una solicitud recién registrada.'),
    ('SOLICITUD',             'Avisa a GTH las vacantes que Gerencia aprobó.'),
    ('TI_VACANTES',           'Avisa a TI las vacantes que Gerencia aprobó.'),
    ('LONG_LIST',             'Le manda al solicitante la long list de CVs.'),
    ('LONG_LIST_DECISION',    'Avisa a GTH la decisión del solicitante sobre la long list.'),
    ('FORMULARIO_ENVIO',      'Le manda al postulante el enlace de su formulario.'),
    ('FORMULARIO_COMPLETADO', 'Avisa a GTH que el postulante llenó su formulario.'),
    ('FORMULARIO_CORRECCION', 'Le manda al postulante las observaciones de su formulario.'),
    ('ENTREVISTA',            'Cita al postulante: fecha, hora y lugar.'),
    ('ENTREVISTA_RESPUESTA',  'Avisa a GTH si el candidato confirmó o rechazó su cita.'),
    ('FINALISTA_ENVIO',       'Le manda al solicitante el informe del finalista.'),
    ('FINALISTA_DECISION',    'Avisa a GTH la decisión del solicitante sobre el finalista.'),
    ('AGRADECIMIENTO',        'Le avisa al candidato que no continúa en el proceso.')
) AS v(codigo, descripcion)
WHERE t.codigo = v.codigo
  AND t.state = true
  AND t.descripcion IS DISTINCT FROM v.descripcion;

-- 4) Los destinatarios del catálogo ya se explican solos (nombre + correo al
--    que resuelven hoy): fuera el párrafo de abajo.
UPDATE gth_correo_destinatario
SET    descripcion = NULL
WHERE  state = true AND codigo IS NOT NULL AND descripcion IS NOT NULL;

COMMIT;

-- Verificación
-- SELECT codigo, nombre, descripcion, principal_automatico, principal_automatico_active,
--        principal_automatico_nombre, active, orden
-- FROM gth_correo_tipo WHERE state ORDER BY orden;
