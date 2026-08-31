-- Snapshot semanal del IES (Ranking Eficiencia) por supervisor.
-- Se llena desde el mismo cron que pega a POST /api/v1/ArquitecturaComercial/avance-semanal/snapshot,
-- que ahora también invoca el snapshot de ranking (ver ArquitecturaComercialController.SnapshotAvanceSemanal).

CREATE TABLE IF NOT EXISTS ac_ranking_semanal (
    id               SERIAL PRIMARY KEY,
    user_id          INTEGER NOT NULL,
    semana           DATE NOT NULL,
    ies              NUMERIC(6,2) NOT NULL,
    comp_spi         NUMERIC(6,2) NOT NULL,
    comp_cierre      NUMERIC(6,2) NOT NULL,
    comp_inicio      NUMERIC(6,2) NOT NULL,
    total            INTEGER NOT NULL,
    completadas      INTEGER NOT NULL,
    sin_compromisos  BOOLEAN NOT NULL DEFAULT FALSE,
    created_at       TIMESTAMP NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_ac_ranking_semanal_user_semana
    ON ac_ranking_semanal (user_id, semana);
