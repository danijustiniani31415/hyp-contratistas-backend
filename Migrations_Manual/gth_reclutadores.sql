-- ============================================================================
-- Gestion GTH · Configuracion -> Reclutadores
--
-- Da de alta la funcionalidad "Reclutadores": la pantalla que administra quien
-- del area de Gestion del Talento Humano sale en el desplegable "Responsable
-- del proceso" del detalle de Reclutamiento.
--
-- NO hay cambios de esquema: la tabla filtro `gth_responsable_proceso` ya
-- existe (la creo `gth_detalle_asignacion_publicacion.sql`) y ya tiene el
-- `active` que la pantalla prende y apaga. Lo unico que faltaba era la fila de
-- `feature` + los `role_feature`, que es lo que hace este script.
--
-- Antes la lista de responsables era un seed fijo de 4 correos; desde ahora la
-- pantalla la arma sola con los trabajadores vigentes del area de GTH y esas 4
-- filas se conservan tal cual (siguen activas, nadie cambia de estado al
-- correr esto).
--
-- Idempotente: se puede correr multiples veces sin duplicar nada.
-- Sin tildes a proposito: psql las manda en cp1252 y revienta la conexion.
-- ============================================================================

BEGIN;

-- ── 1) feature ──────────────────────────────────────────────────────────────
-- El module_id se hereda de una feature del mismo modulo en vez de buscar el
-- nombre del modulo por texto (que lleva tilde).
INSERT INTO feature (feature_key, module_id)
SELECT 'gestion-gth.config.reclutadores', f.module_id
FROM feature f
WHERE f.feature_key = 'gestion-gth.reclutamiento'
  AND NOT EXISTS (
      SELECT 1 FROM feature x WHERE x.feature_key = 'gestion-gth.config.reclutadores');

-- ── 2) role_feature ─────────────────────────────────────────────────────────
-- Los mismos roles que ya entran a Reclutamiento: quien lleva los procesos es
-- quien decide que reclutadores existen.
INSERT INTO role_feature (role_id, feature_id)
SELECT rf.role_id, nueva.feature_id
FROM feature nueva
JOIN feature base ON base.feature_key = 'gestion-gth.reclutamiento'
JOIN role_feature rf ON rf.feature_id = base.feature_id
WHERE nueva.feature_key = 'gestion-gth.config.reclutadores'
ON CONFLICT (role_id, feature_id) DO NOTHING;

-- ── 3) Documentacion de la tabla filtro ─────────────────────────────────────
COMMENT ON TABLE gth_responsable_proceso IS
    'Tabla filtro de Reclutadores: lista blanca de trabajadores del area de GTH que pueden ser "Responsable del proceso" de un requerimiento. Aparte de workers a proposito: activar/desactivar aca no toca la ficha del trabajador ni ninguna otra pantalla.';

COMMENT ON COLUMN gth_responsable_proceso.active IS
    'true = sale en el desplegable "Responsable del proceso". Es el interruptor de la pantalla Gestion GTH -> Configuracion -> Reclutadores. No tener fila equivale a inactivo.';

COMMIT;

-- ── Verificacion ────────────────────────────────────────────────────────────
-- SELECT f.feature_id, f.feature_key, r.role_id, r.role_description
-- FROM feature f
-- LEFT JOIN role_feature rf ON rf.feature_id = f.feature_id
-- LEFT JOIN role r ON r.role_id = rf.role_id
-- WHERE f.feature_key = 'gestion-gth.config.reclutadores';
