-- Auditoría: inspecciones históricas donde inspector_nombre (texto libre) no matchea
-- el nombre oficial del trabajador (person.full_name / workers.apellido_nombre).
--
-- Contexto: antes de la corrección en InspeccionController/Service/Repository,
-- created_by NUNCA se guardaba (para nadie, staff o contratista). El "Desempeño
-- Supervisor" (DesempenoSupervisorRepository.cs) hace fallback a comparar
-- inspector_nombre contra el nombre oficial del supervisor, pero con IGUALDAD
-- EXACTA de string (nombre.ToUpper().Trim()) — el mismo patrón se repite para
-- RAC (reportante_nombre) y OPT (observador_nombre). Cualquier variación (orden
-- de palabras, tildes, doble espacio, nombre de pila incompleto) hace que la
-- inspección/reporte no cuente para ese supervisor.
--
-- Este script NO modifica nada. Solo genera la lista de mismatches para revisión
-- manual antes de decidir qué corregir (UPDATE inspector_nombre, o poblar
-- created_by retroactivamente donde el match sea confiable).
--
-- Requiere pg_trgm (ya instalado, ver migración AddPgTrgmExtension) y unaccent.

CREATE EXTENSION IF NOT EXISTS unaccent;

WITH staff_activo AS (
    SELECT
        w.id                                                    AS worker_id,
        COALESCE(
            NULLIF(TRIM(p.full_name), ''),
            NULLIF(TRIM(CONCAT_WS(' ', p.first_names, p.first_last_name, p.second_last_name)), ''),
            w.apellido_nombre
        )                                                       AS nombre_oficial
    FROM workers w
    LEFT JOIN person p ON p.person_id = w.person_id
    WHERE w.obra_oficina = 'Staff'
      AND w.estado = 'ACTIVO'
),
staff_norm AS (
    SELECT
        worker_id,
        nombre_oficial,
        -- normalizado para comparar: minúsculas, sin tildes, espacios colapsados
        REGEXP_REPLACE(LOWER(unaccent(nombre_oficial)), '\s+', ' ', 'g') AS nombre_norm,
        -- conjunto de tokens (palabras) para comparar sin importar el orden
        (
            SELECT ARRAY_AGG(DISTINCT t ORDER BY t)
            FROM UNNEST(STRING_TO_ARRAY(REGEXP_REPLACE(LOWER(unaccent(nombre_oficial)), '\s+', ' ', 'g'), ' ')) AS t
            WHERE t <> ''
        ) AS tokens
    FROM staff_activo
    WHERE nombre_oficial IS NOT NULL
),
inspecciones_candidatas AS (
    SELECT
        i.id            AS inspeccion_id,
        i.proyecto_id,
        i.fecha,
        i.estado,
        i.inspector_nombre,
        REGEXP_REPLACE(LOWER(unaccent(i.inspector_nombre)), '\s+', ' ', 'g') AS inspector_norm,
        (
            SELECT ARRAY_AGG(DISTINCT t ORDER BY t)
            FROM UNNEST(STRING_TO_ARRAY(REGEXP_REPLACE(LOWER(unaccent(i.inspector_nombre)), '\s+', ' ', 'g'), ' ')) AS t
            WHERE t <> ''
        ) AS inspector_tokens
    FROM ssoma_inspeccion i
    WHERE i.created_by IS NULL
      AND i.inspector_nombre IS NOT NULL
      AND TRIM(i.inspector_nombre) <> ''
      AND i.estado <> 'Borrador'
),
matches AS (
    SELECT
        ic.inspeccion_id,
        ic.proyecto_id,
        ic.fecha,
        ic.inspector_nombre,
        sn.worker_id,
        sn.nombre_oficial,
        -- exacto tras normalizar (tildes/espacios/mayúsculas) — ya lo captura el código actual? NO:
        -- el código actual compara con ToUpper().Trim() SIN sacar tildes ni colapsar espacios dobles.
        (ic.inspector_norm = sn.nombre_norm)                       AS match_exacto_normalizado,
        -- mismo conjunto de palabras, sin importar el orden (cubre "Apellido Nombre" vs "Nombre Apellido")
        (ic.inspector_tokens = sn.tokens)                          AS match_mismo_set_tokens,
        -- todas las palabras del nombre oficial están en el texto libre (cubre nombre incompleto / con cargo pegado)
        (sn.tokens <@ ic.inspector_tokens OR ic.inspector_tokens <@ sn.tokens) AS match_subset_tokens,
        SIMILARITY(ic.inspector_norm, sn.nombre_norm)              AS score_trigram
    FROM inspecciones_candidatas ic
    JOIN staff_norm sn
      ON SIMILARITY(ic.inspector_norm, sn.nombre_norm) > 0.35
      OR ic.inspector_tokens && sn.tokens   -- comparten al menos un token (evita perder candidatos con score bajo por nombres cortos)
),
mejor_match AS (
    SELECT DISTINCT ON (inspeccion_id)
        *
    FROM matches
    ORDER BY
        inspeccion_id,
        match_mismo_set_tokens DESC,
        match_subset_tokens DESC,
        score_trigram DESC
)
SELECT
    ic.inspeccion_id,
    ic.proyecto_id,
    ic.fecha,
    ic.inspector_nombre                              AS inspector_nombre_libre,
    mm.worker_id                                      AS worker_id_sugerido,
    mm.nombre_oficial                                 AS nombre_oficial_sugerido,
    ROUND(mm.score_trigram::numeric, 3)               AS score_trigram,
    mm.match_exacto_normalizado,
    mm.match_mismo_set_tokens,
    mm.match_subset_tokens,
    CASE
        WHEN mm.worker_id IS NULL THEN 'SIN_CANDIDATO'
        WHEN mm.match_exacto_normalizado THEN 'ALTA_CONFIANZA_exacto_normalizado'
        WHEN mm.match_mismo_set_tokens THEN 'ALTA_CONFIANZA_mismas_palabras'
        WHEN mm.match_subset_tokens AND mm.score_trigram > 0.6 THEN 'MEDIA_CONFIANZA_revisar'
        ELSE 'BAJA_CONFIANZA_revisar_manual'
    END AS confianza
FROM inspecciones_candidatas ic
LEFT JOIN mejor_match mm ON mm.inspeccion_id = ic.inspeccion_id
ORDER BY
    CASE
        WHEN mm.worker_id IS NULL THEN 0
        ELSE 1
    END,
    confianza,
    ic.fecha DESC;

-- Nota: mismo patrón de bug (comparación exacta de nombre libre) aplica también a
-- ssoma_rac.reportante_nombre y ssoma_opt.observador_nombre — si esta auditoría
-- confirma mismatches relevantes en inspecciones, vale la pena replicar este
-- script para esas dos tablas también.
