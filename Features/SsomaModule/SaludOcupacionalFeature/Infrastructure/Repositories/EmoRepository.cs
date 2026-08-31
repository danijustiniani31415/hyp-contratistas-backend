using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Emo;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Features.Habilitacion.Infrastructure.Helpers;
using Abril_Backend.Shared.Constants;
using Abril_Backend.Shared.Helpers;
using Abril_Backend.Shared.Services.ReclutamientoEmoIngreso.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Repositories
{
    public class EmoRepository : IEmoRepository
    {
        private const int PageSize = 10;
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly ISharePointHabService _sharePoint;
        private readonly ILogger<EmoRepository> _logger;

        /// <summary>
        /// El puente con Reclutamiento: la aptitud de un EMO de Ingreso es lo que cierra —o
        /// reabre— el requerimiento que dejó a esa persona como finalista aprobado.
        /// </summary>
        private readonly IReclutamientoEmoIngresoService _reclutamientoEmo;

        public EmoRepository(
            IDbContextFactory<AppDbContext> factory,
            ISharePointHabService sharePoint,
            ILogger<EmoRepository> logger,
            IReclutamientoEmoIngresoService reclutamientoEmo)
        {
            _factory = factory;
            _sharePoint = sharePoint;
            _logger = logger;
            _reclutamientoEmo = reclutamientoEmo;
        }

        public async Task<PagedResult<EmoListItemDto>> ListPaged(EmoFilterDto filter)
        {
            using var ctx = _factory.CreateDbContext();
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            var q =
                from e in ctx.WorkerEmo
                join w in ctx.Worker on e.WorkerId equals w.Id
                join t in ctx.SsEmoTipo on e.TipoEmoId equals t.Id into tj
                from t in tj.DefaultIfEmpty()
                join em in ctx.Contributor on e.EmpresaOrigenId equals em.ContributorId into ej
                from em in ej.DefaultIfEmpty()
                select new { e, w, t, em };

            if (filter.WorkerId.HasValue)
                q = q.Where(x => x.e.WorkerId == filter.WorkerId.Value);
            if (!string.IsNullOrWhiteSpace(filter.Estado))
                q = q.Where(x => x.e.Estado == filter.Estado);
            if (!string.IsNullOrWhiteSpace(filter.Aptitud))
                q = q.Where(x => x.e.Aptitud == filter.Aptitud);
            if (filter.EmpresaId.HasValue)
                q = q.Where(x => x.e.EmpresaOrigenId == filter.EmpresaId.Value);

            var total = await q.CountAsync();
            var page = filter.Page < 1 ? 1 : filter.Page;

            var items = await q
                .OrderByDescending(x => x.e.FechaEmo)
                .ThenByDescending(x => x.e.Id)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(x => new EmoListItemDto
                {
                    Id = x.e.Id,
                    WorkerId = x.e.WorkerId,
                    WorkerNombre = x.w.Person != null ? x.w.Person.FullName : null,
                    WorkerDni = x.w.Person != null ? x.w.Person.DocumentIdentityCode : null,
                    TipoEmo = x.t != null ? x.t.Nombre : null,
                    Empresa = x.em != null ? x.em.ContributorName : null,
                    FechaEmo = x.e.FechaEmo,
                    FechaVencimiento = x.e.FechaVencimientoCalculada ?? x.e.FechaVencimiento,
                    Aptitud = x.e.Aptitud,
                    Estado = x.e.Estado,
                    UrlResultado = x.e.UrlResultado,
                    UrlAptitud = x.e.UrlAptitud,
                    UrlEmoCompleto = x.e.UrlEmoCompleto
                })
                .ToListAsync();

            foreach (var it in items)
            {
                if (it.FechaVencimiento.HasValue)
                    it.DiasParaVencer = it.FechaVencimiento.Value.DayNumber - hoy.DayNumber;
            }

            return new PagedResult<EmoListItemDto>
            {
                Page = page,
                PageSize = PageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)PageSize),
                Data = items
            };
        }

        public async Task<PagedResult<EmoPorTrabajadorDto>> ListPorTrabajador(EmoPorTrabajadorFilterDto filter)
        {
            using var ctx = _factory.CreateDbContext();
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            // Último EMO activo por worker (WHERE NOT EXISTS emo posterior del mismo worker)
            var ultimoEmo = ctx.WorkerEmo
                .Where(e => e.Activo)
                .Where(e => !ctx.WorkerEmo.Any(e2 =>
                    e2.Activo
                    && e2.WorkerId == e.WorkerId
                    && (e2.FechaEmo > e.FechaEmo || (e2.FechaEmo == e.FechaEmo && e2.Id > e.Id))));

            // Vinculación vigente por worker (la más reciente cuya fecha_fin es null o >= hoy)
            var vinculacionVigente = ctx.WorkerVinculacion
                .Where(v => v.FechaFin == null || v.FechaFin >= hoy)
                .Where(v => !ctx.WorkerVinculacion.Any(v2 =>
                    (v2.FechaFin == null || v2.FechaFin >= hoy)
                    && v2.WorkerId == v.WorkerId
                    && (v2.FechaInicio > v.FechaInicio || (v2.FechaInicio == v.FechaInicio && v2.Id > v.Id))));

            var q =
                from w in ctx.Worker
                join ue in ultimoEmo on w.Id equals ue.WorkerId into ueJ
                from ue in ueJ.DefaultIfEmpty()
                join t in ctx.SsEmoTipo on ue.TipoEmoId equals t.Id into tJ
                from t in tJ.DefaultIfEmpty()
                join vv in vinculacionVigente on w.Id equals vv.WorkerId into vvJ
                from vv in vvJ.DefaultIfEmpty()
                join em in ctx.Contributor on vv.EmpresaId equals em.ContributorId into emJ
                from em in emJ.DefaultIfEmpty()
                join eop in ctx.Contributor on ue.EmpresaOrigenId equals eop.ContributorId into eopJ
                from eop in eopJ.DefaultIfEmpty()
                join proy in ctx.Project on (vv != null ? vv.ProyectoId : -1) equals proy.ProjectId into proyJ
                from proy in proyJ.DefaultIfEmpty()
                select new { w, ue, t, vv, em, eop, proy };

            if (filter.TodasLasFichas)
            {
                // Configuracion -> Trabajadores: el mantenedor del catalogo de fichas. Ve TODA
                // la tabla workers y el unico filtro es el soft delete, que para una ficha vive
                // en person.state (workers no tiene columna state). Una ficha sin persona no
                // tiene state, asi que no se descarta: igual que el conteo de Categorias y
                // Puestos, que la trae por LEFT JOIN para no desalinearse.
                q = q.Where(x => x.w.Person == null || x.w.Person.State);
            }
            else
            {
                // Opt-in explicito de las fichas de pre-ingreso: un finalista aprobado no tiene
                // vinculacion (no firmo contrato), asi que el filtro de empresa Abril lo dejaba
                // fuera. Es justo la gente a la que GTH tiene que programarle el EMO de Ingreso
                // antes de contratarla, y esta es la unica pantalla de EMOs donde aparece: en el
                // resto del sistema la ausencia de vinculacion la sigue manteniendo invisible.
                q = q.Where(x => (x.em != null && x.em.EsAbril)
                              || x.w.WorkersEstadoId == WorkersEstadoIds.FinalistaAprobado);
            }

            if (filter.WorkerId.HasValue)
                q = q.Where(x => x.w.Id == filter.WorkerId.Value);

            // Búsqueda por palabras en cualquier orden, insensible a mayúsculas y tildes
            // (alineada con app-search-input del front: "perez juan" coincide con "JUAN PÉREZ").
            // Cada palabra debe estar en el nombre o en el DNI.
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                foreach (var word in filter.Search.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var pattern = $"%{word}%";
                    q = q.Where(x =>
                        (x.w.Person != null && x.w.Person.FullName != null &&
                         EF.Functions.ILike(AppDbContext.Unaccent(x.w.Person.FullName), AppDbContext.Unaccent(pattern)))
                        || (x.w.Person != null && x.w.Person.DocumentIdentityCode != null &&
                         EF.Functions.ILike(x.w.Person.DocumentIdentityCode, pattern)));
                }
            }
            if (!string.IsNullOrWhiteSpace(filter.Aptitud))
                q = q.Where(x => x.ue != null && x.ue.Aptitud == filter.Aptitud);
            if (!string.IsNullOrWhiteSpace(filter.Estado))
            {
                if (filter.Estado == "Sin EMO")
                    q = q.Where(x => x.ue == null);
                else
                    q = q.Where(x => x.ue != null && x.ue.Estado == filter.Estado);
            }
            if (filter.EmpresaId.HasValue)
                q = q.Where(x => x.vv != null && x.vv.EmpresaId == filter.EmpresaId.Value);
            if (filter.ProyectoId.HasValue)
                q = q.Where(x => x.vv != null && x.vv.ProyectoId == filter.ProyectoId.Value);
            if (filter.AreaScopeId.HasValue)
            {
                var idsArea = await ctx.ResolveDescendantsAsync(filter.AreaScopeId.Value);
                q = q.Where(x => x.w.AreaScopeId != null && idsArea.Contains(x.w.AreaScopeId.Value));
            }
            if (filter.FechaEmoDesde.HasValue)
                q = q.Where(x => x.ue != null && x.ue.FechaEmo >= filter.FechaEmoDesde.Value);
            if (filter.FechaEmoHasta.HasValue)
                q = q.Where(x => x.ue != null && x.ue.FechaEmo <= filter.FechaEmoHasta.Value);
            if (filter.SinLectura)
                q = q.Where(x => x.ue != null && x.ue.UrlResultado == null);
            if (filter.SinCertificado)
                q = q.Where(x => x.ue != null && x.ue.UrlAptitud == null);
            if (filter.SinEmoCompleto)
                q = q.Where(x => x.ue != null && x.ue.UrlEmoCompleto == null);
            if (filter.SinInterconsulta)
                q = q.Where(x => ctx.SsInterconsulta.Any(ic =>
                    ic.WorkerId == x.w.Id && ic.Estado != "Cancelada" && ic.UrlInforme == null));
            if (filter.PendienteLecturaAbril)
                q = q.Where(x => x.ue != null && x.ue.RequiereLecturaAbril && x.ue.UrlResultado == null);

            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = filter.PageSize <= 0 ? 50 : Math.Min(filter.PageSize, 200);

            var total = await q.CountAsync();

            q = filter.SortBy switch
            {
                "fechaEmo" => filter.SortDesc
                    ? q.OrderByDescending(x => x.ue != null ? (DateOnly?)x.ue.FechaEmo : null)
                    : q.OrderBy(x => x.ue != null ? (DateOnly?)x.ue.FechaEmo : null),
                "fechaVencimiento" => filter.SortDesc
                    ? q.OrderByDescending(x => x.ue != null ? (x.ue.FechaVencimientoCalculada ?? x.ue.FechaVencimiento) : null)
                    : q.OrderBy(x => x.ue != null ? (x.ue.FechaVencimientoCalculada ?? x.ue.FechaVencimiento) : null),
                _ => q.OrderBy(x => x.w.Person != null ? x.w.Person.FullName : null)
            };

            var rows = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new EmoPorTrabajadorDto
                {
                    WorkerId = x.w.Id,
                    NombreCompleto = (x.w.Person != null ? x.w.Person.FullName : null) ?? string.Empty,
                    Dni = (x.w.Person != null ? x.w.Person.DocumentIdentityCode : null) ?? string.Empty,
                    DocumentIdentityTypeId = x.w.Person != null ? x.w.Person.DocumentIdentityTypeId : null,
                    Cumpleanos = x.w.Person != null ? x.w.Person.FechaNacimiento : null,
                    // Sin vinculacion (finalista aprobado) se cae al contributor de la ficha, que
                    // es la razon social del requerimiento: si no, la fila sale sin empresa.
                    EmpresaId = x.vv != null ? x.vv.EmpresaId : x.w.ContributorId,
                    Empresa = x.em != null ? x.em.ContributorName
                            : (x.w.Contributor != null ? x.w.Contributor.ContributorName : null),
                    EmpresaOrigenNombre = x.eop != null ? x.eop.ContributorName : null,
                    ProyectoNombre = x.proy != null ? x.proy.ProjectDescription : null,
                    ObraOficinaStaffId = x.w.ObraOficinaStaffId,
                    ObraOficina = x.w.ObraOficinaStaff != null ? x.w.ObraOficinaStaff.Name : null,
                    TipoContrata = x.w.ContrataCasa,
                    Categoria = x.w.PuestoCatalogo == null || x.w.PuestoCatalogo.Categoria == null ? null : x.w.PuestoCatalogo.Categoria.Nombre,
                    Puesto = x.w.PuestoCatalogo == null ? null : x.w.PuestoCatalogo.Nombre,
                    AreaScopeId = x.w.AreaScopeId,
                    CategoriaId = x.w.PuestoCatalogo != null ? x.w.PuestoCatalogo.CategoriaId : (int?)null,
                    PuestoId = x.w.PuestoId,
                    EmailCorporativo = x.w.EmailCorporativo,
                    EmailPersonal = x.w.Person != null ? x.w.Person.Email : null,
                    EsFinalistaAprobado = x.w.WorkersEstadoId == WorkersEstadoIds.FinalistaAprobado,
                    TieneEmo = x.ue != null,
                    EmoId = x.ue != null ? x.ue.Id : (int?)null,
                    TipoEmo = x.t != null ? x.t.Nombre : null,
                    FechaEmo = x.ue != null ? (DateOnly?)x.ue.FechaEmo : null,
                    FechaVencimiento = x.ue != null ? (x.ue.FechaVencimientoCalculada ?? x.ue.FechaVencimiento) : null,
                    Aptitud = x.ue != null ? x.ue.Aptitud : null,
                    Estado = x.ue != null ? x.ue.Estado : null,
                    UrlAptitud = x.ue != null ? x.ue.UrlAptitud : null,
                    UrlEmoCompleto = x.ue != null ? x.ue.UrlEmoCompleto : null,
                    UrlResultado = x.ue != null ? x.ue.UrlResultado : null,
                    RequiereLecturaAbril = x.ue != null && x.ue.RequiereLecturaAbril,
                    RequiereInterconsulta = x.ue != null && x.ue.RequiereInterconsulta,
                    // Se busca por WorkerId (la interconsulta más reciente del trabajador), no por
                    // el EmoId del EMO activo actual: cuando se registra un EMO de seguimiento que
                    // resuelve la interconsulta, ese EMO pasa a ser "el activo" pero la interconsulta
                    // sigue apuntando al EMO original (ya inactivo) — filtrar por EmoId la dejaba invisible.
                    InterconsultaId = ctx.SsInterconsulta
                        .Where(ic => ic.WorkerId == x.w.Id)
                        .OrderByDescending(ic => ic.CreatedAt)
                        .Select(ic => (int?)ic.Id).FirstOrDefault(),
                    InterconsultaEspecialidad = ctx.SsInterconsulta
                        .Where(ic => ic.WorkerId == x.w.Id)
                        .OrderByDescending(ic => ic.CreatedAt)
                        .Select(ic => ic.Especialidad).FirstOrDefault(),
                    InterconsultaEstado = ctx.SsInterconsulta
                        .Where(ic => ic.WorkerId == x.w.Id)
                        .OrderByDescending(ic => ic.CreatedAt)
                        .Select(ic => ic.Estado).FirstOrDefault(),
                    InterconsultaUrlInforme = ctx.SsInterconsulta
                        .Where(ic => ic.WorkerId == x.w.Id)
                        .OrderByDescending(ic => ic.CreatedAt)
                        .Select(ic => ic.UrlInforme).FirstOrDefault(),
                    EstadoProgramacionEmo = ctx.SsProgramacionEmo
                        .Where(pe => pe.WorkerId == x.w.Id && pe.State)
                        .OrderByDescending(pe => pe.FechaProgramada)
                        .ThenByDescending(pe => pe.Id)
                        .Select(pe => (string?)pe.Estado)
                        .FirstOrDefault()
                })
                .ToListAsync();

            foreach (var r in rows)
            {
                if (r.FechaVencimiento.HasValue)
                    r.DiasRestantes = r.FechaVencimiento.Value.DayNumber - hoy.DayNumber;
            }

            return new PagedResult<EmoPorTrabajadorDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Data = rows
            };
        }

        public async Task<EmoDetalleDto> GetById(int id)
        {
            using var ctx = _factory.CreateDbContext();
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            var row = await (
                from e in ctx.WorkerEmo
                join w in ctx.Worker on e.WorkerId equals w.Id
                join t in ctx.SsEmoTipo on e.TipoEmoId equals t.Id into tj
                from t in tj.DefaultIfEmpty()
                join em in ctx.Contributor on e.EmpresaOrigenId equals em.ContributorId into ej
                from em in ej.DefaultIfEmpty()
                join c in ctx.SsClinica on e.ClinicaId equals c.Id into cj
                from c in cj.DefaultIfEmpty()
                join m in ctx.SsMedicoOcupacional on e.MedicoId equals m.Id into mj
                from m in mj.DefaultIfEmpty()
                where e.Id == id
                select new
                {
                    e, w, t, em, c, m,
                    WorkerNombre = w.Person != null ? w.Person.FullName : null,
                    WorkerDni = w.Person != null ? w.Person.DocumentIdentityCode : null
                }
            ).FirstOrDefaultAsync()
              ?? throw new AbrilException("EMO no encontrado.", 404);

            var examenes = await (
                from d in ctx.SsEmoExamenDetalle
                join x in ctx.SsExamenTipo on d.ExamenTipoId equals x.Id
                where d.EmoId == id
                select new EmoExamenDetalleDto
                {
                    Id = d.Id,
                    ExamenTipoId = d.ExamenTipoId,
                    ExamenNombre = x.Nombre,
                    Categoria = x.Categoria,
                    Resultado = d.Resultado,
                    Valor = d.Valor,
                    Unidad = d.Unidad,
                    Observacion = d.Observacion
                }).ToListAsync();

            var restricciones = await (
                from r in ctx.SsEmoRestriccion
                join rt in ctx.SsRestriccionTipo on r.RestriccionTipoId equals rt.Id into rtj
                from rt in rtj.DefaultIfEmpty()
                where r.EmoId == id
                select new EmoRestriccionDetalleDto
                {
                    Id = r.Id,
                    RestriccionTipoId = r.RestriccionTipoId,
                    RestriccionDescripcion = rt != null ? rt.Descripcion : null,
                    DescripcionLibre = r.DescripcionLibre,
                    Vigente = r.Vigente
                }).ToListAsync();

            var convalidaciones = await (
                from cv in ctx.WorkerEmoConvalidacion
                join em in ctx.Contributor on cv.EmpresaDestinoId equals em.ContributorId into ej
                from em in ej.DefaultIfEmpty()
                where cv.EmoId == id
                orderby cv.FechaConvalidacion descending
                select new EmoConvalidacionResumenDto
                {
                    Id = cv.Id,
                    EmpresaDestinoId = cv.EmpresaDestinoId,
                    EmpresaDestinoNombre = em != null ? em.ContributorName : null,
                    FechaConvalidacion = cv.FechaConvalidacion,
                    Resultado = cv.Resultado,
                    FechaVencimiento = cv.FechaVencimiento,
                    Observaciones = cv.Observaciones,
                    UrlDocumento = cv.UrlDocumento
                }).ToListAsync();

            var programacion = await (
                from p in ctx.SsProgramacionEmo
                join c in ctx.SsClinica on p.ClinicaId equals c.Id into cj
                from c in cj.DefaultIfEmpty()
                join m in ctx.SsMedicoOcupacional on p.MedicoId equals m.Id into mj
                from m in mj.DefaultIfEmpty()
                where p.EmoResultadoId == id && p.State
                orderby p.FechaProgramada descending
                select new EmoProgramacionDetalleDto
                {
                    Id = p.Id,
                    FechaProgramada = p.FechaProgramada,
                    HoraProgramada = p.HoraProgramada,
                    CheckInHora = p.CheckInHora,
                    ClinicaNombre = c != null ? c.Nombre : null,
                    MedicoNombre = m != null ? m.ApellidoNombre : null,
                    Estado = p.Estado,
                    Origen = p.Origen,
                    MotivoRechazo = p.MotivoRechazo
                }).FirstOrDefaultAsync();

            var interconsulta = await (
                from i in ctx.SsInterconsulta
                join m in ctx.SsMedicoOcupacional on i.MedicoDerivaId equals m.Id into mj
                from m in mj.DefaultIfEmpty()
                where i.EmoId == id
                orderby i.CreatedAt descending
                select new EmoInterconsultaResumenDto
                {
                    Id = i.Id,
                    Especialidad = i.Especialidad,
                    MedicoDeriva = m != null ? m.ApellidoNombre : null,
                    FechaDerivacion = i.FechaDerivacion,
                    FechaAtencion = i.FechaAtencion,
                    CentroAtencion = i.CentroAtencion,
                    Diagnostico = i.Diagnostico,
                    Cie10 = i.Cie10,
                    Resultado = i.Resultado,
                    Estado = i.Estado,
                    RequiereSeguimiento = i.RequiereSeguimiento,
                    UrlInforme = i.UrlInforme
                }).FirstOrDefaultAsync();

            var fechaVenc = row.e.FechaVencimientoCalculada ?? row.e.FechaVencimiento;

            return new EmoDetalleDto
            {
                Id = row.e.Id,
                WorkerId = row.e.WorkerId,
                WorkerNombre = row.WorkerNombre,
                WorkerDni = row.WorkerDni,
                TipoEmoId = row.e.TipoEmoId,
                TipoEmoNombre = row.t != null ? row.t.Nombre : null,
                EmpresaOrigenId = row.e.EmpresaOrigenId,
                EmpresaOrigenNombre = row.em != null ? row.em.ContributorName : null,
                FechaEmo = row.e.FechaEmo,
                FechaVencimiento = row.e.FechaVencimiento,
                FechaVencimientoCalculada = row.e.FechaVencimientoCalculada,
                ClinicaId = row.e.ClinicaId,
                ClinicaNombre = row.c != null ? row.c.Nombre : null,
                MedicoId = row.e.MedicoId,
                MedicoNombre = row.m != null ? row.m.ApellidoNombre : null,
                Aptitud = row.e.Aptitud,
                RequiereInterconsulta = row.e.RequiereInterconsulta,
                NumeroInforme = row.e.NumeroInforme,
                UrlResultado = row.e.UrlResultado,
                UrlAptitud = row.e.UrlAptitud,
                UrlEmoCompleto = row.e.UrlEmoCompleto,
                RequiereLecturaAbril = row.e.RequiereLecturaAbril,
                Estado = row.e.Estado,
                Notas = row.e.Notas,
                Activo = row.e.Activo,
                DiasParaVencer = fechaVenc.HasValue ? fechaVenc.Value.DayNumber - hoy.DayNumber : (int?)null,
                Examenes = examenes,
                Restricciones = restricciones,
                Convalidaciones = convalidaciones,
                Programacion = programacion,
                Interconsulta = interconsulta
            };
        }

        public async Task<WorkerEmoHistorialDto> GetHistorialByWorker(int workerId)
        {
            using var ctx = _factory.CreateDbContext();
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            var w = await ctx.Worker
                .Include(x => x.Person)
                .FirstOrDefaultAsync(x => x.Id == workerId)
                ?? throw new AbrilException("Trabajador no encontrado.", 404);

            var vinculaciones = await (
                from v in ctx.WorkerVinculacion
                join em in ctx.Contributor on v.EmpresaId equals em.ContributorId into ej
                from em in ej.DefaultIfEmpty()
                where v.WorkerId == workerId
                orderby v.FechaInicio descending
                select new VinculacionHistorialDto
                {
                    Id = v.Id,
                    EmpresaId = v.EmpresaId,
                    EmpresaNombre = em != null ? em.ContributorName : null,
                    Puesto = v.Puesto,
                    TipoVinculacion = v.TipoVinculacion,
                    FechaInicio = v.FechaInicio,
                    FechaFin = v.FechaFin,
                    MotivoRetiro = v.MotivoRetiro
                }).ToListAsync();

            var emos = await (
                from e in ctx.WorkerEmo
                join t in ctx.SsEmoTipo on e.TipoEmoId equals t.Id into tj
                from t in tj.DefaultIfEmpty()
                join em in ctx.Contributor on e.EmpresaOrigenId equals em.ContributorId into ej
                from em in ej.DefaultIfEmpty()
                where e.WorkerId == workerId
                orderby e.FechaEmo descending
                select new EmoListItemDto
                {
                    Id = e.Id,
                    WorkerId = e.WorkerId,
                    WorkerNombre = w.Person != null ? w.Person.FullName : null,
                    WorkerDni = w.Person != null ? w.Person.DocumentIdentityCode : null,
                    TipoEmo = t != null ? t.Nombre : null,
                    Empresa = em != null ? em.ContributorName : null,
                    FechaEmo = e.FechaEmo,
                    FechaVencimiento = e.FechaVencimientoCalculada ?? e.FechaVencimiento,
                    Aptitud = e.Aptitud,
                    Estado = e.Estado
                }).ToListAsync();

            foreach (var em in emos)
                if (em.FechaVencimiento.HasValue)
                    em.DiasParaVencer = em.FechaVencimiento.Value.DayNumber - hoy.DayNumber;

            foreach (var v in vinculaciones)
            {
                var fin = v.FechaFin ?? DateOnly.MaxValue;
                v.Emos = emos
                    .Where(e => e.FechaEmo >= v.FechaInicio && e.FechaEmo <= fin)
                    .ToList();
            }

            return new WorkerEmoHistorialDto
            {
                WorkerId = w.Id,
                ApellidoNombre = w.Person?.FullName,
                Dni = w.Person?.DocumentIdentityCode,
                ContrataCasa = w.ContrataCasa,
                HabilitadoObra = w.HabilitadoObra,
                Vinculaciones = vinculaciones
            };
        }

        public async Task<EmoCreateResultDto> Create(EmoCreateDto dto, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var tipo = await ctx.SsEmoTipo.FirstOrDefaultAsync(t => t.Id == dto.TipoEmoId)
                ?? throw new AbrilException("Tipo de EMO no válido.", 400);
            var worker = await ctx.Worker.FirstOrDefaultAsync(w => w.Id == dto.WorkerId)
                ?? throw new AbrilException("Trabajador no encontrado.", 404);

            var vigenciaMeses = tipo.VigenciaMeses ?? 0;
            if (worker.ObraOficinaStaffId == ObraOficinaStaffIds.OficinaCentral)
                vigenciaMeses = 24;
            var fechaVencCalc = vigenciaMeses > 0
                ? (DateOnly?)dto.FechaEmo.AddMonths(vigenciaMeses)
                : null;

            var esApto = !string.Equals(dto.Aptitud, "Observado", StringComparison.OrdinalIgnoreCase)
                      && !string.Equals(dto.Aptitud, "No Apto", StringComparison.OrdinalIgnoreCase);

            var emo = new WorkerEmo
            {
                WorkerId = dto.WorkerId,
                EmpresaOrigenId = dto.EmpresaOrigenId,
                TipoEmoId = dto.TipoEmoId,
                FechaEmo = dto.FechaEmo,
                FechaVencimiento = esApto ? fechaVencCalc : null,
                FechaVencimientoCalculada = esApto ? fechaVencCalc : null,
                ClinicaId = dto.ClinicaId,
                MedicoId = dto.MedicoId,
                Aptitud = dto.Aptitud,
                RequiereInterconsulta = dto.RequiereInterconsulta,
                NumeroInforme = dto.NumeroInforme,
                FechaLectura = dto.FechaLectura,
                UrlResultado = dto.UrlResultado,
                RequiereLecturaAbril = dto.RequiereLecturaAbril,
                Notas = dto.Notas,
                Estado = esApto ? "Vigente" : "Observado",
                Activo = true,
                RegistradoPorId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            var emosAnteriores = await ctx.WorkerEmo
                .Where(e => e.WorkerId == emo.WorkerId && e.Activo)
                .ToListAsync();
            foreach (var e in emosAnteriores)
            {
                e.Activo = false;
                e.UpdatedAt = DateTimeOffset.UtcNow;
            }

            // Si el EMO anterior tenía una convalidación "Pendiente" sin resolver, este EMO nuevo
            // la vuelve obsoleta: ya no hay nada que un médico deba decidir sobre un EMO que
            // acaba de ser reemplazado. Sin este cierre automático, la fila queda "Pendiente"
            // para siempre, apuntando a un EmoId inactivo y sin los documentos reales (que ahora
            // están en el EMO nuevo) — el caso visto en el listado de Convalidaciones donde el
            // detalle mostraba "Sin archivo" pese a que el EMO vigente del trabajador sí los tenía.
            var emoIdsAnteriores = emosAnteriores.Select(e => e.Id).ToList();
            if (emoIdsAnteriores.Count > 0)
            {
                var pendientesObsoletas = await ctx.WorkerEmoConvalidacion
                    .Where(cv => emoIdsAnteriores.Contains(cv.EmoId) && cv.Resultado == "Pendiente")
                    .ToListAsync();
                foreach (var cv in pendientesObsoletas)
                {
                    cv.Resultado = "Descartada";
                    cv.Observaciones = string.IsNullOrWhiteSpace(cv.Observaciones)
                        ? "Descartada automáticamente: se registró un EMO nuevo para el trabajador."
                        : cv.Observaciones + " | Descartada automáticamente: se registró un EMO nuevo para el trabajador.";
                    cv.UpdatedAt = DateTimeOffset.UtcNow;
                }

                // Mismo huérfano, otro caso real (Polanco): una interconsulta "Pendiente" atada a
                // un EMO que ya quedó inactivo porque se registró uno nuevo. El caso original ya
                // no tiene sentido — el nuevo EMO es el que decide la aptitud ahora — pero sin este
                // cierre la fila se queda "Pendiente" indefinidamente (se vieron casos con 300+
                // días pendientes en el listado de Interconsultas).
                var interconsultasObsoletas = await ctx.SsInterconsulta
                    .Where(i => i.EmoId.HasValue && emoIdsAnteriores.Contains(i.EmoId.Value) && i.Estado == "Pendiente")
                    .ToListAsync();
                foreach (var ic in interconsultasObsoletas)
                {
                    ic.Estado = "Cancelada";
                    ic.Diagnostico = string.IsNullOrWhiteSpace(ic.Diagnostico)
                        ? "Cancelada automáticamente: se registró un EMO nuevo para el trabajador."
                        : ic.Diagnostico + " | Cancelada automáticamente: se registró un EMO nuevo para el trabajador.";
                    ic.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            ctx.WorkerEmo.Add(emo);
            await ctx.SaveChangesAsync();  // necesario para generar emo.Id antes de usarlo

            // Vincular interconsulta pendiente (sin EmoId) al nuevo EMO
            var interconsultaPendiente = await ctx.SsInterconsulta
                .Where(i => i.WorkerId == dto.WorkerId
                         && i.EmoId == null
                         && i.Estado == "Pendiente")
                .OrderByDescending(i => i.CreatedAt)
                .FirstOrDefaultAsync();

            if (interconsultaPendiente != null)
            {
                interconsultaPendiente.EmoId = emo.Id;
                interconsultaPendiente.UpdatedAt = DateTimeOffset.UtcNow;

                if (dto.DocumentoInterconsulta != null && dto.DocumentoInterconsulta.Length > 0)
                {
                    try
                    {
                        using var stream = dto.DocumentoInterconsulta.OpenReadStream();
                        interconsultaPendiente.UrlInforme = await _sharePoint.SubirArchivoAsync(
                            stream, dto.DocumentoInterconsulta.FileName, "interconsulta");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Error subiendo documento de interconsulta para worker {WorkerId}", dto.WorkerId);
                    }
                }
            }

            foreach (var ex in dto.Examenes)
            {
                ctx.SsEmoExamenDetalle.Add(new SsEmoExamenDetalle
                {
                    EmoId = emo.Id,
                    ExamenTipoId = ex.ExamenTipoId,
                    Resultado = ex.Resultado,
                    Valor = ex.Valor,
                    Unidad = ex.Unidad,
                    Observacion = ex.Observacion,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            foreach (var r in dto.Restricciones)
            {
                ctx.SsEmoRestriccion.Add(new SsEmoRestriccion
                {
                    EmoId = emo.Id,
                    RestriccionTipoId = r.RestriccionTipoId,
                    DescripcionLibre = r.DescripcionLibre,
                    Vigente = r.Vigente,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            // Un "No Apto" ya NO abre interconsulta por su cuenta. Es un veredicto cerrado —el
            // trabajador queda bloqueado en habilitación y su proceso de selección se detiene—, así
            // que la derivación automática solo dejaba una interconsulta "Por definir" en estado
            // Pendiente que nadie iba a atender, y de paso empujaba la cita a "En Interconsulta"
            // cuando en realidad ya estaba resuelta. La aptitud que sí la necesita es "Observado":
            // esa significa literalmente que falta el resultado real. Si la clínica igual carga una
            // derivación a mano, `InterconsultaInline` la sigue creando para cualquier aptitud.
            if (dto.InterconsultaInline != null ||
                (dto.Aptitud == "Observado" && dto.RequiereInterconsulta))
            {
                var ic = dto.InterconsultaInline;
                ctx.SsInterconsulta.Add(new SsInterconsulta
                {
                    EmoId = emo.Id,
                    WorkerId = emo.WorkerId,
                    Especialidad = ic?.Especialidad ?? "Por definir",
                    MedicoDerivaId = ic?.MedicoDerivaId ?? dto.MedicoId,
                    FechaDerivacion = dto.FechaEmo,
                    CentroAtencion = ic?.CentroAtencion,
                    Diagnostico = ic?.Diagnostico,
                    Cie10 = ic?.Cie10,
                    Estado = "Pendiente",
                    RequiereSeguimiento = ic?.RequiereSeguimiento ?? false,
                    RegistradoPorId = userId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            }

            if (dto.Aptitud == "No Apto")
            {
                worker.HabilitadoObra = false;
                worker.UpdatedAt = DateTimeOffset.UtcNow;
            }

            if (string.Equals(tipo.Nombre, "Retiro", StringComparison.OrdinalIgnoreCase))
            {
                var previos = await ctx.WorkerEmo
                    .Where(e => e.WorkerId == emo.WorkerId
                             && e.EmpresaOrigenId == emo.EmpresaOrigenId
                             && e.Id != emo.Id
                             && e.Activo)
                    .ToListAsync();
                foreach (var p in previos)
                {
                    p.Activo = false;
                    p.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            _logger.LogInformation("ArchivoLectura: {val}", dto.ArchivoLectura == null ? "NULL" : dto.ArchivoLectura.FileName);
            if (dto.ArchivoLectura != null && dto.ArchivoLectura.Length > 0)
            {
                try
                {
                    using var stream = dto.ArchivoLectura.OpenReadStream();
                    emo.UrlResultado = await _sharePoint.SubirArchivoAsync(
                        stream, dto.ArchivoLectura.FileName, "lectura-emo");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Error subiendo archivo de lectura EMO para worker {WorkerId}", dto.WorkerId);
                }
            }

            await SincronizarEntregableEmoAsync(ctx, emo, worker);

            var progActiva = await ctx.SsProgramacionEmo
                .Where(p => p.State
                         && p.WorkerId == emo.WorkerId
                         && p.Estado == "En Atención")
                .OrderByDescending(p => p.FechaProgramada)
                .FirstOrDefaultAsync();
            if (progActiva != null)
            {
                progActiva.EmoResultadoId = emo.Id;
                progActiva.UpdatedAt = DateTimeOffset.UtcNow;
                if (emo.RequiereInterconsulta == true)
                {
                    // La clínica maneja el levantamiento; el estado refleja la espera
                    progActiva.Estado = "En Interconsulta";
                }
                else
                {
                    progActiva.Estado = "Completado";
                }
            }

            if (dto.FechaLectura.HasValue)
            {
                var lecturaEmo = await ObtenerOCrearHabAsync(ctx, emo.WorkerId, HabItemIds.LecturaEmo);
                lecturaEmo.Estado = "Aprobado";
                // Vigencia = vencimiento del EMO (no la fecha de lectura, que es hoy y lo marcaría
                // como vencido de inmediato). Ver mismo criterio en Update().
                var fechaVencLectura = emo.FechaVencimientoCalculada ?? emo.FechaVencimiento;
                lecturaEmo.Vigencia = fechaVencLectura.HasValue
                    ? HabilitacionDateHelper.AsUtc(fechaVencLectura.Value.ToDateTime(TimeOnly.MinValue))
                    : HabilitacionDateHelper.AsUtc(dto.FechaLectura.Value.ToDateTime(TimeOnly.MinValue));
                lecturaEmo.UpdatedAt = DateTime.UtcNow;
            }

            // Cierre (o reapertura) del proceso de Reclutamiento del que viene esta persona. Va
            // antes del SaveChanges final para que el requerimiento y el EMO se guarden juntos: un
            // EMO Apto cuyo requerimiento no llegó a cerrarse deja el proceso trabado en la fase del
            // examen sin nada que lo destrabe. Solo hace algo con el EMO de Ingreso de una ficha de
            // pre-ingreso; en cualquier otro caso no toca nada.
            await _reclutamientoEmo.AplicarAptitudAsync(ctx, worker, tipo.Nombre, dto.Aptitud, userId);

            await ctx.SaveChangesAsync();
            return new EmoCreateResultDto
            {
                EmoId = emo.Id,
                InterconsultaId = interconsultaPendiente?.Id
            };
        }

        public async Task Update(int id, EmoUpdateDto dto, int? userId)
        {
            using var ctx = _factory.CreateDbContext();
            var emo = await ctx.WorkerEmo.FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new AbrilException("EMO no encontrado.", 404);

            var tipo = await ctx.SsEmoTipo.FirstOrDefaultAsync(t => t.Id == dto.TipoEmoId)
                ?? throw new AbrilException("Tipo de EMO no válido.", 400);

            var worker = await ctx.Worker.FirstOrDefaultAsync(w => w.Id == emo.WorkerId)
                ?? throw new AbrilException("Trabajador no encontrado.", 404);

            var vigenciaMesesUpd = tipo.VigenciaMeses ?? 0;
            if (worker.ObraOficinaStaffId == ObraOficinaStaffIds.OficinaCentral)
                vigenciaMesesUpd = 24;
            var fechaVencCalc = vigenciaMesesUpd > 0
                ? (DateOnly?)dto.FechaEmo.AddMonths(vigenciaMesesUpd)
                : null;

            var esApto = !string.Equals(dto.Aptitud, "Observado", StringComparison.OrdinalIgnoreCase)
                      && !string.Equals(dto.Aptitud, "No Apto", StringComparison.OrdinalIgnoreCase);

            emo.TipoEmoId = dto.TipoEmoId;
            emo.EmpresaOrigenId = dto.EmpresaOrigenId;
            emo.FechaEmo = dto.FechaEmo;
            emo.FechaVencimiento = fechaVencCalc;
            emo.FechaVencimientoCalculada = fechaVencCalc;
            emo.ClinicaId = dto.ClinicaId;
            emo.MedicoId = dto.MedicoId;
            emo.Aptitud = dto.Aptitud;
            emo.RequiereInterconsulta = dto.RequiereInterconsulta;
            emo.NumeroInforme = dto.NumeroInforme;
            emo.UrlResultado = dto.UrlResultado;
            emo.RequiereLecturaAbril = dto.RequiereLecturaAbril;
            emo.Notas = dto.Notas;
            emo.Estado = esApto ? "Vigente" : "Observado";
            emo.UpdatedAt = DateTimeOffset.UtcNow;

            // Si al editar el EMO la aptitud pasa a Apto/Apto con Restricciones, cualquier
            // interconsulta "Pendiente" ligada a este mismo EMO se da por resuelta acá: el
            // médico que edita el EMO ya tomó la decisión de aptitud, y sin este cierre la
            // interconsulta se queda "Pendiente" para siempre (visible en el listado de
            // Interconsultas) aunque el Certificado de Aptitud ya muestre "Aprobado" — mismo
            // huérfano que el caso ya resuelto en Create() para EMOs reemplazados, pero acá el
            // EMO es el mismo, solo se le cambió la aptitud sin pasar por el flujo de
            // InterconsultaService.UpdateResultado.
            if (esApto)
            {
                var interconsultasAbiertas = await ctx.SsInterconsulta
                    .Where(i => i.EmoId == id && i.Estado == "Pendiente")
                    .ToListAsync();
                foreach (var ic in interconsultasAbiertas)
                {
                    ic.Estado = "Completado";
                    ic.FechaAtencion ??= dto.FechaEmo;
                    ic.Resultado = string.IsNullOrWhiteSpace(ic.Resultado)
                        ? "Resuelta automáticamente: la aptitud del EMO se actualizó a " + dto.Aptitud + "."
                        : ic.Resultado;
                    ic.UpdatedAt = DateTimeOffset.UtcNow;
                }
                emo.InterconsultaResuelta = true;

                // Se persiste ya: SincronizarEntregableEmoAsync (más abajo) consulta
                // ss_interconsultas directo contra la base para decidir si el Certificado de
                // Aptitud puede pasar a "Aprobado" — sin este guardado intermedio vería el
                // estado "Pendiente" todavía no confirmado y dejaría el ítem en "En plazo" por
                // error, aunque la interconsulta ya se acaba de cerrar en esta misma edición.
                await ctx.SaveChangesAsync();
            }

            var examenesExistentes = await ctx.SsEmoExamenDetalle.Where(x => x.EmoId == id).ToListAsync();
            ctx.SsEmoExamenDetalle.RemoveRange(examenesExistentes);
            foreach (var ex in dto.Examenes)
            {
                ctx.SsEmoExamenDetalle.Add(new SsEmoExamenDetalle
                {
                    EmoId = id,
                    ExamenTipoId = ex.ExamenTipoId,
                    Resultado = ex.Resultado,
                    Valor = ex.Valor,
                    Unidad = ex.Unidad,
                    Observacion = ex.Observacion,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            var restriccionesExistentes = await ctx.SsEmoRestriccion.Where(x => x.EmoId == id).ToListAsync();
            ctx.SsEmoRestriccion.RemoveRange(restriccionesExistentes);
            foreach (var r in dto.Restricciones)
            {
                ctx.SsEmoRestriccion.Add(new SsEmoRestriccion
                {
                    EmoId = id,
                    RestriccionTipoId = r.RestriccionTipoId,
                    DescripcionLibre = r.DescripcionLibre,
                    Vigente = r.Vigente,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            if (worker != null)
            {
                if (dto.Aptitud == "No Apto")
                {
                    worker.HabilitadoObra = false;
                    worker.UpdatedAt = DateTimeOffset.UtcNow;
                }
                await SincronizarEntregableEmoAsync(ctx, emo, worker);

                // Editar la aptitud vale lo mismo que registrarla: es el camino por el que se
                // resuelve un "Observado" tras la interconsulta (y por el que se corrige un No Apto
                // mal cargado), así que el requerimiento tiene que moverse igual que en Create.
                await _reclutamientoEmo.AplicarAptitudAsync(ctx, worker, tipo.Nombre, dto.Aptitud, userId);
            }

            await ctx.SaveChangesAsync();
        }

        /// <summary>
        /// Completa la lectura de un EMO marcado como RequiereLecturaAbril: guarda la fecha y el
        /// archivo, y corre la misma sincronización de habilitaciones (SincronizarEntregableEmoAsync)
        /// que Create()/Update() — el mismo proceso que sigue una clínica al subir su lectura.
        /// </summary>
        public async Task CompletarLecturaAbril(int id, DateOnly fechaLectura, string urlResultado, int? userId)
        {
            using var ctx = _factory.CreateDbContext();
            var emo = await ctx.WorkerEmo.FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new AbrilException("EMO no encontrado.", 404);
            var worker = await ctx.Worker.FirstOrDefaultAsync(w => w.Id == emo.WorkerId)
                ?? throw new AbrilException("Trabajador no encontrado.", 404);

            emo.FechaLectura = fechaLectura;
            emo.UrlResultado = urlResultado;
            emo.UpdatedAt = DateTimeOffset.UtcNow;

            await SincronizarEntregableEmoAsync(ctx, emo, worker);
            await ctx.SaveChangesAsync();
        }

        public async Task UpdateEstado(int id, string estado, int? userId)
        {
            using var ctx = _factory.CreateDbContext();
            var emo = await ctx.WorkerEmo.FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new AbrilException("EMO no encontrado.", 404);
            emo.Estado = estado;
            emo.UpdatedAt = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }

        /// <summary>
        /// Devuelve la fila de ss_hab_trabajador del par (worker, ítem), creándola si no existe.
        /// Mira primero las entidades ya rastreadas por el contexto: una fila agregada antes en
        /// esta misma unidad de trabajo (todavía sin SaveChanges) es invisible para la consulta a
        /// la base, así que un segundo bloque la volvía a crear y las dos altas salían en el mismo
        /// SaveChanges — que la restricción UNIQUE (worker_id, item_id) rechaza con 23505. Era el
        /// caso real al registrar un resultado de EMO con fecha de lectura y archivo de lectura a
        /// la vez: SincronizarEntregableEmoAsync creaba la fila de "Lectura de EMO" y el bloque de
        /// FechaLectura en Create() la creaba de nuevo, tumbando todo el registro.
        /// </summary>
        private static async Task<SsHabTrabajador> ObtenerOCrearHabAsync(AppDbContext ctx, int workerId, int itemId)
        {
            var hab = ctx.SsHabTrabajador.Local
                    .FirstOrDefault(h => h.WorkerId == workerId && h.ItemId == itemId)
                ?? await ctx.SsHabTrabajador
                    .FirstOrDefaultAsync(h => h.WorkerId == workerId && h.ItemId == itemId);

            if (hab == null)
            {
                hab = new SsHabTrabajador
                {
                    WorkerId = workerId,
                    ItemId = itemId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                ctx.SsHabTrabajador.Add(hab);
            }

            return hab;
        }

        private static async Task SincronizarEntregableEmoAsync(AppDbContext ctx, WorkerEmo emo, Worker worker)
        {
            var hab = await ObtenerOCrearHabAsync(ctx, emo.WorkerId, HabItemIds.CertAptitud);

            // Aunque la aptitud del EMO ya sea Apto/Apto con Restricciones, si sigue habiendo una
            // interconsulta "Pendiente" ligada a este EMO el caso no está realmente cerrado — no
            // tiene sentido mostrar el Certificado de Aptitud como "Aprobado" mientras el médico
            // todavía no atendió esa derivación. Se marca "En plazo" (mismo estado que usa el caso
            // "Observado" más abajo) hasta que la interconsulta se resuelva.
            var interconsultaPendienteEmo = await ctx.SsInterconsulta
                .AnyAsync(i => i.EmoId == emo.Id && i.Estado == "Pendiente");

            switch (emo.Aptitud)
            {
                case "Apto":
                case "Apto con Restricciones":
                    if (interconsultaPendienteEmo)
                    {
                        // "Falta" (no "En plazo") para que coincida con el otro camino de cálculo
                        // de este mismo ítem (HabTrabajadorRepository.GetById, cuando no existe
                        // fila en ss_hab_trabajador): ese usa "Falta" como el estado "todavía no
                        // aprobado" — es la palabra que ya reconoce el frontend/usuarios, no
                        // "En plazo".
                        hab.Estado = "Falta";
                        break;
                    }

                    // Si después de la fecha de este EMO el trabajador tuvo un cambio de empresa,
                    // puesto o clasificación (CambiarObraAsync) y ese cambio nunca generó una
                    // convalidación (p.ej. porque el EMO todavía no estaba Activo en ese momento —
                    // caso Reyes Carbajal), no corresponde aprobar el certificado directo: hay que
                    // armarle al médico la convalidación pendiente, igual que si el cambio de obra
                    // hubiera ocurrido con el EMO ya activo.
                    var fechaEmoDt = DateTime.SpecifyKind(emo.FechaEmo.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
                    var tieneCambioSinConvalidar = await ctx.WorkerEvento.AnyAsync(ev =>
                        ev.WorkerId == emo.WorkerId
                        && ev.CreatedAt > fechaEmoDt
                        && (ev.TipoEvento == WorkerTipoEvento.CambioEmpresa
                            || ev.TipoEvento == WorkerTipoEvento.CambioPuesto
                            || ev.TipoEvento == WorkerTipoEvento.CambioRiesgo));

                    if (tieneCambioSinConvalidar)
                    {
                        var yaConvalidado = await ctx.WorkerEmoConvalidacion
                            .AnyAsync(cv => cv.EmoId == emo.Id);

                        if (!yaConvalidado)
                        {
                            var vinculacionActual = await ctx.WorkerVinculacion
                                .Where(v => v.WorkerId == emo.WorkerId && v.FechaFin == null)
                                .OrderByDescending(v => v.FechaInicio)
                                .FirstOrDefaultAsync();

                            ctx.WorkerEmoConvalidacion.Add(new WorkerEmoConvalidacion
                            {
                                EmoId = emo.Id,
                                EmpresaDestinoId = vinculacionActual?.EmpresaId ?? emo.EmpresaOrigenId,
                                FechaConvalidacion = DateOnly.FromDateTime(DateTime.UtcNow),
                                Resultado = "Pendiente",
                                PuestoOrigen = emo.Worker?.PuestoCatalogo?.Nombre,
                                PuestoDestino = vinculacionActual?.Puesto ?? worker.PuestoCatalogo?.Nombre,
                                ObraOficinaStaffOrigenId = worker.ObraOficinaStaffId,
                                ObraOficinaStaffDestinoId = vinculacionActual?.ObraOficinaStaffId ?? worker.ObraOficinaStaffId,
                                CambioRiesgo = false,
                                CreatedAt = DateTimeOffset.UtcNow,
                                UpdatedAt = DateTimeOffset.UtcNow
                            });

                            emo.Estado = "Pendiente";
                            hab.Estado = "Pendiente";
                            break;
                        }
                    }

                    hab.Estado = "Aprobado";
                    var fv = emo.FechaVencimientoCalculada ?? emo.FechaVencimiento;
                    if (fv.HasValue)
                        hab.Vigencia = DateTime.SpecifyKind(fv.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
                    break;
                case "No Apto":
                    hab.Estado = "Rechazado";
                    break;
                case "Observado":
                    hab.Estado = "En plazo";
                    break;
                default:
                    return;
            }

            hab.UpdatedAt = DateTime.UtcNow;

            if (emo.UrlResultado != null &&
                (emo.Aptitud == "Apto" || emo.Aptitud == "Apto con Restricciones"))
            {
                var habLectura = await ObtenerOCrearHabAsync(ctx, emo.WorkerId, HabItemIds.LecturaEmo);
                habLectura.Estado = "Aprobado";
                var fvLectura = emo.FechaVencimientoCalculada ?? emo.FechaVencimiento;
                if (fvLectura.HasValue)
                    habLectura.Vigencia = DateTime.SpecifyKind(fvLectura.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
                habLectura.ArchivoUrl = emo.UrlResultado;
                habLectura.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
