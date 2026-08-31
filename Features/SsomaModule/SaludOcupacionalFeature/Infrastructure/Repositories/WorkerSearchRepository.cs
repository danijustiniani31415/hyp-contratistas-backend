using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Workers;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Services;
using Abril_Backend.Shared.Services.AreaScope.Interfaces;
using Abril_Backend.Shared.Services.Revisores.Interfaces;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Repositories
{
    public class WorkerSearchRepository : IWorkerSearchRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IAreaScopeLegacyResolver _areaLegacyResolver;
        private readonly IJefePersonalizadoService _jefePersonalizado;

        public WorkerSearchRepository(
            IDbContextFactory<AppDbContext> factory,
            IAreaScopeLegacyResolver areaLegacyResolver,
            IJefePersonalizadoService jefePersonalizado)
        {
            _factory = factory;
            _areaLegacyResolver = areaLegacyResolver;
            _jefePersonalizado = jefePersonalizado;
        }

        /// <summary>
        /// Área a persistir.
        ///
        /// Si el formulario mandó el nodo del árbol, ese es la fuente de verdad y los campos legacy
        /// que hayan llegado en null se derivan de él (dirección nueva). Los que lleguen con valor se
        /// respetan: así un formulario que muestra los desplegables de área manda los tres en null y
        /// deja que se deriven, y uno que no los muestra puede reenviar intactos los que ya estaban
        /// guardados sin que se reescriban.
        ///
        /// Si no vino nodo se conserva el comportamiento viejo: se guardan los textos capturados y se
        /// intenta derivar el nodo a partir de la subárea (dirección original de AreaScopeMatcher).
        /// </summary>
        private async Task<(int? AreaScopeId, string? Area, string? Subarea, string? Jefatura)> ResolverAreaAsync(
            int? areaScopeId, string? area, string? subarea, string? jefatura)
        {
            if (areaScopeId is > 0)
            {
                var eq = await _areaLegacyResolver.ResolveAsync(areaScopeId);
                return (areaScopeId, area ?? eq?.Area, subarea ?? eq?.Subarea, jefatura ?? eq?.Jefatura);
            }

            return (Abril_Backend.Shared.Services.AreaScopeMatcher.Resolve(area, subarea),
                    area, subarea, jefatura);
        }

        public async Task<List<WorkerSearchResultDto>> Search(string? q, int limit, int? empresaIdContratista = null)
        {
            using var ctx = _factory.CreateDbContext();
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            var workers = ctx.Worker.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                workers = workers.Where(w =>
                    (w.Person != null && w.Person.FullName != null && w.Person.FullName.ToLower().Contains(term))
                    || (w.Person != null && w.Person.DocumentIdentityCode != null && w.Person.DocumentIdentityCode.ToLower().Contains(term)));
            }

            // Un contratista solo debe poder buscar/seleccionar trabajadores
            // vinculados actualmente a su propia empresa.
            if (empresaIdContratista.HasValue)
            {
                workers = workers.Where(w => ctx.WorkerVinculacion.Any(v =>
                    v.WorkerId == w.Id
                    && v.EmpresaId == empresaIdContratista.Value
                    && (v.FechaFin == null || v.FechaFin >= hoy)));
            }

            return await EnrichAsync(ctx, workers.OrderBy(w => w.Person != null ? w.Person.FullName : null).Take(limit), hoy);
        }

        public async Task<WorkerSearchResultDto?> GetByUserId(int userId, bool esContratista)
        {
            using var ctx = _factory.CreateDbContext();
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            IQueryable<Worker> workers;
            if (esContratista)
            {
                // El login de un contratista no tiene Person propia: el vínculo a su ficha de
                // trabajador va por ss_contratista_usuario.worker_id (asignado al dar de alta el
                // acceso), no por Person.UserId.
                var workerId = await ctx.SsContratistaUsuarios
                    .Where(cu => cu.UserId == userId && cu.Activo && cu.WorkerId != null)
                    .Select(cu => cu.WorkerId)
                    .FirstOrDefaultAsync();
                workers = ctx.Worker.Where(w => w.Id == workerId).Take(1);
            }
            else
            {
                // Una misma persona puede tener más de una ficha de trabajador (reingreso:
                // la ficha anterior queda RETIRADA con su vinculación cerrada y se crea una
                // nueva). El Take(1) sin orden devolvía cualquiera de las dos — en la práctica
                // la más antigua, ya retirada y sin vinculación vigente — y el llamador se
                // quedaba sin EmpresaActualId/Cargo. Se prioriza la ficha realmente vigente.
                workers = ctx.Worker
                    .Where(w => w.Person != null && w.Person.UserId == userId)
                    .OrderByDescending(w => ctx.WorkerVinculacion.Any(v =>
                        v.WorkerId == w.Id && (v.FechaFin == null || v.FechaFin >= hoy)))
                    .ThenByDescending(w => w.WorkersEstadoId == WorkersEstadoIds.Activo ? 1 : 0)
                    .ThenByDescending(w => w.Id)
                    .Take(1);
            }

            var result = await EnrichAsync(ctx, workers, hoy);
            return result.FirstOrDefault();
        }

        private static async Task<List<WorkerSearchResultDto>> EnrichAsync(AppDbContext ctx, IQueryable<Worker> workersQuery, DateOnly hoy)
        {
            var baseList = await workersQuery
                .Select(w => new
                {
                    w.Id,
                    ApellidoNombre = w.Person != null ? w.Person.FullName : null,
                    Dni = w.Person != null ? w.Person.DocumentIdentityCode : null,
                    w.EmailCorporativo,
                    Puesto = w.PuestoCatalogo == null ? null : w.PuestoCatalogo.Nombre,
                    Categoria = w.PuestoCatalogo == null || w.PuestoCatalogo.Categoria == null
                        ? null : w.PuestoCatalogo.Categoria.Nombre,
                    w.WorkersEstadoId,
                    w.AniosExperiencia,
                    // Fecha de ingreso del último periodo laboral (ver WorkersPeriodoLaboral):
                    // antes era la columna workers.fecha_ingreso.
                    FechaIngreso = w.PeriodosLaborales
                        .Where(p => p.State)
                        .OrderByDescending(p => p.FechaIngreso)
                        .ThenByDescending(p => p.WorkersPeriodoLaboralId)
                        .Select(p => (DateOnly?)p.FechaIngreso)
                        .FirstOrDefault(),
                    w.ObraOficinaStaffId,
                    ObraOficinaStaffNombre = w.ObraOficinaStaff != null ? w.ObraOficinaStaff.Name : null
                })
                .ToListAsync();

            var ids = baseList.Select(b => b.Id).ToList();

            var vinculacionActual = await (
                from v in ctx.WorkerVinculacion
                join em in ctx.Contributor on v.EmpresaId equals em.ContributorId into ej
                from em in ej.DefaultIfEmpty()
                where ids.Contains(v.WorkerId)
                      && (v.FechaFin == null || v.FechaFin >= hoy)
                orderby v.FechaInicio descending
                select new
                {
                    v.WorkerId,
                    v.EmpresaId,
                    EmpresaNombre = em != null ? em.ContributorName : null,
                    v.Puesto,
                    v.FechaInicio
                }).ToListAsync();

            var porWorker = vinculacionActual
                .GroupBy(x => x.WorkerId)
                .ToDictionary(g => g.Key, g => g.First());

            // Obtener EsAbril por empresa
            var empresaIds = vinculacionActual.Where(v => v.EmpresaId.HasValue)
                .Select(v => v.EmpresaId!.Value).Distinct().ToList();
            var esAbrilPorEmpresa = await ctx.Contributor
                .Where(c => empresaIds.Contains(c.ContributorId))
                .Select(c => new { c.ContributorId, c.EsAbril })
                .ToDictionaryAsync(c => c.ContributorId, c => c.EsAbril);

            // Calcular inhabilitados por puntaje SSOMA (>= 10 puntos acumulados)
            var inhabilitadosSet = await ctx.SsomaAmonestaciones
                .Where(a => ids.Contains(a.WorkerId) && a.State)
                .GroupBy(a => a.WorkerId)
                .Where(g => g.Sum(a => a.PuntosInfraccion) >= 10)
                .Select(g => g.Key)
                .ToHashSetAsync();

            return baseList.Select(b =>
            {
                porWorker.TryGetValue(b.Id, out var vin);
                return new WorkerSearchResultDto
                {
                    Id = b.Id,
                    ApellidoNombre = b.ApellidoNombre,
                    Dni = b.Dni,
                    EmailCorporativo = b.EmailCorporativo,
                    Puesto = b.Puesto,
                    Categoria = b.Categoria,
                    // El cargo histórico de la vinculación; si no hay, el puesto vigente.
                    Cargo = vin?.Puesto ?? b.Puesto,
                    ObraOficinaStaffId = b.ObraOficinaStaffId,
                    ObraOficinaStaffNombre = b.ObraOficinaStaffNombre,
                    EmpresaActualId = vin?.EmpresaId,
                    EmpresaActual = vin?.EmpresaNombre,
                    Activo = b.WorkersEstadoId == WorkersEstadoIds.Activo,
                    AniosExperiencia = b.AniosExperiencia,
                    FechaIngreso = b.FechaIngreso,
                    InhabilitadoSsoma = inhabilitadosSet.Contains(b.Id)
                                     || b.WorkersEstadoId == WorkersEstadoIds.InhabilitadoSsoma,
                    EsAbril = vin?.EmpresaId.HasValue == true
                        && esAbrilPorEmpresa.TryGetValue(vin!.EmpresaId!.Value, out var ea) && ea
                };
            }).ToList();
        }

        public async Task<List<DocumentTypeDto>> GetDocumentTypes()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.DocumentIdentityType
                .Where(t => t.Active && t.State)
                .OrderBy(t => t.DocumentIdentityTypeId)
                .Select(t => new DocumentTypeDto
                {
                    Id = t.DocumentIdentityTypeId,
                    Abreviatura = t.DocumentIdentityTypeAbbreviation,
                    Descripcion = t.DocumentIdentityTypeDescription,
                })
                .ToListAsync();
        }

        public async Task<List<WorkerCategoryDto>> GetWorkerCategories()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Categoria
                .Where(c => c.Active && c.State)
                .OrderBy(c => c.Nombre)
                .Select(c => new WorkerCategoryDto
                {
                    Id = c.CategoriaId,
                    Nombre = c.Nombre,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Una sola consulta con dos CTEs: <c>self</c> (el trabajador que se está editando, para
        /// saber si su correo es corporativo y si realmente cambió) y <c>ocupado</c> (el primer
        /// trabajador NO retirado que ya tiene ese correo). Un trabajador retirado libera su
        /// buzón, igual que el índice único parcial de la BD.
        /// </summary>
        public async Task<EmailCorporativoContextoDto> GetContextoEmailCorporativo(string? emailNormalizado, int? workerId)
        {
            using var ctx = _factory.CreateDbContext();
            await ctx.Database.OpenConnectionAsync();
            var conn = ctx.Database.GetDbConnection();

            const string sql = """
                WITH self AS (
                    SELECT w.contrata_casa, w.obra_oficina_staff_id, w.email_corporativo, p.email AS email_personal
                    FROM workers w
                    LEFT JOIN person p ON p.person_id = w.person_id
                    WHERE w.state AND @workerId::int IS NOT NULL AND w.id = @workerId::int
                ),
                ocupado AS (
                    SELECT w.id, p.full_name, p.document_identity_code
                    FROM workers w
                    LEFT JOIN person p ON p.person_id = w.person_id
                    WHERE w.state AND @email::text IS NOT NULL
                      AND lower(btrim(w.email_corporativo)) = @email::text
                      AND w.workers_estado_id IN (SELECT workers_estado_id FROM workers_estado
                                                   WHERE state AND codigo IN ('ACTIVO','INHABILITADO_SSOMA'))
                      AND (@workerId::int IS NULL OR w.id <> @workerId::int)
                    ORDER BY w.id
                    LIMIT 1
                )
                SELECT
                    EXISTS (SELECT 1 FROM self)                     AS worker_encontrado,
                    (SELECT contrata_casa           FROM self)      AS worker_contrata_casa,
                    (SELECT obra_oficina_staff_id   FROM self)      AS worker_obra_oficina_staff_id,
                    (SELECT email_corporativo       FROM self)      AS worker_email_actual,
                    (SELECT email_personal          FROM self)      AS worker_email_personal_actual,
                    (SELECT id                      FROM ocupado)   AS ocupado_por_worker_id,
                    (SELECT full_name               FROM ocupado)   AS ocupado_por_nombre,
                    (SELECT document_identity_code  FROM ocupado)   AS ocupado_por_dni
                """;

            // Tipos explícitos: los parámetros pueden llegar en null y Npgsql no puede inferir
            // el tipo de un null suelto.
            var parametros = new DynamicParameters();
            parametros.Add("email", emailNormalizado, DbType.String);
            parametros.Add("workerId", workerId, DbType.Int32);

            var contexto = await conn.QueryFirstOrDefaultAsync<EmailCorporativoContextoDto>(sql, parametros);

            return contexto ?? new EmailCorporativoContextoDto();
        }

        public async Task<int> Create(WorkerCreateDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var dniUpper = dto.Dni.Trim().ToUpper();

            // Se mira el estado de la ficha, no solo ACTIVO: si la persona ya viene de
            // Reclutamiento como finalista aprobado, su ficha existe y la va a heredar el
            // onboarding al firmar. Dar de alta otra por acá dejaría dos fichas vivas para
            // el mismo DNI, y el EMO de Ingreso ya programado colgaría de la equivocada.
            var estadoFichaExistente = await ctx.Worker
                .Where(w => w.Person != null && w.Person.DocumentIdentityCode != null
                         && w.Person.DocumentIdentityCode.ToUpper() == dniUpper
                         && (w.WorkersEstadoId == WorkersEstadoIds.Activo
                          || w.WorkersEstadoId == WorkersEstadoIds.FinalistaAprobado))
                .Select(w => (int?)w.WorkersEstadoId)
                .FirstOrDefaultAsync();
            if (estadoFichaExistente == WorkersEstadoIds.Activo)
                throw new AbrilException("Ya existe un trabajador activo con ese DNI.", 409);
            if (estadoFichaExistente == WorkersEstadoIds.FinalistaAprobado)
                throw new AbrilException(
                    "Ese DNI ya tiene una ficha de finalista aprobado en Reclutamiento. "
                    + "Se convierte en trabajador desde Onboarding al firmar el contrato, no dándolo de alta acá.", 409);

            var workerExistente = await ctx.Worker
                .Where(w => w.Person != null && w.Person.DocumentIdentityCode != null
                         && w.Person.DocumentIdentityCode.ToUpper() == dniUpper)
                .Select(w => new { w.Id })
                .FirstOrDefaultAsync();
            if (workerExistente != null)
                await VerificarNoActivoEnOtraEmpresaAsync(ctx, workerExistente.Id, dto.EmpresaId);

            var now = DateTimeOffset.UtcNow;

            // Reusar Person existente para evitar error 23505 (unique en document_identity_code)
            var person = await ctx.Person
                .FirstOrDefaultAsync(p => p.DocumentIdentityCode != null
                                       && p.DocumentIdentityCode.ToUpper() == dniUpper);
            if (person == null)
            {
                person = new Person
                {
                    FullName = dto.ApellidoNombre,
                    DocumentIdentityCode = dniUpper,
                    PhoneNumber = int.TryParse(dto.Celular, out var ph1) ? ph1 : (int?)null,
                    Email = dto.EmailPersonal,
                    Active = true,
                    State = true,
                    CreatedDateTime = DateTime.UtcNow
                };
                ctx.Person.Add(person);
                await ctx.SaveChangesAsync();
            }
            if (!string.IsNullOrWhiteSpace(dto.Sexo)) person.SexoId = await ResolveSexoIdAsync(ctx, dto.Sexo) ?? person.SexoId;
            if (dto.FechaNacimiento.HasValue) person.FechaNacimiento = dto.FechaNacimiento;
            if (dto.MostrarEnBoletin.HasValue) person.MostrarEnBoletin = dto.MostrarEnBoletin.Value;
            // Al reusar una Person existente (reingreso) se actualiza el correo de contacto solo si
            // vino uno: un campo vacío no borra el dato que ya estaba registrado.
            if (dto.EmailPersonal is not null) person.Email = dto.EmailPersonal;

            var areaResuelta = await ResolverAreaAsync(
                dto.AreaScopeId, dto.Area, dto.Subarea, dto.Jefatura);

            // La categoría del trabajador es la de su puesto: se lee acá para poder congelarla
            // en la vinculación de alta más abajo.
            var categoriaDelPuesto = dto.PuestoId.HasValue
                ? await ctx.Puesto
                    .Where(pu => pu.PuestoId == dto.PuestoId.Value)
                    .Select(pu => (int?)pu.CategoriaId)
                    .FirstOrDefaultAsync()
                : null;

            var worker = new Worker
            {
                Person = person,
                EmailCorporativo = dto.EmailCorporativo,
                PuestoId = dto.PuestoId,
                AreaScopeId = areaResuelta.AreaScopeId,
                Area = areaResuelta.Area,
                Subarea = areaResuelta.Subarea,
                ContrataCasa = dto.ContrataCasa,
                ObraOficinaStaffId = dto.ObraOficinaStaffId,
                Jefatura = areaResuelta.Jefatura,
                Procedencia = dto.Procedencia,
                CondicionMedica = dto.CondicionMedica,
                Notas = dto.Notas,
                Sctr = dto.Sctr,
                HabilitadoObra = dto.HabilitadoObra,
                AniosExperiencia = dto.AniosExperiencia,
                Estado = "ACTIVO",
                WorkersEstadoId = WorkersEstadoIds.Activo,
                CreatedAt = now,
                UpdatedAt = now,
            };

            // El ingreso es su primer periodo laboral; sin fecha no se abre ninguno y la
            // ficha queda sin periodos, igual que antes quedaba con la columna en NULL.
            if (dto.FechaIngreso.HasValue)
            {
                worker.PeriodosLaborales.Add(new WorkersPeriodoLaboral
                {
                    FechaIngreso = dto.FechaIngreso.Value,
                    CreatedDateTime = now,
                });
            }

            ctx.Worker.Add(worker);
            await GuardarCuidandoEmailUnicoAsync(ctx);

            if (dto.EmpresaId.HasValue || dto.ProyectoId.HasValue)
            {
                ctx.WorkerVinculacion.Add(new WorkerVinculacion
                {
                    WorkerId = worker.Id,
                    EmpresaId = dto.EmpresaId,
                    ProyectoId = dto.ProyectoId,
                    CategoriaId = categoriaDelPuesto,
                    FechaInicio = DateOnly.FromDateTime(DateTime.Today),
                    CreatedAt = now,
                });
                await ctx.SaveChangesAsync();
            }

            // Jefe personalizado (checkbox del formulario). Solo se toca cuando el formulario
            // gestiona el campo: en obreros y contratistas ni siquiera se muestra.
            if (dto.GestionaJefe)
                await _jefePersonalizado.SetAsync(worker.Id, dto.JefePersonalizadoWorkerId);

            return worker.Id;
        }

        /// <summary>
        /// Resuelve el texto de sexo ('M'/'F' o 'Masculino'/'Femenino') que envía el frontend
        /// al id del catálogo normalizado <c>sexo</c>. Devuelve null si no hay match.
        /// </summary>
        private static async Task<int?> ResolveSexoIdAsync(AppDbContext ctx, string? sexo)
        {
            if (string.IsNullOrWhiteSpace(sexo)) return null;
            var s = sexo.Trim().ToUpperInvariant();
            var codigo = s.StartsWith("M") ? "M" : s.StartsWith("F") ? "F" : null;
            if (codigo is null) return null;
            return await ctx.Sexo.Where(x => x.State && x.Codigo == codigo)
                                 .Select(x => (int?)x.SexoId).FirstOrDefaultAsync();
        }

        public async Task Update(int id, WorkerUpdateDto dto, bool puedeEditarDni)
        {
            using var ctx = _factory.CreateDbContext();

            var worker = await ctx.Worker.Include(w => w.Person).FirstOrDefaultAsync(w => w.Id == id);
            if (worker == null)
                throw new AbrilException("Trabajador no encontrado.", 404);

            if (worker.Person != null)
            {
                worker.Person.FullName      = dto.ApellidoNombre;
                worker.Person.PhoneNumber   = int.TryParse(dto.Celular, out var ph2) ? ph2 : (int?)null;
                worker.Person.Email         = dto.EmailPersonal;
                if (!string.IsNullOrWhiteSpace(dto.Sexo)) worker.Person.SexoId = await ResolveSexoIdAsync(ctx, dto.Sexo) ?? worker.Person.SexoId;
                if (dto.FechaNacimiento.HasValue) worker.Person.FechaNacimiento = dto.FechaNacimiento;
                if (dto.MostrarEnBoletin.HasValue) worker.Person.MostrarEnBoletin = dto.MostrarEnBoletin.Value;

                if (!string.IsNullOrWhiteSpace(dto.Dni))
                {
                    var nuevoDoc = dto.Dni.Trim().ToUpper();
                    if (!string.Equals(nuevoDoc, worker.Person.DocumentIdentityCode, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!puedeEditarDni)
                            throw new AbrilException("Solo un Administrador de Obra puede modificar el número de documento.", 403);

                        var duplicado = await ctx.Person.AnyAsync(p =>
                            p.PersonId != worker.Person.PersonId
                            && p.DocumentIdentityCode != null
                            && p.DocumentIdentityCode.ToUpper() == nuevoDoc);
                        if (duplicado)
                            throw new AbrilException("Ya existe otra persona con ese número de documento.", 409);

                        worker.Person.DocumentIdentityCode = nuevoDoc;
                    }
                }
            }
            worker.EmailCorporativo = dto.EmailCorporativo;
            // La fecha de ingreso corrige el último periodo laboral, no la ficha. Sin fecha
            // no se borra el periodo: el formulario manda null también cuando el campo no se
            // tocó, y borrarlo perdería el paso completo del trabajador por Abril.
            if (dto.FechaIngreso.HasValue)
                await WorkersPeriodoLaboralHelper.SetFechaIngresoAsync(
                    ctx, worker.Id, dto.FechaIngreso.Value, DateTimeOffset.UtcNow);
            // Obra, razón social, puesto (categoría/ocupación) y clasificación (Obra/Staff/
            // Oficina Central) NO se tocan desde esta edición general — deliberado. Cambiarlas
            // acá bypaseaba por completo el flujo de "Cambiar obra / puesto de trabajo"
            // (CambiarObraAsync): no reseteaba el certificado de aptitud, no dejaba auditoría
            // (WorkerEvento), no bloqueaba la subida de riesgo Oficina Central → Staff/Obra, y
            // encima MUTABA la vinculación vigente en vez de cerrarla y abrir una nueva,
            // corrompiendo el historial. Esos cinco campos ahora son de solo lectura en el
            // formulario de edición; el único camino soportado es CambiarObraAsync.
            //
            // ÚNICA excepción: ASIGNAR la clasificación a una ficha que no tiene ninguna. No es
            // un cambio de clasificación — no hay origen del que salir, así que no hay aptitud
            // que revisar (EsCambioRiesgoCritico(null, x) es siempre false porque RiesgoEmo(null)
            // es null) ni vinculación que cerrar. Sin esto, las fichas con la columna en NULL
            // (miles, heredadas de antes de normalizar el catálogo) quedaban sin salida: el
            // formulario decide qué campos mostrar a partir de la clasificación, así que sin ella
            // no ofrece ni área ni jefe, y "Cambiar obra" exige un proyecto destino. Cambiar una
            // clasificación YA asignada sigue siendo exclusivo de CambiarObraAsync.
            if (worker.ObraOficinaStaffId is null && dto.ObraOficinaStaffId.HasValue)
                worker.ObraOficinaStaffId = dto.ObraOficinaStaffId;

            var areaResuelta = await ResolverAreaAsync(
                dto.AreaScopeId, dto.Area, dto.Subarea, dto.Jefatura);
            worker.AreaScopeId = areaResuelta.AreaScopeId;
            worker.Area = areaResuelta.Area;
            worker.Subarea = areaResuelta.Subarea;
            worker.ContrataCasa = dto.ContrataCasa;
            worker.Jefatura = areaResuelta.Jefatura;
            worker.Procedencia = dto.Procedencia;
            worker.CondicionMedica = dto.CondicionMedica;
            worker.Notas = dto.Notas;
            worker.Sctr = dto.Sctr;
            worker.HabilitadoObra = dto.HabilitadoObra;
            if (dto.AniosExperiencia.HasValue) worker.AniosExperiencia = dto.AniosExperiencia;
            worker.UpdatedAt = DateTimeOffset.UtcNow;

            await GuardarCuidandoEmailUnicoAsync(ctx);

            // Jefe personalizado (checkbox del formulario). Solo se toca cuando el formulario
            // gestiona el campo: en obreros y contratistas ni siquiera se muestra.
            if (dto.GestionaJefe)
                await _jefePersonalizado.SetAsync(id, dto.JefePersonalizadoWorkerId);
        }

        public async Task UpdateDatosBasicos(int id, WorkerDatosBasicosDto dto, bool puedeEditarDni)
        {
            using var ctx = _factory.CreateDbContext();

            var worker = await ctx.Worker.Include(w => w.Person).FirstOrDefaultAsync(w => w.Id == id);
            if (worker == null)
                throw new AbrilException("Trabajador no encontrado.", 404);
            if (worker.Person == null)
                throw new AbrilException("El trabajador no tiene datos de persona asociados.", 400);

            if (string.IsNullOrWhiteSpace(dto.NombreCompleto))
                throw new AbrilException("El nombre completo es obligatorio.", 400);

            worker.Person.FullName = dto.NombreCompleto.Trim();

            if (dto.DocumentIdentityTypeId.HasValue)
                worker.Person.DocumentIdentityTypeId = dto.DocumentIdentityTypeId;

            if (!string.IsNullOrWhiteSpace(dto.NumeroDocumento))
            {
                var nuevoDoc = dto.NumeroDocumento.Trim().ToUpper();
                if (!string.Equals(nuevoDoc, worker.Person.DocumentIdentityCode, StringComparison.OrdinalIgnoreCase))
                {
                    if (!puedeEditarDni)
                        throw new AbrilException("Solo un Administrador de Obra puede modificar el número de documento.", 403);

                    var duplicado = await ctx.Person.AnyAsync(p =>
                        p.PersonId != worker.Person.PersonId
                        && p.DocumentIdentityCode != null
                        && p.DocumentIdentityCode.ToUpper() == nuevoDoc);
                    if (duplicado)
                        throw new AbrilException("Ya existe otra persona con ese número de documento.", 409);
                    worker.Person.DocumentIdentityCode = nuevoDoc;
                }
            }

            worker.Person.FechaNacimiento = dto.Cumpleanos;
            worker.Person.UpdatedDateTime = DateTime.UtcNow;

            // El puesto es la única vía a la categoría, así que se valida como tal: un
            // puesto muerto dejaría al trabajador fuera de todo filtro y de toda regla.
            if (dto.PuestoId.HasValue && dto.PuestoId != worker.PuestoId)
            {
                var existePuesto = await ctx.Puesto
                    .AnyAsync(pu => pu.PuestoId == dto.PuestoId.Value && pu.State);
                if (!existePuesto)
                    throw new AbrilException("El puesto seleccionado no existe.", 400);
            }
            worker.PuestoId = dto.PuestoId;

            if (dto.AreaScopeId.HasValue && dto.AreaScopeId != worker.AreaScopeId)
            {
                var existeNodo = await ctx.AreaScope
                    .AnyAsync(s => s.AreaScopeId == dto.AreaScopeId.Value && s.State);
                if (!existeNodo)
                    throw new AbrilException("El área seleccionada no existe en la jerarquía de áreas.", 400);
            }
            worker.AreaScopeId = dto.AreaScopeId;

            // Ambos correos llegan ya normalizados y validados (formato, existencia en el tenant,
            // unicidad del corporativo y "al menos uno") por WorkerEmailValidator; aquí solo se persisten.
            worker.EmailCorporativo = dto.EmailCorporativo;
            worker.Person.Email = dto.EmailPersonal;

            worker.UpdatedAt = DateTimeOffset.UtcNow;

            await GuardarCuidandoEmailUnicoAsync(ctx);
        }

        /// <summary>
        /// Guarda traduciendo la violación del índice <c>ux_workers_email_corporativo_vigente</c>
        /// a un 409 legible. El servicio ya valida el correo antes de llegar aquí, así que este
        /// camino solo se da en una carrera entre dos altas simultáneas con el mismo buzón.
        /// </summary>
        private static async Task GuardarCuidandoEmailUnicoAsync(AppDbContext ctx)
        {
            try
            {
                await ctx.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (
                ex.InnerException is PostgresException pg
                && pg.SqlState == PostgresErrorCodes.UniqueViolation
                && pg.ConstraintName == "ux_workers_email_corporativo_vigente")
            {
                throw new AbrilException(
                    "Ese correo corporativo acaba de ser asignado a otro trabajador. Usa un correo distinto.", 409);
            }
        }

        public async Task Retirar(int id)
        {
            using var ctx = _factory.CreateDbContext();

            var worker = await ctx.Worker.FirstOrDefaultAsync(w => w.Id == id);
            if (worker == null)
                throw new AbrilException("Trabajador no encontrado.", 404);

            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var ahora = DateTimeOffset.UtcNow;

            worker.WorkersEstadoId = WorkersEstadoIds.Retirado;
            worker.UpdatedAt = ahora;

            // Cierra el periodo laboral vigente en vez de escribir la fecha en la ficha: si
            // mañana reingresa se le abre otro y el paso de hoy queda registrado.
            await WorkersPeriodoLaboralHelper.CerrarAsync(ctx, id, hoy, ahora);

            var vinculacionesAbiertas = await ctx.WorkerVinculacion
                .Where(v => v.WorkerId == id && v.FechaFin == null)
                .ToListAsync();

            foreach (var v in vinculacionesAbiertas)
            {
                v.FechaFin = hoy;
                v.UpdatedAt = ahora;
            }

            await ctx.SaveChangesAsync();
        }

        private static async Task VerificarNoActivoEnOtraEmpresaAsync(AppDbContext ctx, int workerId, int? empresaIdNueva)
        {
            var vinculActiva = await ctx.WorkerVinculacion
                .Where(v => v.WorkerId == workerId && v.FechaFin == null)
                .Select(v => new { v.EmpresaId })
                .FirstOrDefaultAsync();

            if (vinculActiva != null && vinculActiva.EmpresaId.HasValue && vinculActiva.EmpresaId != empresaIdNueva)
                throw new AbrilException(
                    "El trabajador ya se encuentra activo en otra empresa. Debe ser retirado antes de poder registrarlo en una nueva empresa.",
                    400);
        }
    }
}
