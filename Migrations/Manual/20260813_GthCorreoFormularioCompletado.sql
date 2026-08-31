-- ============================================================================
-- Gestión GTH · Reclutamiento — Correo "formulario del postulante completado"
-- Fecha: 2026-08-13
--
-- Nuevo tipo de correo configurable (gth_correo_tipo): FORMULARIO_COMPLETADO.
-- Sale solo, sin que nadie lo dispare, en cuanto el postulante envía su
-- formulario desde la página pública (y también cuando reenvía las correcciones
-- de uno rechazado), para que GTH sepa que ya lo puede revisar.
--
-- Se administra desde la nueva pantalla /gestion-gth/reclutamiento/configuracion
-- junto con LONG_LIST, con el mismo mecanismo que ya usa Solicitud de Personal:
-- interruptor maestro por correo + destinatarios del catálogo (dinámicos) +
-- correos adicionales escritos a mano.
--
-- Destinatario inicial: el área de Gestión del Talento Humano (dinámico
-- GTH_AREA), que toma su correo de area_scope al momento de enviar — el mismo
-- que ya usan SOLICITUD, LONG_LIST_DECISION y FINALISTA_DECISION. No se
-- siembra ningún correo personal: los adicionales se agregan desde la pantalla.
--
-- Además agrega gth_correo_tipo.principal_automatico: hay correos cuyo
-- destinatario principal lo pone el backend solo (la long list va SIEMPRE al
-- solicitante que registró la solicitud). En esos la pantalla no debe advertir
-- "no hay ningún destinatario principal activo", porque sí le llega a alguien.
-- Es un dato del catálogo y no un código quemado en el frontend.
--
-- Idempotente. Aplicar en dev y prod.
-- ============================================================================

-- ── 0) Correos cuyo destinatario principal es automático ────────────────────
ALTER TABLE gth_correo_tipo
    ADD COLUMN IF NOT EXISTS principal_automatico boolean NOT NULL DEFAULT false;

UPDATE gth_correo_tipo
SET principal_automatico = true
WHERE codigo = 'LONG_LIST' AND state AND principal_automatico = false;

-- ── 1) Tipo de correo ───────────────────────────────────────────────────────
INSERT INTO gth_correo_tipo (codigo, nombre, descripcion, orden)
SELECT 'FORMULARIO_COMPLETADO',
       'Formulario del postulante completado',
       'Se envía a GTH apenas el postulante termina de llenar su formulario de información (o reenvía las correcciones de uno rechazado), para que sepan que ya lo pueden revisar y aprobar o rechazar.',
       6
WHERE NOT EXISTS (
    SELECT 1 FROM gth_correo_tipo WHERE codigo = 'FORMULARIO_COMPLETADO' AND state
);

-- ── 2) Destinatario del catálogo: área de GTH ───────────────────────────────
INSERT INTO gth_correo_destinatario (gth_correo_tipo_id, codigo, nombre, descripcion, es_copia, orden, active)
SELECT t.gth_correo_tipo_id,
       'GTH_AREA',
       'Área de Gestión del Talento Humano',
       'Se toma el correo configurado para el área de GTH en Configuración → Áreas.',
       false,
       1,
       true
FROM gth_correo_tipo t
WHERE t.codigo = 'FORMULARIO_COMPLETADO' AND t.state
  AND NOT EXISTS (
      SELECT 1 FROM gth_correo_destinatario d
      WHERE d.gth_correo_tipo_id = t.gth_correo_tipo_id
        AND upper(d.codigo) = 'GTH_AREA' AND d.state
  );
