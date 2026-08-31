-- ═══════════════════════════════════════════════════════════════════════════
-- Gestión GTH · Reclutamiento: el candidato FFT se registra en `person` al pedirlo
-- ═══════════════════════════════════════════════════════════════════════════
--
-- Hasta ahora la casilla FFT de «Nueva solicitud de personal» pedía nombre y
-- correo, y la persona recién entraba a la base maestra mucho después: cuando el
-- postulante llenaba su formulario y GTH lo aprobaba
-- (PostulanteFormularioRepository.SincronizarPersonAsync). Entre el pedido y ese
-- momento el candidato no existía en ningún lado consultable.
--
-- Ahora la casilla pide también el DNI, y con eso el candidato entra a `person`
-- en el mismo momento en que se registra la solicitud.
--
-- Qué agrega:
--   1) gth_requerimiento.fft_candidato_documento — el DNI que declaró el
--      solicitante. Va junto al nombre y al correo por el mismo motivo que ellos:
--      es parte de lo que se pidió y de lo que Gerencia General aprueba.
--   2) gth_requerimiento.fft_person_id — a qué fila de `person` quedó enganchado
--      ese candidato. No es un dato del pedido sino el resultado del registro, y
--      sirve para dos cosas: rastrear la fila que se creó, y que el aviso de
--      «esta persona ya existe» que ve GTH al aprobar el formulario no señale a
--      la fila que este mismo pedido acababa de crear (CoincidenciaPersonaQuery).
--
-- Lo que NO se toca, a propósito:
--   El CHECK ck_gth_requerimiento_fft_candidato NO se amplía para exigir el
--   documento. Los FFT ya registrados no lo tienen, y un CHECK —aunque se agregue
--   NOT VALID— se revalida en cada UPDATE de esas filas: el primer cambio de
--   estado de un requerimiento FFT viejo moriría. La obligatoriedad del DNI vive
--   en ReclutamientoService.Create, que es quien crea las filas nuevas.
--
-- Idempotente: se puede correr más de una vez sin duplicar nada.
-- ═══════════════════════════════════════════════════════════════════════════

BEGIN;

SET client_encoding TO 'UTF8';

-- ───────────────────────────────────────────────────────────────────────────
-- 1) El DNI del candidato FFT y la fila de `person` a la que quedó enganchado
-- ───────────────────────────────────────────────────────────────────────────
ALTER TABLE gth_requerimiento
    ADD COLUMN IF NOT EXISTS fft_candidato_documento text,
    ADD COLUMN IF NOT EXISTS fft_person_id integer;

COMMENT ON COLUMN gth_requerimiento.fft_candidato_documento IS
    'DNI del candidato FFT que declaro el solicitante (8 digitos). Null en las vacantes normales y en los FFT anteriores a que se pidiera el dato.';
COMMENT ON COLUMN gth_requerimiento.fft_person_id IS
    'Fila de person en la que quedo registrado el candidato FFT al crearse la solicitud. Null en las vacantes normales y en los FFT anteriores.';

DO $fk$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'fk_gth_requerimiento_fft_person'
    ) THEN
        ALTER TABLE gth_requerimiento
            ADD CONSTRAINT fk_gth_requerimiento_fft_person
            FOREIGN KEY (fft_person_id) REFERENCES person (person_id);
    END IF;
END
$fk$;

COMMIT;

-- Verificación
-- SELECT codigo, es_fft, fft_candidato_nombre, fft_candidato_documento, fft_person_id
-- FROM gth_requerimiento WHERE es_fft ORDER BY gth_requerimiento_id DESC LIMIT 20;
