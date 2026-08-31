-- ============================================================================
-- Gestión GTH · Reclutamiento — Fases del pipeline (seguimiento del requerimiento)
--
-- 1) Agrega la columna `descripcion` a gth_estado_requerimiento (texto de ayuda
--    mostrado en el seguimiento vertical del modal "Estado del reclutamiento").
-- 2) Registra/actualiza las 9 fases del pipeline de reclutamiento (catálogo).
--
-- Idempotente: se puede correr múltiples veces sin duplicar ni romper nada.
-- El upsert usa el índice único parcial ix_gth_estado_requerimiento_codigo_vivo
-- (codigo) WHERE state = true.
-- ============================================================================

ALTER TABLE gth_estado_requerimiento
    ADD COLUMN IF NOT EXISTS descripcion text;

INSERT INTO gth_estado_requerimiento (codigo, nombre, descripcion, orden, created_date_time, active, state)
VALUES
    ('NUEVO',              'Nuevo / Solicitud',            'El área solicitante registra puesto, proyecto, cantidad, fecha requerida, justificación y adjunto de sustento.', 1, now(), true, true),
    ('APROBACION_GG',      'Aprobación Gerencia General',  'Solo aplica cuando es puesto nuevo o perfil fuera del catálogo aprobado.',                                       2, now(), true, true),
    ('VALIDACION_GTH',     'Validación GTH',               'GTH valida el perfil, la base maestra, contrato/duración y datos mínimos.',                                      3, now(), true, true),
    ('PUBLICACION',        'Publicación de vacante',       'GTH publica la vacante en canales internos o externos y marca el check al publicarla.',                          4, now(), true, true),
    ('LONG_LIST',          'Long list / CVs',              'GTH carga o adjunta la long list de candidatos y CVs para revisión del cliente interno.',                        5, now(), true, true),
    ('SELECCION_JEFATURA', 'Selección jefatura',           'El solicitante revisa los CVs y marca qué candidatos pasan a evaluación.',                                       6, now(), true, true),
    ('EVALUACION',         'Evaluación',                   'GTH carga resultados psicotécnicos y técnicos de los candidatos seleccionados.',                                7, now(), true, true),
    ('ENTREVISTAS',        'Entrevistas',                  'Se programan entrevistas y se selecciona al candidato final.',                                                  8, now(), true, true),
    ('OFERTA_CIERRE',      'Oferta y cierre',              'Se envía carta oferta; al aceptar, el candidato pasa a ficha de datos y onboarding.',                            9, now(), true, true)
ON CONFLICT (codigo) WHERE state = true
DO UPDATE SET
    nombre            = EXCLUDED.nombre,
    descripcion       = EXCLUDED.descripcion,
    orden             = EXCLUDED.orden,
    active            = true,
    updated_date_time = now();
