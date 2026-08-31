-- ===========================================================================
-- ss_equipo: correos de admin y SSOMA (alinear prod con dev y con el modelo)
-- ===========================================================================
--
-- El modelo `SsEquipo` declara `EmailAdmin` / `EmailSsoma` desde que existe el
-- modulo de Habilitacion, y el formulario de equipos del frontend los captura
-- (equipo-form: "Email admin" / "Email SSOMA"). Pero en PROD la tabla nunca
-- tuvo esas dos columnas: no salieron de un ALTER posterior, la tabla se creo
-- distinta en cada ambiente (en dev estan en medio del orden de columnas, no
-- al final, que es donde caeria un ADD COLUMN).
--
-- Consecuencia: cualquier consulta EF que materialice la entidad completa
-- (`ctx.SsEquipo.Include(...)`, que es lo que hace la lista paginada de
-- EquipoRepository.GetPagedAsync) pide `email_admin` y `email_ssoma` y Postgres
-- responde 42703. Por eso "Gestion de Ingresos > Equipos" devolvia 500 en prod
-- y la pantalla mostraba "Error del servidor. Por favor contactar al
-- administrador del sistema." mientras en dev funcionaba.
--
-- Tipo y nulabilidad copiados de dev tal cual: varchar(200) NULL, sin default.
--
-- Idempotente: se puede correr mas de una vez.
-- ===========================================================================

BEGIN;

ALTER TABLE ss_equipo ADD COLUMN IF NOT EXISTS email_admin character varying(200);
ALTER TABLE ss_equipo ADD COLUMN IF NOT EXISTS email_ssoma character varying(200);

COMMIT;

-- Verificacion:
-- SELECT column_name, data_type, character_maximum_length, is_nullable
-- FROM information_schema.columns
-- WHERE table_schema = 'public' AND table_name = 'ss_equipo'
--   AND column_name IN ('email_admin', 'email_ssoma');
