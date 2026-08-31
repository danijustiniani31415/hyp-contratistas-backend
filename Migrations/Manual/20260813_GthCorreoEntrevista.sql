-- ============================================================================
-- Gestión GTH · Reclutamiento — Correo "invitación a entrevista" configurable
-- Fecha: 2026-08-13
--
-- Nuevo tipo de correo (gth_correo_tipo): ENTREVISTA. Es el correo que se le
-- manda al postulante al programarle la cita desde "Programación de
-- entrevistas". Hasta ahora salía solo al postulante, sin forma de agregar
-- copias; con esto se administra desde /gestion-gth/reclutamiento/configuracion
-- igual que los otros.
--
-- principal_automatico = true: el destinatario principal (Para) lo pone el
-- sistema —es el postulante citado, con el correo que declaró en su
-- formulario—, así que en la pantalla solo se agregan principales adicionales
-- y copias.
--
-- El destinatario de catálogo (área de GTH, en copia) se siembra APAGADO a
-- propósito: hacerlo configurable no debe cambiar por sí solo a quién le llega
-- hoy la invitación. Queda a un clic de distancia en la pantalla.
--
-- La columna principal_automatico se crea también acá (IF NOT EXISTS) para que
-- este script funcione suelto, sin depender del orden con
-- 20260813_GthCorreoFormularioCompletado.sql.
--
-- Idempotente. Aplicar en dev y prod.
-- ============================================================================

ALTER TABLE gth_correo_tipo
    ADD COLUMN IF NOT EXISTS principal_automatico boolean NOT NULL DEFAULT false;

-- ── 1) Tipo de correo ───────────────────────────────────────────────────────
INSERT INTO gth_correo_tipo (codigo, nombre, descripcion, orden, principal_automatico)
SELECT 'ENTREVISTA',
       'Invitación a entrevista',
       'Se envía al postulante cuando GTH le programa la entrevista, con la fecha, hora y lugar de la cita. El destinatario principal es SIEMPRE el postulante citado; acá solo se definen principales adicionales y copias.',
       7,
       true
WHERE NOT EXISTS (
    SELECT 1 FROM gth_correo_tipo WHERE codigo = 'ENTREVISTA' AND state
);

-- ── 2) Destinatario del catálogo: área de GTH en copia (apagado) ────────────
INSERT INTO gth_correo_destinatario (gth_correo_tipo_id, codigo, nombre, descripcion, es_copia, orden, active)
SELECT t.gth_correo_tipo_id,
       'GTH_AREA',
       'Área de Gestión del Talento Humano',
       'Se toma el correo configurado para el área de GTH en Configuración → Áreas.',
       true,
       1,
       false
FROM gth_correo_tipo t
WHERE t.codigo = 'ENTREVISTA' AND t.state
  AND NOT EXISTS (
      SELECT 1 FROM gth_correo_destinatario d
      WHERE d.gth_correo_tipo_id = t.gth_correo_tipo_id
        AND upper(d.codigo) = 'GTH_AREA' AND d.state
  );
