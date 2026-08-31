-- Autorización de firma electrónica del médico ocupacional (SSO-FO-149) + auditoría
-- de cada firma de convalidación (timestamp, IP, user-agent, hash del documento).
-- Ejecutar manualmente en pgAdmin contra la base de datos objetivo.

ALTER TABLE ss_medicos_ocupacionales
  ADD COLUMN IF NOT EXISTS dni varchar(15),
  ADD COLUMN IF NOT EXISTS fecha_autorizacion_firma timestamptz,
  ADD COLUMN IF NOT EXISTS url_autorizacion_firmada varchar(500),
  ADD COLUMN IF NOT EXISTS pin_firma_hash varchar(200),
  ADD COLUMN IF NOT EXISTS pin_firma_intentos_fallidos integer NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS pin_firma_bloqueado_hasta timestamptz;

CREATE TABLE IF NOT EXISTS ss_convalidacion_firma_log (
  id serial PRIMARY KEY,
  convalidacion_id integer NOT NULL REFERENCES worker_emo_convalidaciones(id),
  medico_id integer REFERENCES ss_medicos_ocupacionales(id),
  fecha_hora timestamptz NOT NULL DEFAULT now(),
  ip varchar(64),
  user_agent varchar(400),
  documento_hash varchar(128) NOT NULL,
  resultado varchar(30) NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_ss_convalidacion_firma_log_convalidacion_id
  ON ss_convalidacion_firma_log(convalidacion_id);
