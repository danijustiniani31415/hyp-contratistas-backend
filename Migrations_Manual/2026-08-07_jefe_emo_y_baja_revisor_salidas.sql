-- ============================================================================
-- 2026-08-07 · Jefe del trabajador en los correos de EMO + baja de la pantalla
--              "Revisores de Trabajadores" (/configuracion/revisor-salidas).
--
-- 1) Nuevo destinatario fijo/dinámico JEFE en la configuración de correos de EMO
--    (/ssoma/salud-ocupacional/emos/configuracion). Igual que CLINICA: no guarda
--    un correo, se resuelve al enviar con IJefeRevisorResolver (jefe personalizado
--    del trabajador → revisor de su área → GTH), así que siempre refleja lo último
--    configurado. Solo se puede activar/desactivar.
--
-- 2) El jefe por trabajador ya no se configura en /configuracion/revisor-salidas
--    (pantalla retirada) sino con el checkbox "Jefe personalizado" del formulario
--    de Gestión de Ingresos → Trabajadores. Los datos siguen en workers_revisores:
--    NO se borra nada, solo se da de baja la feature para que no aparezca en el
--    menú ni en las pestañas de Configuración.
-- ============================================================================

BEGIN;

-- ── 1) Destinatario JEFE de los correos de programación de EMO ───────────────
INSERT INTO ss_emo_correo_destinatario
    (tipo_id, codigo, email, nombre, descripcion, editable, orden, active, state)
SELECT
    t.id,
    'JEFE',
    NULL,
    'Jefe del trabajador',
    'Se resuelve al enviar: su jefe personalizado o, si no tiene, el revisor de su área.',
    false,
    0,
    true,
    true
FROM ss_emo_correo_tipo t
WHERE t.state
  AND upper(t.codigo) = 'PRINCIPAL'
  AND NOT EXISTS (
      SELECT 1 FROM ss_emo_correo_destinatario d
      WHERE d.state AND upper(d.codigo) = 'JEFE'
  );

-- ── 2) Baja de la feature "Revisores de Trabajadores" ───────────────────────
DELETE FROM role_feature
WHERE feature_id IN (SELECT feature_id FROM feature WHERE feature_key = 'configuracion.revisor-salidas');

DELETE FROM feature
WHERE feature_key = 'configuracion.revisor-salidas';

COMMIT;
