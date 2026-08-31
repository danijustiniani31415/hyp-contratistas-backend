-- Carga complementaria de Horas Hombre por planilla/Tareo semanal (Excel), para completar el
-- driver HH del presupuesto de materiales cuando el Tareo de Control de Acceso (ss_tareo) no
-- arranca junto con el proyecto — ver comentario en RatioDriverRepository.ObtenerHhRealPorProyectoAsync.
-- Mismo patrón de carga acumulativa idempotente que ss_consumo_linea (2026-08-28_ss_consumo_linea_carga_incremental.sql):
-- identidad de línea = proyecto+año+semana+trabajador+ocupación+partida+ocurrencia, para poder
-- diferenciar altas/regularizaciones/bajas entre una carga y la siguiente sin duplicar HH.

CREATE TABLE ss_hh_carga (
    id                  serial PRIMARY KEY,
    project_id          int NOT NULL REFERENCES project(project_id),
    nombre_archivo      varchar(255) NOT NULL,
    hash_archivo        varchar(64) NOT NULL,
    anio_min            int NOT NULL,
    semana_min          int NOT NULL,
    anio_max            int NOT NULL,
    semana_max          int NOT NULL,
    total_lineas        int NOT NULL DEFAULT 0,
    lineas_nuevas       int NOT NULL DEFAULT 0,
    lineas_actualizadas int NOT NULL DEFAULT 0,
    lineas_eliminadas   int NOT NULL DEFAULT 0,
    estado              varchar(20) NOT NULL DEFAULT 'ACTIVA',
    subido_por          int NOT NULL,
    creado_en           timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE ss_hh_carga_linea (
    id                  bigserial PRIMARY KEY,
    carga_id            int NOT NULL REFERENCES ss_hh_carga(id),
    project_id          int NOT NULL REFERENCES project(project_id),
    anio                int NOT NULL,
    semana_num          int NOT NULL,
    trabajador           varchar(200) NOT NULL,
    ocupacion           varchar(150),
    partida_control     varchar(150),
    horas_laboradas     numeric(10,2) NOT NULL,
    costo_hh_normal     numeric(12,4),
    parcial             numeric(12,2),
    ocurrencia          int NOT NULL DEFAULT 1,
    activo              boolean NOT NULL DEFAULT true,
    motivo_inactivo     varchar(200),
    actualizado_en      timestamptz,
    creado_en           timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX ux_hh_carga_linea_clave_natural
    ON ss_hh_carga_linea (project_id, anio, semana_num, trabajador, ocupacion, partida_control, ocurrencia)
    WHERE activo = true;

CREATE INDEX ix_hh_carga_linea_project_activo ON ss_hh_carga_linea (project_id, activo);
