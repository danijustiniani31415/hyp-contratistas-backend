-- ============================================================================
-- Gestión GTH · Reclutamiento — Decisión de long list del solicitante
-- Fecha: 2026-07-30
--
-- Habilita que el solicitante apruebe/rechace por candidato la long list que GTH
-- le envió y envíe su decisión (correo a GTH). Tres cambios de datos (sin cambios
-- de esquema):
--
-- 1) Estado del pipeline LONG_LIST_APROBADA ("Long list aprobada"): fase que sigue
--    a LONG_LIST_ENVIADA. Se alcanza cuando el solicitante aprueba al menos un
--    candidato. Se inserta en orden 7 y se recorren las fases posteriores
--    (Selección jefatura → Oferta y cierre).
--    Nota: si el solicitante rechaza a TODOS, el requerimiento NO llega a esta fase;
--    vuelve a LONG_LIST para que GTH envíe una nueva long list (lógica en el backend).
--
-- 2) Tipo de correo LONG_LIST_DECISION ("Decisión de long list (a GTH)"): correo
--    independiente que se envía a GTH cuando el solicitante registra su decisión.
--    Tiene su propio juego de destinatarios en gth_correo_destinatario (no comparte
--    con SOLICITUD ni LONG_LIST).
--
-- 3) Seed del destinatario principal del nuevo correo SOLO en dev (BD != 'abril'):
--    calvarez@abril.pe. En producción NO se siembra: se configura desde la UI
--    (Solicitud de Personal → Configuración).
--
-- Idempotente. Aplicar en dev y prod.
-- ============================================================================

-- ── 1) Estado LONG_LIST_APROBADA + reordenamiento del pipeline ──────────────
INSERT INTO gth_estado_requerimiento (codigo, nombre, orden, descripcion)
SELECT 'LONG_LIST_APROBADA', 'Long list aprobada', 7,
       'El solicitante aprobó la long list; GTH continúa el proceso (plantilla y evaluaciones) con los candidatos aprobados.'
WHERE NOT EXISTS (
    SELECT 1 FROM gth_estado_requerimiento WHERE codigo = 'LONG_LIST_APROBADA' AND state
);

-- Orden explícito de TODO el pipeline (idempotente; no hay unique sobre orden, así
-- que los duplicados temporales durante el UPDATE no son problema).
UPDATE gth_estado_requerimiento AS e SET orden = v.orden
FROM (VALUES
    ('NUEVO', 1),
    ('APROBACION_GG', 2),
    ('VALIDACION_GTH', 3),
    ('PUBLICACION', 4),
    ('LONG_LIST', 5),
    ('LONG_LIST_ENVIADA', 6),
    ('LONG_LIST_APROBADA', 7),
    ('SELECCION_JEFATURA', 8),
    ('EVALUACION', 9),
    ('ENTREVISTAS', 10),
    ('OFERTA_CIERRE', 11)
) AS v(codigo, orden)
WHERE e.codigo = v.codigo AND e.state AND e.orden <> v.orden;

-- ── 2) Tipo de correo LONG_LIST_DECISION ────────────────────────────────────
INSERT INTO gth_correo_tipo (codigo, nombre, orden)
SELECT 'LONG_LIST_DECISION', 'Decisión de long list (a GTH)', 3
WHERE NOT EXISTS (
    SELECT 1 FROM gth_correo_tipo WHERE codigo = 'LONG_LIST_DECISION' AND state
);

-- ── 3) Seed del destinatario principal — SOLO dev (BD != 'abril') ───────────
-- En prod NO se siembra (el usuario lo configura desde la UI). Solo se inserta si
-- aún no hay destinatarios vigentes para ese tipo (no pisa lo editado en la UI).
INSERT INTO gth_correo_destinatario (email, es_copia, gth_correo_tipo_id)
SELECT 'calvarez@abril.pe', false, t.gth_correo_tipo_id
FROM gth_correo_tipo t
WHERE t.codigo = 'LONG_LIST_DECISION' AND t.state
  AND current_database() <> 'abril'
  AND NOT EXISTS (
      SELECT 1 FROM gth_correo_destinatario d
      WHERE d.gth_correo_tipo_id = t.gth_correo_tipo_id AND d.state = true
  );
