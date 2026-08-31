-- ============================================================================
-- Eliminar cat_jefatura — el jefe de un trabajador sale de la config global
--
-- Contexto: `cat_jefatura` era un catálogo cargo → correo (19 filas) que se
-- cruzaba por NOMBRE contra el texto libre `workers.jefatura`. Quedó redundante:
-- todos sus consumidores pasaron a IJefeRevisorResolver, que resuelve el jefe
-- desde la configuración global de revisores (revisor directo en
-- workers_revisores → revisor del área en area_revisores → fallback GTH):
--
--   • ProgramacionEmoRepository — correos [EMO Confirmado] / [EMO Rechazado]
--   • InterconsultaRepository   — lista de interconsultas y su envío de correos
--   • EvRecordatorioRepository  — CC del jefe en los recordatorios de evaluación
--
-- No existe ninguna pantalla que administre cat_jefatura (nunca tuvo CRUD), así
-- que al soltarla no queda ninguna UI huérfana.
--
-- ⚠️ OJO: la columna `workers.jefatura` NO se toca. Sigue mostrándose y
--    editándose en el perfil del trabajador; lo que desaparece es únicamente el
--    mapeo nombre-de-cargo → correo.
--
-- ⚠️ Correr SOLO después de desplegar el backend que ya no usa la tabla.
--    Este script es el ÚNICO paso destructivo de este cambio: el código ya no la
--    lee, así que se puede posponer sin romper nada.
-- ============================================================================

BEGIN;

DROP TABLE IF EXISTS cat_jefatura;

COMMIT;

-- ============================================================================
-- Rollback (contenido exacto de la tabla en prod al 2026-08-04, por auditoría):
--
-- CREATE TABLE cat_jefatura (
--     id     serial            PRIMARY KEY,
--     nombre character varying NOT NULL UNIQUE,
--     email  character varying,
--     activo boolean           NOT NULL DEFAULT true
-- );
-- (Ninguna tabla la referenciaba: cero llaves foráneas entrantes, por eso el DROP
--  no necesita CASCADE ni tocar nada más.)
-- INSERT INTO cat_jefatura (id, nombre, email, activo) VALUES
--   (1, 'Jefe de Calidad', 'aprado@abril.pe', true),
--   (2, 'Sub Gerente de Finanzas', 'clopez@abril.pe', true),
--   (3, 'Jefe de TI', 'dmoran@abril.pe', true),
--   (4, 'Jefe de Costos y Presupuestos', 'eaguinaga@abril.pe', true),
--   (5, 'Jefe de Gestiones Administrativas', 'etrujillo@abril.pe', true),
--   (6, 'Jefe de Marketing', 'dcarranza@abril.pe', true),
--   (7, 'Jefe de Arquitectura Comercial', 'fgodenzi@abril.pe', true),
--   (8, 'Jefe de Arquitectura', 'gperez@abril.pe', true),
--   (9, 'Jefe de logistica', 'lgarro@abril.pe', true),
--   (10, 'Jefe de GTH', 'avrivera@abril.pe', true),
--   (11, 'Jefe de Seguridad y Salud en el trabajo', 'sjustiniani@abril.pe', true),
--   (12, 'Gerente de Marketing', 'aoviedo@abril.pe', true),
--   (13, 'Gerente De Proyectos', 'coriundo@abril.pe', true),
--   (14, 'Director', 'fftoratto@abril.pe', true),
--   (15, 'Gerente de Administración y Finanzas', 'rvera@abril.pe', true),
--   (16, 'Jefe de Proyectos', 'hmamani@abril.pe', true),
--   (17, 'Coordinador Legal', 'snamuche@abril.pe', true),
--   (18, 'Jefe de Ventas', 'jponce@abril.pe', true),
--   (19, 'Jefe de Post Venta', 'fbarreda@abril.pe', true);
-- SELECT setval(pg_get_serial_sequence('cat_jefatura','id'), 19);
-- ============================================================================
