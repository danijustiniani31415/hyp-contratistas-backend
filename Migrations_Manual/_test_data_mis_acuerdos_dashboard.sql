-- ─────────────────────────────────────────────────────────────────────────────
-- SOLO PARA PRUEBAS MANUALES: crea 5 acuerdos de prueba, todos contigo (por tu
-- email corporativo) como responsable, para ver poblado el Dashboard "Mis
-- Acuerdos" con las 4 categorías (Vencidos, Críticos, Otros pendientes,
-- Cumplidos). Usa una reunión existente cualquiera — no crea reuniones nuevas.
--
-- Ejecutar en pgAdmin. Al final hay una consulta DELETE comentada para
-- limpiar todo lo insertado aquí cuando termines de probar.
-- ─────────────────────────────────────────────────────────────────────────────

DO $$
DECLARE
  v_worker_id   INT;
  v_user_id     INT;
  v_reunion_id  INT;
  v_estado_pend INT;
  v_estado_cump INT;
  v_acuerdo_id  INT;
BEGIN
  -- Ajusta el email si quieres probar con otro trabajador.
  SELECT w.id, p.user_id INTO v_worker_id, v_user_id
  FROM workers w
  JOIN person p ON p.person_id = w.person_id
  WHERE w.email_corporativo ILIKE 'sjustiniani@abril.pe'
  LIMIT 1;

  IF v_worker_id IS NULL THEN
    RAISE EXCEPTION 'No se encontró un worker con ese email_corporativo.';
  END IF;

  SELECT reunion_acuerdo_estado_id INTO v_estado_pend
  FROM reunion_acuerdo_estado WHERE descripcion = 'PENDIENTE' LIMIT 1;
  SELECT reunion_acuerdo_estado_id INTO v_estado_cump
  FROM reunion_acuerdo_estado WHERE descripcion = 'CUMPLIDO' LIMIT 1;

  -- Cualquier reunión activa existente sirve; los acuerdos de prueba no dependen
  -- de que seas participante de ella.
  SELECT reunion_id INTO v_reunion_id FROM reunion WHERE state = true ORDER BY reunion_id LIMIT 1;

  IF v_reunion_id IS NULL THEN
    RAISE EXCEPTION 'No hay ninguna reunión activa para colgar los acuerdos de prueba.';
  END IF;

  -- 1) Crítico y vencido (fecha programada hace 5 días)
  INSERT INTO reunion_acuerdo
    (reunion_id, descripcion, acciones, fecha_programada, reunion_acuerdo_estado_id, orden,
     criticidad, requiere_aceptacion, requiere_evidencia, created_date_time, created_user_id, active, state)
  VALUES
    (v_reunion_id, 'PRUEBA DASHBOARD: acuerdo crítico vencido', NULL, CURRENT_DATE - INTERVAL '5 days',
     v_estado_pend, 9001, 'CRITICO', false, false, now(), v_user_id, true, true)
  RETURNING reunion_acuerdo_id INTO v_acuerdo_id;

  INSERT INTO reunion_acuerdo_responsable
    (reunion_acuerdo_id, worker_id, estado_aceptacion, es_principal, created_date_time, created_user_id, active, state)
  VALUES (v_acuerdo_id, v_worker_id, 'ACEPTADO', true, now(), v_user_id, true, true);

  -- 2) Crítico, aún no vencido (programado en 3 días)
  INSERT INTO reunion_acuerdo
    (reunion_id, descripcion, acciones, fecha_programada, reunion_acuerdo_estado_id, orden,
     criticidad, requiere_aceptacion, requiere_evidencia, created_date_time, created_user_id, active, state)
  VALUES
    (v_reunion_id, 'PRUEBA DASHBOARD: acuerdo crítico a tiempo', NULL, CURRENT_DATE + INTERVAL '3 days',
     v_estado_pend, 9002, 'CRITICO', false, false, now(), v_user_id, true, true)
  RETURNING reunion_acuerdo_id INTO v_acuerdo_id;

  INSERT INTO reunion_acuerdo_responsable
    (reunion_acuerdo_id, worker_id, estado_aceptacion, es_principal, created_date_time, created_user_id, active, state)
  VALUES (v_acuerdo_id, v_worker_id, 'ACEPTADO', true, now(), v_user_id, true, true);

  -- 3) Medio, pendiente (sin fecha programada)
  INSERT INTO reunion_acuerdo
    (reunion_id, descripcion, acciones, fecha_programada, reunion_acuerdo_estado_id, orden,
     criticidad, requiere_aceptacion, requiere_evidencia, created_date_time, created_user_id, active, state)
  VALUES
    (v_reunion_id, 'PRUEBA DASHBOARD: acuerdo medio', NULL, NULL,
     v_estado_pend, 9003, 'MEDIO', false, false, now(), v_user_id, true, true)
  RETURNING reunion_acuerdo_id INTO v_acuerdo_id;

  INSERT INTO reunion_acuerdo_responsable
    (reunion_acuerdo_id, worker_id, estado_aceptacion, es_principal, created_date_time, created_user_id, active, state)
  VALUES (v_acuerdo_id, v_worker_id, 'ACEPTADO', true, now(), v_user_id, true, true);

  -- 4) Normal, pendiente, con OTRO responsable además de ti (para ver "esPrincipal"
  --    y "otrosResponsables" en las tarjetas). Toma cualquier otro worker con email.
  INSERT INTO reunion_acuerdo
    (reunion_id, descripcion, acciones, fecha_programada, reunion_acuerdo_estado_id, orden,
     criticidad, requiere_aceptacion, requiere_evidencia, created_date_time, created_user_id, active, state)
  VALUES
    (v_reunion_id, 'PRUEBA DASHBOARD: acuerdo normal compartido', NULL, CURRENT_DATE + INTERVAL '10 days',
     v_estado_pend, 9004, 'NORMAL', false, false, now(), v_user_id, true, true)
  RETURNING reunion_acuerdo_id INTO v_acuerdo_id;

  INSERT INTO reunion_acuerdo_responsable
    (reunion_acuerdo_id, worker_id, estado_aceptacion, es_principal, created_date_time, created_user_id, active, state)
  VALUES (v_acuerdo_id, v_worker_id, 'ACEPTADO', true, now(), v_user_id, true, true);

  INSERT INTO reunion_acuerdo_responsable
    (reunion_acuerdo_id, worker_id, estado_aceptacion, es_principal, created_date_time, created_user_id, active, state)
  SELECT v_acuerdo_id, w2.id, 'ACEPTADO', false, now(), v_user_id, true, true
  FROM workers w2
  JOIN person p2 ON p2.person_id = w2.person_id
  WHERE w2.email_corporativo IS NOT NULL AND w2.id <> v_worker_id
  ORDER BY w2.id
  LIMIT 1;

  -- 5) Cumplido hace pocos días (para la sección "Cumplidos recientemente")
  INSERT INTO reunion_acuerdo
    (reunion_id, descripcion, acciones, fecha_programada, fecha_cumplimiento, reunion_acuerdo_estado_id, orden,
     criticidad, requiere_aceptacion, requiere_evidencia, created_date_time, created_user_id, active, state)
  VALUES
    (v_reunion_id, 'PRUEBA DASHBOARD: acuerdo cumplido', NULL, CURRENT_DATE - INTERVAL '10 days', CURRENT_DATE - INTERVAL '2 days',
     v_estado_cump, 9005, 'NORMAL', false, false, now(), v_user_id, true, true)
  RETURNING reunion_acuerdo_id INTO v_acuerdo_id;

  INSERT INTO reunion_acuerdo_responsable
    (reunion_acuerdo_id, worker_id, estado_aceptacion, es_principal, created_date_time, created_user_id, active, state)
  VALUES (v_acuerdo_id, v_worker_id, 'ACEPTADO', true, now(), v_user_id, true, true);

  RAISE NOTICE 'Listo: 5 acuerdos de prueba creados en reunion_id=%', v_reunion_id;
END $$;

-- ── Limpieza (ejecutar cuando termines de probar) ───────────────────────────
-- DELETE FROM reunion_acuerdo_responsable
--   WHERE reunion_acuerdo_id IN (SELECT reunion_acuerdo_id FROM reunion_acuerdo WHERE descripcion LIKE 'PRUEBA DASHBOARD:%');
-- DELETE FROM reunion_acuerdo WHERE descripcion LIKE 'PRUEBA DASHBOARD:%';
