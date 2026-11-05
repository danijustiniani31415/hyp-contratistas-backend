-- Guías de remisión: datos maestros de transporte (se registran una vez y se eligen en cada guía).
-- La guía sigue guardando su propia copia de los datos (placa, conductor, transportista): estos maestros
-- solo rellenan el formulario, así las guías ya emitidas no cambian si un maestro se edita después.

CREATE TABLE IF NOT EXISTS lb_guia_transportista (
    id            BIGSERIAL PRIMARY KEY,
    ruc           VARCHAR(11)  NOT NULL UNIQUE,
    razon_social  VARCHAR(200) NOT NULL,
    nro_mtc       VARCHAR(30),                       -- registro MTC de la empresa de transporte
    activo        BOOLEAN      NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS lb_guia_vehiculo (
    id               BIGSERIAL PRIMARY KEY,
    placa            VARCHAR(7)  NOT NULL UNIQUE,    -- normalizada: mayúsculas, sin guiones ni espacios
    nro_circulacion  VARCHAR(30),                    -- tarjeta de circulación / certificado de habilitación
    activo           BOOLEAN     NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS lb_guia_conductor (
    id         BIGSERIAL PRIMARY KEY,
    tipo_doc   VARCHAR(1)   NOT NULL DEFAULT '1',    -- catálogo 06 SUNAT: 1 = DNI
    num_doc    VARCHAR(15)  NOT NULL UNIQUE,
    nombres    VARCHAR(100) NOT NULL,
    apellidos  VARCHAR(100) NOT NULL,
    licencia   VARCHAR(20)  NOT NULL,
    activo     BOOLEAN      NOT NULL DEFAULT TRUE
);

-- La guía guarda apellidos aparte: SUNAT los pide separados de los nombres.
ALTER TABLE lb_guia_remision ADD COLUMN IF NOT EXISTS conductor_apellidos VARCHAR(100);
