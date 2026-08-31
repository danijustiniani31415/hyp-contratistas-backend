-- ============================================================================
-- Gestión GTH · Onboarding — Firma de la carta oferta por el propio postulante
--
-- Cambia el primer paso del onboarding. Antes: GTH le enviaba la carta oferta
-- ADJUNTA por correo, el postulante la firmaba a mano, la devolvía por correo y
-- GTH subía ese archivo al intranet. Ahora: GTH sube la carta y el correo que
-- sale lleva SOLO un enlace con token a una página pública donde el postulante
-- ve la carta, registra su firma y la firma ahí mismo. El resultado cae en las
-- mismas columnas carta_firmada_* que antes llenaba GTH a mano, así que el
-- checklist («Revisar y aprobar carta oferta firmada») y el avance de fase no
-- cambian: GTH sigue revisando y aprobando lo que quedó firmado.
--
-- 1) carta_oferta_token: el token del enlace público, uno por onboarding. Mismo
--    criterio que gth_postulante_formulario.token (hex de 24 bytes, url-safe) y
--    misma decisión: vive como columna de la fila a la que da acceso, no en una
--    tabla aparte, porque es 1 a 1 con ella. Se genera al abrir el onboarding y
--    NO se rota al reenviar el enlace: un mismo postulante puede recibir el
--    correo dos veces y los dos enlaces tienen que seguir funcionando.
--
-- 2) carta_firmada_postulante_date_time: cuándo firmó el postulante desde la
--    página pública. Es lo que distingue las dos procedencias del documento
--    firmado sin necesidad de un catálogo de "tipos": con fecha = lo firmó el
--    postulante en la intranet; sin fecha y con carta_firmada_url = lo subió
--    GTH a mano (la vía de respaldo, que se conserva). Las columnas de
--    carta_firmada_subida_user_id quedan en NULL cuando firma el postulante,
--    porque no es un usuario del sistema.
--
-- La firma en sí NO se guarda acá: va a person.signature_image_bytes, las mismas
-- columnas que usa la firma del Gerente General de Contabilidad. Por eso el
-- envío del enlace exige que el candidato ya tenga ficha en person (la crea la
-- aprobación de su formulario de postulante en Reclutamiento).
--
-- Idempotente: se puede correr múltiples veces sin duplicar ni romper nada.
-- ============================================================================

BEGIN;

ALTER TABLE gth_onboarding
    ADD COLUMN IF NOT EXISTS carta_oferta_token                 varchar(100),
    ADD COLUMN IF NOT EXISTS carta_firmada_postulante_date_time timestamptz;

COMMENT ON COLUMN gth_onboarding.carta_oferta_token IS
    'Token del enlace público con el que el postulante ve y firma su carta oferta (/postulante/carta-oferta?token=). Uno por onboarding; no se rota al reenviar el enlace.';
COMMENT ON COLUMN gth_onboarding.carta_firmada_postulante_date_time IS
    'Momento en que el postulante firmó la carta desde la página pública. NULL con carta_firmada_url llena = la subió GTH a mano (vía de respaldo).';

-- El token es la única credencial del enlace: no puede repetirse entre onboardings
-- vigentes. Parcial por state = true, igual que el resto de los únicos del proyecto,
-- para que dar de baja una fila no bloquee el token de una nueva.
CREATE UNIQUE INDEX IF NOT EXISTS ix_gth_onboarding_carta_oferta_token
    ON gth_onboarding (carta_oferta_token)
    WHERE state = true AND carta_oferta_token IS NOT NULL;

COMMIT;

-- Verificación: las dos columnas nuevas y el índice único del token.
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'gth_onboarding'
  AND column_name IN ('carta_oferta_token', 'carta_firmada_postulante_date_time')
ORDER BY column_name;

SELECT indexname FROM pg_indexes
WHERE tablename = 'gth_onboarding' AND indexname = 'ix_gth_onboarding_carta_oferta_token';
