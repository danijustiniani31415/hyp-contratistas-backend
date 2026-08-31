-- ============================================================================
-- Gestion de Salidas: aprobacion del REEMBOLSO, firma de la planilla y rol TESORERO
-- ============================================================================
-- Cierra el ciclo de la rendicion. Hasta ahora una salida terminaba en "Rendido"
-- y ahi se quedaba: el jefe no tenia como dar el visto bueno al gasto ni como
-- firmar la planilla, y tesoreria no tenia donde marcar lo pagado.
--
-- El nuevo eje es `ga_solicitud_salida.estado_reembolso_id` y es INDEPENDIENTE de
-- los otros dos (aprobacion de la salida y rendicion). Solo empieza a moverse
-- cuando la salida ya esta Rendida Y tiene adjunto el Consolidado del S10
-- (ga_consolidado_s10, propio o heredado de su planilla): eso es lo que el jefe
-- revisa para aprobar el reembolso.
--
--   Pendiente --aprueba el jefe--> Aprobado --firma el jefe--> Firmado
--        ^                                                        |
--        |                                                        v
--        +--- el trabajador vuelve a subir el S10 ---- Rechazado  Pagado (tesoreria)
--
-- Rechazado guarda la observacion del jefe (`observacion_reembolso`) y vuelve a
-- Pendiente cuando el trabajador subsana subiendo otra vez el Consolidado del S10.
--
-- Idempotente: se puede correr mas de una vez.
-- ============================================================================

BEGIN;

-- ── 1. Catalogo de estados del reembolso ────────────────────────────────────
-- Los ids son fijos por diseno: los usa EstadosSalida.Reembolso en el backend.

CREATE TABLE IF NOT EXISTS ga_estado_reembolso (
    id          integer PRIMARY KEY,
    descripcion varchar(50) NOT NULL,
    orden       integer NOT NULL DEFAULT 0,
    activo      boolean NOT NULL DEFAULT true
);

COMMENT ON TABLE ga_estado_reembolso IS
  'Estados del reembolso de una salida ya rendida con Consolidado del S10 adjunto: Pendiente -> Aprobado -> Firmado -> Pagado, o Rechazado (con observacion) hasta que el trabajador subsane.';

INSERT INTO ga_estado_reembolso (id, descripcion, orden) VALUES
    (1, 'Pendiente', 1),
    (2, 'Aprobado',  2),
    (3, 'Rechazado', 3),
    (4, 'Firmado',   4),
    (5, 'Pagado',    5)
ON CONFLICT (id) DO UPDATE SET descripcion = EXCLUDED.descripcion, orden = EXCLUDED.orden;

-- ── 2. Columnas del reembolso en la solicitud ───────────────────────────────

ALTER TABLE ga_solicitud_salida
    ADD COLUMN IF NOT EXISTS estado_reembolso_id       integer     NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS observacion_reembolso     varchar(1000) NULL,
    ADD COLUMN IF NOT EXISTS reembolso_decidido_por_id integer     NULL,
    ADD COLUMN IF NOT EXISTS reembolso_decidido_at     timestamptz NULL,
    ADD COLUMN IF NOT EXISTS revisor_notificado_at     timestamptz NULL,
    ADD COLUMN IF NOT EXISTS revisor_notificado_por_id integer     NULL,
    ADD COLUMN IF NOT EXISTS firmado_por_id            integer     NULL,
    ADD COLUMN IF NOT EXISTS firmado_at                timestamptz NULL,
    ADD COLUMN IF NOT EXISTS pagado_por_id             integer     NULL,
    ADD COLUMN IF NOT EXISTS pagado_at                 timestamptz NULL;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_ga_solicitud_salida_estado_reembolso') THEN
        ALTER TABLE ga_solicitud_salida
            ADD CONSTRAINT fk_ga_solicitud_salida_estado_reembolso
            FOREIGN KEY (estado_reembolso_id) REFERENCES ga_estado_reembolso(id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_ga_solicitud_salida_reembolso_decidido_por') THEN
        ALTER TABLE ga_solicitud_salida
            ADD CONSTRAINT fk_ga_solicitud_salida_reembolso_decidido_por
            FOREIGN KEY (reembolso_decidido_por_id) REFERENCES app_user(user_id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_ga_solicitud_salida_revisor_notificado_por') THEN
        ALTER TABLE ga_solicitud_salida
            ADD CONSTRAINT fk_ga_solicitud_salida_revisor_notificado_por
            FOREIGN KEY (revisor_notificado_por_id) REFERENCES app_user(user_id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_ga_solicitud_salida_firmado_por') THEN
        ALTER TABLE ga_solicitud_salida
            ADD CONSTRAINT fk_ga_solicitud_salida_firmado_por
            FOREIGN KEY (firmado_por_id) REFERENCES app_user(user_id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_ga_solicitud_salida_pagado_por') THEN
        ALTER TABLE ga_solicitud_salida
            ADD CONSTRAINT fk_ga_solicitud_salida_pagado_por
            FOREIGN KEY (pagado_por_id) REFERENCES app_user(user_id);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_ga_solicitud_salida_estado_reembolso_id
    ON ga_solicitud_salida (estado_reembolso_id);

COMMENT ON COLUMN ga_solicitud_salida.estado_reembolso_id IS
  'FK a ga_estado_reembolso. Solo se mueve cuando la salida esta Rendida y tiene Consolidado del S10 adjunto.';
COMMENT ON COLUMN ga_solicitud_salida.observacion_reembolso IS
  'Observacion que escribe el jefe al RECHAZAR el reembolso. Es lo que el trabajador tiene que subsanar.';

-- ── 3. Planilla firmada (PDF estampado con la firma de la jefatura) ─────────
-- El PDF que se firma es el de la planilla de rendicion, que puede cubrir varias
-- salidas: por eso la copia firmada vive en ga_rendicion (una por planilla) y el
-- estado Firmado vive en cada salida.

ALTER TABLE ga_rendicion
    ADD COLUMN IF NOT EXISTS pdf_firmado_url      text        NULL,
    ADD COLUMN IF NOT EXISTS pdf_firmado_item_id  text        NULL,
    ADD COLUMN IF NOT EXISTS pdf_firmado_filename text        NULL,
    ADD COLUMN IF NOT EXISTS firmado_por_id       integer     NULL,
    ADD COLUMN IF NOT EXISTS firmado_at           timestamptz NULL;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_ga_rendicion_firmado_por') THEN
        ALTER TABLE ga_rendicion
            ADD CONSTRAINT fk_ga_rendicion_firmado_por
            FOREIGN KEY (firmado_por_id) REFERENCES app_user(user_id);
    END IF;
END $$;

COMMENT ON COLUMN ga_rendicion.pdf_firmado_url IS
  'webUrl de la copia FIRMADA de la planilla en SharePoint. El PDF original (pdf_url) no se toca.';

-- ── 4. Los tres correos nuevos del flujo ────────────────────────────────────
-- Aparecen solos en Gestion Administrativa -> Configuracion -> Correos: esa
-- pantalla lista ga_correo_evento tal cual. Los tres se pueden apagar
-- (permite_desactivar_envio = true), igual que la configuracion de GTH.

WITH nuevos(codigo, nombre, descripcion, orden, principal) AS (VALUES
    ('S10_REVISOR',
     'Consolidado del S10 adjuntado · al revisor',
     'Lo dispara el trabajador desde Solicitud de Salidas cuando ya adjuntó el Consolidado del S10 de una salida rendida. Avisa al jefe/revisor que tiene un reembolso por revisar.',
     5, 'El jefe/revisor del solicitante'),
    ('REEMBOLSO_APROBADO',
     'Reembolso aprobado · al solicitante',
     'Se envía cuando el jefe aprueba el reembolso de una salida rendida. Lleva el botón para ver la solicitud en la intranet.',
     6, 'El solicitante'),
    ('REEMBOLSO_RECHAZADO',
     'Reembolso rechazado · al solicitante',
     'Se envía cuando el jefe rechaza el reembolso. Lleva la observación y el botón para subsanarla en la intranet.',
     7, 'El solicitante')
),
insertados AS (
    INSERT INTO ga_correo_evento
        (codigo, nombre, descripcion, orden, active,
         destinatario_principal_nombre, destinatario_principal_activo,
         permite_desactivar_envio, permite_desactivar_principal)
    SELECT n.codigo, n.nombre, n.descripcion, n.orden, true,
           n.principal, true, true, true
    FROM nuevos n
    WHERE NOT EXISTS (SELECT 1 FROM ga_correo_evento e WHERE e.codigo = n.codigo)
    RETURNING codigo
)
-- Los textos visibles se re-aplican siempre: así corregir una redacción es volver a correr el
-- script y no un UPDATE suelto. Los interruptores NO se tocan: si alguien apagó un correo desde
-- la pantalla, tiene que seguir apagado después de correr esto.
UPDATE ga_correo_evento e
   SET nombre                        = n.nombre,
       descripcion                   = n.descripcion,
       orden                         = n.orden,
       destinatario_principal_nombre = n.principal,
       updated_at                    = now()
  FROM nuevos n
 WHERE e.codigo = n.codigo
   AND (e.nombre IS DISTINCT FROM n.nombre
     OR e.descripcion IS DISTINCT FROM n.descripcion
     OR e.orden IS DISTINCT FROM n.orden
     OR e.destinatario_principal_nombre IS DISTINCT FROM n.principal);

-- ── 5. Categoria TESORERO (46) ──────────────────────────────────────────────
-- La categoria sale del puesto (workers.puesto_id -> puesto.categoria_id), asi
-- que "ser tesorero" se lee ahi y no en un texto suelto de la ficha.
--
-- La categoria NO la crea este script: ya existe en produccion con el id 46, y
-- dev se alineo a ese id con _dev_alinear_categorias_roles_con_prod.sql. El id se
-- usa como constante en Shared/Constants/CategoriaIds.cs, asi que tiene que ser
-- el mismo en los dos entornos: aca solo se verifica y se repuntan los puestos.
--
-- (La primera version de este script creaba la categoria con el id 43. Reventó
-- en prod porque ese id ya era ABOGADO: prod venia creando catalogos en paralelo.
-- Por eso ahora se verifica en vez de insertar.)

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM categoria WHERE categoria_id = 46 AND upper(nombre) = 'TESORERO' AND state) THEN
        RAISE EXCEPTION 'Falta la categoria TESORERO con id 46 (es la constante CategoriaIds.Tesorero). Crearla antes de correr esto.';
    END IF;
END $$;

-- Los puestos de tesoreria cuelgan de esa categoria. Se hace por nombre porque
-- los ids de puesto no son iguales entre entornos.
UPDATE puesto
   SET categoria_id = 46, updated_date_time = now()
 WHERE state
   AND upper(nombre) IN ('TESORERO', 'TESORERA')
   AND categoria_id <> 46;

-- ── 6. Rol TESORERO (83) ────────────────────────────────────────────────────
-- Id EXPLICITO para que dev y prod queden iguales: el frontend y el backend lo
-- usan como constante (core/constants/roles.ts y Shared/Constants/Roles.cs).
-- El 83 es el primero libre en los dos entornos (prod llegaba hasta el 82).
--
-- OJO: tener el rol NO alcanza. GetAllowedFeaturesAsync solo concede las features
-- de este rol si ademas el puesto del trabajador es de categoria TESORERO (46).

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM role WHERE role_id = 83 AND upper(role_description) <> 'TESORERO') THEN
        RAISE EXCEPTION 'role_id 83 ya esta ocupado por otro rol: no insertar TESORERO a ciegas, avisar para elegir otro id en AMBOS entornos.';
    END IF;
END $$;

INSERT INTO role (role_id, role_description, created_user_id, active, state)
SELECT 83, 'TESORERO', (SELECT min(user_id) FROM app_user), true, true
WHERE NOT EXISTS (SELECT 1 FROM role WHERE role_id = 83);

SELECT setval('role_role_id_seq', GREATEST((SELECT max(role_id) FROM role), 83), true);

-- Acceso a Gestion de Salidas (la pantalla donde el tesorero marca lo pagado).
INSERT INTO role_feature (role_id, feature_id)
SELECT 83, f.feature_id
FROM feature f
WHERE f.feature_key = 'gestion-administrativa.gestion-salidas'
  AND NOT EXISTS (
      SELECT 1 FROM role_feature rf
      WHERE rf.role_id = 83 AND rf.feature_id = f.feature_id);

COMMIT;

-- ============================================================================
-- Verificacion
-- ============================================================================
-- SELECT * FROM ga_estado_reembolso ORDER BY id;
-- SELECT codigo, nombre, orden, active FROM ga_correo_evento ORDER BY orden;
-- SELECT categoria_id, nombre FROM categoria WHERE categoria_id = 43;
-- SELECT puesto_id, nombre, categoria_id FROM puesto WHERE upper(nombre) IN ('TESORERO','TESORERA');
-- SELECT role_id, role_description FROM role WHERE role_id = 80;
-- SELECT rf.* FROM role_feature rf JOIN feature f USING (feature_id)
--  WHERE rf.role_id = 80 AND f.feature_key = 'gestion-administrativa.gestion-salidas';
