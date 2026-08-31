-- ============================================================================
-- Planeamiento BIM · Carga Diaria — 3 causas nuevas de incumplimiento
--
-- Punto #4 de las observaciones de Planeamiento (prioridad alta): ampliar el
-- catálogo "Tipo de Causa" del modal de Carga Diaria. bim_causa_no_cumplimiento
-- es tabla catálogo (id, nombre, orden) — no un enum en C#, ver
-- Features/PlaneamientoBimFeature/Infrastructure/Models/BimCausaNoCumplimiento.cs.
-- Este cambio es puramente de datos: no toca ningún archivo de código, no
-- requiere migración EF ni build/redeploy del backend.
--
-- 5 causas existentes hoy (orden 1-5, verificado contra prod): Falta de
-- material, Clima, Mano de obra, Diseño / Información pendiente, Otro.
-- Se agregan 3 nuevas, orden 6-8: Falla de contratista, Retrabajos,
-- Reprocesos por calidad.
--
-- Idempotente vía ON CONFLICT (nombre) DO NOTHING — bim_causa_no_cumplimiento
-- tiene UNIQUE(nombre) real (verificado contra prod antes de escribir esto).
-- Aplicar en local y producción.
-- ============================================================================

INSERT INTO bim_causa_no_cumplimiento (nombre, orden) VALUES
    ('Falla de contratista', 6),
    ('Retrabajos', 7),
    ('Reprocesos por calidad', 8)
ON CONFLICT (nombre) DO NOTHING;

-- ── Verificación ────────────────────────────────────────────────────────────
-- SELECT id, nombre, orden FROM bim_causa_no_cumplimiento ORDER BY orden;
-- Debe devolver 8 filas.
