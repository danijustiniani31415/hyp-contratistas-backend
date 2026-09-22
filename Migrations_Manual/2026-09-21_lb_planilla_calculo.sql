-- Fase 3 del motor de Planillas: conceptos de planilla (reglas configurables, estilo salary.rule de
-- Odoo — NUNCA se hardcodean tasas de AFP/ONP/EsSalud en código, las define el usuario desde el
-- frontend en "Conceptos de planilla"), períodos y el detalle calculado por persona.
-- Idempotente: seguro correr más de una vez.

CREATE TABLE IF NOT EXISTS lb_concepto_planilla (
    id              SERIAL PRIMARY KEY,
    codigo          VARCHAR(30) NOT NULL UNIQUE,
    nombre          VARCHAR(120) NOT NULL,
    tipo            VARCHAR(20) NOT NULL CHECK (tipo IN ('INGRESO', 'DESCUENTO', 'APORTE_EMPLEADOR')),
    -- NULL = aplica a ambas categorías (obrero y empleado).
    categoria_laboral VARCHAR(20) NULL,
    -- FIJO: valor tal cual. PORCENTAJE_SUELDO: valor% del sueldo_base (empleado).
    -- PORCENTAJE_JORNAL: valor% de (jornal * dias_trabajados) (obrero).
    -- POR_DIA_TAREO: valor soles por cada día trabajado en el tareo del período.
    forma_calculo   VARCHAR(30) NOT NULL CHECK (forma_calculo IN ('FIJO', 'PORCENTAJE_SUELDO', 'PORCENTAJE_JORNAL', 'POR_DIA_TAREO')),
    valor           NUMERIC(12, 4) NOT NULL DEFAULT 0,
    orden           INT NOT NULL DEFAULT 0,
    activo          BOOLEAN NOT NULL DEFAULT TRUE,
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS lb_planilla_periodo (
    id                              SERIAL PRIMARY KEY,
    anio                            INT NOT NULL,
    mes                             INT NOT NULL CHECK (mes BETWEEN 1 AND 12),
    -- NULL = todos los proyectos en un solo período.
    proyecto_id                     INT NULL REFERENCES lb_proyecto(id),
    estado                          VARCHAR(20) NOT NULL DEFAULT 'BORRADOR' CHECK (estado IN ('BORRADOR', 'CALCULADO', 'CERRADO')),
    calculado_en                    TIMESTAMPTZ NULL,
    calculado_por_usuario_sistema_id BIGINT NULL,
    cerrado_en                      TIMESTAMPTZ NULL,
    creado_en                       TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_lb_planilla_periodo_anio_mes ON lb_planilla_periodo (anio, mes);

CREATE TABLE IF NOT EXISTS lb_planilla_detalle (
    id                  BIGSERIAL PRIMARY KEY,
    periodo_id          INT NOT NULL REFERENCES lb_planilla_periodo(id),
    persona_id          INT NOT NULL REFERENCES lb_persona(id),
    -- Snapshot al momento del cálculo — si luego cambian los datos de planilla de la persona, el
    -- histórico de esta planilla ya calculada no se ve afectado.
    categoria_laboral   VARCHAR(20) NULL,
    sueldo_base         NUMERIC(10, 2) NULL,
    jornal              NUMERIC(10, 2) NULL,
    dias_trabajados     NUMERIC(5, 2) NOT NULL DEFAULT 0,
    dias_falta          NUMERIC(5, 2) NOT NULL DEFAULT 0,
    total_ingresos      NUMERIC(10, 2) NOT NULL DEFAULT 0,
    total_descuentos    NUMERIC(10, 2) NOT NULL DEFAULT 0,
    total_aportes_empleador NUMERIC(10, 2) NOT NULL DEFAULT 0,
    neto_pagar          NUMERIC(10, 2) NOT NULL DEFAULT 0,
    creado_en           TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (periodo_id, persona_id)
);

CREATE TABLE IF NOT EXISTS lb_planilla_detalle_concepto (
    id                      BIGSERIAL PRIMARY KEY,
    detalle_id              BIGINT NOT NULL REFERENCES lb_planilla_detalle(id) ON DELETE CASCADE,
    concepto_planilla_id    INT NOT NULL REFERENCES lb_concepto_planilla(id),
    -- Snapshot del nombre/tipo — si el concepto cambia de nombre después, la línea histórica no cambia.
    concepto_nombre         VARCHAR(120) NOT NULL,
    tipo                    VARCHAR(20) NOT NULL,
    monto                   NUMERIC(10, 2) NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_lb_planilla_detalle_concepto_detalle ON lb_planilla_detalle_concepto (detalle_id);

-- Permisos nuevos — mismo criterio del resto del sistema: no se hardcodea el rol en el backend,
-- se asigna acá vía lb_rol_permiso. Solo ADMIN y GERENTE_GENERAL por ahora (nivel gerencial); se
-- puede ampliar a un rol propio de RRHH más adelante desde /security/roles sin tocar código.
INSERT INTO lb_permiso (codigo, descripcion)
SELECT codigo, descripcion FROM (VALUES
    ('PLANILLA_CONFIGURAR', 'Crear y editar conceptos de planilla (reglas de cálculo)'),
    ('PLANILLA_CALCULAR',   'Crear períodos de planilla y calcular/cerrar boletas')
) AS nuevos(codigo, descripcion)
WHERE NOT EXISTS (SELECT 1 FROM lb_permiso WHERE lb_permiso.codigo = nuevos.codigo);

INSERT INTO lb_rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id FROM lb_rol r, lb_permiso p
WHERE r.codigo IN ('ADMIN', 'GERENTE_GENERAL')
  AND p.codigo IN ('PLANILLA_CONFIGURAR', 'PLANILLA_CALCULAR')
  AND NOT EXISTS (
      SELECT 1 FROM lb_rol_permiso rp WHERE rp.rol_id = r.id AND rp.permiso_id = p.id
  );
