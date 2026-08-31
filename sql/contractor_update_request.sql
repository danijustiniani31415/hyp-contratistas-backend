-- ============================================================================
-- Solicitud de actualización de datos de contratistas ya registrados
-- ----------------------------------------------------------------------------
-- Nuevo estado "APROBADO - ACTUALIZACIÓN PENDIENTE" + tablas de staging para
-- proponer datos nuevos sobre un contratista ya APROBADO sin tocar los datos
-- vigentes hasta que el área de costos apruebe.
--
-- Seguro de re-ejecutar (idempotente).
-- ============================================================================

BEGIN;

-- 1. Nuevo estado de contratista (id 4) -------------------------------------
INSERT INTO contractor_state (contractor_state_id, contractor_state_description, created_date_time, created_user_id, active, state)
OVERRIDING SYSTEM VALUE
VALUES (4, 'APROBADO - ACTUALIZACIÓN PENDIENTE', now(), (SELECT MIN(user_id) FROM app_user), true, true)
ON CONFLICT (contractor_state_id) DO NOTHING;

-- 2. El índice único parcial que garantiza un único contratista vigente en
--    estados "operativos" debe incluir el nuevo estado 4 (sigue siendo el
--    único registro activo mientras la actualización está pendiente).
DROP INDEX IF EXISTS uq_contractor_one_active_pending_approved;
CREATE UNIQUE INDEX uq_contractor_one_active_pending_approved
  ON contractor (contributor_id)
  WHERE contractor_state_id = ANY (ARRAY[1, 2, 4]) AND state = true;

-- 3. Catálogo de estados de la solicitud de actualización -------------------
CREATE TABLE IF NOT EXISTS contractor_update_state (
  contractor_update_state_id          integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  contractor_update_state_description varchar(100) NOT NULL,
  created_date_time                   timestamptz  NOT NULL DEFAULT now(),
  created_user_id                     integer,
  updated_date_time                   timestamptz,
  updated_user_id                     integer,
  active                              boolean      NOT NULL DEFAULT true,
  state                               boolean      NOT NULL DEFAULT true,
  CONSTRAINT uq_contractor_update_state_description UNIQUE (contractor_update_state_description),
  CONSTRAINT fk_contractor_update_state_created_user FOREIGN KEY (created_user_id) REFERENCES app_user(user_id),
  CONSTRAINT fk_contractor_update_state_updated_user FOREIGN KEY (updated_user_id) REFERENCES app_user(user_id)
);

INSERT INTO contractor_update_state (contractor_update_state_id, contractor_update_state_description, created_user_id)
OVERRIDING SYSTEM VALUE
VALUES
  (1, 'PENDIENTE',  (SELECT MIN(user_id) FROM app_user)),
  (2, 'APLICADA',   (SELECT MIN(user_id) FROM app_user)),
  (3, 'RECHAZADA',  (SELECT MIN(user_id) FROM app_user))
ON CONFLICT (contractor_update_state_id) DO NOTHING;

-- identidad por delante de las filas sembradas
SELECT setval(pg_get_serial_sequence('contractor_update_state', 'contractor_update_state_id'),
              (SELECT MAX(contractor_update_state_id) FROM contractor_update_state));

-- 4. Solicitud de actualización (datos propuestos en staging) ---------------
CREATE TABLE IF NOT EXISTS contractor_update_request (
  contractor_update_request_id              integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  contractor_id                             integer NOT NULL,
  contractor_update_state_id                integer NOT NULL,
  contributor_ruc                           varchar(11)   NOT NULL,
  contributor_name                          varchar(1000) NOT NULL,
  contributor_address                       varchar(1000),
  contributor_economic_activity_description varchar(1000),
  contributor_district                      varchar(150),
  contributor_province                      varchar(150),
  contributor_department                    varchar(150),
  legal_representative_dni                  varchar(20),
  legal_representative_full_name            varchar(1000),
  legal_entity_registry_number              varchar(30),
  logo_file_url                             varchar(500),
  brochure_file_url                         text,
  ficha_ruc_file_url                        text,
  references_list_file_url                  text,
  created_date_time                         timestamptz NOT NULL DEFAULT now(),
  created_user_id                           integer,
  updated_date_time                         timestamptz,
  updated_user_id                           integer,
  active                                    boolean     NOT NULL DEFAULT true,
  state                                     boolean     NOT NULL DEFAULT true,
  CONSTRAINT fk_contractor_update_request_contractor   FOREIGN KEY (contractor_id)              REFERENCES contractor(contractor_id)                          ON DELETE RESTRICT,
  CONSTRAINT fk_contractor_update_request_state        FOREIGN KEY (contractor_update_state_id) REFERENCES contractor_update_state(contractor_update_state_id) ON DELETE RESTRICT,
  CONSTRAINT fk_contractor_update_request_created_user FOREIGN KEY (created_user_id)             REFERENCES app_user(user_id),
  CONSTRAINT fk_contractor_update_request_updated_user FOREIGN KEY (updated_user_id)             REFERENCES app_user(user_id)
);

-- Una sola solicitud PENDIENTE (estado 1) vigente por contratista.
CREATE UNIQUE INDEX IF NOT EXISTS ux_contractor_update_request_pendiente
  ON contractor_update_request (contractor_id)
  WHERE state = true AND contractor_update_state_id = 1;

CREATE INDEX IF NOT EXISTS ix_contractor_update_request_contractor_id
  ON contractor_update_request (contractor_id);

-- 5. Correos propuestos de la solicitud de actualización --------------------
CREATE TABLE IF NOT EXISTS contractor_update_request_email (
  contractor_update_request_email_id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  contractor_update_request_id       integer      NOT NULL,
  contractor_email                   varchar(100) NOT NULL,
  contractor_person_type_id          integer,
  created_date_time                  timestamptz  NOT NULL DEFAULT now(),
  created_user_id                    integer,
  updated_date_time                  timestamptz,
  updated_user_id                    integer,
  active                             boolean      NOT NULL DEFAULT true,
  state                              boolean      NOT NULL DEFAULT true,
  CONSTRAINT fk_cur_email_request     FOREIGN KEY (contractor_update_request_id) REFERENCES contractor_update_request(contractor_update_request_id) ON DELETE CASCADE,
  CONSTRAINT fk_cur_email_person_type FOREIGN KEY (contractor_person_type_id)    REFERENCES contractor_person_type(contractor_person_type_id)       ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS ix_contractor_update_request_email_request_id
  ON contractor_update_request_email (contractor_update_request_id);

COMMIT;
