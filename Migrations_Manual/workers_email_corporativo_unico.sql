-- Regla de negocio: un buzón corporativo pertenece a UN solo trabajador vigente.
--
-- Ojo con el alcance del índice: workers.email_corporativo cumple dos roles según el
-- trabajador (el formulario de Habilitación cambia la etiqueta del campo):
--   * Casa + Staff/Oficina Central  -> "Email corporativo" (buzón del tenant de Abril).
--   * Casa + Obra y contratistas    -> "Email personal", que SÍ se repite legítimamente:
--     varias fichas de una misma contratista comparten el correo de su RR.HH.
--     (p. ej. rrhh2.gcg.peru@gmail.com en 14 trabajadores, notiene@gmail.com en 11).
-- Por eso el índice es PARCIAL: solo cubre los correos corporativos (dominio de Abril o
-- trabajador de Staff/Oficina Central). Un UNIQUE sobre toda la columna fallaría contra
-- los datos actuales y no se puede limpiar borrando fichas (no se elimina nada por auditoría).
--
-- El soft delete de esta tabla es estado = 'RETIRADO' (no hay columna state), así que el
-- índice solo aplica a los no retirados: al retirar a alguien su buzón queda libre para
-- reasignarse, y siguen pudiendo existir N fichas retiradas con el mismo correo.
--
-- Verificado antes de crearlo (dev y prod, 2026-08-04): 0 grupos duplicados bajo este
-- predicado. Para re-verificar:
--   SELECT lower(btrim(email_corporativo)), count(*)
--   FROM workers
--   WHERE email_corporativo IS NOT NULL AND btrim(email_corporativo) <> ''
--     AND coalesce(estado, 'ACTIVO') <> 'RETIRADO'
--     AND (lower(btrim(email_corporativo)) LIKE '%@abril.pe'
--          OR (contrata_casa = 'Casa' AND obra_oficina IN ('Staff', 'Oficina Central')))
--   GROUP BY 1 HAVING count(*) > 1;

CREATE UNIQUE INDEX IF NOT EXISTS ux_workers_email_corporativo_vigente
    ON workers (lower(btrim(email_corporativo)))
 WHERE email_corporativo IS NOT NULL
   AND btrim(email_corporativo) <> ''
   AND coalesce(estado, 'ACTIVO') <> 'RETIRADO'
   AND (lower(btrim(email_corporativo)) LIKE '%@abril.pe'
        OR (contrata_casa = 'Casa' AND obra_oficina IN ('Staff', 'Oficina Central')));
