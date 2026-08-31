-- ============================================================================
-- 2026-08-07 · Matriz de destinatarios de los correos de EMO
--
-- Antes: cada uno de los 4 correos de EMO armaba su lista de destinatarios en
-- un sitio distinto del código, y los correos de área (GTH, medicina
-- ocupacional, ArqCom/PostVenta) salían de claves "EmailsArea:*" del
-- appsettings del servidor — invisibles y no editables desde la aplicación.
--
-- Ahora: una sola matriz configurable desde
-- /ssoma/salud-ocupacional/emos/configuracion
--
--     correo (evento)  ×  perfil del trabajador  ×  destinatario  →  activo
--
-- Cuatro tablas:
--   ss_emo_correo_evento        los 4 correos
--   ss_emo_correo_perfil        Oficina Central / Staff / Obra
--   ss_emo_correo_destinatario  el "quién" (se amplía la tabla que ya existía)
--   ss_emo_correo_regla         la celda de la matriz (evento × perfil × dest)
--
-- El seed reproduce EXACTAMENTE el comportamiento vigente, con un único
-- cambio pedido: los 4 correos de ArqCom/PostVenta ahora también van en el
-- correo de rechazo (antes solo en el de aceptación).
--
-- Idempotente: se puede correr más de una vez sin duplicar nada.
-- ============================================================================

BEGIN;

-- ── 1) Catálogo de correos (eventos) ────────────────────────────────────────
CREATE TABLE IF NOT EXISTS ss_emo_correo_evento (
    id          serial PRIMARY KEY,
    codigo      varchar(40)  NOT NULL,
    nombre      varchar(120) NOT NULL,
    descripcion varchar(300),
    orden       int          NOT NULL DEFAULT 0,
    active      boolean      NOT NULL DEFAULT true,
    state       boolean      NOT NULL DEFAULT true,
    created_at  timestamptz  NOT NULL DEFAULT now(),
    updated_at  timestamptz  NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_ss_emo_correo_evento_codigo
    ON ss_emo_correo_evento (upper(codigo)) WHERE state;

INSERT INTO ss_emo_correo_evento (codigo, nombre, descripcion, orden)
SELECT v.codigo, v.nombre, v.descripcion, v.orden
FROM (VALUES
    ('PROGRAMACION_AUTOMATICA', 'Programación automática',
     'Resumen que sale del cron diario cuando el sistema programa EMOs por vencimiento. La clínica todavía tiene que aceptar o rechazar.', 1),
    ('PROGRAMACION_MANUAL', 'Programación manual',
     'Sale al programar un EMO a mano desde EMOs o Programaciones. La clínica todavía tiene que aceptar o rechazar.', 2),
    ('ACEPTADA', 'Programación aceptada por la clínica',
     'Confirmación de la cita: fecha, hora, clínica y dirección. Lo dispara la clínica desde su agenda.', 3),
    ('RECHAZADA', 'Programación rechazada por la clínica',
     'Aviso de que la clínica rechazó la cita, con el motivo. Hay que coordinar una nueva fecha.', 4)
) AS v(codigo, nombre, descripcion, orden)
WHERE NOT EXISTS (
    SELECT 1 FROM ss_emo_correo_evento e WHERE e.state AND upper(e.codigo) = v.codigo
);

-- ── 2) Catálogo de perfiles de trabajador ───────────────────────────────────
-- Se deriva de workers.contrata_casa + workers.obra_oficina_staff_id. Solo
-- cubre al personal de casa: por negocio, Abril controla únicamente el EMO de
-- sus propios trabajadores — las contratistas manejan el de los suyos por su
-- cuenta, y el cron de auto-programación ya las excluye filtrando por
-- contributor.es_abril. Un trabajador de contratista no cae en ningún perfil y
-- por lo tanto no recibe ninguno de estos 4 correos.
--
-- No se reutiliza workers_obra_oficina_staff (mismos 3 valores) para no atar
-- este catálogo de configuración a un catálogo de datos maestros de RR.HH.
CREATE TABLE IF NOT EXISTS ss_emo_correo_perfil (
    id          serial PRIMARY KEY,
    codigo      varchar(30)  NOT NULL,
    nombre      varchar(80)  NOT NULL,
    descripcion varchar(300),
    orden       int          NOT NULL DEFAULT 0,
    active      boolean      NOT NULL DEFAULT true,
    state       boolean      NOT NULL DEFAULT true,
    created_at  timestamptz  NOT NULL DEFAULT now(),
    updated_at  timestamptz  NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_ss_emo_correo_perfil_codigo
    ON ss_emo_correo_perfil (upper(codigo)) WHERE state;

INSERT INTO ss_emo_correo_perfil (codigo, nombre, descripcion, orden)
SELECT v.codigo, v.nombre, v.descripcion, v.orden
FROM (VALUES
    ('OFICINA_CENTRAL', 'Oficina Central', 'Personal de casa con modalidad Oficina Central.', 1),
    ('STAFF',           'Staff',           'Personal de casa de oficina técnica destacado en proyecto.', 2),
    ('OBRA',            'Obra',            'Personal de casa en obra. Incluye a quien no tenga modalidad registrada.', 3)
) AS v(codigo, nombre, descripcion, orden)
WHERE NOT EXISTS (
    SELECT 1 FROM ss_emo_correo_perfil p WHERE p.state AND upper(p.codigo) = v.codigo
);

-- Una versión previa de este mismo script (nunca desplegada a producción) creaba
-- además un perfil CONTRATISTA. Se borra en duro y no por state=false porque son
-- filas de configuración de este mismo script que jamás llegaron a producción ni
-- las vio un usuario: dejarlas apagadas solo ensuciaría la tabla. No hay ningún
-- registro de negocio apuntando a ellas.
DELETE FROM ss_emo_correo_regla
WHERE perfil_id IN (SELECT id FROM ss_emo_correo_perfil WHERE upper(codigo) = 'CONTRATISTA');

DELETE FROM ss_emo_correo_perfil WHERE upper(codigo) = 'CONTRATISTA';

-- ── 3) Destinatarios ────────────────────────────────────────────────────────
-- La tabla ya existía con 2 filas (CLINICA, JEFE) para el correo de
-- programación. Se amplía para cubrir a todos los destinatarios de los 4
-- correos. Reglas de la columna `codigo`:
--   • codigo NOT NULL + email NULL  → destinatario dinámico: su correo se
--     resuelve al enviar (clínica de la cita, jefe, trabajador, correos del
--     proyecto, administrador de la razón social, GTH).
--   • codigo NOT NULL + email       → buzón fijo de área: el correo vive acá
--     y se edita desde la pantalla (medicina ocupacional, ArqCom, PostVenta).
--   • codigo NULL                   → correo adicional agregado a mano, con
--     alta/edición/baja completa desde la pantalla.
ALTER TABLE ss_emo_correo_destinatario
    ALTER COLUMN descripcion TYPE varchar(300);

-- El CHECK viejo obligaba a que toda fila editable tuviera correo. Los buzones
-- de área nacen vacíos (hay que cargarlos desde la pantalla), así que la regla
-- pasa a ser: solo los correos adicionales están obligados a traer email.
ALTER TABLE ss_emo_correo_destinatario
    DROP CONSTRAINT IF EXISTS ck_ss_emo_correo_destinatario_email;

ALTER TABLE ss_emo_correo_destinatario
    ADD CONSTRAINT ck_ss_emo_correo_destinatario_email
    CHECK (codigo IS NOT NULL OR email IS NOT NULL);

-- `active` a nivel de destinatario deja de decidir el envío: el interruptor
-- real es ahora ss_emo_correo_regla.active, una celda por correo y perfil.
UPDATE ss_emo_correo_destinatario SET active = true, updated_at = now()
WHERE state AND NOT active;

-- El índice viejo prohibía repetir un correo dentro del mismo tipo (Para/CC), lo
-- que impedía algo perfectamente válido: que la misma persona sea, por ejemplo,
-- jefe de Arquitectura Comercial y de Post Venta. La unicidad solo tiene sentido
-- entre los correos adicionales, donde repetir uno sí sería un duplicado real.
DROP INDEX IF EXISTS ux_ss_emo_correo_dest_tipo_email;

CREATE UNIQUE INDEX IF NOT EXISTS ux_ss_emo_correo_dest_adicional_email
    ON ss_emo_correo_destinatario (tipo_id, lower(email))
    WHERE state AND email IS NOT NULL AND codigo IS NULL;

INSERT INTO ss_emo_correo_destinatario
    (tipo_id, codigo, email, nombre, descripcion, editable, orden, active, state)
SELECT t.id, v.codigo, v.email, v.nombre, v.descripcion, v.editable, v.orden, true, true
FROM (VALUES
    ('CLINICA', NULL, 'Clínica asignada',
     'Correos de contacto de la clínica de la programación (Catálogos → Clínicas).', false, 1),
    ('JEFE', NULL, 'Jefe del trabajador',
     'Se resuelve al enviar: su jefe personalizado o, si no tiene, el revisor de su área.', false, 2),
    ('TRABAJADOR', NULL, 'Trabajador',
     'Correo corporativo del propio trabajador. El personal de obra normalmente no tiene.', false, 3),
    ('RESIDENTE', NULL, 'Residente del proyecto',
     'Correo del residente del proyecto donde está vinculado el trabajador.', false, 4),
    ('COORD_ADMIN', NULL, 'Coordinador administrativo del proyecto',
     'Correo del coordinador administrativo del proyecto donde está vinculado el trabajador.', false, 5),
    ('COORD_SSOMA', NULL, 'Coordinador SSOMA del proyecto',
     'Correo del coordinador SSOMA del proyecto donde está vinculado el trabajador.', false, 6),
    ('ADMIN_RAZON_SOCIAL', NULL, 'Administrador de la razón social',
     'Correo del administrador de la empresa a la que pertenece el trabajador.', false, 7),
    ('GTH', NULL, 'Gestión del Talento Humano',
     'Se resuelve al enviar desde el correo del área de Gestión del Talento Humano (Configuración → Áreas).', false, 8),
    ('MEDICINA_OCUPACIONAL', 'medicinaocupacionalnm@abril.pe', 'Medicina Ocupacional',
     'Buzón del área de medicina ocupacional.', true, 9),
    ('ARQCOM_JEFE', NULL, 'Jefe de Arquitectura Comercial',
     'Solo se le escribe si el proyecto del trabajador tiene arquitectura comercial.', true, 10),
    ('ARQCOM_PREVENCIONISTA', NULL, 'Prevencionista de Arquitectura Comercial',
     'Solo se le escribe si el proyecto del trabajador tiene arquitectura comercial.', true, 11),
    ('POSTVENTA_JEFE', NULL, 'Jefe de Post Venta',
     'Solo se le escribe si el proyecto del trabajador tiene arquitectura comercial.', true, 12),
    ('POSTVENTA_PREVENCIONISTA', NULL, 'Prevencionista de Post Venta',
     'Solo se le escribe si el proyecto del trabajador tiene arquitectura comercial.', true, 13)
) AS v(codigo, email, nombre, descripcion, editable, orden)
CROSS JOIN LATERAL (
    SELECT id FROM ss_emo_correo_tipo WHERE state AND upper(codigo) = 'PRINCIPAL' LIMIT 1
) t
WHERE NOT EXISTS (
    SELECT 1 FROM ss_emo_correo_destinatario d WHERE d.state AND upper(d.codigo) = v.codigo
);

-- Las 2 filas que ya existían se actualizan al texto y orden nuevos.
UPDATE ss_emo_correo_destinatario d
SET nombre      = v.nombre,
    descripcion = v.descripcion,
    editable    = v.editable,
    orden       = v.orden,
    updated_at  = now()
FROM (VALUES
    ('CLINICA', 'Clínica asignada',
     'Correos de contacto de la clínica de la programación (Catálogos → Clínicas).', false, 1),
    ('JEFE', 'Jefe del trabajador',
     'Se resuelve al enviar: su jefe personalizado o, si no tiene, el revisor de su área.', false, 2)
) AS v(codigo, nombre, descripcion, editable, orden)
WHERE d.state AND upper(d.codigo) = v.codigo;

-- ── 4) La matriz ────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS ss_emo_correo_regla (
    id              serial PRIMARY KEY,
    evento_id       int         NOT NULL REFERENCES ss_emo_correo_evento (id),
    perfil_id       int         NOT NULL REFERENCES ss_emo_correo_perfil (id),
    destinatario_id int         NOT NULL REFERENCES ss_emo_correo_destinatario (id),
    active          boolean     NOT NULL DEFAULT false,
    state           boolean     NOT NULL DEFAULT true,
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_ss_emo_correo_regla_celda
    ON ss_emo_correo_regla (evento_id, perfil_id, destinatario_id) WHERE state;

CREATE INDEX IF NOT EXISTS ix_ss_emo_correo_regla_evento
    ON ss_emo_correo_regla (evento_id) WHERE state;

-- Una celda por cada combinación evento × perfil × destinatario de catálogo.
-- Los correos adicionales (codigo IS NULL) crean sus celdas al darlos de alta
-- desde la pantalla, no acá.
WITH activos(evento, perfil, dest) AS (
    -- Programaciones (automática y manual): mismo grupo en los 3 perfiles.
    SELECT e.codigo, p.codigo, d.codigo
    FROM      (VALUES ('PROGRAMACION_AUTOMATICA'), ('PROGRAMACION_MANUAL'))     e(codigo)
    CROSS JOIN (VALUES ('OFICINA_CENTRAL'), ('STAFF'), ('OBRA'))                p(codigo)
    CROSS JOIN (VALUES ('CLINICA'), ('JEFE'))                                   d(codigo)

    UNION ALL
    -- Aceptada / Rechazada · Oficina Central
    SELECT e.codigo, 'OFICINA_CENTRAL', d.codigo
    FROM      (VALUES ('ACEPTADA'), ('RECHAZADA')) e(codigo)
    CROSS JOIN (VALUES ('TRABAJADOR'), ('JEFE'), ('GTH'), ('MEDICINA_OCUPACIONAL'),
                       ('ADMIN_RAZON_SOCIAL'), ('ARQCOM_JEFE'), ('ARQCOM_PREVENCIONISTA'),
                       ('POSTVENTA_JEFE'), ('POSTVENTA_PREVENCIONISTA')) d(codigo)

    UNION ALL
    -- Aceptada / Rechazada · Staff
    SELECT e.codigo, 'STAFF', d.codigo
    FROM      (VALUES ('ACEPTADA'), ('RECHAZADA')) e(codigo)
    CROSS JOIN (VALUES ('TRABAJADOR'), ('RESIDENTE'), ('COORD_ADMIN'), ('COORD_SSOMA'),
                       ('ADMIN_RAZON_SOCIAL'), ('ARQCOM_JEFE'), ('ARQCOM_PREVENCIONISTA'),
                       ('POSTVENTA_JEFE'), ('POSTVENTA_PREVENCIONISTA')) d(codigo)

    UNION ALL
    -- Aceptada / Rechazada · Obra
    SELECT e.codigo, 'OBRA', d.codigo
    FROM      (VALUES ('ACEPTADA'), ('RECHAZADA')) e(codigo)
    CROSS JOIN (VALUES ('RESIDENTE'), ('COORD_ADMIN'), ('COORD_SSOMA'), ('MEDICINA_OCUPACIONAL'),
                       ('ADMIN_RAZON_SOCIAL'), ('ARQCOM_JEFE'), ('ARQCOM_PREVENCIONISTA'),
                       ('POSTVENTA_JEFE'), ('POSTVENTA_PREVENCIONISTA')) d(codigo)
)
INSERT INTO ss_emo_correo_regla (evento_id, perfil_id, destinatario_id, active, state)
SELECT e.id, p.id, d.id,
       EXISTS (SELECT 1 FROM activos a
               WHERE a.evento = upper(e.codigo)
                 AND a.perfil = upper(p.codigo)
                 AND a.dest   = upper(d.codigo)),
       true
FROM ss_emo_correo_evento e
CROSS JOIN ss_emo_correo_perfil p
CROSS JOIN ss_emo_correo_destinatario d
WHERE e.state AND p.state AND d.state AND d.codigo IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM ss_emo_correo_regla r
      WHERE r.state AND r.evento_id = e.id AND r.perfil_id = p.id AND r.destinatario_id = d.id
  );

COMMIT;

-- ============================================================================
-- Verificación: debe imprimir la misma matriz de las 3 tablas del análisis.
--
-- SELECT d.nombre AS destinatario,
--        e.nombre AS correo,
--        max(CASE WHEN p.codigo = 'OFICINA_CENTRAL' THEN r.active::int END) AS of_central,
--        max(CASE WHEN p.codigo = 'STAFF'           THEN r.active::int END) AS staff,
--        max(CASE WHEN p.codigo = 'OBRA'            THEN r.active::int END) AS obra,
--        max(CASE WHEN p.codigo = 'CONTRATISTA'     THEN r.active::int END) AS contratista
-- FROM ss_emo_correo_regla r
-- JOIN ss_emo_correo_evento e       ON e.id = r.evento_id
-- JOIN ss_emo_correo_perfil p       ON p.id = r.perfil_id
-- JOIN ss_emo_correo_destinatario d ON d.id = r.destinatario_id
-- WHERE r.state
-- GROUP BY d.orden, d.nombre, e.orden, e.nombre
-- ORDER BY e.orden, d.orden;
-- ============================================================================
