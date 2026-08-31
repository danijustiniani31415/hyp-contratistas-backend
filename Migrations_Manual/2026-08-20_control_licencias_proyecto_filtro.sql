-- ============================================================================
-- Control de Licencias: registra la funcionalidad en el catálogo
-- proyecto_filtro_funcionalidad (permite ocultar el módulo por proyecto desde
-- el admin de filtros, igual que VECINOS_GESTION / VECINOS_CROQUIS).
-- Ejecutar manualmente en pgAdmin.
-- ============================================================================

INSERT INTO proyecto_filtro_funcionalidad (id, codigo, nombre) VALUES
    (15, 'CONTROL_LICENCIAS', 'Vecinos — Control de Licencias')
ON CONFLICT (id) DO NOTHING;

SELECT setval(
    pg_get_serial_sequence('proyecto_filtro_funcionalidad', 'id'),
    GREATEST((SELECT MAX(id) FROM proyecto_filtro_funcionalidad), 1)
);
