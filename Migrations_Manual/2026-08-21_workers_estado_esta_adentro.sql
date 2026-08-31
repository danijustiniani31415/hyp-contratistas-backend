-- ═══════════════════════════════════════════════════════════════════════════
-- workers_estado.esta_adentro
-- ═══════════════════════════════════════════════════════════════════════════
--
-- Contexto: al aprobar el formulario de un postulante, GTH escribe/actualiza su
-- ficha en `person`. El postulante puede haber declarado el documento de alguien
-- que YA existe en la base, incluido un trabajador de Abril, y GTH tiene que
-- saberlo antes de aprobar. Lo que decide si se le permite aprobar o no es si esa
-- persona esta ADENTRO de la empresa hoy, y esa pregunta no la responde
-- `llego_a_ingresar`.
--
-- Diferencia con llego_a_ingresar (que ya existe y NO se toca):
--
--   codigo               llego_a_ingresar   esta_adentro
--   ─────────────────    ────────────────   ────────────
--   ACTIVO               true               true
--   RETIRADO             true               false   ← la unica fila donde difieren
--   INHABILITADO_SSOMA   true               true
--   FINALISTA_APROBADO   false              false
--   NO_INGRESO           false              false
--
--   llego_a_ingresar = hecho consumado ("alguna vez ingreso?"). Una vez true no
--                      vuelve a false. Separa las fichas de pre-ingreso del resto.
--   esta_adentro     = condicion actual ("trabaja aca hoy?"). Si cambia en ambos
--                      sentidos (un retirado que reingresa vuelve a ACTIVO).
--
-- Sustituye al array WorkersEstadoIds.NoRetirados, que respondia esta misma
-- pregunta pero nombrada por lo que excluye y sin respaldo en la base: un estado
-- nuevo obligaba a acordarse de editar el array en C#.
--
-- Idempotente: se puede correr mas de una vez sin cambiar nada.
-- ═══════════════════════════════════════════════════════════════════════════

BEGIN;

-- Sin DEFAULT a proposito, igual que llego_a_ingresar: agregar un estado nuevo
-- tiene que obligar a decidir el valor en vez de heredar en silencio uno de los
-- dos, porque cualquiera de los dos por omision es un bug distinto (true de mas
-- bloquea aprobaciones legitimas; false de mas deja pasar a un trabajador
-- actual, que es justo lo que esta columna existe para frenar).
ALTER TABLE workers_estado ADD COLUMN IF NOT EXISTS esta_adentro boolean;

UPDATE workers_estado
   SET esta_adentro = (codigo IN ('ACTIVO', 'INHABILITADO_SSOMA')),
       updated_date_time = now()
 WHERE state
   AND esta_adentro IS DISTINCT FROM (codigo IN ('ACTIVO', 'INHABILITADO_SSOMA'));

-- Freno: si quedara algun estado sin valor (una fila con state = false, o un
-- codigo que no estaba previsto), el script aborta en vez de dejar la columna a
-- medio poblar y el NOT NULL fallando con un mensaje que no dice cual falta.
DO $$
DECLARE v_faltan text;
BEGIN
    SELECT string_agg(codigo, ', ' ORDER BY workers_estado_id)
      INTO v_faltan
      FROM workers_estado
     WHERE esta_adentro IS NULL;

    IF v_faltan IS NOT NULL THEN
        RAISE EXCEPTION
            'Quedaron estados de workers_estado sin esta_adentro: %. Decidir su valor antes de continuar.',
            v_faltan;
    END IF;
END $$;

ALTER TABLE workers_estado ALTER COLUMN esta_adentro SET NOT NULL;

COMMIT;

-- ═══════════════════════════════════════════════════════════════════════════
-- Verificacion (correr despues; solo lectura)
-- ═══════════════════════════════════════════════════════════════════════════
-- SELECT workers_estado_id, codigo, llego_a_ingresar, esta_adentro
--   FROM workers_estado ORDER BY workers_estado_id;
--
-- Esperado:
--   1 ACTIVO             t t
--   2 RETIRADO           t f
--   3 INHABILITADO_SSOMA t t
--   4 FINALISTA_APROBADO f f
--   5 NO_INGRESO         f f
-- ═══════════════════════════════════════════════════════════════════════════
