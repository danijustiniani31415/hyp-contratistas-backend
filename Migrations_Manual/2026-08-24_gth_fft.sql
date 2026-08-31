-- ═══════════════════════════════════════════════════════════════════════════
-- Gestión GTH · Reclutamiento: flujo FFT (ingreso directo)
-- ═══════════════════════════════════════════════════════════════════════════
--
-- FFT es el ingreso que pide la gerencia/jefatura con nombre propio: ya sabe a
-- quién quiere, así que el proceso no publica la vacante, no arma long list, no
-- entrevista y no manda finalistas. Del pedido se salta al formulario de
-- información del postulante y, cuando GTH lo aprueba, directo a la
-- programación de su EMO de ingreso (regla 8.8 / RG-06 / RF-REC-06 del
-- requerimiento funcional).
--
-- Qué agrega:
--   1) gth_requerimiento.es_fft + el candidato que nombró el solicitante
--      (nombre completo y correo personal). Van en el requerimiento y no en una
--      tabla aparte porque son parte de lo que el solicitante declaró y de lo
--      que Gerencia General aprueba: son el registro del pedido.
--   2) Tres tipos de correo nuevos, uno por cada momento del flujo FFT:
--      • FFT_SOLICITUD_GG  → lo dispara el propio Gerente General al registrar
--        el pedido (su aprobación se omite: se estaría aprobando a sí mismo).
--        Se configura desde Solicitud de Personal.
--      • FFT_APROBACION_GG → lo dispara la aprobación de Gerencia General
--        cuando el pedido FFT lo registró alguien más. Se configura desde
--        Aprobaciones, que es donde se toma esa decisión.
--      • FFT_EMO           → lo dispara GTH al aprobar el formulario del
--        candidato FFT: se saltan entrevistas y finalistas, así que este correo
--        es el que avisa que pasa a su EMO. Se configura desde Reclutamiento.
--   3) El `orden` de los tipos se renumera para que cada correo nuevo quede al
--      lado del momento del flujo al que pertenece.
--
-- Idempotente: se puede correr más de una vez sin duplicar nada.
-- ═══════════════════════════════════════════════════════════════════════════

BEGIN;

SET client_encoding TO 'UTF8';

-- ───────────────────────────────────────────────────────────────────────────
-- 1) El requerimiento sabe si es FFT y a quién nombró el solicitante
-- ───────────────────────────────────────────────────────────────────────────
ALTER TABLE gth_requerimiento
    ADD COLUMN IF NOT EXISTS es_fft boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS fft_candidato_nombre text,
    ADD COLUMN IF NOT EXISTS fft_candidato_correo text;

COMMENT ON COLUMN gth_requerimiento.es_fft IS
    'true = ingreso directo FFT: omite publicacion, revision de CV, long list, entrevistas y finalistas.';
COMMENT ON COLUMN gth_requerimiento.fft_candidato_nombre IS
    'Nombre completo del candidato que nombro el solicitante. Obligatorio cuando es_fft; null en el resto.';
COMMENT ON COLUMN gth_requerimiento.fft_candidato_correo IS
    'Correo personal del candidato FFT: es el buzon al que GTH le manda su formulario.';

-- Una vacante FFT sin candidato no tiene flujo posible (no hay a quién mandarle
-- el formulario), así que la regla vive en la base y no solo en el servicio.
DO $ck$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'ck_gth_requerimiento_fft_candidato'
    ) THEN
        ALTER TABLE gth_requerimiento
            ADD CONSTRAINT ck_gth_requerimiento_fft_candidato
            CHECK (es_fft = false
                   OR (fft_candidato_nombre IS NOT NULL AND fft_candidato_correo IS NOT NULL));
    END IF;
END
$ck$;

-- La bandeja de GTH y el seguimiento preguntan por el flujo del requerimiento;
-- son pocas filas FFT sobre el total, así que el índice va parcial.
CREATE INDEX IF NOT EXISTS ix_gth_requerimiento_es_fft
    ON gth_requerimiento (es_fft) WHERE es_fft = true;

-- ───────────────────────────────────────────────────────────────────────────
-- 2) Los tres correos del flujo FFT
-- ───────────────────────────────────────────────────────────────────────────
INSERT INTO gth_correo_tipo (codigo, nombre, descripcion, orden,
                             principal_automatico, principal_automatico_active,
                             principal_automatico_nombre, active, state)
SELECT v.codigo, v.nombre, v.descripcion, v.orden, false, true, NULL, true, true
FROM  (VALUES
    ('FFT_SOLICITUD_GG',  'Candidato FFT pedido por Gerencia General',
     'Avisa a GTH el candidato FFT que pidió Gerencia General (sin pasar por aprobación).', 2),
    ('FFT_APROBACION_GG', 'Candidato FFT aprobado por Gerencia General',
     'Avisa a GTH el candidato FFT que Gerencia General aprobó.', 5),
    ('FFT_EMO',           'Candidato FFT pasa a su EMO',
     'Avisa que el candidato FFT ya tiene su formulario aprobado y pasa a su EMO.', 11)
) AS v(codigo, nombre, descripcion, orden)
WHERE NOT EXISTS (
    SELECT 1 FROM gth_correo_tipo t WHERE t.codigo = v.codigo AND t.state = true
);

-- Nombre y descripción también en el rerun (por si el texto se afinó después).
UPDATE gth_correo_tipo t
SET    nombre      = v.nombre,
       descripcion = v.descripcion
FROM  (VALUES
    ('FFT_SOLICITUD_GG',  'Candidato FFT pedido por Gerencia General',
     'Avisa a GTH el candidato FFT que pidió Gerencia General (sin pasar por aprobación).'),
    ('FFT_APROBACION_GG', 'Candidato FFT aprobado por Gerencia General',
     'Avisa a GTH el candidato FFT que Gerencia General aprobó.'),
    ('FFT_EMO',           'Candidato FFT pasa a su EMO',
     'Avisa que el candidato FFT ya tiene su formulario aprobado y pasa a su EMO.')
) AS v(codigo, nombre, descripcion)
WHERE t.codigo = v.codigo
  AND t.state = true
  AND (t.nombre IS DISTINCT FROM v.nombre OR t.descripcion IS DISTINCT FROM v.descripcion);

-- ───────────────────────────────────────────────────────────────────────────
-- 3) Orden del flujo completo, con los nuevos en su lugar
-- ───────────────────────────────────────────────────────────────────────────
UPDATE gth_correo_tipo t
SET    orden = v.orden
FROM  (VALUES
    ('APROBACION_GG',         1),
    ('FFT_SOLICITUD_GG',      2),
    ('SOLICITUD',             3),
    ('TI_VACANTES',           4),
    ('FFT_APROBACION_GG',     5),
    ('LONG_LIST',             6),
    ('LONG_LIST_DECISION',    7),
    ('FORMULARIO_ENVIO',      8),
    ('FORMULARIO_COMPLETADO', 9),
    ('FORMULARIO_CORRECCION', 10),
    ('FFT_EMO',               11),
    ('ENTREVISTA',            12),
    ('ENTREVISTA_RESPUESTA',  13),
    ('FINALISTA_ENVIO',       14),
    ('FINALISTA_DECISION',    15),
    ('AGRADECIMIENTO',        16)
) AS v(codigo, orden)
WHERE t.codigo = v.codigo
  AND t.state = true
  AND t.orden IS DISTINCT FROM v.orden;

-- ───────────────────────────────────────────────────────────────────────────
-- 4) Destinatario dinámico de los tres: el área de GTH
-- ───────────────────────────────────────────────────────────────────────────
-- Se siembra APAGADO, igual que en el resto de los correos del módulo: el buzón
-- del área se prende desde la pantalla de Configuración cuando GTH lo decide, y
-- así ningún correo nuevo empieza escribiéndole a alguien que no lo esperaba.
INSERT INTO gth_correo_destinatario (gth_correo_tipo_id, codigo, nombre, es_copia,
                                     orden, active, state)
SELECT t.gth_correo_tipo_id, 'GTH_AREA', 'Área de Gestión del Talento Humano',
       false, 1, false, true
FROM   gth_correo_tipo t
WHERE  t.state = true
  AND  t.codigo IN ('FFT_SOLICITUD_GG', 'FFT_APROBACION_GG', 'FFT_EMO')
  AND  NOT EXISTS (
        SELECT 1 FROM gth_correo_destinatario d
        WHERE  d.gth_correo_tipo_id = t.gth_correo_tipo_id
          AND  d.state = true
          AND  upper(d.codigo) = 'GTH_AREA');

COMMIT;

-- Verificación
-- SELECT codigo, nombre, orden, active FROM gth_correo_tipo WHERE state ORDER BY orden;
-- SELECT t.codigo AS tipo, d.codigo, d.email, d.es_copia, d.active
-- FROM gth_correo_destinatario d
-- JOIN gth_correo_tipo t ON t.gth_correo_tipo_id = d.gth_correo_tipo_id
-- WHERE d.state AND t.codigo LIKE 'FFT%' ORDER BY t.orden, d.orden;
-- SELECT es_fft, count(*) FROM gth_requerimiento GROUP BY es_fft;
