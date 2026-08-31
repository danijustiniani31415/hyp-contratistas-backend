-- Motor de ratios historicos para HH y N Trabajadores (drivers de proyecto),
-- analogo a ss_ratio_proyecto pero para los drivers del proyecto en si (no materiales).
-- No segmenta por tipo de proyecto (a proposito, los proyectos de esta cartera son
-- homogeneos). El outlier es solo una bandera informativa: incluido_manual es la
-- unica autoridad real sobre que proyecto entra al calculo del ratio recomendado,
-- igual que incluido_manual_ratio/precio en ss_ratio_proyecto.

CREATE TABLE ss_ratio_proyecto_driver (
    id              SERIAL PRIMARY KEY,
    tipo_driver     VARCHAR(20)     NOT NULL,      -- 'HH' | 'TRABAJADORES'
    project_id      INTEGER         NOT NULL REFERENCES project(project_id),
    area_techada    NUMERIC         NOT NULL,      -- denominador (m2)
    cantidad        NUMERIC         NOT NULL,      -- HH total o N trabajadores (numerador)
    ratio           NUMERIC         NOT NULL,      -- cantidad / area_techada
    es_outlier      BOOLEAN         NOT NULL DEFAULT FALSE,
    incluido_manual BOOLEAN         NOT NULL DEFAULT TRUE,
    calculado_en    TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    UNIQUE (tipo_driver, project_id)
);

CREATE INDEX ix_ss_ratio_proyecto_driver_tipo ON ss_ratio_proyecto_driver (tipo_driver);
