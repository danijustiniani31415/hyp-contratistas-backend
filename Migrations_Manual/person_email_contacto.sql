-- person.email pasa a ser el CORREO DE CONTACTO (personal) del trabajador.
--
-- 1) Se quita el UNIQUE global. El correo de autenticación vive únicamente en
--    app_user.email: quien llena el PersonDTO.Email que va al claim del JWT es
--    AuthRepository.ValidateUserAsync, y lo hace desde app_user — nunca lee esta
--    columna. Por eso person.email no necesita ser único: varios trabajadores de una
--    misma contratista comparten legítimamente el correo de RR.HH. de su empresa como
--    dato de contacto. Es un índice suelto, no una constraint, así que va con DROP INDEX.
--
-- 2) Se mueven a person.email los correos personales que hoy viven en
--    workers.email_corporativo por falta de un campo propio: los de Obra y de
--    contratistas, es decir todo lo que NO es un buzón @abril.pe y no pertenece a
--    Staff/Oficina Central. Los 2 casos de @abril.pe fuera de Staff/Oficina Central se
--    quedan donde están: son buzones corporativos de verdad.
--
-- Verificado antes de correrlo (prod, 2026-08-04): 360 filas a mover sobre 349 personas,
-- todas con person.email vacío → 0 sobrescrituras. Hay 1 persona con dos fichas y correos
-- distintos (una RETIRADA con el correo de RR.HH. de la contratista y una ACTIVA con su
-- correo propio); el desempate se queda con la ficha vigente y más reciente, y la otra
-- conserva su valor en workers.email_corporativo para no perderlo.
--
-- Son solo dos statements y ninguno depende de estado del otro (nada de tablas
-- temporales), así que corre igual en psql -f, en pgAdmin, y statement por statement.
-- El orden sí importa: el índice tiene que caer antes del UPDATE, porque el UPDATE crea
-- correos repetidos a propósito. Si se corre al revés, el UPDATE falla con violación de
-- unicidad y no deja nada a medias.

BEGIN;

DROP INDEX IF EXISTS person_email_key;

WITH a_mover AS (
    -- Correos personales que hoy están en la columna equivocada.
    SELECT w.id                                          AS worker_id,
           w.person_id,
           lower(btrim(w.email_corporativo))             AS email,
           (coalesce(w.estado, 'ACTIVO') <> 'RETIRADO')  AS vigente,
           w.created_at
    FROM workers w
    WHERE w.person_id IS NOT NULL
      AND w.email_corporativo IS NOT NULL
      AND btrim(w.email_corporativo) <> ''
      AND lower(btrim(w.email_corporativo)) NOT LIKE '%@abril.pe'
      AND NOT (w.contrata_casa = 'Casa' AND w.obra_oficina IN ('Staff', 'Oficina Central'))
),
elegido AS (
    -- Una persona puede tener varias fichas: gana la vigente y, a igualdad, la más reciente.
    SELECT DISTINCT ON (person_id) person_id, email
    FROM a_mover
    ORDER BY person_id, vigente DESC, created_at DESC NULLS LAST, worker_id DESC
),
actualizadas AS (
    -- Solo se completa a quien no tenía correo de contacto: nunca se pisa un dato existente.
    UPDATE person p
       SET email             = e.email,
           updated_date_time = now()
      FROM elegido e
     WHERE p.person_id = e.person_id
       AND coalesce(btrim(p.email), '') = ''
    RETURNING p.person_id, p.email
)
-- Se limpia workers solo donde el valor quedó efectivamente guardado en person, para no
-- perder el correo de una ficha cuyo valor no ganó el desempate. Se cruza contra el
-- RETURNING de arriba (no contra un SELECT a person, que en el mismo statement vería
-- todavía el valor viejo).
UPDATE workers w
   SET email_corporativo = NULL,
       updated_at        = now()
  FROM a_mover m
  JOIN actualizadas a ON a.person_id = m.person_id AND a.email = m.email
 WHERE w.id = m.worker_id;

COMMIT;

-- Comprobación posterior. Debe quedar solo 1 fila: la ficha retirada que no ganó el
-- desempate y conserva su correo.
--   SELECT count(*) FROM workers
--    WHERE email_corporativo IS NOT NULL AND btrim(email_corporativo) <> ''
--      AND lower(btrim(email_corporativo)) NOT LIKE '%@abril.pe'
--      AND NOT (contrata_casa = 'Casa' AND obra_oficina IN ('Staff', 'Oficina Central'));
