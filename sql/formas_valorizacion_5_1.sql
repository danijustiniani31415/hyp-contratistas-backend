-- ============================================================================
-- Formas de valorización en cláusulas 5.1 (por PARTIDA / work_item)
-- Ejecutar en PRODUCCIÓN.
--
-- Reemplaza, en la cláusula 5.1 del contrato, el texto por defecto
--   "Las valorizaciones se determinan a partir del inicio de los trabajos…"
-- por el desglose de porcentajes de la partida, p. ej.:
--   "Las valorizaciones serán de acuerdo con los siguientes porcentajes de
--    valorización: 60% por instalación, 30% por acabado y 10% por
--    levantamiento de observaciones."
--
-- 1) DDL: crea la tabla y su índice.
-- 2) Población: inserta las formas de las imágenes, emparejando por
--    DESCRIPCIÓN de partida (ignorando mayúsculas y tildes). Solo inserta
--    cuando existe una partida activa con esa descripción y aún no tiene
--    formas registradas (idempotente: se puede re-ejecutar sin duplicar).
--
--  ⚠ Verifica que las descripciones de las partidas en producción coincidan
--    con las etiquetas de abajo. Si una partida tiene otro nombre, ajusta la
--    columna 'partida' del VALUES o relaciónala manualmente desde la UI
--    (Configuración → Partidas → Editar).
-- ============================================================================

-- ─────────────────────────────────────────────────────────────────────────
-- 1) DDL
-- ─────────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS public.work_item_valorization_form (
    work_item_valorization_form_id SERIAL PRIMARY KEY,
    work_item_id        integer NOT NULL,
    concept             text    NOT NULL,
    percentage          numeric(5,2) NOT NULL,
    sort_order          integer NOT NULL DEFAULT 0,
    state               boolean NOT NULL DEFAULT true,
    created_datetime    timestamp with time zone NOT NULL DEFAULT now(),
    created_user_id     integer,
    updated_datetime    timestamp with time zone,
    updated_user_id     integer,
    CONSTRAINT fk_work_item_valorization_form_work_item
        FOREIGN KEY (work_item_id) REFERENCES public.work_item(work_item_id)
);

CREATE INDEX IF NOT EXISTS ix_work_item_valorization_form_work_item_id
    ON public.work_item_valorization_form (work_item_id);

-- ─────────────────────────────────────────────────────────────────────────
-- 2) Población de datos a partir de las imágenes
--    (partida, sort_order, porcentaje, concepto)
-- ─────────────────────────────────────────────────────────────────────────
WITH forms(partida, sort_order, percentage, concept) AS (
    VALUES
    -- TABIQUERIA
    ('TABIQUERIA',            0, 40, 'por material'),
    ('TABIQUERIA',            1, 35, 'por asentado'),
    ('TABIQUERIA',            2, 15, 'por solaqueo'),
    ('TABIQUERIA',            3, 10, 'por levantamiento de observaciones'),
    -- ENCHAPE
    ('ENCHAPE',               0, 80, 'por colocación'),
    ('ENCHAPE',               1, 10, 'por fragua'),
    ('ENCHAPE',               2, 10, 'por levantamiento de observaciones'),
    -- PINTURA INTERIOR
    ('PINTURA INTERIOR',      0, 10, 'por imprimado'),
    ('PINTURA INTERIOR',      1, 10, 'por empaste grueso'),
    ('PINTURA INTERIOR',      2, 10, 'por empaste fino'),
    ('PINTURA INTERIOR',      3, 20, 'por primera mano'),
    ('PINTURA INTERIOR',      4, 10, 'por levantamiento de observaciones de primera mano'),
    ('PINTURA INTERIOR',      5, 30, 'por segunda mano'),
    ('PINTURA INTERIOR',      6, 10, 'por levantamiento de observaciones de segunda mano'),
    -- CARPINTERIA METALICA
    ('CARPINTERIA METALICA',  0, 60, 'por instalación'),
    ('CARPINTERIA METALICA',  1, 30, 'por acabado'),
    ('CARPINTERIA METALICA',  2, 10, 'por levantamiento de observaciones'),
    -- PISO LAMINADO
    ('PISO LAMINADO',         0, 90, 'por instalación'),
    ('PISO LAMINADO',         1, 10, 'por levantamiento de observaciones'),
    -- PAPEL MURAL
    ('PAPEL MURAL',           0, 30, 'por empaste de 02 manos'),
    ('PAPEL MURAL',           1, 10, 'por liberación de calidad de empaste (colocación de pegamento)'),
    ('PAPEL MURAL',           2, 35, 'por colocación de papel'),
    ('PAPEL MURAL',           3, 10, 'por entrega de calidad (papel)'),
    ('PAPEL MURAL',           4, 15, 'por levantamiento de observaciones'),
    -- LIMPIEZA FINA  (⚠ la imagen tiene 2 columnas DPTOS/AACC; se usan los % de DPTOS.
    --                  Para AACC: 1era 50%, 2da 50%. Ajustar si aplica por tipo.)
    ('LIMPIEZA FINA',         0, 30, 'por primera limpieza'),
    ('LIMPIEZA FINA',         1, 35, 'por segunda limpieza'),
    ('LIMPIEZA FINA',         2, 35, 'por tercera limpieza'),
    -- VIDRIOS
    ('VIDRIOS',               0, 75, 'por instalación'),
    ('VIDRIOS',               1, 15, 'por sellado y accesorios'),
    ('VIDRIOS',               2, 10, 'por levantamiento de observaciones'),
    -- PINTURA FACHADA
    ('PINTURA FACHADA',       0,  5, 'por imprimado'),
    ('PINTURA FACHADA',       1, 20, 'por empaste grueso'),
    ('PINTURA FACHADA',       2, 20, 'por empaste fino'),
    ('PINTURA FACHADA',       3, 20, 'por primera mano'),
    ('PINTURA FACHADA',       4, 25, 'por segunda mano'),
    ('PINTURA FACHADA',       5, 10, 'por levantamiento de observaciones de segunda mano'),
    -- GRANITO
    ('GRANITO',               0, 40, 'por material'),
    ('GRANITO',               1, 30, 'por instalación'),
    ('GRANITO',               2, 20, 'por sellado / siliconeado'),
    ('GRANITO',               3, 10, 'por levantamiento de observaciones'),
    -- DRYWALL
    ('DRYWALL',               0, 35, 'por metalado'),
    ('DRYWALL',               1, 40, 'por planchado'),
    ('DRYWALL',               2, 15, 'por masillado'),
    ('DRYWALL',               3, 10, 'por levantamiento de observaciones'),
    -- MUEBLES DE MELAMINE
    ('MUEBLES DE MELAMINE',   0, 50, 'por estructura instalada'),
    ('MUEBLES DE MELAMINE',   1, 10, 'por instalación de puertas'),
    ('MUEBLES DE MELAMINE',   2, 25, 'por acabado y limpieza'),
    ('MUEBLES DE MELAMINE',   3, 15, 'por levantamiento de observaciones'),
    -- PUERTAS
    ('PUERTAS',               0, 60, 'por instalación'),
    ('PUERTAS',               1, 20, 'por acabado'),
    ('PUERTAS',               2, 20, 'por levantamiento de observaciones'),
    -- PINTURA DE TRAFICO
    ('PINTURA DE TRAFICO',    0, 70, 'por pintura'),
    ('PINTURA DE TRAFICO',    1, 30, 'por levantamiento de observaciones'),
    -- PINTURA DE CAL NIEVE
    ('PINTURA DE CAL NIEVE',  0, 60, 'por primera mano'),
    ('PINTURA DE CAL NIEVE',  1, 30, 'por segunda mano'),
    ('PINTURA DE CAL NIEVE',  2, 10, 'por levantamiento de observaciones'),
    -- PUERTAS CORTAFUEGO
    ('PUERTAS CORTAFUEGO',    0, 70, 'por instalación'),
    ('PUERTAS CORTAFUEGO',    1, 20, 'por sellado'),
    ('PUERTAS CORTAFUEGO',    2, 10, 'por levantamiento de observaciones'),
    -- PUERTAS DE MONTANTES
    ('PUERTAS DE MONTANTES',  0, 60, 'por instalación'),
    ('PUERTAS DE MONTANTES',  1, 30, 'por acabado'),
    ('PUERTAS DE MONTANTES',  2, 10, 'por levantamiento de observaciones'),
    -- PREPARACION DE MUROS (imagen 3)
    ('PREPARACION DE MUROS',  0, 15, 'por imprimado'),
    ('PREPARACION DE MUROS',  1, 65, 'por empastado'),
    ('PREPARACION DE MUROS',  2, 15, 'por levantamiento de observaciones'),
    ('PREPARACION DE MUROS',  3,  5, 'por lijado'),
    -- INSTALACION DE PAPEL MURAL (imagen 4)
    ('INSTALACION DE PAPEL MURAL', 0, 60, 'por instalación'),
    ('INSTALACION DE PAPEL MURAL', 1, 30, 'por entrega'),
    ('INSTALACION DE PAPEL MURAL', 2, 10, 'por levantamiento de observaciones')
)
INSERT INTO public.work_item_valorization_form
    (work_item_id, concept, percentage, sort_order, state, created_datetime, created_user_id)
SELECT wi.work_item_id, f.concept, f.percentage, f.sort_order, true, now(), NULL
FROM forms f
JOIN public.work_item wi
  ON upper(translate(btrim(wi.work_item_description),
                      'ÁÉÍÓÚÜÑáéíóúüñ', 'AEIOUUNaeiouun'))
   = upper(translate(btrim(f.partida),
                      'ÁÉÍÓÚÜÑáéíóúüñ', 'AEIOUUNaeiouun'))
 AND wi.state = true
WHERE NOT EXISTS (
    SELECT 1 FROM public.work_item_valorization_form v
    WHERE v.work_item_id = wi.work_item_id AND v.state = true
);

-- Diagnóstico opcional: ¿qué etiquetas de la imagen NO encontraron partida?
-- (Revisar nombres antes de dar por completa la población.)
--
-- SELECT DISTINCT f.partida
-- FROM (VALUES ('TABIQUERIA'),('ENCHAPE'),('PINTURA INTERIOR'),('CARPINTERIA METALICA'),
--              ('PISO LAMINADO'),('PAPEL MURAL'),('LIMPIEZA FINA'),('VIDRIOS'),
--              ('PINTURA FACHADA'),('GRANITO'),('DRYWALL'),('MUEBLES DE MELAMINE'),
--              ('PUERTAS'),('PINTURA DE TRAFICO'),('PINTURA DE CAL NIEVE'),
--              ('PUERTAS CORTAFUEGO'),('PUERTAS DE MONTANTES'),('PREPARACION DE MUROS'),
--              ('INSTALACION DE PAPEL MURAL')) AS f(partida)
-- WHERE NOT EXISTS (
--     SELECT 1 FROM public.work_item wi
--     WHERE wi.state = true
--       AND upper(translate(btrim(wi.work_item_description),'ÁÉÍÓÚÜÑáéíóúüñ','AEIOUUNaeiouun'))
--         = upper(translate(btrim(f.partida),'ÁÉÍÓÚÜÑáéíóúüñ','AEIOUUNaeiouun'))
-- );
