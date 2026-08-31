-- ─────────────────────────────────────────────────────────────────────────────
-- Convocatoria recurrente por tema: permite excluir a mano ciertos trabajadores
-- de una regla "Staff de un proyecto". La regla sigue siendo dinámica (si mañana
-- entra personal nuevo a la obra, se convoca solo), pero estos workers puntuales
-- no se convocan aunque sigan con vinculación vigente al proyecto.
-- ─────────────────────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS reunion_tema_regla_worker_excluido (
    reunion_tema_regla_worker_excluido_id  SERIAL PRIMARY KEY,
    reunion_tema_regla_id                    INT NOT NULL REFERENCES reunion_tema_regla(reunion_tema_regla_id),
    worker_id                                  INT NOT NULL REFERENCES workers(id),
    created_date_time                            TIMESTAMPTZ NOT NULL,
    created_user_id                                 INT NOT NULL,
    active                                             BOOLEAN NOT NULL DEFAULT TRUE,
    state                                              BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE INDEX IF NOT EXISTS ix_reunion_tema_regla_worker_excluido_regla ON reunion_tema_regla_worker_excluido(reunion_tema_regla_id);
