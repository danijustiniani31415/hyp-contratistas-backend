-- ============================================================================
-- SSO-FO-150 — Autorización de tratamiento de datos biométricos (Tareo, Arquitectura Comercial)
-- ============================================================================
-- El trabajador firma en físico el SSO-FO-150 (generado con su nombre/DNI), el coordinador
-- escanea y sube la evidencia acá. El enrolamiento facial (ac_tareo_enrolamiento) queda
-- BLOQUEADO hasta que exista una fila para ese worker_id — ver
-- ArquitecturaComercialTareoService.EnrolarWorker.
-- Idempotente: se puede correr más de una vez sin duplicar.
-- ============================================================================

BEGIN;

CREATE TABLE IF NOT EXISTS ac_tareo_autorizacion (
    id                  SERIAL PRIMARY KEY,
    worker_id           INTEGER NOT NULL UNIQUE REFERENCES workers (id),
    url_documento       TEXT NOT NULL,
    subido_por_user_id  INTEGER REFERENCES app_user (user_id),
    subido_en           TIMESTAMPTZ NOT NULL DEFAULT now()
);

COMMIT;
