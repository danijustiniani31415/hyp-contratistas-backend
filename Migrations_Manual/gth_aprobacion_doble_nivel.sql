-- ============================================================================
-- Gestión GTH · Aprobaciones — dos niveles de decisión (gerente del área + GG)
--
-- Contexto: la solicitud de personal la decidía UN solo actor, el Gerente
-- General, y `gth_aprobacion_gg` guardaba esa única decisión. El negocio pidió
-- que la MISMA solicitud reciba dos vistos buenos independientes:
--
--   • Gerente del área (workers.categoria_id = GERENTE, 11): opina sobre las
--     solicitudes de su area_scope hacia abajo. Su decisión es un endoso: NO
--     mueve el requerimiento ni dispara el correo a GTH.
--   • Gerencia General (workers.categoria_id = GERENTE GENERAL, 39): decide
--     TODAS las solicitudes. Su decisión es la obligatoria: es la que mueve las
--     vacantes a VALIDACION_GTH / RECHAZADO_GG y la que envía el correo a GTH.
--
-- Las dos casillas son independientes y sin orden impuesto: el correo inicial
-- le llega a los dos a la vez y cualquiera puede decidir primero. El gerente
-- del área puede registrar su visto bueno incluso después de que el GG cerró
-- la solicitud (queda como constancia).
--
-- Este script:
--   1) Renombra las columnas de la decisión existente para que digan de QUIÉN
--      son (todo lo que había es del Gerente General). Es un RENAME, no un
--      drop: la data histórica se conserva intacta.
--   2) Agrega las columnas espejo del gerente del área.
--
-- El nombre de las tablas (`gth_aprobacion_gg*`) se conserva por compatibilidad
-- aunque el sufijo "gg" ya quede corto: hoy guardan las dos decisiones. El
-- catálogo `gth_aprobacion_gg_estado` (PENDIENTE / APROBADA / APROBADA_PARCIAL
-- / RECHAZADA) se reutiliza para ambos niveles, no hace falta sembrarlo de nuevo.
--
-- Idempotente: se puede correr varias veces sin duplicar ni pisar nada.
-- ============================================================================

BEGIN;

-- ─────────────────────────────────────────────────────────────────────────────
-- 1) Cabecera: lo existente pasa a llamarse "del Gerente General".
-- ─────────────────────────────────────────────────────────────────────────────
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_name = 'gth_aprobacion_gg' AND column_name = 'gth_aprobacion_gg_estado_id') THEN
        ALTER TABLE gth_aprobacion_gg RENAME COLUMN gth_aprobacion_gg_estado_id TO estado_gerente_general_id;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_name = 'gth_aprobacion_gg' AND column_name = 'decidido_date_time') THEN
        ALTER TABLE gth_aprobacion_gg RENAME COLUMN decidido_date_time TO gerente_general_decidido_date_time;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_name = 'gth_aprobacion_gg' AND column_name = 'decidido_user_id') THEN
        ALTER TABLE gth_aprobacion_gg RENAME COLUMN decidido_user_id TO gerente_general_decidido_user_id;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_name = 'gth_aprobacion_gg' AND column_name = 'comentario') THEN
        ALTER TABLE gth_aprobacion_gg RENAME COLUMN comentario TO gerente_general_comentario;
    END IF;

    -- Los constraints siguen a la columna en un RENAME, pero su nombre queda
    -- ambiguo ahora que hay dos niveles.
    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_gag_estado') THEN
        ALTER TABLE gth_aprobacion_gg RENAME CONSTRAINT fk_gag_estado TO fk_gag_estado_gg;
    END IF;
    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_gag_decidido_user') THEN
        ALTER TABLE gth_aprobacion_gg RENAME CONSTRAINT fk_gag_decidido_user TO fk_gag_gg_decidido_user;
    END IF;
END $$;

-- Casilla del gerente del área. `estado_gerente_area_id` nace NULL para poder
-- rellenar las filas ya existentes y recién después se pone NOT NULL: una
-- columna NOT NULL sin default rompería el ALTER en una tabla con datos.
ALTER TABLE gth_aprobacion_gg
    ADD COLUMN IF NOT EXISTS estado_gerente_area_id          integer,
    ADD COLUMN IF NOT EXISTS gerente_area_decidido_date_time timestamptz,
    ADD COLUMN IF NOT EXISTS gerente_area_decidido_user_id   integer,
    ADD COLUMN IF NOT EXISTS gerente_area_comentario         text;

-- Todo lo que ya existe nace PENDIENTE del lado del gerente del área: nadie de
-- ese nivel llegó a opinar (la casilla no existía).
UPDATE gth_aprobacion_gg
SET    estado_gerente_area_id = (
           SELECT gth_aprobacion_gg_estado_id
           FROM   gth_aprobacion_gg_estado
           WHERE  codigo = 'PENDIENTE' AND state = true
           LIMIT  1)
WHERE  estado_gerente_area_id IS NULL;

ALTER TABLE gth_aprobacion_gg
    ALTER COLUMN estado_gerente_area_id SET NOT NULL;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_gag_estado_ga') THEN
        ALTER TABLE gth_aprobacion_gg
            ADD CONSTRAINT fk_gag_estado_ga
            FOREIGN KEY (estado_gerente_area_id) REFERENCES gth_aprobacion_gg_estado (gth_aprobacion_gg_estado_id);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_gag_ga_decidido_user') THEN
        ALTER TABLE gth_aprobacion_gg
            ADD CONSTRAINT fk_gag_ga_decidido_user
            FOREIGN KEY (gerente_area_decidido_user_id) REFERENCES app_user (user_id);
    END IF;
END $$;

-- ─────────────────────────────────────────────────────────────────────────────
-- 2) Detalle por vacante: misma operación, una decisión por nivel.
-- ─────────────────────────────────────────────────────────────────────────────
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_name = 'gth_aprobacion_gg_detalle' AND column_name = 'aprobado') THEN
        ALTER TABLE gth_aprobacion_gg_detalle RENAME COLUMN aprobado TO aprobado_gerente_general;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_name = 'gth_aprobacion_gg_detalle' AND column_name = 'decidido_date_time') THEN
        ALTER TABLE gth_aprobacion_gg_detalle RENAME COLUMN decidido_date_time TO gerente_general_decidido_date_time;
    END IF;
END $$;

ALTER TABLE gth_aprobacion_gg_detalle
    ADD COLUMN IF NOT EXISTS aprobado_gerente_area           boolean,
    ADD COLUMN IF NOT EXISTS gerente_area_decidido_date_time timestamptz;

-- ─────────────────────────────────────────────────────────────────────────────
-- 3) Documentación de las columnas (queda en la BD, no solo en el código).
-- ─────────────────────────────────────────────────────────────────────────────
COMMENT ON COLUMN gth_aprobacion_gg.estado_gerente_general_id IS
    'Estado de la decisión de Gerencia General (la obligatoria: mueve las vacantes y avisa a GTH).';
COMMENT ON COLUMN gth_aprobacion_gg.estado_gerente_area_id IS
    'Estado del visto bueno del gerente del área del solicitante. Independiente del anterior; no mueve el flujo.';
COMMENT ON COLUMN gth_aprobacion_gg_detalle.aprobado_gerente_general IS
    'Decisión del Gerente General sobre ESTA vacante: true = aprobada, false = rechazada, null = sin decidir.';
COMMENT ON COLUMN gth_aprobacion_gg_detalle.aprobado_gerente_area IS
    'Visto bueno del gerente del área sobre ESTA vacante: true / false / null = sin decidir.';

COMMIT;
