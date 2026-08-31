-- ============================================================================
-- Configuración de EMOs → Correos de programación
--
-- Destinatarios (Para / CC, sin copias ocultas) de los correos que se envían al
-- programar un EMO: tanto la programación manual desde /emos como el resumen
-- del cron diario de programación automática.
--
-- Comportamiento tras aplicar este script: idéntico al actual. La fila fija
-- CLINICA queda activa, así que el correo sigue yendo a los emails de contacto
-- de la clínica (ss_clinica_emails); las listas de correos fijos y de copias
-- arrancan vacías.
-- ============================================================================

-- ── Catálogo de tipo de destinatario ────────────────────────────────────────
CREATE TABLE IF NOT EXISTS ss_emo_correo_tipo (
    id          SERIAL PRIMARY KEY,
    codigo      VARCHAR(20)  NOT NULL,
    nombre      VARCHAR(60)  NOT NULL,
    orden       INTEGER      NOT NULL DEFAULT 0,
    active      BOOLEAN      NOT NULL DEFAULT TRUE,
    state       BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at  TIMESTAMPTZ  NOT NULL DEFAULT now()
);

-- Un solo registro vivo por código (múltiples con state = false permitidos).
CREATE UNIQUE INDEX IF NOT EXISTS ux_ss_emo_correo_tipo_codigo
    ON ss_emo_correo_tipo (upper(codigo)) WHERE state;

-- ── Destinatarios ───────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS ss_emo_correo_destinatario (
    id          SERIAL PRIMARY KEY,
    tipo_id     INTEGER      NOT NULL REFERENCES ss_emo_correo_tipo (id),
    -- Solo para destinatarios dinámicos fijos (hoy únicamente CLINICA).
    codigo      VARCHAR(30)  NULL,
    -- NULL solo en destinatarios dinámicos: su correo se resuelve al enviar.
    email       VARCHAR(150) NULL,
    nombre      VARCHAR(120) NULL,
    descripcion VARCHAR(200) NULL,
    -- FALSE = fila fija: solo se puede activar/desactivar, no editar ni eliminar.
    editable    BOOLEAN      NOT NULL DEFAULT TRUE,
    orden       INTEGER      NOT NULL DEFAULT 0,
    active      BOOLEAN      NOT NULL DEFAULT TRUE,
    state       BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at  TIMESTAMPTZ  NOT NULL DEFAULT now(),
    CONSTRAINT ck_ss_emo_correo_destinatario_email
        CHECK (editable = FALSE OR email IS NOT NULL)
);

-- Un solo correo vivo por lista (Para / CC).
CREATE UNIQUE INDEX IF NOT EXISTS ux_ss_emo_correo_dest_tipo_email
    ON ss_emo_correo_destinatario (tipo_id, lower(email))
    WHERE state AND email IS NOT NULL;

-- Un solo destinatario dinámico vivo por código.
CREATE UNIQUE INDEX IF NOT EXISTS ux_ss_emo_correo_dest_codigo
    ON ss_emo_correo_destinatario (upper(codigo))
    WHERE state AND codigo IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_ss_emo_correo_dest_tipo
    ON ss_emo_correo_destinatario (tipo_id) WHERE state;

-- ── Semilla ─────────────────────────────────────────────────────────────────
INSERT INTO ss_emo_correo_tipo (codigo, nombre, orden)
SELECT v.codigo, v.nombre, v.orden
FROM (VALUES
    ('PRINCIPAL', 'Destinatarios principales', 1),
    ('COPIA',     'Copias (CC)',               2)
) AS v(codigo, nombre, orden)
WHERE NOT EXISTS (
    SELECT 1 FROM ss_emo_correo_tipo t
    WHERE upper(t.codigo) = upper(v.codigo) AND t.state
);

-- Destinatario fijo: los emails de contacto de la clínica de la programación.
INSERT INTO ss_emo_correo_destinatario
    (tipo_id, codigo, email, nombre, descripcion, editable, orden, active)
SELECT t.id, 'CLINICA', NULL, 'Clínica asignada',
       'Correos de contacto de la clínica de la programación (Catálogos → Clínicas).',
       FALSE, 0, TRUE
FROM ss_emo_correo_tipo t
WHERE upper(t.codigo) = 'PRINCIPAL' AND t.state
  AND NOT EXISTS (
      SELECT 1 FROM ss_emo_correo_destinatario d
      WHERE upper(d.codigo) = 'CLINICA' AND d.state
  );

-- ── Feature + permisos ──────────────────────────────────────────────────────
-- module_id 8 = SSOMA (mismo módulo que el resto de features de Salud Ocupacional).
INSERT INTO feature (feature_key, module_id)
SELECT 'ssoma.salud-ocupacional.emos.configuracion', 8
WHERE NOT EXISTS (
    SELECT 1 FROM feature WHERE feature_key = 'ssoma.salud-ocupacional.emos.configuracion'
);

-- Igual que la configuración de Mi Salud: solo ADMINISTRADOR DEL SISTEMA (role_id 1).
INSERT INTO role_feature (role_id, feature_id)
SELECT 1, f.feature_id
FROM feature f
WHERE f.feature_key = 'ssoma.salud-ocupacional.emos.configuracion'
  AND NOT EXISTS (
      SELECT 1 FROM role_feature rf
      WHERE rf.role_id = 1 AND rf.feature_id = f.feature_id
  );
