-- ============================================================================
-- Asignación de Residente de Obra: Martín Véliz -> SAUCE ZEN
--
-- Habilita el proyecto "SAUCE ZEN" en el selector de proyectos de las pantallas
-- de Planeamiento BIM (Carga Diaria / Configuración Inicial / Bloqueos), las cuales
-- consumen GET /api/v1/projectResident/projects (ProjectResidentRepository.GetProjectsDescription).
--
-- Consulta base exige INNER JOIN con project_resident (active=true, state=true).
--
-- Idempotente vía ON CONFLICT (project_id, user_id) DO UPDATE.
-- Cumple regla D3 (sin IDs hardcodeados, resuelve via subqueries).
-- ============================================================================

INSERT INTO project_resident (project_id, user_id, created_user_id, active, state)
SELECT 
    p.project_id,
    u.user_id,
    COALESCE(admin.user_id, 1),
    TRUE,
    TRUE
FROM project p
CROSS JOIN app_user u
LEFT JOIN app_user admin ON admin.email = 'vcolonio@abril.pe'
WHERE p.project_description = 'SAUCE ZEN' AND p.state = TRUE
  AND u.email = 'mveliz@abril.pe'
ON CONFLICT (project_id, user_id) 
DO UPDATE SET 
    active = TRUE,
    state = TRUE,
    updated_date_time = NOW() AT TIME ZONE 'UTC',
    updated_user_id = EXCLUDED.created_user_id;

-- ── Verificación ────────────────────────────────────────────────────────────
-- SELECT pr.project_resident_id, p.project_description, per.full_name, u.email, pr.active, pr.state
-- FROM project_resident pr
-- JOIN project p ON pr.project_id = p.project_id
-- JOIN app_user u ON pr.user_id = u.user_id
-- JOIN person per ON u.user_id = per.user_id
-- WHERE p.project_description = 'SAUCE ZEN';
