# CONTEXT_LOGISTICA.md — Proyecto "Las Bravas"
## ERP Logístico para HP Constructores Generales

> **v3** — revisado a nivel de diseño antes de implementar Fase 1, para no pasar un mes corrigiendo
> errores de esquema en producción. Los cambios respecto a v1 están marcados con **[REVISADO]** y
> justificados citando errores reales ya vividos en el repo hermano (Abril-Backend), del cual este
> proyecto es fork/plantilla. Las dos decisiones que quedaban abiertas en v2 (vínculo laboral único
> por persona, y estrategia de concurrencia del stock) ya fueron resueltas — ver **[DECIDIDO]**
> en las secciones 4.3 y 7.

Este documento es la fuente única de verdad para Claude Code. Cada sesión debe leer SOLO la
sección de la fase/módulo que va a implementar (indicar en el prompt: "Lee CONTEXT_LOGISTICA.md
sección X e implementa solo [tarea]"). Meta: 5-10k tokens por sesión, 0 errores, sin exploración
innecesaria.

## 0. PROTOCOLO ANTIDESASTRE (obligatorio en toda sesión)

Execution Runtime primero: antes de escribir código, auditar DB real (queries a
`information_schema`) + estructura de frontend/backend existente. **NUNCA lanzar agentes
automáticos ni exploración recursiva.** Solo: queries puntuales a `information_schema`, búsqueda de
archivos específicos, grep simple. El resultado de esta fase es metadata JSON — 0 código.

Spec verificada: confirmar columnas exactas de DB antes de generar entidades/DTOs (no asumir
nombres de columnas).

Instrucción directa: 5-10 líneas, sin ambigüedad, una tarea por sesión.

Orden fijo: Runtime audit → spec → Claude Code implementa → verificar.

Errores: mostrar el error exacto → auditar la causa puntual → corregir. Nunca "explorar" el
proyecto completo para debuggear.

**[REVISADO] Antes de modelar cualquier entidad nueva, revisar si ya existe un patrón equivalente
resuelto en este mismo repo** (heredado de Abril-Backend): `Infrastructure/Models/Worker.cs`,
`WorkerVinculacion.cs`, `Shared/Services/Jwt/JWTService.cs`, `Infrastructure/Repositories/UserRepository.cs`
(usa `IPasswordHasher<T>` de ASP.NET Identity, no reinventar hashing). Estos archivos tienen
comentarios que documentan errores de producción reales — léelos antes de repetirlos.

Comando de referencia:

```
claude --model claude-sonnet-4-6 --dangerously-skip-permissions
```

## 1. CONTEXTO DEL NEGOCIO

HP Constructores Generales brinda servicios de perforación de chimeneas en socavón (minería
subterránea). Sede administrativa: Lima. Actualmente 3 proyectos activos (escalable a 6-7). Cada
proyecto tiene su propia gestión de almacén/pañol, pero el despacho general parte de Lima (sin
transferencias directas entre proyectos por ahora, aunque el modelo de datos debe soportarlo a
futuro).

Es un proyecto totalmente nuevo e independiente de Plataforma Abril (Abril Grupo Inmobiliario).
Comparte el patrón arquitectónico y el nivel de profesionalismo, pero es otro sistema, otra base de
datos, otro dominio.

Restricción real de presupuesto: solo existe una casilla corporativa paga,
`admin@hpconstructoresgenerales.com`, usada por la contadora (rol LOCADOR/administrativo). El
Gerente General y el resto del personal usan correos personales (Gmail/Hotmail) como contacto — el
login del sistema no depende del dominio corporativo; cualquier email (corporativo o personal)
sirve como `email_login` en `lb_usuario_sistema`. Esto debe quedar así de diseño: NO asumir que
solo emails `@hpconstructoresgenerales.com` son válidos para iniciar sesión o recibir
notificaciones.

## 2. STACK TÉCNICO

- Frontend: Angular 21 (repo `hyp-contratistas-frontend` — confirmar puerto para no chocar con
  Abril-Frontend en la misma máquina de desarrollo)
- Backend: .NET 10 (repo `hyp-contratistas-backend`)
- Base de datos: PostgreSQL. **[REVISADO]** `appsettings.Local.json` ya tiene una instancia local
  configurada (`Host=localhost;Database=hyp_local`) — confirmar solo si producción será Aiven o
  servidor propio, el entorno local ya está resuelto.
- Documentos: SharePoint (site YA EXISTE — pendiente confirmar nombre exacto y sacar SiteId con
  `/_api/site/id`, mismo patrón que Abril)
- Terminales separadas FRONTEND/BACKEND igual que en Abril — toda instrucción a Claude Code debe
  indicar a cuál corresponde.
- Diseño mobile-first (375px) igual que Abril: el personal de almacén y campo va a usar celular,
  no PC, para entregas de EPP y control de acceso.
- **[REVISADO] Convención EF Core obligatoria**: como todas las tablas nuevas usan prefijo `lb_`
  en snake_case, la convención automática de nombres de EF Core NO va a producir esos nombres
  solo por el nombre de la clase C#. Cada entidad nueva necesita mapeo explícito
  (`[Table("lb_persona")]`, `[Column("numero_documento")]`), igual que ya hace `Worker.cs`. No
  asumir que el `UseSnakeCaseNamingConvention()` ya configurado en `AppDbContext` alcanza solo.
- Optimización respecto a Abril: se toma el patrón general (capas, convenciones, protocolo
  antidesastre), pero el modelo de personas/roles NO copia el de Abril tal cual — se rediseña
  desde cero como se detalla en la sección 4, corrigiendo puntualmente los errores que el modelo
  `Worker`/`WorkerVinculacion` de Abril ya sufrió y documentó.

## 3. MÓDULOS Y ORDEN DE IMPLEMENTACIÓN

**FASE 1 — Cimientos**
1. Modelo de Personas (persona → vínculo laboral → usuario sistema → rol/permiso/scope)
2. Catálogo Maestro (productos, categorías, fuzzy search)
3. Almacén/Kardex multi-almacén

**FASE 2 — Operación diaria**
4. Pedidos (creación → aprobación Gerencia General → estados)
5. EPP (tallas, entrega individual, cargo digital)
6. Herramientas y Equipos (préstamo, hoja de ruta de retiro)

**FASE 3 — Cadena completa**
7. Compras (orden de compra cuando no hay stock)
8. Guías de Remisión SUNAT
9. Motor de Alertas y Notificaciones (el modelo de reglas se diseña ya en la sección 5, para no
   rehacer el modelo de roles cuando llegue esta fase)
10. Documentos/SharePoint (bibliotecas enlazadas)

## 4. MODELO DE PERSONAS Y ROLES (la base de TODO el sistema)

### 4.1 Por qué 4 capas y no una tabla usuario con un campo rol

Un ERP profesional (SAP, Oracle, Odoo) nunca mezcla "quién es la persona" con "qué puede hacer en
el sistema" con "en qué proyecto/almacén aplica". Motivos concretos para HP Constructores:

- Un trabajador puede ser CONTRATISTA en Las Bravas 6 meses, irse, y volver 8 meses después como
  PLANILLA — es la misma persona humana, con historial de EPP/EMO que debe mantenerse trazable a
  través de ambos vínculos.
- La contadora tiene usuario del sistema (login), pero NO es "personal de obra": no le aplican
  EMOs, no recibe EPP, no aparece en control de acceso a socavón. Su tipo de vínculo (LOCADOR)
  determina qué módulos le aplican, automáticamente, sin reglas hardcodeadas por persona.
- El rol (qué permisos tiene: Residente, Almacenero, Logística) es independiente del puesto/cargo
  (cómo se le llama organizacionalmente) y ambos son independientes del scope (a qué
  proyecto/almacén pertenece esa asignación).

**[REVISADO] Lección tomada de `Worker.cs`/`WorkerVinculacion.cs` (Abril):** ese modelo tuvo
exactamente el mismo problema de "vínculo único sin historial" al principio — `fecha_ingreso`/
`fecha_retiro` como columnas planas en `workers` obligaban a pisarlas en cada reingreso, generando
fichas duplicadas que después hubo que fusionar a mano
(`Migrations_Manual/2026-08-25_workers_fusion_fichas_duplicadas.sql`). El diseño de 4 capas de
abajo ya evita ese error desde el día 1 porque `lb_vinculo_laboral` es una tabla de historial, no
una columna en `lb_persona` — no cambiar esto por "simplificar".

### 4.2 Las 4 capas

```
persona            → dato humano único, no cambia aunque cambie su relación laboral
vinculo_laboral     → el tipo de relación con la empresa (con vigencia en el tiempo)
usuario_sistema     → credenciales de login (solo si esa persona necesita loguear)
usuario_asignacion  → rol + permiso + scope (proyecto/almacén) + vigencia
```

### 4.3 DDL — Fase 1 (Modelo de Personas)

```sql
-- ============================================
-- CAPA 1: PERSONA (dato humano único)
-- ============================================
CREATE TABLE lb_persona (
    id              SERIAL PRIMARY KEY, -- [REVISADO] SERIAL (int), no BIGSERIAL: consistente con
                                         -- la convención de IDs del resto del codebase (Worker.Id
                                         -- es int). No hay volumen que justifique bigint.
    nombres         VARCHAR(150) NOT NULL,
    apellidos       VARCHAR(150) NOT NULL,
    tipo_documento  VARCHAR(20) NOT NULL DEFAULT 'DNI', -- DNI, CE, PASAPORTE
    numero_documento VARCHAR(20) NOT NULL,
    fecha_nacimiento DATE,
    telefono        VARCHAR(20),
    email_personal  VARCHAR(150),
    foto_url        TEXT, -- referencia a SharePoint
    -- [REVISADO] soft delete desde el día 1: Worker.State se agregó DESPUÉS en Abril y su propio
    -- comentario advierte "sin el default=true, toda ficha nace eliminada". Acá va desde el
    -- principio, con default correcto y filtro global de EF Core configurado en OnModelCreating
    -- (HasQueryFilter) para que ningún repo tenga que acordarse de filtrar manualmente.
    activo          BOOLEAN NOT NULL DEFAULT true,
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now(),
    actualizado_en  TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (tipo_documento, numero_documento)
);

-- ============================================
-- CATÁLOGO DE TIPOS DE VÍNCULO (abierto, editable desde admin)
-- ============================================
CREATE TABLE lb_tipo_vinculo (
    id              SMALLSERIAL PRIMARY KEY,
    codigo          VARCHAR(30) NOT NULL UNIQUE, -- PLANILLA, CONTRATISTA, LOCADOR, PRACTICANTE
    nombre          VARCHAR(80) NOT NULL,
    requiere_emo        BOOLEAN NOT NULL DEFAULT false,
    requiere_epp        BOOLEAN NOT NULL DEFAULT false,
    requiere_induccion  BOOLEAN NOT NULL DEFAULT false,
    requiere_sctr       BOOLEAN NOT NULL DEFAULT false,
    permite_acceso_obra BOOLEAN NOT NULL DEFAULT false,
    activo          BOOLEAN NOT NULL DEFAULT true
);

INSERT INTO lb_tipo_vinculo (codigo, nombre, requiere_emo, requiere_epp, requiere_induccion, requiere_sctr, permite_acceso_obra) VALUES
('PLANILLA',    'Personal en Planilla',        true,  true,  true,  true,  true),
('CONTRATISTA', 'Contratista / Tercero',       true,  true,  true,  true,  true),
('LOCADOR',     'Locador de Servicios',        false, false, false, false, false),
('PRACTICANTE', 'Practicante',                 true,  true,  true,  false, true);

-- ============================================
-- [REVISADO] CATÁLOGO DE EMPRESAS CONTRATISTAS (antes: texto libre en vinculo_laboral)
-- Abril ya resolvió este mismo problema con "Contributor" (FK desde WorkerVinculacion.EmpresaId).
-- Texto libre para el nombre de la contrata termina en "Constructora ABC SAC" vs "ABC S.A.C."
-- como si fueran empresas distintas. Nace mínima; se amplía en Fase 3 sin romper nada, y NO se
-- fusiona con el futuro "proveedor" de Compras — una da mano de obra, la otra vende materiales.
-- ============================================
CREATE TABLE lb_empresa_contratista (
    id              SERIAL PRIMARY KEY,
    razon_social    VARCHAR(200) NOT NULL,
    ruc             VARCHAR(11) UNIQUE,
    activo          BOOLEAN NOT NULL DEFAULT true
);

-- ============================================
-- [REVISADO] CATÁLOGO DE CARGOS (antes: texto libre en vinculo_laboral)
-- Lección directa de Worker.cs: el campo "puesto" en texto libre, en paralelo con una FK de
-- categoría, terminó contradiciéndose en el 26% de las fichas de producción de Abril (ver
-- comentario en Worker.cs líneas 48-65 y Migrations_Manual/categoria_puesto_unificados.sql).
-- Mismo patrón que Abril adoptó para arreglarlo: el NOMBRE puede editarse libre (presentación),
-- pero el nivel/categoría vive en la FK (lógica) — nunca en el texto.
-- ============================================
CREATE TABLE lb_cargo (
    id              SERIAL PRIMARY KEY,
    nombre          VARCHAR(100) NOT NULL UNIQUE, -- "Residente de Obra", "Jefe de Logística"
    activo          BOOLEAN NOT NULL DEFAULT true
);

-- ============================================
-- CAPA 2: VÍNCULO LABORAL (con vigencia — permite historial completo)
-- ============================================
CREATE TABLE lb_vinculo_laboral (
    id              SERIAL PRIMARY KEY,
    persona_id      INT NOT NULL REFERENCES lb_persona(id),
    tipo_vinculo_id SMALLINT NOT NULL REFERENCES lb_tipo_vinculo(id),
    empresa_contratista_id INT REFERENCES lb_empresa_contratista(id), -- [REVISADO] FK, no texto. NULL si es directo HP.
    cargo_id        INT REFERENCES lb_cargo(id),                      -- [REVISADO] FK, no texto.
    fecha_inicio    DATE NOT NULL,
    fecha_fin       DATE, -- NULL = vigente
    estado          VARCHAR(20) NOT NULL DEFAULT 'ACTIVO', -- ACTIVO, CESADO, SUSPENDIDO
    motivo_cese     VARCHAR(200), -- [REVISADO] igual que WorkerVinculacion.MotivoRetiro en Abril
    registrado_por_usuario_sistema_id BIGINT, -- [REVISADO] trazabilidad: quién dio de alta este vínculo
                                               -- (mismo patrón que WorkerVinculacion.RegistradoPorId)
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_vinculo_persona ON lb_vinculo_laboral(persona_id);
CREATE INDEX idx_vinculo_vigente ON lb_vinculo_laboral(persona_id, estado) WHERE fecha_fin IS NULL;

-- [DECIDIDO] Una persona tiene máximo un vínculo laboral vigente a la vez (no se permiten
-- vínculos paralelos). Antes de dar de alta un vínculo nuevo, la aplicación debe cerrar el
-- anterior (poner fecha_fin) en la misma transacción; si se intenta insertar un segundo vínculo
-- vigente para la misma persona, este índice lo bloquea con un error de constraint — el backend
-- debe traducir eso a un mensaje claro ("esta persona ya tiene un vínculo activo, ciérralo
-- primero"), no dejar que el 500 genérico llegue al usuario.
CREATE UNIQUE INDEX uq_vinculo_laboral_vigente_por_persona
    ON lb_vinculo_laboral(persona_id) WHERE fecha_fin IS NULL;

-- ============================================
-- CAPA 3: USUARIO DEL SISTEMA (solo si loguea — no toda persona tiene uno)
-- ============================================
CREATE TABLE lb_usuario_sistema (
    id              BIGSERIAL PRIMARY KEY,
    persona_id      INT NOT NULL REFERENCES lb_persona(id),
    email_login     VARCHAR(150) NOT NULL UNIQUE, -- puede ser Gmail, Hotmail o corporativo
    password_hash   TEXT NOT NULL, -- [REVISADO] usar IPasswordHasher<T> de ASP.NET Identity, ya
                                    -- usado en Infrastructure/Repositories/UserRepository.cs — no
                                    -- introducir BCrypt ni otra librería nueva para esto.
    estado          VARCHAR(20) NOT NULL DEFAULT 'ACTIVO', -- ACTIVO, BLOQUEADO, INACTIVO
    ultimo_acceso   TIMESTAMPTZ,
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now()
);
-- [REVISADO] Regla: 1 persona = máximo 1 usuario de sistema. Si una persona vuelve a necesitar
-- login tras haberlo perdido (ej. vuelve como PLANILLA tras un periodo LOCADOR), la lógica de
-- aplicación debe REACTIVAR (estado = ACTIVO) esta fila existente, nunca intentar insertar una
-- nueva — violaría este constraint a propósito.
ALTER TABLE lb_usuario_sistema ADD CONSTRAINT uq_usuario_sistema_persona UNIQUE (persona_id);

-- ============================================
-- CATÁLOGO DE ROLES (abierto — se agregan/quitan sin tocar código)
-- ============================================
CREATE TABLE lb_rol (
    id              SMALLSERIAL PRIMARY KEY,
    codigo          VARCHAR(30) NOT NULL UNIQUE, -- GERENTE_GENERAL, RESIDENTE, ALMACENERO, LOGISTICA, COMPRAS, ADMIN
    nombre          VARCHAR(80) NOT NULL,
    descripcion     TEXT,
    es_global       BOOLEAN NOT NULL DEFAULT false, -- true = no requiere proyecto_id (ej. Gerencia)
    activo          BOOLEAN NOT NULL DEFAULT true
);

INSERT INTO lb_rol (codigo, nombre, es_global) VALUES
('GERENTE_GENERAL', 'Gerente General',        true),
('LOGISTICA',       'Logística Central Lima', true),
('COMPRAS',         'Compras',                true),
('RESIDENTE',       'Residente de Proyecto',  false),
('ALMACENERO',      'Almacenero de Proyecto', false),
('ADMIN',           'Administrador del Sistema', true);

-- ============================================
-- [REVISADO] CATÁLOGO DE PERMISOS + RELACIÓN ROL-PERMISO
-- Sin esto, el código termina con `if (rol.Codigo == "ALMACENERO")` repartido por todos los
-- controllers — exactamente lo que un ERP de verdad (SAP, Odoo) evita. Con esta capa, "el
-- Residente ahora también puede aprobar compras menores a S/500" es una fila nueva en
-- lb_rol_permiso, no un deploy. Meterla ahora en Fase 1 es barato; meterla después con roles ya
-- en producción y controllers ya escritos contra el código del rol es caro — por eso va desde
-- el día 1 aunque Fase 1 todavía no tenga módulos que la usen de verdad.
-- ============================================
CREATE TABLE lb_permiso (
    id              SERIAL PRIMARY KEY,
    codigo          VARCHAR(50) NOT NULL UNIQUE, -- PEDIDO_APROBAR, EPP_ENTREGAR, STOCK_AJUSTAR, KARDEX_VER
    descripcion     VARCHAR(150)
);
CREATE TABLE lb_rol_permiso (
    rol_id      SMALLINT NOT NULL REFERENCES lb_rol(id),
    permiso_id  INT NOT NULL REFERENCES lb_permiso(id),
    PRIMARY KEY (rol_id, permiso_id)
);

-- ============================================
-- PROYECTOS Y ALMACENES (necesario para el scope)
-- ============================================
CREATE TABLE lb_proyecto (
    id              SERIAL PRIMARY KEY,
    codigo          VARCHAR(20) NOT NULL UNIQUE,
    nombre          VARCHAR(150) NOT NULL,
    ubicacion       VARCHAR(200),
    estado          VARCHAR(20) NOT NULL DEFAULT 'ACTIVO',
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE TABLE lb_almacen (
    id              SERIAL PRIMARY KEY,
    codigo          VARCHAR(20) NOT NULL UNIQUE,
    nombre          VARCHAR(150) NOT NULL,
    tipo            VARCHAR(20) NOT NULL, -- CENTRAL, PROYECTO
    proyecto_id     INT REFERENCES lb_proyecto(id), -- NULL si es almacén CENTRAL Lima
    activo          BOOLEAN NOT NULL DEFAULT true
);

-- ============================================
-- CAPA 4: ASIGNACIÓN (rol + scope + vigencia) — el corazón del control de acceso
-- ============================================
CREATE TABLE lb_usuario_asignacion (
    id                  BIGSERIAL PRIMARY KEY,
    usuario_sistema_id  BIGINT NOT NULL REFERENCES lb_usuario_sistema(id),
    rol_id              SMALLINT NOT NULL REFERENCES lb_rol(id),
    proyecto_id         INT REFERENCES lb_proyecto(id), -- NULL si el rol es global (es_global=true)
    -- [REVISADO] scope fino por almacén: un proyecto puede tener pañol central + pañol de frente,
    -- y un Almacenero de uno no debería ver el otro. NULL = todos los almacenes de ese proyecto.
    almacen_id          INT REFERENCES lb_almacen(id),
    fecha_inicio        DATE NOT NULL DEFAULT CURRENT_DATE,
    fecha_fin           DATE, -- NULL = vigente
    otorgado_por_usuario_sistema_id BIGINT REFERENCES lb_usuario_sistema(id), -- [REVISADO] quién
        -- concedió este rol — importante porque ALMACENERO/RESIDENTE controlan acceso físico al
        -- socavón, no solo datos.
    creado_en           TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_asignacion_usuario ON lb_usuario_asignacion(usuario_sistema_id) WHERE fecha_fin IS NULL;
CREATE INDEX idx_asignacion_proyecto_rol ON lb_usuario_asignacion(proyecto_id, rol_id) WHERE fecha_fin IS NULL;
```

### 4.4 Resolución de "quién soy en este proyecto, y qué puedo hacer" (query base para todo el sistema)

```sql
-- Dado un usuario logueado, obtener todos sus roles vigentes con su scope y sus permisos
SELECT r.codigo AS rol, r.es_global, ua.proyecto_id, ua.almacen_id, p.nombre AS proyecto_nombre,
       array_agg(DISTINCT perm.codigo) AS permisos
FROM lb_usuario_asignacion ua
JOIN lb_rol r ON r.id = ua.rol_id
LEFT JOIN lb_proyecto p ON p.id = ua.proyecto_id
LEFT JOIN lb_rol_permiso rp ON rp.rol_id = r.id
LEFT JOIN lb_permiso perm ON perm.id = rp.permiso_id
WHERE ua.usuario_sistema_id = :usuario_id
  AND ua.fecha_fin IS NULL
GROUP BY r.codigo, r.es_global, ua.proyecto_id, ua.almacen_id, p.nombre;
```

Con esto el backend arma el JWT con claims `[{rol: "RESIDENTE", proyecto_id: 3, almacen_id: null,
permisos: [...]}, ...]` y cada endpoint valida **permiso** (no el código del rol directamente) +
scope antes de ejecutar la acción — un Residente de Las Bravas no puede aprobar pedidos de otro
proyecto, y agregar un permiso nuevo a un rol no requiere tocar el endpoint.

## 5. MOTOR DE NOTIFICACIONES (transversal — diseñar ahora, usar desde Fase 2)

### 5.1 Principio: nunca hardcodear destinatarios

Se resuelve por evento + regla de scope, no por email fijo en código.

```sql
CREATE TABLE lb_notif_regla (
    id              SERIAL PRIMARY KEY,
    evento          VARCHAR(50) NOT NULL, -- PEDIDO_CREADO, PEDIDO_APROBADO, DESPACHO_ENVIADO, STOCK_MINIMO, HERRAMIENTA_VENCIDA
    rol_destino_id  SMALLINT REFERENCES lb_rol(id), -- a quién notificar (por rol)
    scope_type      VARCHAR(20) NOT NULL DEFAULT 'MISMO_PROYECTO', -- GLOBAL, MISMO_PROYECTO, SOLICITANTE
    canal           VARCHAR(20) NOT NULL DEFAULT 'EMAIL', -- EMAIL, PUSH, AMBOS
    activo          BOOLEAN NOT NULL DEFAULT true
);

-- Caso especial: notificar a una persona/email puntual además del rol
CREATE TABLE lb_notif_destinatario_extra (
    id              SERIAL PRIMARY KEY,
    notif_regla_id  INT NOT NULL REFERENCES lb_notif_regla(id),
    email           VARCHAR(150) NOT NULL,
    descripcion     VARCHAR(100) -- "Contabilidad", "Auditoría externa"
);

-- [REVISADO] Bitácora de envío. Sin esto no hay forma de auditar "¿le llegó el aviso al Residente
-- o no?" — crítico porque el sitio es un socavón con mala conectividad y va a haber reintentos.
CREATE TABLE lb_notif_envio (
    id                  BIGSERIAL PRIMARY KEY,
    notif_regla_id      INT NOT NULL REFERENCES lb_notif_regla(id),
    usuario_sistema_id  BIGINT REFERENCES lb_usuario_sistema(id), -- NULL si fue a un email extra
    email_destino       VARCHAR(150) NOT NULL,
    canal               VARCHAR(20) NOT NULL,
    estado              VARCHAR(20) NOT NULL DEFAULT 'PENDIENTE', -- PENDIENTE, ENVIADO, FALLIDO
    error               TEXT,
    referencia_tipo     VARCHAR(30), -- mismo valor que el evento origen (PEDIDO, COMPRA, ...)
    referencia_id       BIGINT,
    creado_en           TIMESTAMPTZ NOT NULL DEFAULT now(),
    enviado_en          TIMESTAMPTZ
);
CREATE INDEX idx_notif_envio_estado ON lb_notif_envio(estado) WHERE estado != 'ENVIADO';
```

Semilla de reglas iniciales:

| Evento | Rol destino | Scope |
|---|---|---|
| PEDIDO_CREADO | GERENTE_GENERAL | GLOBAL |
| PEDIDO_APROBADO | LOGISTICA | GLOBAL |
| PEDIDO_APROBADO | ALMACENERO | MISMO_PROYECTO |
| DESPACHO_ENVIADO | RESIDENTE | SOLICITANTE (el que creó el pedido) |
| STOCK_MINIMO | ALMACENERO, LOGISTICA | scope del almacén afectado |
| HERRAMIENTA_VENCIDA | ALMACENERO | MISMO_PROYECTO |

`scope_type = SOLICITANTE` es un caso especial: no busca por rol/proyecto, sino directo al
`usuario_sistema` que generó el registro origen.

**[REVISADO] Contrato obligatorio para que `SOLICITANTE` sea genérico:** toda tabla transaccional
que dispare un evento (pedido, compra, préstamo de herramienta, etc.) debe tener una columna
`solicitante_usuario_sistema_id` con ese nombre exacto. Si cada módulo la llama distinto, el motor
de notificaciones termina con un `switch` por `referencia_tipo` en vez de ser genérico — lo
contrario de lo que este diseño busca. Fijar esta convención ahora, antes de que exista el primer
módulo de Fase 2 que la necesite.

### 5.2 Resolución de destinatarios en backend (pseudocódigo)

```
al disparar evento X con proyecto_id=Y:
  reglas = SELECT * FROM lb_notif_regla WHERE evento = X AND activo
  para cada regla:
    si scope_type = GLOBAL:
        destinatarios = usuarios con asignación vigente rol=regla.rol_destino_id, proyecto_id IS NULL
    si scope_type = MISMO_PROYECTO:
        destinatarios = usuarios con asignación vigente rol=regla.rol_destino_id, proyecto_id = Y
    si scope_type = SOLICITANTE:
        destinatarios = [usuario_sistema_id del registro origen, columna solicitante_usuario_sistema_id]
    + destinatarios extra de lb_notif_destinatario_extra para esa regla
  por cada destinatario: insertar fila en lb_notif_envio (estado=PENDIENTE) y enviar por
  email_login (personal o corporativo, no importa el dominio) reusando Shared/Services/Email
  existente; actualizar estado a ENVIADO o FALLIDO+error.
```

## 6. CATÁLOGO MAESTRO (Fase 1, punto 2)

```sql
CREATE TABLE lb_categoria_producto (
    id          SERIAL PRIMARY KEY,
    nombre      VARCHAR(100) NOT NULL,
    tipo        VARCHAR(20) NOT NULL -- EPP, MATERIAL, HERRAMIENTA, EQUIPO
);

CREATE TABLE lb_producto (
    id              BIGSERIAL PRIMARY KEY,
    codigo          VARCHAR(30) UNIQUE, -- autogenerado o manual
    nombre          VARCHAR(200) NOT NULL,
    descripcion     TEXT,
    categoria_id    INT NOT NULL REFERENCES lb_categoria_producto(id),
    unidad_medida   VARCHAR(20) NOT NULL, -- UND, PAR, KG, GAL, etc.
    requiere_talla  BOOLEAN NOT NULL DEFAULT false, -- true para EPP con talla
    es_retornable   BOOLEAN NOT NULL DEFAULT false, -- true para herramientas/equipos en préstamo
    -- [REVISADO] stock_minimo/stock_maximo SE MUEVEN a lb_stock (sección 7): el kardex es
    -- multi-almacén y un umbral global por producto no sirve — Las Bravas y un proyecto chico
    -- necesitan umbrales distintos para el mismo producto, y el evento STOCK_MINIMO de la
    -- sección 5 depende de eso.
    activo          BOOLEAN NOT NULL DEFAULT true,
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Fuzzy search: usar extensión pg_trgm para similarity() al buscar duplicados
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE INDEX idx_producto_nombre_trgm ON lb_producto USING gin (nombre gin_trgm_ops);
```

Al crear un producto nuevo, el frontend debe consultar
`SELECT nombre, similarity(nombre, :busqueda) AS score FROM lb_producto ORDER BY score DESC LIMIT 5`
y mostrar sugerencias antes de permitir crear uno nuevo.

## 7. ALMACÉN / KARDEX (Fase 1, punto 3)

```sql
CREATE TABLE lb_stock (
    id              BIGSERIAL PRIMARY KEY,
    almacen_id      INT NOT NULL REFERENCES lb_almacen(id),
    producto_id     BIGINT NOT NULL REFERENCES lb_producto(id),
    -- [REVISADO] NOT NULL DEFAULT '' en vez de NULL: en Postgres un UNIQUE(...) con NULL nunca
    -- detecta duplicados (NULL != NULL), así que para productos sin talla el UNIQUE de abajo NO
    -- protegía nada — se podían crear dos filas de stock para el mismo almacén+producto. Con
    -- talla='' como valor sentinela, el UNIQUE sí funciona.
    talla           VARCHAR(10) NOT NULL DEFAULT '',
    cantidad_actual NUMERIC(12,2) NOT NULL DEFAULT 0,
    -- [REVISADO] movidos desde lb_producto — ver nota en sección 6
    stock_minimo    NUMERIC(12,2) NOT NULL DEFAULT 0,
    stock_maximo    NUMERIC(12,2),
    actualizado_en  TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE (almacen_id, producto_id, talla)
);

CREATE TABLE lb_movimiento (
    id              BIGSERIAL PRIMARY KEY,
    almacen_id      INT NOT NULL REFERENCES lb_almacen(id),
    producto_id     BIGINT NOT NULL REFERENCES lb_producto(id),
    talla           VARCHAR(10) NOT NULL DEFAULT '',
    tipo_movimiento VARCHAR(20) NOT NULL, -- INGRESO, SALIDA, TRANSFERENCIA (no usado aún, pero soportado)
    cantidad        NUMERIC(12,2) NOT NULL,
    costo_unitario  NUMERIC(12,2), -- [REVISADO] nullable desde ya: Fase 3 (Compras) y el eventual
                                    -- reporte SUNAT (PLE - Registro de Inventario Permanente) lo
                                    -- van a necesitar; agregarlo ahora evita una migración
                                    -- invasiva sobre una tabla que para entonces ya tendrá datos.
    referencia_tipo VARCHAR(30), -- PEDIDO, COMPRA, ENTREGA_EPP, PRESTAMO_HERRAMIENTA
    referencia_id   BIGINT,
    usuario_sistema_id BIGINT REFERENCES lb_usuario_sistema(id),
    creado_en       TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_movimiento_almacen_producto ON lb_movimiento(almacen_id, producto_id, creado_en);
-- [REVISADO] faltaba: necesaria para "mostrar todos los movimientos de este pedido/compra"
CREATE INDEX idx_movimiento_referencia ON lb_movimiento(referencia_tipo, referencia_id);
```

**[DECIDIDO] Concurrencia de `lb_stock.cantidad_actual`:** se mantiene como contador (no se
recalcula sumando el historial completo cada vez) — necesario para que consultar el stock actual
desde el celular sea instantáneo. Para que no se pisen dos descuentos simultáneos, todo movimiento
se registra así, dentro de una sola transacción de base de datos:

```sql
BEGIN;
SELECT cantidad_actual FROM lb_stock
  WHERE almacen_id = :almacen AND producto_id = :producto AND talla = :talla
  FOR UPDATE; -- bloquea esta fila puntual hasta el COMMIT; el segundo almacenero espera su turno

INSERT INTO lb_movimiento (almacen_id, producto_id, talla, tipo_movimiento, cantidad, ...)
  VALUES (:almacen, :producto, :talla, 'SALIDA', :cantidad, ...);

UPDATE lb_stock SET cantidad_actual = cantidad_actual - :cantidad, actualizado_en = now()
  WHERE almacen_id = :almacen AND producto_id = :producto AND talla = :talla;
COMMIT;
```

El `FOR UPDATE` bloquea solo esa fila de `lb_stock` (ese almacén+producto+talla puntual), no la
tabla entera — otros productos siguen registrando movimientos en paralelo sin esperar. Si la
conexión del celular se cae a mitad de la transacción, Postgres la revierte sola (no queda un
movimiento sin su descuento de stock, o viceversa).

## 8. PRIMER PROMPT SUGERIDO PARA CLAUDE CODE

```
Lee CONTEXT_LOGISTICA.md secciones 0, 4 y 6.
Ejecuta primero el protocolo antidesastre: audita si ya existe alguna tabla
lb_* en la base de datos actual (information_schema.tables).
Si no existe nada, implementa el DDL completo de la sección 4 (modelo de
personas) y sección 6 (catálogo maestro), en ese orden — dentro de la
sección 4, respeta el orden: catálogos (lb_tipo_vinculo, lb_empresa_contratista,
lb_cargo, lb_rol, lb_permiso, lb_proyecto, lb_almacen) antes que las tablas
que los referencian (lb_vinculo_laboral, lb_usuario_sistema,
lb_usuario_asignacion, lb_rol_permiso).
No implementes backend/frontend todavía, solo el schema + migración.
Confírmame con un resumen de tablas creadas antes de continuar.
```

## 9. PENDIENTES DE DEFINICIÓN (no bloquean Fase 1, resolver antes de Fase 2/3)

- Nombre exacto del site SharePoint existente y SiteId (pendiente que Daniel lo pase para sacarlo
  con `/_api/site/id`)
- ~~Confirmar si PostgreSQL será instancia nueva en Aiven o servidor propio~~ **[REVISADO]** para
  desarrollo local ya está resuelto (`hyp_local` en Postgres local, ver `appsettings.Local.json`);
  falta confirmar solo el destino de producción.
- Puerto de desarrollo del frontend Angular (para no chocar con Abril-Frontend en la máquina de
  desarrollo)
- Nombres definitivos de bibliotecas SharePoint (propuesta: CargosEPP, CargosHerramientas,
  GuiasRemision, OrdenesCompra, HojasRetiro)
- ~~Confirmar si un vínculo laboral vigente por persona es una regla dura~~ **[DECIDIDO]**: sí,
  uno solo a la vez (constraint en 4.3).
- ~~Definir la estrategia de concurrencia de `lb_stock.cantidad_actual`~~ **[DECIDIDO]**: contador
  con `SELECT ... FOR UPDATE` transaccional (sección 7).

## 10. NOTAS DE IMPLEMENTACIÓN PARA NO REPETIR ERRORES YA CONOCIDOS

Estas son referencias directas a código y comentarios que ya existen en este repo
(`hyp-contratistas-backend`, heredado de Abril-Backend) y que documentan errores reales de
producción. Antes de escribir las entidades EF Core de la sección 4, leer:

- `Infrastructure/Models/Worker.cs` — comentarios sobre `PuestoId` vs. texto libre (líneas ~48-65)
  y sobre soft-delete (`State`, líneas ~209-225).
- `Infrastructure/Models/WorkerVinculacion.cs` — patrón de historial con `fecha_inicio`/`fecha_fin`
  por fila, más `motivo_retiro` y `registrado_por_id`.
- `Infrastructure/Repositories/UserRepository.cs` — uso de `IPasswordHasher<T>`, reutilizar tal
  cual para `lb_usuario_sistema`.
- `Shared/Services/Jwt/JWTService.cs` / `Infrastructure/Interfaces/IJWTService.cs` — reutilizar
  para emitir el JWT con los claims de rol/permiso/scope de la sección 4.4.
- `Shared/Data/AppContext.cs`, método `ConfigurePostgreSQL` — patrón a seguir para cualquier
  override de nombre de columna/tabla que choque con palabras reservadas de Postgres o con la
  convención snake_case.
