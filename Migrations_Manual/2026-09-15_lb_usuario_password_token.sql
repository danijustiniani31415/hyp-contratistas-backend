-- Restablecer contraseña para lb_usuario_sistema (Las Bravas / HP Constructores).
-- Mismo patrón que el legacy de Abril (ss_reset_token / user_password_token): tabla propia de
-- tokens de un solo uso, en vez de columnas en el usuario — así no hay que limpiar nada a mano
-- para pedir un token nuevo, solo se invalidan (usado = true) los anteriores.

CREATE TABLE lb_usuario_password_token (
    id                  BIGSERIAL PRIMARY KEY,
    usuario_sistema_id  BIGINT NOT NULL REFERENCES lb_usuario_sistema(id),
    token               TEXT NOT NULL,
    expira_en           TIMESTAMPTZ NOT NULL,
    usado               BOOLEAN NOT NULL DEFAULT false,
    creado_en           TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX idx_lb_usuario_password_token_token ON lb_usuario_password_token(token);
CREATE INDEX idx_lb_usuario_password_token_usuario ON lb_usuario_password_token(usuario_sistema_id) WHERE usado = false;
