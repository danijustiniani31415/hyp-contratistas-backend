using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Infrastructure.Models;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Application.Dtos;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Helpers;
using Abril_Backend.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Repositories
{
    public class ActasReunionRepository : IActasReunionRepository
    {
        public const string EstadoProgramada = "PROGRAMADA";
        public const string EstadoRealizada = "REALIZADA";
        public const string EstadoCancelada = "CANCELADA";
        public const string AcuerdoPendiente = "PENDIENTE";
        public const string AcuerdoCumplido = "CUMPLIDO";

        private readonly IDbContextFactory<AppDbContext> _factory;

        public ActasReunionRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        // ── Listado ──────────────────────────────────────────────────────────
        public async Task<ReunionPaginaInicialDto> GetPaginaInicial(ReunionFiltroRequest filtro, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var proyectos = await ctx.Project
                .Where(p => p.State && p.Active)
                .OrderBy(p => p.ProjectDescription)
                .Select(p => new ProyectoFiltroDto
                {
                    ProjectId = p.ProjectId,
                    ProjectDescription = p.ProjectDescription,
                })
                .ToListAsync();

            var estados = await ctx.ReunionEstado
                .Where(e => e.State && e.Active)
                .OrderBy(e => e.ReunionEstadoId)
                .Select(e => new CatalogoDto { Id = e.ReunionEstadoId, Descripcion = e.Descripcion })
                .ToListAsync();

            var trabajadores = await GetTrabajadoresAbril(ctx);

            var temas = await GetTemas(ctx);

            var reuniones = await GetReunionesInterno(ctx, filtro, userId);

            return new ReunionPaginaInicialDto
            {
                Proyectos = proyectos,
                ReunionEstados = estados,
                Trabajadores = trabajadores,
                Temas = temas,
                Reuniones = reuniones,
            };
        }

        public async Task<PagedResultDto<ReunionListItemDto>> GetReuniones(ReunionFiltroRequest filtro, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            return await GetReunionesInterno(ctx, filtro, userId);
        }

        /// <summary>Excepción temporal de pruebas: este usuario sigue viendo todas las reuniones de
        /// la organización en vez de solo las suyas, mientras se prueba el resto del módulo con
        /// datos de otros convocados. Quitar esta constante cuando ya no haga falta.</summary>
        private const int UserIdSinFiltroDeConvocatoria = 20;

        private static async Task<PagedResultDto<ReunionListItemDto>> GetReunionesInterno(AppDbContext ctx, ReunionFiltroRequest filtro, int userId)
        {
            var query = ctx.Reunion.Where(r => r.State);

            if (userId != UserIdSinFiltroDeConvocatoria)
            {
                // Solo se ven las reuniones propias: las que uno organizó (creó) o a las que fue
                // convocado como participante. El resto de la organización no debe aparecer.
                var workerId = await ResolveWorkerId(ctx, userId);
                query = query.Where(r =>
                    r.CreatedUserId == userId
                    || (workerId != null && ctx.ReunionParticipante.Any(p =>
                        p.ReunionId == r.ReunionId && p.State && p.WorkerId == workerId.Value)));
            }

            if (filtro.ProjectId.HasValue)
                query = query.Where(r => r.ProjectId == filtro.ProjectId.Value);
            if (filtro.AreaScopeId.HasValue)
            {
                var descendientes = await ctx.ResolveDescendantsAsync(filtro.AreaScopeId.Value);
                query = query.Where(r => r.AreaScopeId.HasValue && descendientes.Contains(r.AreaScopeId.Value));
            }
            if (filtro.ReunionEstadoId.HasValue)
                query = query.Where(r => r.ReunionEstadoId == filtro.ReunionEstadoId.Value);
            if (filtro.Desde.HasValue)
                query = query.Where(r => r.Fecha >= filtro.Desde.Value);
            if (filtro.Hasta.HasValue)
                query = query.Where(r => r.Fecha <= filtro.Hasta.Value);

            var page = filtro.Page < 1 ? 1 : filtro.Page;
            var pageSize = filtro.PageSize < 1 ? 10 : filtro.PageSize;

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(r => r.Fecha)
                .ThenByDescending(r => r.ReunionId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReunionListItemDto
                {
                    ReunionId = r.ReunionId,
                    ProjectId = r.ProjectId,
                    ProjectDescription = r.ProjectId == null ? null : ctx.Project
                        .Where(p => p.ProjectId == r.ProjectId.Value)
                        .Select(p => p.ProjectDescription)
                        .First(),
                    AreaScopeId = r.AreaScopeId,
                    AreaScopeDescripcion = r.AreaScopeId == null ? null : ctx.AreaScope
                        .Where(s => s.AreaScopeId == r.AreaScopeId.Value)
                        .Select(s => s.AreaItem!.AreaItemName)
                        .First(),
                    Numero = r.Numero,
                    Tema = r.Tema,
                    Lugar = r.Lugar,
                    Fecha = r.Fecha,
                    HoraInicio = r.HoraInicio,
                    HoraFin = r.HoraFin,
                    ReunionEstadoId = r.ReunionEstadoId,
                    ReunionEstado = ctx.ReunionEstado
                        .Where(e => e.ReunionEstadoId == r.ReunionEstadoId)
                        .Select(e => e.Descripcion)
                        .First(),
                    TotalAcuerdos = ctx.ReunionAcuerdo
                        .Count(a => a.ReunionId == r.ReunionId && a.State),
                    AcuerdosCumplidos = ctx.ReunionAcuerdo
                        .Count(a => a.ReunionId == r.ReunionId && a.State
                            && ctx.ReunionAcuerdoEstado
                                .Any(e => e.ReunionAcuerdoEstadoId == a.ReunionAcuerdoEstadoId
                                    && e.Descripcion == AcuerdoCumplido)),
                    VecesReprogramada = ctx.ReunionReprogramacion
                        .Count(x => x.ReunionId == r.ReunionId && x.State),
                    TotalArchivos = ctx.ReunionArchivo
                        .Count(x => x.ReunionId == r.ReunionId && x.State),
                })
                .ToListAsync();

            return new PagedResultDto<ReunionListItemDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Data = data,
            };
        }

        // ── Detalle ──────────────────────────────────────────────────────────
        public async Task<ReunionDetalleDto> GetDetalle(int reunionId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var detalle = await ctx.Reunion
                .Where(r => r.ReunionId == reunionId && r.State)
                .Select(r => new ReunionDetalleDto
                {
                    ReunionId = r.ReunionId,
                    CreatedUserId = r.CreatedUserId,
                    ProjectId = r.ProjectId,
                    ProjectDescription = r.ProjectId == null ? null : ctx.Project
                        .Where(p => p.ProjectId == r.ProjectId.Value)
                        .Select(p => p.ProjectDescription)
                        .First(),
                    AreaScopeId = r.AreaScopeId,
                    AreaScopeDescripcion = r.AreaScopeId == null ? null : ctx.AreaScope
                        .Where(s => s.AreaScopeId == r.AreaScopeId.Value)
                        .Select(s => s.AreaItem!.AreaItemName)
                        .First(),
                    Numero = r.Numero,
                    Tema = r.Tema,
                    ConvocadoPor = r.ConvocadoPor,
                    Lugar = r.Lugar,
                    Fecha = r.Fecha,
                    HoraInicio = r.HoraInicio,
                    HoraFin = r.HoraFin,
                    ReunionEstadoId = r.ReunionEstadoId,
                    ReunionEstado = ctx.ReunionEstado
                        .Where(e => e.ReunionEstadoId == r.ReunionEstadoId)
                        .Select(e => e.Descripcion)
                        .First(),
                    Observaciones = r.Observaciones,
                    ReunionAnteriorId = r.ReunionAnteriorId,
                })
                .FirstOrDefaultAsync();

            if (detalle is null)
                throw new AbrilException("El acta de reunión no existe.", 404);

            detalle.PuedeEditar = await PuedeEditarActa(ctx, reunionId, detalle.CreatedUserId, userId);

            if (detalle.ReunionAnteriorId.HasValue)
            {
                var anterior = await ctx.Reunion
                    .Where(r => r.ReunionId == detalle.ReunionAnteriorId.Value && r.State)
                    .Select(r => new { r.Numero, r.Tema })
                    .FirstOrDefaultAsync();
                detalle.ReunionAnteriorNumero = anterior?.Numero;
                detalle.ReunionAnteriorTema = anterior?.Tema;
            }

            var siguiente = await ctx.Reunion
                .Where(r => r.ReunionAnteriorId == reunionId && r.State)
                .OrderBy(r => r.ReunionId)
                .Select(r => new { r.ReunionId, r.Numero, r.Tema })
                .FirstOrDefaultAsync();
            detalle.ReunionSiguienteId = siguiente?.ReunionId;
            detalle.ReunionSiguienteNumero = siguiente?.Numero;
            detalle.ReunionSiguienteTema = siguiente?.Tema;

            detalle.Participantes = await ctx.ReunionParticipante
                .Where(p => p.ReunionId == reunionId && p.State)
                .OrderBy(p => p.Orden).ThenBy(p => p.ReunionParticipanteId)
                .Select(p => new ReunionParticipanteDto
                {
                    ReunionParticipanteId = p.ReunionParticipanteId,
                    WorkerId = p.WorkerId,
                    Nombre = p.Nombre,
                    Cargo = p.Cargo,
                    Iniciales = p.Iniciales,
                    Asistio = p.Asistio,
                    Orden = p.Orden,
                    EsCoautor = p.EsCoautor,
                })
                .ToListAsync();

            detalle.Acuerdos = await ctx.ReunionAcuerdo
                .Where(a => a.ReunionId == reunionId && a.State)
                .OrderBy(a => a.Orden).ThenBy(a => a.ReunionAcuerdoId)
                .Select(a => new ReunionAcuerdoDto
                {
                    ReunionAcuerdoId = a.ReunionAcuerdoId,
                    Descripcion = a.Descripcion,
                    Acciones = a.Acciones,
                    FechaProgramada = a.FechaProgramada,
                    FechaReprogramacion = a.FechaReprogramacion,
                    FechaCumplimiento = a.FechaCumplimiento,
                    ReunionAcuerdoEstadoId = a.ReunionAcuerdoEstadoId,
                    ReunionAcuerdoEstado = ctx.ReunionAcuerdoEstado
                        .Where(e => e.ReunionAcuerdoEstadoId == a.ReunionAcuerdoEstadoId)
                        .Select(e => e.Descripcion)
                        .First(),
                    Orden = a.Orden,
                    Criticidad = a.Criticidad,
                    RequiereAceptacion = a.RequiereAceptacion,
                    RequiereEvidencia = a.RequiereEvidencia,
                    EvidenciaUrl = a.EvidenciaUrl,
                    EsInformativo = a.EsInformativo,
                    ComentarioCumplimiento = a.ComentarioCumplimiento,
                    VecesReprogramado = a.VecesReprogramado,
                    UltimoMotivoReprogramacion = a.UltimoMotivoReprogramacion,
                })
                .ToListAsync();

            // Responsables de todos los acuerdos en una sola consulta
            if (detalle.Acuerdos.Count > 0)
            {
                var acuerdoIds = detalle.Acuerdos.Select(a => a.ReunionAcuerdoId).ToList();
                var responsables = await (
                    from x in ctx.ReunionAcuerdoResponsable
                    where acuerdoIds.Contains(x.ReunionAcuerdoId) && x.State && x.WorkerId != null
                    join w in ctx.Worker on x.WorkerId equals w.Id
                    join per in ctx.Person on w.PersonId equals per.PersonId
                    select new
                    {
                        x.ReunionAcuerdoId,
                        x.ReunionAcuerdoResponsableId,
                        WorkerId = x.WorkerId!.Value,
                        WorkerNombre = per.FullName,
                        x.EstadoAceptacion,
                        x.MotivoRechazo,
                        x.EsPrincipal,
                    }
                ).ToListAsync();
                var porAcuerdo = responsables.GroupBy(x => x.ReunionAcuerdoId).ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new ReunionAcuerdoResponsableDto
                    {
                        ReunionAcuerdoResponsableId = x.ReunionAcuerdoResponsableId,
                        WorkerId = x.WorkerId,
                        WorkerNombre = x.WorkerNombre,
                        EstadoAceptacion = x.EstadoAceptacion,
                        MotivoRechazo = x.MotivoRechazo,
                        EsPrincipal = x.EsPrincipal,
                    }).ToList());
                foreach (var acuerdo in detalle.Acuerdos)
                    acuerdo.Responsables = porAcuerdo.TryGetValue(acuerdo.ReunionAcuerdoId, out var lista)
                        ? lista
                        : new List<ReunionAcuerdoResponsableDto>();
            }

            detalle.Archivos = await ctx.ReunionArchivo
                .Where(x => x.ReunionId == reunionId && x.State)
                .OrderBy(x => x.ReunionArchivoId)
                .Select(x => new ReunionArchivoDto
                {
                    ReunionArchivoId = x.ReunionArchivoId,
                    ArchivoUrl = x.ArchivoUrl,
                    OriginalFileName = x.OriginalFileName,
                    CreatedDateTime = x.CreatedDateTime,
                })
                .ToListAsync();

            detalle.Reprogramaciones = await ctx.ReunionReprogramacion
                .Where(x => x.ReunionId == reunionId && x.State)
                .OrderByDescending(x => x.ReunionReprogramacionId)
                .Select(x => new ReunionReprogramacionDto
                {
                    ReunionReprogramacionId = x.ReunionReprogramacionId,
                    FechaAnterior = x.FechaAnterior,
                    HoraInicioAnterior = x.HoraInicioAnterior,
                    HoraFinAnterior = x.HoraFinAnterior,
                    FechaNueva = x.FechaNueva,
                    HoraInicioNueva = x.HoraInicioNueva,
                    HoraFinNueva = x.HoraFinNueva,
                    Motivo = x.Motivo,
                    CreatedDateTime = x.CreatedDateTime,
                    CreatedUserName = ctx.Person
                        .Where(p => p.UserId == x.CreatedUserId)
                        .Select(p => p.FullName)
                        .FirstOrDefault(),
                })
                .ToListAsync();

            detalle.AcuerdoEstados = await ctx.ReunionAcuerdoEstado
                .Where(e => e.State && e.Active)
                .OrderBy(e => e.ReunionAcuerdoEstadoId)
                .Select(e => new CatalogoDto { Id = e.ReunionAcuerdoEstadoId, Descripcion = e.Descripcion })
                .ToListAsync();

            detalle.Trabajadores = await GetTrabajadoresAbril(ctx);

            detalle.Temas = await GetTemas(ctx);

            return detalle;
        }

        // ── Creación / edición ───────────────────────────────────────────────
        public async Task<int> Create(ReunionCreateRequest request, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            if (request.ProjectId.HasValue)
            {
                var proyectoExiste = await ctx.Project.AnyAsync(p => p.ProjectId == request.ProjectId.Value && p.State);
                if (!proyectoExiste)
                    throw new AbrilException("El proyecto seleccionado no existe.", 400);
            }
            if (request.AreaScopeId.HasValue)
            {
                var areaScopeExiste = await ctx.AreaScope.AnyAsync(s => s.AreaScopeId == request.AreaScopeId.Value && s.State);
                if (!areaScopeExiste)
                    throw new AbrilException("El área/gerencia seleccionada no existe.", 400);
            }
            if (request.ReunionTemaId.HasValue)
            {
                var temaExiste = await ctx.ReunionTema.AnyAsync(t => t.ReunionTemaId == request.ReunionTemaId.Value && t.State);
                if (!temaExiste)
                    throw new AbrilException("El tema del catálogo seleccionado no existe.", 400);
            }

            if (request.ReunionAnteriorId.HasValue)
            {
                var anteriorValida = await ctx.Reunion.AnyAsync(r =>
                    r.ReunionId == request.ReunionAnteriorId.Value
                    && r.ProjectId == request.ProjectId
                    && r.AreaScopeId == request.AreaScopeId
                    && r.State);
                if (!anteriorValida)
                    throw new AbrilException("La reunión anterior indicada no pertenece al mismo ámbito.", 400);
            }

            var estadoProgramadaId = await GetEstadoReunionId(ctx, EstadoProgramada);

            var numero = (await ctx.Reunion
                .Where(r => r.ProjectId == request.ProjectId && r.AreaScopeId == request.AreaScopeId && r.State)
                .MaxAsync(r => (int?)r.Numero) ?? 0) + 1;

            var now = DateTime.UtcNow;
            var reunion = new Reunion
            {
                ProjectId = request.ProjectId,
                AreaScopeId = request.AreaScopeId,
                ReunionTemaId = request.ReunionTemaId,
                Numero = numero,
                Tema = request.Tema.Trim(),
                ConvocadoPor = request.ConvocadoPor?.Trim(),
                Lugar = request.Lugar?.Trim(),
                Fecha = request.Fecha,
                HoraInicio = request.HoraInicio,
                HoraFin = request.HoraFin,
                ReunionEstadoId = estadoProgramadaId,
                ReunionAnteriorId = request.ReunionAnteriorId,
                AgendaTexto = request.AgendaTexto?.Trim(),
                CreatedDateTime = now,
                CreatedUserId = userId,
                Active = true,
                State = true,
            };
            ctx.Reunion.Add(reunion);
            await ctx.SaveChangesAsync();

            var entrantes = request.Participantes.Where(p => !string.IsNullOrWhiteSpace(p.Nombre)).ToList();
            var orden = 0;
            foreach (var p in entrantes)
            {
                ctx.ReunionParticipante.Add(new ReunionParticipante
                {
                    ReunionId = reunion.ReunionId,
                    WorkerId = p.WorkerId,
                    Nombre = p.Nombre.Trim(),
                    Cargo = p.Cargo?.Trim(),
                    Iniciales = p.Iniciales?.Trim(),
                    Asistio = p.Asistio,
                    Orden = orden++,
                    CreatedDateTime = now,
                    CreatedUserId = userId,
                    Active = true,
                    State = true,
                });
            }
            await BackfillPuestoTrabajadores(ctx, entrantes);
            if (orden > 0 || ctx.ChangeTracker.HasChanges())
                await ctx.SaveChangesAsync();

            return reunion.ReunionId;
        }

        public async Task<List<int>> Update(int reunionId, ReunionUpdateRequest request, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var reunion = await GetReunionOrThrow(ctx, reunionId);
            await EnsurePuedeEditarActa(ctx, reunionId, reunion.CreatedUserId, userId);

            var now = DateTime.UtcNow;
            reunion.Tema = request.Tema.Trim();
            reunion.ConvocadoPor = request.ConvocadoPor?.Trim();
            reunion.Lugar = request.Lugar?.Trim();
            reunion.HoraInicio = request.HoraInicio;
            reunion.HoraFin = request.HoraFin;
            reunion.Observaciones = request.Observaciones?.Trim();
            reunion.UpdatedDateTime = now;
            reunion.UpdatedUserId = userId;

            var existentes = await ctx.ReunionParticipante
                .Where(p => p.ReunionId == reunionId && p.State)
                .ToListAsync();

            var entrantes = request.Participantes
                .Where(p => !string.IsNullOrWhiteSpace(p.Nombre))
                .ToList();
            var idsEntrantes = entrantes
                .Where(p => p.ReunionParticipanteId.HasValue)
                .Select(p => p.ReunionParticipanteId!.Value)
                .ToHashSet();

            // Participantes quitados: soft delete + soft delete de sus responsabilidades
            var eliminados = existentes.Where(p => !idsEntrantes.Contains(p.ReunionParticipanteId)).ToList();
            if (eliminados.Count > 0)
            {
                var idsEliminados = eliminados.Select(p => p.ReunionParticipanteId).ToList();
                var responsabilidades = await ctx.ReunionAcuerdoResponsable
                    .Where(x => x.ReunionParticipanteId != null && idsEliminados.Contains(x.ReunionParticipanteId.Value) && x.State)
                    .ToListAsync();
                foreach (var resp in responsabilidades)
                {
                    resp.State = false;
                    resp.UpdatedDateTime = now;
                    resp.UpdatedUserId = userId;
                }
                foreach (var p in eliminados)
                {
                    p.State = false;
                    p.UpdatedDateTime = now;
                    p.UpdatedUserId = userId;
                }
            }

            // Participantes nuevos (con WorkerId) para avisarles por correo tras guardar — los que
            // ya estaban no se vuelven a notificar en cada edición.
            var nuevosWorkerIds = new List<int>();

            var orden = 0;
            foreach (var input in entrantes)
            {
                if (input.ReunionParticipanteId.HasValue)
                {
                    var existente = existentes.FirstOrDefault(p => p.ReunionParticipanteId == input.ReunionParticipanteId.Value);
                    if (existente is null)
                        throw new AbrilException("Uno de los participantes enviados no pertenece a la reunión.", 400);
                    existente.WorkerId = input.WorkerId;
                    existente.Nombre = input.Nombre.Trim();
                    existente.Cargo = input.Cargo?.Trim();
                    existente.Iniciales = input.Iniciales?.Trim();
                    existente.Asistio = input.Asistio;
                    existente.EsCoautor = input.WorkerId.HasValue && input.EsCoautor;
                    existente.Orden = orden++;
                    existente.UpdatedDateTime = now;
                    existente.UpdatedUserId = userId;
                }
                else
                {
                    ctx.ReunionParticipante.Add(new ReunionParticipante
                    {
                        ReunionId = reunionId,
                        WorkerId = input.WorkerId,
                        Nombre = input.Nombre.Trim(),
                        Cargo = input.Cargo?.Trim(),
                        Iniciales = input.Iniciales?.Trim(),
                        Asistio = input.Asistio,
                        EsCoautor = input.WorkerId.HasValue && input.EsCoautor,
                        Orden = orden++,
                        CreatedDateTime = now,
                        CreatedUserId = userId,
                        Active = true,
                        State = true,
                    });
                    if (input.WorkerId.HasValue) nuevosWorkerIds.Add(input.WorkerId.Value);
                }
            }

            await BackfillPuestoTrabajadores(ctx, entrantes);

            await ctx.SaveChangesAsync();

            return nuevosWorkerIds;
        }

        public async Task Reprogramar(int reunionId, ReunionReprogramarRequest request, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var reunion = await GetReunionOrThrow(ctx, reunionId);
            await EnsurePuedeEditarActa(ctx, reunionId, reunion.CreatedUserId, userId);

            var estadoActual = await ctx.ReunionEstado
                .Where(e => e.ReunionEstadoId == reunion.ReunionEstadoId)
                .Select(e => e.Descripcion)
                .FirstAsync();
            if (estadoActual == EstadoRealizada)
                throw new AbrilException("No se puede reprogramar una reunión que ya fue realizada.", 400);

            var now = DateTime.UtcNow;
            ctx.ReunionReprogramacion.Add(new ReunionReprogramacion
            {
                ReunionId = reunionId,
                FechaAnterior = reunion.Fecha,
                HoraInicioAnterior = reunion.HoraInicio,
                HoraFinAnterior = reunion.HoraFin,
                FechaNueva = request.Fecha,
                HoraInicioNueva = request.HoraInicio,
                HoraFinNueva = request.HoraFin,
                Motivo = request.Motivo?.Trim(),
                CreatedDateTime = now,
                CreatedUserId = userId,
                Active = true,
                State = true,
            });

            reunion.Fecha = request.Fecha;
            reunion.HoraInicio = request.HoraInicio;
            reunion.HoraFin = request.HoraFin;
            reunion.UpdatedDateTime = now;
            reunion.UpdatedUserId = userId;

            // Reprogramar una reunión cancelada la vuelve a dejar programada.
            if (estadoActual == EstadoCancelada)
                reunion.ReunionEstadoId = await GetEstadoReunionId(ctx, EstadoProgramada);

            await ctx.SaveChangesAsync();
        }

        public async Task CambiarEstado(int reunionId, string estado, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var reunion = await GetReunionOrThrow(ctx, reunionId);
            await EnsurePuedeEditarActa(ctx, reunionId, reunion.CreatedUserId, userId);
            var estadoId = await GetEstadoReunionId(ctx, estado);

            reunion.ReunionEstadoId = estadoId;
            reunion.UpdatedDateTime = DateTime.UtcNow;
            reunion.UpdatedUserId = userId;
            await ctx.SaveChangesAsync();
        }

        public async Task Eliminar(int reunionId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var reunion = await GetReunionOrThrow(ctx, reunionId);
            await EnsurePuedeEditarActa(ctx, reunionId, reunion.CreatedUserId, userId);

            var tieneSiguiente = await ctx.Reunion.AnyAsync(r => r.ReunionAnteriorId == reunionId && r.State);
            if (tieneSiguiente)
                throw new AbrilException("No se puede eliminar: otra reunión promovió su tema desde esta acta.", 400);

            // Eliminar es un borrado lógico total (oculta la reunión y todo lo que cuelga de ella).
            // Si ya se realizó o ya tiene acuerdos/archivos, esconderla de golpe sería peligroso para
            // el seguimiento de compromisos: en esos casos corresponde "Cancelar" (conserva el registro
            // visible) en vez de "Eliminar".
            var estadoRealizadaId = await GetEstadoReunionId(ctx, EstadoRealizada);
            if (reunion.ReunionEstadoId == estadoRealizadaId)
                throw new AbrilException("No se puede eliminar: esta reunión ya fue realizada. Usa \"Cancelar\" en su lugar.", 400);

            var tieneAcuerdos = await ctx.ReunionAcuerdo.AnyAsync(a => a.ReunionId == reunionId && a.State);
            if (tieneAcuerdos)
                throw new AbrilException("No se puede eliminar: esta reunión ya tiene acuerdos registrados. Usa \"Cancelar\" en su lugar.", 400);

            var tieneArchivos = await ctx.ReunionArchivo.AnyAsync(a => a.ReunionId == reunionId && a.State);
            if (tieneArchivos)
                throw new AbrilException("No se puede eliminar: esta reunión ya tiene archivos adjuntos. Usa \"Cancelar\" en su lugar.", 400);

            var now = DateTime.UtcNow;
            reunion.State = false;
            reunion.UpdatedDateTime = now;
            reunion.UpdatedUserId = userId;

            // El borrado debe propagarse: participantes y temas de agenda ya cargados no deben
            // quedar "vivos" sueltos de una reunión que ya no existe para nadie (acuerdos/archivos
            // ya se descartaron arriba, así que no hay responsables que limpiar acá).
            var participantes = await ctx.ReunionParticipante
                .Where(p => p.ReunionId == reunionId && p.State)
                .ToListAsync();
            foreach (var p in participantes)
            {
                p.State = false;
                p.UpdatedDateTime = now;
                p.UpdatedUserId = userId;
            }

            var agendaItems = await ctx.ReunionAgendaItem
                .Where(a => a.ReunionId == reunionId && a.State)
                .ToListAsync();
            foreach (var a in agendaItems)
            {
                a.State = false;
                a.UpdatedDateTime = now;
                a.UpdatedUserId = userId;
            }

            await ctx.SaveChangesAsync();
        }

        // ── Acuerdos ─────────────────────────────────────────────────────────
        public async Task<int> CrearAcuerdo(int reunionId, ReunionAcuerdoRequest request, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var reunion = await GetReunionOrThrow(ctx, reunionId);
            await EnsurePuedeEditarActa(ctx, reunionId, reunion.CreatedUserId, userId);
            await ValidarResponsables(ctx, request.ResponsableWorkerIds);

            var estadoId = request.ReunionAcuerdoEstadoId
                ?? await GetEstadoAcuerdoId(ctx, AcuerdoPendiente);

            var now = DateTime.UtcNow;
            var orden = (await ctx.ReunionAcuerdo
                .Where(a => a.ReunionId == reunionId && a.State)
                .MaxAsync(a => (int?)a.Orden) ?? 0) + 1;

            var acuerdo = new ReunionAcuerdo
            {
                ReunionId = reunionId,
                Descripcion = request.Descripcion.Trim(),
                Acciones = request.Acciones?.Trim(),
                FechaProgramada = request.FechaProgramada,
                FechaReprogramacion = request.FechaReprogramacion,
                FechaCumplimiento = request.FechaCumplimiento,
                ReunionAcuerdoEstadoId = estadoId,
                Orden = orden,
                Criticidad = string.IsNullOrWhiteSpace(request.Criticidad) ? "NORMAL" : request.Criticidad,
                RequiereAceptacion = request.RequiereAceptacion,
                RequiereEvidencia = request.RequiereEvidencia,
                EvidenciaUrl = request.EvidenciaUrl?.Trim(),
                EsInformativo = request.EsInformativo,
                CreatedDateTime = now,
                CreatedUserId = userId,
                Active = true,
                State = true,
            };
            ctx.ReunionAcuerdo.Add(acuerdo);
            await ctx.SaveChangesAsync();

            var estadoAceptacionInicial = request.RequiereAceptacion ? "PENDIENTE" : "ACEPTADO";
            foreach (var workerId in request.ResponsableWorkerIds.Distinct())
            {
                ctx.ReunionAcuerdoResponsable.Add(new ReunionAcuerdoResponsable
                {
                    ReunionAcuerdoId = acuerdo.ReunionAcuerdoId,
                    WorkerId = workerId,
                    EstadoAceptacion = estadoAceptacionInicial,
                    EsPrincipal = request.ResponsablePrincipalWorkerId.HasValue && request.ResponsablePrincipalWorkerId.Value == workerId,
                    CreatedDateTime = now,
                    CreatedUserId = userId,
                    Active = true,
                    State = true,
                });
            }
            if (request.ResponsableWorkerIds.Count > 0)
                await ctx.SaveChangesAsync();

            return acuerdo.ReunionAcuerdoId;
        }

        public async Task ActualizarAcuerdo(int reunionAcuerdoId, ReunionAcuerdoRequest request, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var acuerdo = await ctx.ReunionAcuerdo
                .FirstOrDefaultAsync(a => a.ReunionAcuerdoId == reunionAcuerdoId && a.State);
            if (acuerdo is null)
                throw new AbrilException("El acuerdo no existe.", 404);

            await EnsurePuedeEditarActaByReunionId(ctx, acuerdo.ReunionId, userId);
            await ValidarResponsables(ctx, request.ResponsableWorkerIds);

            if (request.RequiereEvidencia && string.IsNullOrWhiteSpace(request.EvidenciaUrl))
            {
                var estadoDestinoId = request.ReunionAcuerdoEstadoId ?? acuerdo.ReunionAcuerdoEstadoId;
                var estadoDestino = await GetEstadoAcuerdoDescripcion(ctx, estadoDestinoId);
                if (estadoDestino == AcuerdoCumplido)
                    throw new AbrilException("Este acuerdo requiere evidencia para poder marcarse como cumplido.", 400);
            }

            var now = DateTime.UtcNow;
            acuerdo.Descripcion = request.Descripcion.Trim();
            acuerdo.Acciones = request.Acciones?.Trim();
            acuerdo.FechaProgramada = request.FechaProgramada;
            acuerdo.FechaReprogramacion = request.FechaReprogramacion;
            acuerdo.FechaCumplimiento = request.FechaCumplimiento;
            acuerdo.Criticidad = string.IsNullOrWhiteSpace(request.Criticidad) ? "NORMAL" : request.Criticidad;
            acuerdo.RequiereAceptacion = request.RequiereAceptacion;
            acuerdo.RequiereEvidencia = request.RequiereEvidencia;
            acuerdo.EvidenciaUrl = request.EvidenciaUrl?.Trim();
            acuerdo.EsInformativo = request.EsInformativo;
            if (request.ReunionAcuerdoEstadoId.HasValue)
                acuerdo.ReunionAcuerdoEstadoId = request.ReunionAcuerdoEstadoId.Value;
            acuerdo.UpdatedDateTime = now;
            acuerdo.UpdatedUserId = userId;

            // Sincroniza responsables: agrega los nuevos y desactiva los quitados. Solo se
            // comparan contra responsables ya basados en worker_id (el legacy por participante
            // no se toca acá y se desactiva únicamente cuando se quita al participante).
            var actuales = await ctx.ReunionAcuerdoResponsable
                .Where(x => x.ReunionAcuerdoId == reunionAcuerdoId && x.State && x.WorkerId != null)
                .ToListAsync();
            var idsNuevos = request.ResponsableWorkerIds.Distinct().ToHashSet();

            foreach (var actual in actuales.Where(x => !idsNuevos.Contains(x.WorkerId!.Value)))
            {
                actual.State = false;
                actual.UpdatedDateTime = now;
                actual.UpdatedUserId = userId;
            }
            var idsActuales = actuales.Select(x => x.WorkerId!.Value).ToHashSet();
            var estadoAceptacionInicial = request.RequiereAceptacion ? "PENDIENTE" : "ACEPTADO";
            foreach (var workerId in idsNuevos.Where(id => !idsActuales.Contains(id)))
            {
                ctx.ReunionAcuerdoResponsable.Add(new ReunionAcuerdoResponsable
                {
                    ReunionAcuerdoId = reunionAcuerdoId,
                    WorkerId = workerId,
                    EstadoAceptacion = estadoAceptacionInicial,
                    EsPrincipal = request.ResponsablePrincipalWorkerId.HasValue && request.ResponsablePrincipalWorkerId.Value == workerId,
                    CreatedDateTime = now,
                    CreatedUserId = userId,
                    Active = true,
                    State = true,
                });
            }

            // El principal también puede cambiar entre responsables que ya estaban.
            foreach (var actual in actuales.Where(x => idsNuevos.Contains(x.WorkerId!.Value)))
            {
                var debeSerPrincipal = request.ResponsablePrincipalWorkerId.HasValue
                    && request.ResponsablePrincipalWorkerId.Value == actual.WorkerId!.Value;
                if (actual.EsPrincipal != debeSerPrincipal)
                {
                    actual.EsPrincipal = debeSerPrincipal;
                    actual.UpdatedDateTime = now;
                    actual.UpdatedUserId = userId;
                }
            }

            await ctx.SaveChangesAsync();
        }

        public async Task EliminarAcuerdo(int reunionAcuerdoId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var acuerdo = await ctx.ReunionAcuerdo
                .FirstOrDefaultAsync(a => a.ReunionAcuerdoId == reunionAcuerdoId && a.State);
            if (acuerdo is null)
                throw new AbrilException("El acuerdo no existe.", 404);

            await EnsurePuedeEditarActaByReunionId(ctx, acuerdo.ReunionId, userId);

            var now = DateTime.UtcNow;
            acuerdo.State = false;
            acuerdo.UpdatedDateTime = now;
            acuerdo.UpdatedUserId = userId;

            var responsables = await ctx.ReunionAcuerdoResponsable
                .Where(x => x.ReunionAcuerdoId == reunionAcuerdoId && x.State)
                .ToListAsync();
            foreach (var resp in responsables)
            {
                resp.State = false;
                resp.UpdatedDateTime = now;
                resp.UpdatedUserId = userId;
            }

            await ctx.SaveChangesAsync();
        }

        // ── Archivos ─────────────────────────────────────────────────────────
        public async Task<List<ReunionArchivoDto>> AgregarArchivos(int reunionId, List<(string Url, string? OriginalFileName)> archivos, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            await GetReunionOrThrow(ctx, reunionId);

            var now = DateTime.UtcNow;
            var entidades = archivos.Select(a => new ReunionArchivo
            {
                ReunionId = reunionId,
                ArchivoUrl = a.Url,
                OriginalFileName = a.OriginalFileName,
                CreatedDateTime = now,
                CreatedUserId = userId,
                Active = true,
                State = true,
            }).ToList();

            ctx.ReunionArchivo.AddRange(entidades);
            await ctx.SaveChangesAsync();

            return entidades.Select(e => new ReunionArchivoDto
            {
                ReunionArchivoId = e.ReunionArchivoId,
                ArchivoUrl = e.ArchivoUrl,
                OriginalFileName = e.OriginalFileName,
                CreatedDateTime = e.CreatedDateTime,
            }).ToList();
        }

        public async Task EliminarArchivo(int reunionArchivoId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var archivo = await ctx.ReunionArchivo
                .FirstOrDefaultAsync(x => x.ReunionArchivoId == reunionArchivoId && x.State);
            if (archivo is null)
                throw new AbrilException("El archivo no existe.", 404);

            await EnsurePuedeEditarActaByReunionId(ctx, archivo.ReunionId, userId);

            archivo.State = false;
            archivo.UpdatedDateTime = DateTime.UtcNow;
            archivo.UpdatedUserId = userId;
            await ctx.SaveChangesAsync();
        }

        // ── Aceptación de acuerdos + envío del acta al marcar Realizada ──────

        /// <summary>Resuelve el workerId del usuario autenticado y valida que sea (por persona, no
        /// worker_id exacto) el responsable dueño de esta fila. Lanza 403/404 si no corresponde.</summary>
        private static async Task<ReunionAcuerdoResponsable> GetResponsableAutorizado(
            AppDbContext ctx, int reunionAcuerdoResponsableId, int userId)
        {
            var responsable = await ctx.ReunionAcuerdoResponsable
                .FirstOrDefaultAsync(r => r.ReunionAcuerdoResponsableId == reunionAcuerdoResponsableId && r.State);
            if (responsable is null)
                throw new AbrilException("El acuerdo no existe.", 404);
            if (responsable.WorkerId is null)
                throw new AbrilException("Este responsable no está vinculado a un trabajador.", 400);

            var workerId = await ResolveWorkerId(ctx, userId);
            if (workerId is null)
                throw new AbrilException("No se encontró un trabajador asociado a este usuario.", 400);

            if (workerId.Value != responsable.WorkerId.Value)
            {
                // Igual que en GuardarMisTemas: compara por persona, no por worker_id exacto,
                // para tolerar duplicados de worker de la misma persona.
                var personIdActual = await ctx.Worker.Where(w => w.Id == workerId.Value).Select(w => (int?)w.PersonId).FirstOrDefaultAsync();
                var personIdResponsable = await ctx.Worker.Where(w => w.Id == responsable.WorkerId.Value).Select(w => (int?)w.PersonId).FirstOrDefaultAsync();
                if (personIdActual is null || personIdActual != personIdResponsable)
                    throw new AbrilException("Este acuerdo no te corresponde.", 403);
            }

            return responsable;
        }

        public async Task<AcuerdoResponsableInfoDto> GetAcuerdoResponsableInfo(int reunionAcuerdoResponsableId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var responsable = await GetResponsableAutorizado(ctx, reunionAcuerdoResponsableId, userId);

            var acuerdo = await ctx.ReunionAcuerdo
                .FirstOrDefaultAsync(a => a.ReunionAcuerdoId == responsable.ReunionAcuerdoId && a.State);
            if (acuerdo is null)
                throw new AbrilException("El acuerdo no existe.", 404);

            var reunion = await ctx.Reunion
                .Where(r => r.ReunionId == acuerdo.ReunionId)
                .Select(r => new { r.Numero, r.Tema })
                .FirstOrDefaultAsync();
            if (reunion is null)
                throw new AbrilException("El acta de reunión no existe.", 404);

            return new AcuerdoResponsableInfoDto
            {
                ReunionAcuerdoResponsableId = responsable.ReunionAcuerdoResponsableId,
                ReunionId = acuerdo.ReunionId,
                ReunionNumero = reunion.Numero,
                ReunionTema = reunion.Tema,
                AcuerdoDescripcion = acuerdo.Descripcion,
                AcuerdoAcciones = acuerdo.Acciones,
                FechaProgramada = acuerdo.FechaProgramada,
                EstadoAceptacion = responsable.EstadoAceptacion,
                MotivoRechazo = responsable.MotivoRechazo,
            };
        }

        public async Task ResponderAcuerdo(int reunionAcuerdoResponsableId, int userId, AcuerdoResponsableDecisionRequest request)
        {
            using var ctx = _factory.CreateDbContext();

            var responsable = await GetResponsableAutorizado(ctx, reunionAcuerdoResponsableId, userId);

            if (!request.Aceptado && string.IsNullOrWhiteSpace(request.MotivoRechazo))
                throw new AbrilException("Debe indicar el motivo del rechazo.", 400);

            responsable.EstadoAceptacion = request.Aceptado ? "ACEPTADO" : "RECHAZADO";
            responsable.MotivoRechazo = request.Aceptado ? null : request.MotivoRechazo!.Trim();
            responsable.FechaRespuesta = DateTime.UtcNow;
            responsable.UpdatedDateTime = DateTime.UtcNow;
            responsable.UpdatedUserId = userId;
            await ctx.SaveChangesAsync();
        }

        public async Task<List<ActaEnvioDestinatarioDto>> GetDestinatariosActaRealizada(int reunionId)
        {
            using var ctx = _factory.CreateDbContext();

            // Se envía a todo convocado (haya marcado "Asistió" o no), no solo a los que sí
            // asistieron: el checkbox de asistencia queda para el registro del acta, pero no debe
            // ser condición para que alguien reciba el acta de una reunión a la que fue convocado.
            var asistentes = await (
                from p in ctx.ReunionParticipante
                where p.ReunionId == reunionId && p.State && p.WorkerId != null
                join w in ctx.Worker on p.WorkerId!.Value equals w.Id
                where w.EmailCorporativo != null
                select new { WorkerId = w.Id, Nombre = p.Nombre, Email = w.EmailCorporativo! }
            ).Distinct().ToListAsync();

            var responsables = await (
                from ac in ctx.ReunionAcuerdo
                where ac.ReunionId == reunionId && ac.State
                join resp in ctx.ReunionAcuerdoResponsable on ac.ReunionAcuerdoId equals resp.ReunionAcuerdoId
                where resp.State && resp.WorkerId != null
                join w in ctx.Worker on resp.WorkerId!.Value equals w.Id
                join p in ctx.Person on w.PersonId equals p.PersonId
                where w.EmailCorporativo != null
                select new
                {
                    WorkerId = w.Id,
                    Nombre = p.FullName,
                    Email = w.EmailCorporativo!,
                    resp.ReunionAcuerdoResponsableId,
                    ac.Descripcion,
                    resp.EstadoAceptacion,
                    ac.RequiereAceptacion,
                }
            ).ToListAsync();

            var destinatarios = new Dictionary<int, ActaEnvioDestinatarioDto>();
            foreach (var a in asistentes)
            {
                destinatarios[a.WorkerId] = new ActaEnvioDestinatarioDto
                {
                    WorkerId = a.WorkerId,
                    Nombre = a.Nombre,
                    Email = a.Email,
                    Asistio = true,
                };
            }
            foreach (var r in responsables)
            {
                if (!destinatarios.TryGetValue(r.WorkerId, out var dest))
                {
                    dest = new ActaEnvioDestinatarioDto { WorkerId = r.WorkerId, Nombre = r.Nombre, Email = r.Email, Asistio = false };
                    destinatarios[r.WorkerId] = dest;
                }
                if (r.RequiereAceptacion && r.EstadoAceptacion == "PENDIENTE")
                {
                    dest.AcuerdosPendientes.Add(new ActaEnvioAcuerdoPendienteDto
                    {
                        ReunionAcuerdoResponsableId = r.ReunionAcuerdoResponsableId,
                        Descripcion = r.Descripcion,
                    });
                }
            }

            return destinatarios.Values.ToList();
        }

        // ── Carpeta de SharePoint para adjuntos (singleton) ──────────────────
        public async Task<ReunionFolderDto?> GetFolderSingleton()
        {
            using var ctx = _factory.CreateDbContext();

            return await ctx.ReunionFolder
                .Where(f => f.State)
                .OrderBy(f => f.ReunionFolderId)
                .Select(f => new ReunionFolderDto
                {
                    ReunionFolderId = f.ReunionFolderId,
                    LinkUrl = f.LinkUrl,
                    DriveId = f.DriveId,
                    FolderId = f.FolderId,
                    FolderName = f.FolderName,
                    WebUrl = f.WebUrl,
                    Active = f.Active,
                    CreatedDateTime = f.CreatedDateTime.ToOffset(TimeSpan.FromHours(-5)).DateTime,
                    CreatedUserId = f.CreatedUserId,
                })
                .FirstOrDefaultAsync();
        }

        public async Task UpsertFolder(string linkUrl, string driveId, string folderId, string? folderName, string? webUrl, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var record = await ctx.ReunionFolder
                .Where(f => f.State)
                .OrderBy(f => f.ReunionFolderId)
                .FirstOrDefaultAsync();

            if (record == null)
            {
                ctx.ReunionFolder.Add(new ReunionFolder
                {
                    LinkUrl = linkUrl,
                    DriveId = driveId,
                    FolderId = folderId,
                    FolderName = folderName,
                    WebUrl = webUrl,
                    Active = true,
                    State = true,
                    CreatedDateTime = DateTimeOffset.UtcNow,
                    CreatedUserId = userId,
                });
            }
            else
            {
                record.LinkUrl = linkUrl;
                record.DriveId = driveId;
                record.FolderId = folderId;
                record.FolderName = folderName;
                record.WebUrl = webUrl;
                record.Active = true;
                record.UpdatedDateTime = DateTimeOffset.UtcNow;
                record.UpdatedUserId = userId;
            }

            await ctx.SaveChangesAsync();
        }

        public async Task<(string DriveId, string FolderId)?> GetFolderDestination()
        {
            using var ctx = _factory.CreateDbContext();

            var f = await ctx.ReunionFolder
                .Where(x => x.State && x.Active)
                .OrderBy(x => x.ReunionFolderId)
                .Select(x => new { x.DriveId, x.FolderId })
                .FirstOrDefaultAsync();

            return f == null ? null : (f.DriveId, f.FolderId);
        }

        /// <summary>Ámbito (proyecto o área/gerencia) y número de la reunión, para nombrar su subcarpeta en SharePoint.</summary>
        public async Task<(string ProjectDescription, int Numero)> GetDatosCarpetaReunion(int reunionId)
        {
            using var ctx = _factory.CreateDbContext();

            var datos = await ctx.Reunion
                .Where(r => r.ReunionId == reunionId && r.State)
                .Select(r => new
                {
                    r.Numero,
                    ProjectDescription = r.ProjectId == null ? null : ctx.Project
                        .Where(p => p.ProjectId == r.ProjectId.Value)
                        .Select(p => p.ProjectDescription)
                        .First(),
                    AreaScopeDescripcion = r.AreaScopeId == null ? null : ctx.AreaScope
                        .Where(s => s.AreaScopeId == r.AreaScopeId.Value)
                        .Select(s => s.AreaItem!.AreaItemName)
                        .First(),
                })
                .FirstOrDefaultAsync();

            if (datos is null)
                throw new AbrilException("El acta de reunión no existe.", 404);

            var ambitoDescripcion = datos.ProjectDescription ?? datos.AreaScopeDescripcion ?? "ORGANIZACIÓN";
            return (ambitoDescripcion, datos.Numero);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Trabajadores de Abril (workers con email_corporativo @abril.pe) para los desplegables
        /// de "Convocado por" y de participantes. El cargo es el puesto del catálogo.
        /// </summary>
        private static async Task<List<TrabajadorAbrilDto>> GetTrabajadoresAbril(AppDbContext ctx)
        {
            return await (
                from w in ctx.Worker
                where w.EmailCorporativo != null && w.EmailCorporativo.ToLower().Contains("@abril.pe")
                join p in ctx.Person on w.PersonId equals p.PersonId
                where p.State == true
                orderby p.FullName
                select new TrabajadorAbrilDto
                {
                    WorkerId = w.Id,
                    FullName = p.FullName,
                    Cargo = w.PuestoCatalogo == null ? null : w.PuestoCatalogo.Nombre,
                }
            ).ToListAsync();
        }

        /// <summary>
        /// Trabajadores de Abril que calzan con un área/gerencia (incluye toda su descendencia en
        /// el árbol area_scope) y/o una lista de puestos elegidos de un checklist (ej. varias
        /// jefaturas marcadas a la vez). Ambos filtros son opcionales y se combinan con AND;
        /// null/vacío en un filtro significa "cualquiera". Pensado para la convocatoria masiva de
        /// participantes (ej. "todas las jefaturas de Proyectos", "todo el área de Arquitectura
        /// Comercial").
        /// </summary>
        public async Task<List<TrabajadorAbrilDto>> BuscarTrabajadoresPorFiltro(int? areaScopeId, List<int>? puestoIds, int? projectId)
        {
            using var ctx = _factory.CreateDbContext();

            HashSet<int>? descendientes = null;
            if (areaScopeId.HasValue)
                descendientes = await ctx.ResolveDescendantsAsync(areaScopeId.Value);

            // Staff vinculado actualmente a este proyecto: worker_vinculaciones con vinculación
            // vigente (FechaFin == null), igual criterio que usa Habilitación para "proyecto actual"
            // de un trabajador. OJO: antes esto se resolvía vía ss_contratista_usuario (scope
            // POR_PROYECTO), pero esa tabla es acceso al sistema (a quién se le dio login/permisos
            // puntuales en Habilitación/SSOMA para el proyecto) — no asignación laboral. Hay
            // trabajadores vinculados de verdad a la obra que nunca necesitaron ese acceso, así que
            // quedaban fuera de "el staff de esta obra" sin ninguna razón real.
            HashSet<int>? workerIdsDeProyecto = null;
            if (projectId.HasValue)
            {
                workerIdsDeProyecto = (await ctx.WorkerVinculacion
                    .Where(v => v.ProyectoId == projectId.Value && v.FechaFin == null)
                    .Select(v => v.WorkerId)
                    .Distinct()
                    .ToListAsync())
                    .ToHashSet();
            }

            var query =
                from w in ctx.Worker
                where w.EmailCorporativo != null && w.EmailCorporativo.ToLower().Contains("@abril.pe")
                join p in ctx.Person on w.PersonId equals p.PersonId
                where p.State
                select new { w, p };

            if (descendientes != null)
                query = query.Where(x => x.w.AreaScopeId != null && descendientes.Contains(x.w.AreaScopeId.Value));
            if (puestoIds != null && puestoIds.Count > 0)
                query = query.Where(x => x.w.PuestoId != null && puestoIds.Contains(x.w.PuestoId.Value));
            if (workerIdsDeProyecto != null)
                query = query.Where(x => workerIdsDeProyecto.Contains(x.w.Id));

            return await query
                .OrderBy(x => x.p.FullName)
                .Select(x => new TrabajadorAbrilDto
                {
                    WorkerId = x.w.Id,
                    FullName = x.p.FullName,
                    Cargo = x.w.PuestoCatalogo == null ? null : x.w.PuestoCatalogo.Nombre,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Da de alta un tema personalizado en el catálogo reunion_tema, para que quede disponible
        /// como tema recurrente en próximas reuniones. Si ya existe (comparación insensible a
        /// mayúsculas), no duplica: devuelve el existente.
        /// </summary>
        public async Task<CatalogoDto> AgregarTema(string descripcion, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var normalizado = descripcion.Trim();
            var existente = await ctx.ReunionTema
                .Where(t => t.State && t.Descripcion.ToLower() == normalizado.ToLower())
                .Select(t => new CatalogoDto { Id = t.ReunionTemaId, Descripcion = t.Descripcion })
                .FirstOrDefaultAsync();
            if (existente != null) return existente;

            var tema = new ReunionTema
            {
                Descripcion = normalizado,
                CreatedDateTime = DateTime.UtcNow,
                CreatedUserId = userId,
                Active = true,
                State = true,
            };
            ctx.ReunionTema.Add(tema);
            await ctx.SaveChangesAsync();

            return new CatalogoDto { Id = tema.ReunionTemaId, Descripcion = tema.Descripcion };
        }

        /// <summary>
        /// Borrado real (no soft-delete): se pidió explícitamente que el tema desaparezca del todo del
        /// catálogo, no que quede oculto. Las reuniones que ya lo usaban conservan su <see cref="Reunion.Tema"/>
        /// (texto propio, copiado al agendar) intacto — solo se desvincula la referencia al catálogo
        /// (<see cref="Reunion.ReunionTemaId"/> pasa a null), así el borrado no rompe el historial de esas
        /// actas. Ojo: si alguna de esas reuniones estaba PROGRAMADA con agenda dinámica pendiente de
        /// recordatorio, al perder el vínculo deja de calzar en <see cref="GetCandidatosRecordatorioAgenda"/>
        /// y no le llegará el correo — es la consecuencia esperada de "ya no se puede usar hacia adelante".
        /// Devuelve cuántas reuniones se desvincularon, para informar al usuario.
        /// </summary>
        public async Task<int> EliminarTema(int reunionTemaId)
        {
            using var ctx = _factory.CreateDbContext();

            var tema = await ctx.ReunionTema.FirstOrDefaultAsync(t => t.ReunionTemaId == reunionTemaId);
            if (tema == null) throw new AbrilException("El tema no existe.", 404);

            var reuniones = await ctx.Reunion.Where(r => r.ReunionTemaId == reunionTemaId).ToListAsync();
            foreach (var r in reuniones) r.ReunionTemaId = null;

            var puestos = await ctx.ReunionTemaPuesto.Where(p => p.ReunionTemaId == reunionTemaId).ToListAsync();
            ctx.ReunionTemaPuesto.RemoveRange(puestos);
            var reglas = await ctx.ReunionTemaRegla.Where(r => r.ReunionTemaId == reunionTemaId).ToListAsync();
            ctx.ReunionTemaRegla.RemoveRange(reglas);
            await ctx.SaveChangesAsync();

            // Guardado aparte: ReunionTemaPuesto/ReunionTemaRegla no tienen navegación/Fluent config
            // hacia ReunionTema, así que EF no conoce la dependencia y no puede ordenar los deletes en
            // un solo batch (llegó a generar el DELETE del padre antes que el de los hijos, violando
            // la FK real de la BD).
            ctx.ReunionTema.Remove(tema);
            await ctx.SaveChangesAsync();
            return reuniones.Count;
        }

        /// <summary>Catálogo de temas predefinidos (para la pantalla de configuración de convocatoria por tema).</summary>
        public async Task<List<ReunionTemaOpcionDto>> GetTemasCatalogo()
        {
            using var ctx = _factory.CreateDbContext();
            return await GetTemas(ctx);
        }

        /// <summary>Convocatoria recurrente configurada para un tema: sus reglas (área/proyecto + puestos) y agenda.</summary>
        public async Task<TemaConvocatoriaDto> GetConvocatoriaTema(int reunionTemaId)
        {
            using var ctx = _factory.CreateDbContext();

            var tema = await ctx.ReunionTema
                .Where(t => t.ReunionTemaId == reunionTemaId && t.State)
                .Select(t => new { t.AgendaFija, t.AgendaTexto, t.RecordatorioHorasAntes })
                .FirstOrDefaultAsync();
            if (tema is null)
                throw new AbrilException("El tema no existe.", 404);

            var reglas = await ctx.ReunionTemaRegla
                .Where(r => r.ReunionTemaId == reunionTemaId && r.State)
                .OrderBy(r => r.ReunionTemaReglaId)
                .ToListAsync();

            var reglaDtos = new List<TemaConvocatoriaReglaDto>();
            foreach (var regla in reglas)
            {
                var puestoIds = await ctx.ReunionTemaPuesto
                    .Where(p => p.ReunionTemaReglaId == regla.ReunionTemaReglaId && p.State)
                    .Select(p => p.PuestoId)
                    .ToListAsync();
                var workerIdsExcluidos = await ctx.ReunionTemaReglaWorkerExcluido
                    .Where(e => e.ReunionTemaReglaId == regla.ReunionTemaReglaId && e.State)
                    .Select(e => e.WorkerId)
                    .ToListAsync();

                string? areaScopeDescripcion = regla.AreaScopeId.HasValue
                    ? await ctx.AreaScope
                        .Where(s => s.AreaScopeId == regla.AreaScopeId.Value)
                        .Select(s => s.AreaItem!.AreaItemName)
                        .FirstOrDefaultAsync()
                    : null;
                string? projectDescription = regla.ProjectId.HasValue
                    ? await ctx.Project
                        .Where(p => p.ProjectId == regla.ProjectId.Value)
                        .Select(p => p.ProjectDescription)
                        .FirstOrDefaultAsync()
                    : null;

                reglaDtos.Add(new TemaConvocatoriaReglaDto
                {
                    AreaScopeId = regla.AreaScopeId,
                    AreaScopeDescripcion = areaScopeDescripcion,
                    ProjectId = regla.ProjectId,
                    ProjectDescription = projectDescription,
                    PuestoIds = puestoIds,
                    WorkerIdsExcluidos = workerIdsExcluidos,
                });
            }

            return new TemaConvocatoriaDto
            {
                Reglas = reglaDtos,
                AgendaFija = tema.AgendaFija,
                AgendaTexto = tema.AgendaTexto,
                RecordatorioHorasAntes = tema.RecordatorioHorasAntes,
            };
        }

        /// <summary>Reemplaza por completo las reglas de convocatoria y la agenda/recordatorio de un tema.</summary>
        public async Task GuardarConvocatoriaTema(int reunionTemaId, TemaConvocatoriaSaveRequest request, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var tema = await ctx.ReunionTema.FirstOrDefaultAsync(t => t.ReunionTemaId == reunionTemaId && t.State);
            if (tema is null)
                throw new AbrilException("El tema no existe.", 404);

            var now = DateTime.UtcNow;
            // Toda reunión requiere agenda: ya no es opcional, solo se define si es fija o dinámica.
            tema.RequiereAgenda = true;
            tema.AgendaFija = request.AgendaFija;
            tema.AgendaTexto = request.AgendaFija ? request.AgendaTexto?.Trim() : null;
            tema.RecordatorioHorasAntes = !request.AgendaFija ? request.RecordatorioHorasAntes : null;
            tema.UpdatedDateTime = now;
            tema.UpdatedUserId = userId;

            var reglasActuales = await ctx.ReunionTemaRegla
                .Where(r => r.ReunionTemaId == reunionTemaId && r.State)
                .ToListAsync();
            var idsReglasActuales = reglasActuales.Select(r => r.ReunionTemaReglaId).ToList();

            var puestosActuales = await ctx.ReunionTemaPuesto
                .Where(p => p.ReunionTemaReglaId != null && idsReglasActuales.Contains(p.ReunionTemaReglaId.Value) && p.State)
                .ToListAsync();
            foreach (var p in puestosActuales)
                p.State = false;
            var excluidosActuales = await ctx.ReunionTemaReglaWorkerExcluido
                .Where(e => idsReglasActuales.Contains(e.ReunionTemaReglaId) && e.State)
                .ToListAsync();
            foreach (var e in excluidosActuales)
                e.State = false;
            foreach (var r in reglasActuales)
                r.State = false;
            await ctx.SaveChangesAsync();

            foreach (var reglaInput in request.Reglas)
            {
                // Una regla vacía (sin área, proyecto ni puestos) no aportaría a nadie: se descarta.
                if (reglaInput.AreaScopeId == null && reglaInput.ProjectId == null && reglaInput.PuestoIds.Count == 0)
                    continue;

                var regla = new ReunionTemaRegla
                {
                    ReunionTemaId = reunionTemaId,
                    AreaScopeId = reglaInput.AreaScopeId,
                    ProjectId = reglaInput.ProjectId,
                    CreatedDateTime = now,
                    CreatedUserId = userId,
                    Active = true,
                    State = true,
                };
                ctx.ReunionTemaRegla.Add(regla);
                await ctx.SaveChangesAsync(); // se necesita el Id generado antes de crear sus puestos

                foreach (var puestoId in reglaInput.PuestoIds.Distinct())
                {
                    ctx.ReunionTemaPuesto.Add(new ReunionTemaPuesto
                    {
                        ReunionTemaId = reunionTemaId,
                        ReunionTemaReglaId = regla.ReunionTemaReglaId,
                        PuestoId = puestoId,
                        CreatedDateTime = now,
                        CreatedUserId = userId,
                        Active = true,
                        State = true,
                    });
                }

                // Solo tiene sentido para reglas "Staff de un proyecto": a quién excluir de la
                // convocatoria dinámica aunque siga vinculado al proyecto.
                if (reglaInput.ProjectId != null)
                {
                    foreach (var workerId in reglaInput.WorkerIdsExcluidos.Distinct())
                    {
                        ctx.ReunionTemaReglaWorkerExcluido.Add(new ReunionTemaReglaWorkerExcluido
                        {
                            ReunionTemaReglaId = regla.ReunionTemaReglaId,
                            WorkerId = workerId,
                            CreatedDateTime = now,
                            CreatedUserId = userId,
                            Active = true,
                            State = true,
                        });
                    }
                }
            }

            await ctx.SaveChangesAsync();
        }

        // ── Recurrencia (generación automática de la siguiente reunión) ──────────
        public async Task<TemaRecurrenciaDto> GetRecurrenciaTema(int reunionTemaId)
        {
            using var ctx = _factory.CreateDbContext();

            var tema = await ctx.ReunionTema
                .Where(t => t.ReunionTemaId == reunionTemaId && t.State)
                .FirstOrDefaultAsync();
            if (tema is null)
                throw new AbrilException("El tema no existe.", 404);

            DateOnly? proxima = null;
            if (tema.EsRecurrente && tema.IntervaloDias.HasValue)
            {
                proxima = tema.UltimaFechaGenerada.HasValue
                    ? tema.UltimaFechaGenerada.Value.AddDays(tema.IntervaloDias.Value)
                    : tema.FechaAncla;
            }

            var areaScopeDescripcion = tema.AreaScopeId.HasValue
                ? await ctx.AreaScope.Where(s => s.AreaScopeId == tema.AreaScopeId.Value)
                    .Select(s => s.AreaItem!.AreaItemName).FirstOrDefaultAsync()
                : null;

            return new TemaRecurrenciaDto
            {
                EsRecurrente = tema.EsRecurrente,
                RecurrenciaActiva = tema.RecurrenciaActiva,
                AreaScopeId = tema.AreaScopeId,
                AreaScopeDescripcion = areaScopeDescripcion,
                IntervaloDias = tema.IntervaloDias,
                FechaAncla = tema.FechaAncla,
                HoraInicio = tema.HoraInicio,
                HoraFin = tema.HoraFin,
                Lugar = tema.Lugar,
                DiasAnticipacion = tema.DiasAnticipacion,
                UltimaFechaGenerada = tema.UltimaFechaGenerada,
                ProximaFechaEstimada = proxima,
            };
        }

        public async Task GuardarRecurrenciaTema(int reunionTemaId, TemaRecurrenciaSaveRequest request, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var tema = await ctx.ReunionTema.FirstOrDefaultAsync(t => t.ReunionTemaId == reunionTemaId && t.State);
            if (tema is null)
                throw new AbrilException("El tema no existe.", 404);

            if (request.EsRecurrente)
            {
                if (!request.AreaScopeId.HasValue)
                    throw new AbrilException("Indica el área/gerencia a la que pertenecerán las reuniones generadas.", 400);
                if (!request.IntervaloDias.HasValue || request.IntervaloDias.Value <= 0)
                    throw new AbrilException("Indica cada cuántos días se repite la reunión.", 400);
                if (!request.FechaAncla.HasValue)
                    throw new AbrilException("Indica la fecha de la primera ocurrencia de la serie.", 400);
            }

            tema.EsRecurrente = request.EsRecurrente;
            tema.RecurrenciaActiva = request.RecurrenciaActiva;
            tema.AreaScopeId = request.AreaScopeId;
            tema.IntervaloDias = request.IntervaloDias;
            tema.FechaAncla = request.FechaAncla;
            tema.HoraInicio = request.HoraInicio;
            tema.HoraFin = request.HoraFin;
            tema.Lugar = request.Lugar?.Trim();
            tema.DiasAnticipacion = request.DiasAnticipacion <= 0 ? 5 : request.DiasAnticipacion;
            tema.UpdatedDateTime = DateTime.UtcNow;
            tema.UpdatedUserId = userId;

            await ctx.SaveChangesAsync();
        }

        /// <summary>Temas con recurrencia activada y no pausada, con reglas de convocatoria
        /// configuradas, para el job de generación automática.</summary>
        public async Task<List<ReunionTema>> GetTemasRecurrentesActivos()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.ReunionTema
                .Where(t => t.State && t.EsRecurrente && t.RecurrenciaActiva
                    && t.AreaScopeId != null && t.IntervaloDias != null)
                .ToListAsync();
        }

        public async Task<List<(int? AreaScopeId, int? ProjectId, List<int> PuestoIds, List<int> WorkerIdsExcluidos)>> GetReglasTemaParaGeneracion(int reunionTemaId)
        {
            using var ctx = _factory.CreateDbContext();
            var reglas = await ctx.ReunionTemaRegla
                .Where(r => r.ReunionTemaId == reunionTemaId && r.State)
                .ToListAsync();

            var resultado = new List<(int?, int?, List<int>, List<int>)>();
            foreach (var regla in reglas)
            {
                var puestoIds = await ctx.ReunionTemaPuesto
                    .Where(p => p.ReunionTemaReglaId == regla.ReunionTemaReglaId && p.State)
                    .Select(p => p.PuestoId)
                    .ToListAsync();
                var workerIdsExcluidos = await ctx.ReunionTemaReglaWorkerExcluido
                    .Where(e => e.ReunionTemaReglaId == regla.ReunionTemaReglaId && e.State)
                    .Select(e => e.WorkerId)
                    .ToListAsync();
                resultado.Add((regla.AreaScopeId, regla.ProjectId, puestoIds, workerIdsExcluidos));
            }
            return resultado;
        }

        /// <summary>Avanza el puntero de calendario de la serie tras generar una nueva ocurrencia.</summary>
        public async Task AvanzarRecurrenciaTema(int reunionTemaId, DateOnly nuevaFechaGenerada, int nuevaReunionId)
        {
            using var ctx = _factory.CreateDbContext();
            var tema = await ctx.ReunionTema.FirstOrDefaultAsync(t => t.ReunionTemaId == reunionTemaId);
            if (tema is null) return;

            tema.UltimaFechaGenerada = nuevaFechaGenerada;
            tema.UltimaReunionGeneradaId = nuevaReunionId;
            await ctx.SaveChangesAsync();
        }

        // ── Agenda de reunión ────────────────────────────────────────────────
        public async Task<ReunionAgendaDto> GetAgenda(int reunionId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var reunion = await ctx.Reunion
                .Where(r => r.ReunionId == reunionId && r.State)
                .Select(r => new { r.ReunionTemaId, r.AgendaTexto })
                .FirstOrDefaultAsync();
            if (reunion is null)
                throw new AbrilException("El acta de reunión no existe.", 404);

            // Toda reunión requiere agenda. Prioridad: 1) agenda ad-hoc de esta reunión puntual
            // (definida al agendar, si no venía de un tema del catálogo); 2) la del tema, fija si
            // tiene texto; 3) dinámica (cada participante carga sus temas) en cualquier otro caso.
            if (!string.IsNullOrWhiteSpace(reunion.AgendaTexto))
            {
                return new ReunionAgendaDto
                {
                    RequiereAgenda = true,
                    AgendaFija = true,
                    AgendaTexto = reunion.AgendaTexto,
                    WorkerIdActual = await ResolveWorkerId(ctx, userId),
                };
            }

            var config = reunion.ReunionTemaId.HasValue
                ? await ctx.ReunionTema
                    .Where(t => t.ReunionTemaId == reunion.ReunionTemaId.Value)
                    .Select(t => new { t.RequiereAgenda, t.AgendaFija, t.AgendaTexto })
                    .FirstOrDefaultAsync()
                : null;

            var esFija = config != null && config.AgendaFija && !string.IsNullOrWhiteSpace(config.AgendaTexto);

            var dto = new ReunionAgendaDto
            {
                RequiereAgenda = true,
                AgendaFija = esFija,
                AgendaTexto = esFija ? config!.AgendaTexto : null,
                WorkerIdActual = await ResolveWorkerId(ctx, userId),
            };

            // Agenda fija: los puntos únicos ya quedaron en AgendaTexto, pero igual puede haber
            // "temas puntuales" agregados a mano para esta ocurrencia (ver AgregarTemaPuntual) —
            // se muestran junto al texto fijo, sin el concepto de "pendientes" que solo aplica
            // cuando TODOS los convocados deben cargar sus temas (agenda dinámica).
            dto.Items = await CargarAgendaItems(ctx, reunionId);
            if (dto.AgendaFija)
                return dto;

            var participantesConWorker = await ctx.ReunionParticipante
                .Where(p => p.ReunionId == reunionId && p.State && p.WorkerId != null)
                .Select(p => new { p.WorkerId, p.Nombre })
                .ToListAsync();
            var workerIdsConTemas = dto.Items.Select(i => i.WorkerId).ToHashSet();
            dto.ParticipantesPendientes = participantesConWorker
                .Where(p => !workerIdsConTemas.Contains(p.WorkerId!.Value))
                .Select(p => p.Nombre)
                .Distinct()
                .ToList();

            return dto;
        }

        private static Task<List<ReunionAgendaItemDto>> CargarAgendaItems(AppDbContext ctx, int reunionId)
        {
            return (
                from a in ctx.ReunionAgendaItem
                where a.ReunionId == reunionId && a.State
                join w in ctx.Worker on a.WorkerId equals w.Id
                join p in ctx.Person on w.PersonId equals p.PersonId
                orderby a.Orden
                select new ReunionAgendaItemDto
                {
                    ReunionAgendaItemId = a.ReunionAgendaItemId,
                    WorkerId = a.WorkerId,
                    WorkerNombre = p.FullName,
                    SubareaDescripcion = w.AreaScopeId == null ? null : ctx.AreaScope
                        .Where(s => s.AreaScopeId == w.AreaScopeId.Value)
                        .Select(s => s.AreaItem!.AreaItemName)
                        .FirstOrDefault(),
                    Descripcion = a.Descripcion,
                    Orden = a.Orden,
                }
            ).ToListAsync();
        }

        /// <summary>
        /// Agrega UN tema puntual a la agenda de esta ocurrencia (no reemplaza los existentes, a
        /// diferencia de GuardarMisTemas). Pensado para reuniones de agenda fija que necesitan sumar
        /// un punto excepcional para esta sesión, sin activar el flujo completo de agenda dinámica
        /// (que exige a TODOS los convocados cargar sus temas antes de la reunión).
        /// </summary>
        public async Task<ReunionAgendaItemDto> AgregarTemaPuntual(int reunionId, int userId, string descripcion)
        {
            using var ctx = _factory.CreateDbContext();

            var reunionExiste = await ctx.Reunion.AnyAsync(r => r.ReunionId == reunionId && r.State);
            if (!reunionExiste)
                throw new AbrilException("El acta de reunión no existe.", 404);

            var workerId = await ResolveParticipanteWorkerId(ctx, reunionId, userId);

            var now = DateTime.UtcNow;
            var siguienteOrden = 1 + (await ctx.ReunionAgendaItem
                .Where(a => a.ReunionId == reunionId && a.State)
                .Select(a => (int?)a.Orden)
                .MaxAsync() ?? -1);

            var item = new ReunionAgendaItem
            {
                ReunionId = reunionId,
                WorkerId = workerId,
                Descripcion = descripcion.Trim(),
                Orden = siguienteOrden,
                CreatedDateTime = now,
                CreatedUserId = userId,
                Active = true,
                State = true,
            };
            ctx.ReunionAgendaItem.Add(item);
            await ctx.SaveChangesAsync();

            var autor = await (
                from w in ctx.Worker
                where w.Id == workerId
                join p in ctx.Person on w.PersonId equals p.PersonId
                select new
                {
                    p.FullName,
                    SubareaDescripcion = w.AreaScopeId == null ? null : ctx.AreaScope
                        .Where(s => s.AreaScopeId == w.AreaScopeId.Value)
                        .Select(s => s.AreaItem!.AreaItemName)
                        .FirstOrDefault(),
                }
            ).FirstOrDefaultAsync();

            return new ReunionAgendaItemDto
            {
                ReunionAgendaItemId = item.ReunionAgendaItemId,
                WorkerId = workerId,
                WorkerNombre = autor?.FullName ?? "",
                SubareaDescripcion = autor?.SubareaDescripcion,
                Descripcion = item.Descripcion,
                Orden = item.Orden,
            };
        }

        /// <summary>Elimina un tema puntual — solo quien lo agregó puede quitarlo.</summary>
        public async Task EliminarTemaPuntual(int reunionId, int reunionAgendaItemId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var workerId = await ResolveParticipanteWorkerId(ctx, reunionId, userId);

            var item = await ctx.ReunionAgendaItem
                .FirstOrDefaultAsync(a => a.ReunionAgendaItemId == reunionAgendaItemId && a.ReunionId == reunionId && a.State);
            if (item is null)
                throw new AbrilException("El tema no existe.", 404);
            if (item.WorkerId != workerId)
                throw new AbrilException("Solo puedes eliminar los temas que agregaste tú.", 403);

            item.State = false;
            item.UpdatedDateTime = DateTime.UtcNow;
            item.UpdatedUserId = userId;
            await ctx.SaveChangesAsync();
        }

        public async Task GuardarMisTemas(int reunionId, int userId, List<string> temas)
        {
            using var ctx = _factory.CreateDbContext();

            var reunionExiste = await ctx.Reunion.AnyAsync(r => r.ReunionId == reunionId && r.State);
            if (!reunionExiste)
                throw new AbrilException("El acta de reunión no existe.", 404);

            var workerId = await ResolveParticipanteWorkerId(ctx, reunionId, userId);

            var now = DateTime.UtcNow;
            var actuales = await ctx.ReunionAgendaItem
                .Where(a => a.ReunionId == reunionId && a.WorkerId == workerId && a.State)
                .ToListAsync();
            foreach (var actual in actuales)
            {
                actual.State = false;
                actual.UpdatedDateTime = now;
                actual.UpdatedUserId = userId;
            }

            var orden = 0;
            foreach (var descripcion in temas)
            {
                ctx.ReunionAgendaItem.Add(new ReunionAgendaItem
                {
                    ReunionId = reunionId,
                    WorkerId = workerId,
                    Descripcion = descripcion,
                    Orden = orden++,
                    CreatedDateTime = now,
                    CreatedUserId = userId,
                    Active = true,
                    State = true,
                });
            }

            await ctx.SaveChangesAsync();
        }

        /// <summary>Resuelve el workerId del usuario autenticado y valida que sea participante de la
        /// reunión (comparando por persona, con auto-reparación de participantes cargados sin
        /// workerId vinculado). Lanza AbrilException si no corresponde.</summary>
        private async Task<int> ResolveParticipanteWorkerId(AppDbContext ctx, int reunionId, int userId)
        {
            var workerId = await ResolveWorkerId(ctx, userId);
            if (workerId is null)
                throw new AbrilException("No se encontró un trabajador asociado a este usuario.", 400);

            // Se compara por persona (no solo por worker.Id): si la persona tiene más de un registro
            // de worker (histórico/duplicado), un participante agregado con el "otro" worker de la
            // misma persona igual debe reconocerse como "soy yo" — si no, un convocado real recibía
            // "No estás convocado" por una diferencia de fila que no le corresponde resolver a él.
            var personId = await ctx.Worker.Where(w => w.Id == workerId.Value).Select(w => (int?)w.PersonId).FirstOrDefaultAsync();
            var esParticipante = personId.HasValue && await ctx.ReunionParticipante
                .Where(p => p.ReunionId == reunionId && p.State && p.WorkerId != null)
                .Join(ctx.Worker, p => p.WorkerId!.Value, w => w.Id, (p, w) => w.PersonId)
                .AnyAsync(pid => pid == personId.Value);

            if (!esParticipante)
            {
                // Red de seguridad: participantes agregados sin vincular su worker_id (dato viejo o
                // ingresado a mano) no calzan por el join de arriba aunque sí sean la misma persona.
                // Si el nombre calza con el del usuario autenticado, se reconoce igual y de paso se
                // autorepara el registro para que deje de fallar la próxima vez.
                var miNombre = await ctx.Worker
                    .Where(w => w.Id == workerId.Value)
                    .Join(ctx.Person, w => w.PersonId, p => p.PersonId, (w, p) => p.FullName)
                    .FirstOrDefaultAsync();

                var huerfano = !string.IsNullOrWhiteSpace(miNombre)
                    ? await ctx.ReunionParticipante
                        .Where(p => p.ReunionId == reunionId && p.State && p.WorkerId == null)
                        .ToListAsync()
                    : new List<ReunionParticipante>();
                var match = huerfano.FirstOrDefault(p =>
                    string.Equals(p.Nombre.Trim(), miNombre!.Trim(), StringComparison.OrdinalIgnoreCase));

                if (match != null)
                {
                    match.WorkerId = workerId.Value;
                    match.UpdatedDateTime = DateTime.UtcNow;
                    match.UpdatedUserId = userId;
                    await ctx.SaveChangesAsync();
                    esParticipante = true;
                }
            }

            if (!esParticipante)
                throw new AbrilException("No estás convocado a esta reunión.", 403);

            return workerId.Value;
        }

        // ── Dashboard "Mis acuerdos" ──────────────────────────────────────────────
        public async Task<List<MisAcuerdoDto>> GetMisAcuerdos(int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var workerId = await ResolveWorkerId(ctx, userId);
            if (workerId is null) return new List<MisAcuerdoDto>();

            var haceUnMes = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));

            var propios = await (
                from resp in ctx.ReunionAcuerdoResponsable
                where resp.State && resp.WorkerId == workerId.Value
                join ac in ctx.ReunionAcuerdo on resp.ReunionAcuerdoId equals ac.ReunionAcuerdoId
                where ac.State && !ac.EsInformativo
                join estado in ctx.ReunionAcuerdoEstado on ac.ReunionAcuerdoEstadoId equals estado.ReunionAcuerdoEstadoId
                join r in ctx.Reunion on ac.ReunionId equals r.ReunionId
                where r.State
                    && (estado.Descripcion != AcuerdoCumplido || (ac.FechaCumplimiento != null && ac.FechaCumplimiento >= haceUnMes))
                select new
                {
                    resp.ReunionAcuerdoResponsableId,
                    resp.EsPrincipal,
                    ac.RequiereAceptacion,
                    ac.RequiereEvidencia,
                    ac.EvidenciaUrl,
                    ac.ComentarioCumplimiento,
                    resp.EstadoAceptacion,
                    ac.ReunionAcuerdoId,
                    ac.Descripcion,
                    ac.Acciones,
                    ac.Criticidad,
                    ac.FechaProgramada,
                    ac.FechaCumplimiento,
                    ac.ReunionAcuerdoEstadoId,
                    ReunionAcuerdoEstado = estado.Descripcion,
                    r.ReunionId,
                    r.Numero,
                    r.Tema,
                    r.ProjectId,
                    r.AreaScopeId,
                }
            ).ToListAsync();

            if (propios.Count == 0) return new List<MisAcuerdoDto>();

            var acuerdoIds = propios.Select(p => p.ReunionAcuerdoId).Distinct().ToList();
            var todosResponsables = await (
                from x in ctx.ReunionAcuerdoResponsable
                where acuerdoIds.Contains(x.ReunionAcuerdoId) && x.State && x.WorkerId != null
                join w in ctx.Worker on x.WorkerId equals w.Id
                join per in ctx.Person on w.PersonId equals per.PersonId
                select new { x.ReunionAcuerdoId, WorkerId = x.WorkerId!.Value, Nombre = per.FullName }
            ).ToListAsync();
            var otrosPorAcuerdo = todosResponsables
                .GroupBy(x => x.ReunionAcuerdoId)
                .ToDictionary(g => g.Key, g => g.Where(x => x.WorkerId != workerId.Value).Select(x => x.Nombre).ToList());

            var proyectoIds = propios.Where(p => p.ProjectId != null).Select(p => p.ProjectId!.Value).Distinct().ToList();
            var proyectos = await ctx.Project.Where(p => proyectoIds.Contains(p.ProjectId))
                .Select(p => new { p.ProjectId, p.ProjectDescription }).ToListAsync();
            var areaScopeIds = propios.Where(p => p.AreaScopeId != null).Select(p => p.AreaScopeId!.Value).Distinct().ToList();
            var areaScopes = await ctx.AreaScope.Where(s => areaScopeIds.Contains(s.AreaScopeId))
                .Select(s => new { s.AreaScopeId, Nombre = s.AreaItem!.AreaItemName }).ToListAsync();

            return propios.Select(p => new MisAcuerdoDto
            {
                ReunionAcuerdoId = p.ReunionAcuerdoId,
                ReunionAcuerdoResponsableId = p.ReunionAcuerdoResponsableId,
                ReunionId = p.ReunionId,
                ReunionNumero = p.Numero,
                ReunionTema = p.Tema,
                Ambito = p.ProjectId != null
                    ? proyectos.First(x => x.ProjectId == p.ProjectId.Value).ProjectDescription
                    : p.AreaScopeId != null
                        ? areaScopes.First(x => x.AreaScopeId == p.AreaScopeId.Value).Nombre
                        : "Organización",
                Descripcion = p.Descripcion,
                Acciones = p.Acciones,
                Criticidad = p.Criticidad,
                FechaProgramada = p.FechaProgramada,
                ReunionAcuerdoEstadoId = p.ReunionAcuerdoEstadoId,
                ReunionAcuerdoEstado = p.ReunionAcuerdoEstado,
                RequiereEvidencia = p.RequiereEvidencia,
                EvidenciaUrl = p.EvidenciaUrl,
                ComentarioCumplimiento = p.ComentarioCumplimiento,
                FechaCumplimiento = p.FechaCumplimiento,
                EsPrincipal = p.EsPrincipal,
                OtrosResponsables = otrosPorAcuerdo.TryGetValue(p.ReunionAcuerdoId, out var otros) ? otros : new List<string>(),
                RequiereAceptacion = p.RequiereAceptacion,
                EstadoAceptacion = p.EstadoAceptacion,
            })
            .OrderBy(a => a.ReunionAcuerdoEstado == AcuerdoCumplido ? 1 : 0)
            .ThenBy(a => a.FechaProgramada.HasValue && a.FechaProgramada.Value < DateOnly.FromDateTime(DateTime.UtcNow) && a.ReunionAcuerdoEstado != AcuerdoCumplido ? 0 : 1)
            .ThenBy(a => a.Criticidad == "CRITICO" ? 0 : a.Criticidad == "MEDIO" ? 1 : 2)
            .ThenBy(a => a.FechaProgramada)
            .ToList();
        }

        // ── Vista global "Acuerdos" ──────────────────────────────────────────
        public async Task<PagedResultDto<AcuerdoBusquedaItemDto>> GetAcuerdos(AcuerdoBusquedaFiltroRequest filtro, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var workerId = await ResolveWorkerId(ctx, userId);

            // Mismo alcance que GetReuniones: reuniones que el usuario organizó o a las que fue
            // convocado. No se filtra por si es responsable del acuerdo (a diferencia de "Mis
            // Acuerdos"): esta vista es para ver todo lo tratado en las reuniones donde participó.
            var query =
                from ac in ctx.ReunionAcuerdo
                where ac.State
                join r in ctx.Reunion on ac.ReunionId equals r.ReunionId
                where r.State
                    && (r.CreatedUserId == userId
                        || (workerId != null && ctx.ReunionParticipante.Any(p =>
                            p.ReunionId == r.ReunionId && p.State && p.WorkerId == workerId.Value)))
                join estado in ctx.ReunionAcuerdoEstado on ac.ReunionAcuerdoEstadoId equals estado.ReunionAcuerdoEstadoId
                select new { ac, r, EstadoDescripcion = estado.Descripcion };

            if (!string.IsNullOrWhiteSpace(filtro.Estado))
            {
                var estado = filtro.Estado.Trim().ToUpperInvariant();
                query = estado == "INFORMATIVO"
                    ? query.Where(x => x.ac.EsInformativo)
                    : query.Where(x => !x.ac.EsInformativo && x.EstadoDescripcion == estado);
            }
            if (filtro.ResponsableWorkerId.HasValue)
            {
                var respId = filtro.ResponsableWorkerId.Value;
                query = query.Where(x => ctx.ReunionAcuerdoResponsable.Any(resp =>
                    resp.ReunionAcuerdoId == x.ac.ReunionAcuerdoId && resp.State && resp.WorkerId == respId));
            }
            if (filtro.Desde.HasValue)
                query = query.Where(x => x.ac.FechaProgramada != null && x.ac.FechaProgramada >= filtro.Desde.Value);
            if (filtro.Hasta.HasValue)
                query = query.Where(x => x.ac.FechaProgramada != null && x.ac.FechaProgramada <= filtro.Hasta.Value);
            if (!string.IsNullOrWhiteSpace(filtro.Texto))
            {
                var texto = filtro.Texto.Trim().ToLower();
                query = query.Where(x => x.ac.Descripcion.ToLower().Contains(texto) || x.r.Tema.ToLower().Contains(texto));
            }

            var page = filtro.Page < 1 ? 1 : filtro.Page;
            var pageSize = filtro.PageSize < 1 ? 10 : filtro.PageSize;

            var total = await query.CountAsync();

            var pageData = await query
                .OrderByDescending(x => x.ac.FechaProgramada ?? x.r.Fecha)
                .ThenByDescending(x => x.ac.ReunionAcuerdoId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.ac.ReunionAcuerdoId,
                    x.r.ReunionId,
                    x.r.Numero,
                    x.r.Tema,
                    x.r.ProjectId,
                    x.r.AreaScopeId,
                    x.ac.Descripcion,
                    x.ac.Criticidad,
                    x.ac.FechaProgramada,
                    x.ac.FechaCumplimiento,
                    x.ac.ReunionAcuerdoEstadoId,
                    x.EstadoDescripcion,
                    x.ac.EsInformativo,
                    x.ac.RequiereEvidencia,
                    x.ac.EvidenciaUrl,
                    x.ac.ComentarioCumplimiento,
                })
                .ToListAsync();

            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            if (pageData.Count == 0)
                return new PagedResultDto<AcuerdoBusquedaItemDto> { Page = page, PageSize = pageSize, TotalRecords = total, TotalPages = totalPages, Data = new() };

            // Ambito (proyecto/área/organización) y responsables — mismo patrón que GetMisAcuerdos.
            var proyectoIds = pageData.Where(p => p.ProjectId != null).Select(p => p.ProjectId!.Value).Distinct().ToList();
            var proyectos = await ctx.Project.Where(p => proyectoIds.Contains(p.ProjectId))
                .Select(p => new { p.ProjectId, p.ProjectDescription }).ToListAsync();
            var areaScopeIds = pageData.Where(p => p.AreaScopeId != null).Select(p => p.AreaScopeId!.Value).Distinct().ToList();
            var areaScopes = await ctx.AreaScope.Where(s => areaScopeIds.Contains(s.AreaScopeId))
                .Select(s => new { s.AreaScopeId, Nombre = s.AreaItem!.AreaItemName }).ToListAsync();

            var acuerdoIds = pageData.Select(p => p.ReunionAcuerdoId).ToList();
            var responsables = await (
                from x in ctx.ReunionAcuerdoResponsable
                where acuerdoIds.Contains(x.ReunionAcuerdoId) && x.State && x.WorkerId != null
                join w in ctx.Worker on x.WorkerId equals w.Id
                join per in ctx.Person on w.PersonId equals per.PersonId
                select new { x.ReunionAcuerdoId, Nombre = per.FullName }
            ).ToListAsync();
            var responsablesPorAcuerdo = responsables.GroupBy(x => x.ReunionAcuerdoId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Nombre).ToList());

            var data = pageData.Select(p => new AcuerdoBusquedaItemDto
            {
                ReunionAcuerdoId = p.ReunionAcuerdoId,
                ReunionId = p.ReunionId,
                ReunionNumero = p.Numero,
                ReunionTema = p.Tema,
                Ambito = p.ProjectId != null
                    ? proyectos.First(x => x.ProjectId == p.ProjectId.Value).ProjectDescription
                    : p.AreaScopeId != null
                        ? areaScopes.First(x => x.AreaScopeId == p.AreaScopeId.Value).Nombre
                        : "Organización",
                Descripcion = p.Descripcion,
                Criticidad = p.Criticidad,
                FechaProgramada = p.FechaProgramada,
                FechaCumplimiento = p.FechaCumplimiento,
                ReunionAcuerdoEstadoId = p.ReunionAcuerdoEstadoId,
                ReunionAcuerdoEstado = p.EstadoDescripcion,
                EsInformativo = p.EsInformativo,
                RequiereEvidencia = p.RequiereEvidencia,
                EvidenciaUrl = p.EvidenciaUrl,
                ComentarioCumplimiento = p.ComentarioCumplimiento,
                Responsables = responsablesPorAcuerdo.TryGetValue(p.ReunionAcuerdoId, out var list) ? list : new List<string>(),
            }).ToList();

            return new PagedResultDto<AcuerdoBusquedaItemDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = totalPages,
                Data = data,
            };
        }

        // ── Revisión de acuerdos pendientes de ediciones anteriores ──────────────────
        /// <summary>Acuerdos aún no cumplidos (ni anulados) de reuniones anteriores en la misma
        /// cadena de convocatoria recurrente (siguiendo reunion_anterior_id hacia atrás), para
        /// revisarlos al abrir la siguiente edición. No incluye los de <paramref name="reunionId"/>
        /// misma, solo los de ediciones previas.</summary>
        public async Task<List<AcuerdoPendienteAnteriorDto>> GetAcuerdosPendientesAnteriores(int reunionId)
        {
            using var ctx = _factory.CreateDbContext();

            // Sube la cadena de "reunión anterior" hasta el inicio (con un tope de seguridad).
            var reunionesAnteriores = new List<int>();
            var actualId = (int?)reunionId;
            for (var i = 0; i < 50; i++)
            {
                var anteriorId = await ctx.Reunion
                    .Where(r => r.ReunionId == actualId!.Value)
                    .Select(r => r.ReunionAnteriorId)
                    .FirstOrDefaultAsync();
                if (anteriorId is null) break;
                reunionesAnteriores.Add(anteriorId.Value);
                actualId = anteriorId;
            }

            if (reunionesAnteriores.Count == 0) return new List<AcuerdoPendienteAnteriorDto>();

            var acuerdos = await (
                from a in ctx.ReunionAcuerdo
                where reunionesAnteriores.Contains(a.ReunionId) && a.State
                join estado in ctx.ReunionAcuerdoEstado on a.ReunionAcuerdoEstadoId equals estado.ReunionAcuerdoEstadoId
                where estado.Descripcion != AcuerdoCumplido && estado.Descripcion != "ANULADO"
                join r in ctx.Reunion on a.ReunionId equals r.ReunionId
                orderby a.FechaProgramada
                select new AcuerdoPendienteAnteriorDto
                {
                    ReunionAcuerdoId = a.ReunionAcuerdoId,
                    ReunionId = r.ReunionId,
                    ReunionNumero = r.Numero,
                    ReunionTema = r.Tema,
                    Descripcion = a.Descripcion,
                    Acciones = a.Acciones,
                    Criticidad = a.Criticidad,
                    FechaProgramada = a.FechaProgramada,
                    ReunionAcuerdoEstadoId = a.ReunionAcuerdoEstadoId,
                    ReunionAcuerdoEstado = estado.Descripcion,
                    RequiereEvidencia = a.RequiereEvidencia,
                    EvidenciaUrl = a.EvidenciaUrl,
                    VecesReprogramado = a.VecesReprogramado,
                    UltimoMotivoReprogramacion = a.UltimoMotivoReprogramacion,
                }
            ).ToListAsync();

            if (acuerdos.Count == 0) return acuerdos;

            var acuerdoIds = acuerdos.Select(a => a.ReunionAcuerdoId).ToList();
            var responsables = await (
                from x in ctx.ReunionAcuerdoResponsable
                where acuerdoIds.Contains(x.ReunionAcuerdoId) && x.State && x.WorkerId != null
                join w in ctx.Worker on x.WorkerId equals w.Id
                join per in ctx.Person on w.PersonId equals per.PersonId
                select new
                {
                    x.ReunionAcuerdoId,
                    x.ReunionAcuerdoResponsableId,
                    WorkerId = x.WorkerId!.Value,
                    WorkerNombre = per.FullName,
                    x.EstadoAceptacion,
                    x.MotivoRechazo,
                    x.EsPrincipal,
                }
            ).ToListAsync();
            var porAcuerdo = responsables.GroupBy(r => r.ReunionAcuerdoId).ToDictionary(
                g => g.Key,
                g => g.Select(r => new ReunionAcuerdoResponsableDto
                {
                    ReunionAcuerdoResponsableId = r.ReunionAcuerdoResponsableId,
                    WorkerId = r.WorkerId,
                    WorkerNombre = r.WorkerNombre,
                    EstadoAceptacion = r.EstadoAceptacion,
                    MotivoRechazo = r.MotivoRechazo,
                    EsPrincipal = r.EsPrincipal,
                }).ToList());

            foreach (var a in acuerdos)
                a.Responsables = porAcuerdo.TryGetValue(a.ReunionAcuerdoId, out var lista) ? lista : new List<ReunionAcuerdoResponsableDto>();

            // Vencidos y críticos primero.
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            return acuerdos
                .OrderBy(a => a.FechaProgramada.HasValue && a.FechaProgramada.Value < hoy ? 0 : 1)
                .ThenBy(a => a.Criticidad == "CRITICO" ? 0 : a.Criticidad == "MEDIO" ? 1 : 2)
                .ThenBy(a => a.FechaProgramada)
                .ToList();
        }

        /// <summary>Marca un acuerdo como cumplido sin necesitar el payload completo (usado desde la
        /// revisión de pendientes de ediciones anteriores, que solo maneja una proyección reducida).</summary>
        public async Task MarcarAcuerdoCumplido(int reunionAcuerdoId, AcuerdoMarcarCumplidoRequest request, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var acuerdo = await ctx.ReunionAcuerdo
                .FirstOrDefaultAsync(a => a.ReunionAcuerdoId == reunionAcuerdoId && a.State);
            if (acuerdo is null)
                throw new AbrilException("El acuerdo no existe.", 404);

            await EnsurePuedeGestionarAcuerdo(ctx, reunionAcuerdoId, userId);

            // Si esta misma acción trae el archivo recién subido, queda como evidencia del acuerdo
            // antes de evaluar si la exigencia de evidencia quedó satisfecha.
            if (!string.IsNullOrWhiteSpace(request.EvidenciaUrl))
                acuerdo.EvidenciaUrl = request.EvidenciaUrl.Trim();

            if (acuerdo.RequiereEvidencia && string.IsNullOrWhiteSpace(acuerdo.EvidenciaUrl))
                throw new AbrilException("Este acuerdo requiere adjuntar evidencia antes de poder marcarse como cumplido.", 400);

            // Siempre obligatorio: es el registro de cómo se resolvió, tenga o no evidencia adjunta.
            if (string.IsNullOrWhiteSpace(request.Comentario))
                throw new AbrilException("Indica cómo se levantó el acuerdo.", 400);

            acuerdo.ReunionAcuerdoEstadoId = await GetEstadoAcuerdoId(ctx, AcuerdoCumplido);
            acuerdo.FechaCumplimiento = DateOnly.FromDateTime(DateTime.UtcNow);
            acuerdo.ComentarioCumplimiento = request.Comentario?.Trim();
            acuerdo.UpdatedDateTime = DateTime.UtcNow;
            acuerdo.UpdatedUserId = userId;

            await ctx.SaveChangesAsync();
        }

        public async Task ReprogramarAcuerdo(int reunionAcuerdoId, AcuerdoReprogramarRequest request, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var acuerdo = await ctx.ReunionAcuerdo
                .FirstOrDefaultAsync(a => a.ReunionAcuerdoId == reunionAcuerdoId && a.State);
            if (acuerdo is null)
                throw new AbrilException("El acuerdo no existe.", 404);

            await EnsurePuedeGestionarAcuerdo(ctx, reunionAcuerdoId, userId);

            acuerdo.FechaProgramada = request.NuevaFechaProgramada;
            acuerdo.FechaReprogramacion = request.NuevaFechaProgramada;
            acuerdo.VecesReprogramado += 1;
            acuerdo.UltimoMotivoReprogramacion = request.Motivo.Trim();
            acuerdo.UpdatedDateTime = DateTime.UtcNow;
            acuerdo.UpdatedUserId = userId;

            await ctx.SaveChangesAsync();
        }

        // ── Convocatoria inmediata (al agendar) ──────────────────────────────────
        /// <summary>
        /// Datos de la reunión + emails de sus participantes, para el correo de convocatoria.
        /// <paramref name="soloWorkerIds"/> filtra a solo esos workers (ej. al agregar participantes
        /// nuevos en una edición, para no re-notificar a los que ya estaban); null = todos.
        /// </summary>
        public async Task<ReunionConvocatoriaInfoDto?> GetInfoParaConvocatoria(int reunionId, List<int>? soloWorkerIds = null)
        {
            using var ctx = _factory.CreateDbContext();

            var r = await ctx.Reunion.FirstOrDefaultAsync(x => x.ReunionId == reunionId && x.State);
            if (r == null) return null;

            var ambito = r.ProjectId != null
                ? await ctx.Project.Where(p => p.ProjectId == r.ProjectId.Value).Select(p => p.ProjectDescription).FirstOrDefaultAsync()
                : r.AreaScopeId != null
                    ? await ctx.AreaScope.Where(s => s.AreaScopeId == r.AreaScopeId.Value).Select(s => s.AreaItem!.AreaItemName).FirstOrDefaultAsync()
                    : "Organización";

            var destinatarios = await (
                from part in ctx.ReunionParticipante
                where part.ReunionId == r.ReunionId && part.State && part.WorkerId != null
                    && (soloWorkerIds == null || soloWorkerIds.Contains(part.WorkerId.Value))
                join w in ctx.Worker on part.WorkerId equals w.Id
                join p in ctx.Person on w.PersonId equals p.PersonId
                where p.UserId != null && w.EmailCorporativo != null
                select new ReunionRecordatorioDestinatarioDto
                {
                    UserId = p.UserId!.Value,
                    WorkerId = w.Id,
                    Nombre = p.FullName,
                    Email = w.EmailCorporativo!,
                }
            ).Distinct().ToListAsync();

            return new ReunionConvocatoriaInfoDto
            {
                ReunionId = r.ReunionId,
                Numero = r.Numero,
                Tema = r.Tema,
                AmbitoDescripcion = ambito ?? "Organización",
                Fecha = r.Fecha,
                HoraInicio = r.HoraInicio,
                Lugar = r.Lugar,
                Destinatarios = destinatarios,
            };
        }

        // ── Recordatorio de agenda (job) ───────────────────────────────────────
        // Toda reunión requiere agenda (fija o dinámica): cuando el tema no la deja fija con
        // texto, se recuerda a los convocados que carguen sus temas. Aplica también a
        // reuniones con tema personalizado (sin vínculo al catálogo), usando el default abajo.
        private const decimal DefaultRecordatorioHorasAntes = 24m;

        public async Task<List<ReunionRecordatorioCandidatoDto>> GetCandidatosRecordatorioAgenda()
        {
            using var ctx = _factory.CreateDbContext();

            var estadoProgramadaId = await GetEstadoReunionId(ctx, EstadoProgramada);

            var yaEnviadas = await ctx.ReunionRecordatorioLog.Select(l => l.ReunionId).ToListAsync();

            var candidatas = await (
                from r in ctx.Reunion
                where r.State
                    && r.ReunionEstadoId == estadoProgramadaId
                    && r.HoraInicio != null
                    && string.IsNullOrWhiteSpace(r.AgendaTexto)
                    && !yaEnviadas.Contains(r.ReunionId)
                join t in ctx.ReunionTema on r.ReunionTemaId equals t.ReunionTemaId into temaJoin
                from t in temaJoin.DefaultIfEmpty()
                where !(t != null && t.AgendaFija && t.AgendaTexto != null && t.AgendaTexto != "")
                select new
                {
                    r,
                    RecordatorioHorasAntes = t != null && t.RecordatorioHorasAntes != null
                        ? t.RecordatorioHorasAntes.Value
                        : DefaultRecordatorioHorasAntes,
                }
            ).ToListAsync();

            var resultado = new List<ReunionRecordatorioCandidatoDto>();
            foreach (var c in candidatas)
            {
                var ambito = c.r.ProjectId != null
                    ? await ctx.Project.Where(p => p.ProjectId == c.r.ProjectId.Value).Select(p => p.ProjectDescription).FirstOrDefaultAsync()
                    : c.r.AreaScopeId != null
                        ? await ctx.AreaScope.Where(s => s.AreaScopeId == c.r.AreaScopeId.Value).Select(s => s.AreaItem!.AreaItemName).FirstOrDefaultAsync()
                        : "Organización";

                var destinatarios = await (
                    from part in ctx.ReunionParticipante
                    where part.ReunionId == c.r.ReunionId && part.State && part.WorkerId != null
                    join w in ctx.Worker on part.WorkerId equals w.Id
                    join p in ctx.Person on w.PersonId equals p.PersonId
                    where p.UserId != null && w.EmailCorporativo != null
                    select new ReunionRecordatorioDestinatarioDto
                    {
                        UserId = p.UserId!.Value,
                        WorkerId = w.Id,
                        Nombre = p.FullName,
                        Email = w.EmailCorporativo!,
                    }
                ).Distinct().ToListAsync();

                resultado.Add(new ReunionRecordatorioCandidatoDto
                {
                    ReunionId = c.r.ReunionId,
                    Numero = c.r.Numero,
                    Tema = c.r.Tema,
                    AmbitoDescripcion = ambito ?? "Organización",
                    Fecha = c.r.Fecha,
                    HoraInicio = c.r.HoraInicio!.Value,
                    RecordatorioHorasAntes = c.RecordatorioHorasAntes,
                    Destinatarios = destinatarios,
                });
            }

            return resultado;
        }

        public async Task RegistrarRecordatorioEnviado(int reunionId)
        {
            using var ctx = _factory.CreateDbContext();

            var yaRegistrado = await ctx.ReunionRecordatorioLog.AnyAsync(l => l.ReunionId == reunionId);
            if (yaRegistrado) return;

            ctx.ReunionRecordatorioLog.Add(new ReunionRecordatorioLog
            {
                ReunionId = reunionId,
                EnviadoDateTime = DateTime.UtcNow,
            });
            await ctx.SaveChangesAsync();
        }

        public async Task<bool> EsFeriado(DateOnly fecha)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Holiday.AnyAsync(h => h.State && h.Active &&
                ((h.RecurringYearly && h.HolidayDate.Month == fecha.Month && h.HolidayDate.Day == fecha.Day)
                 || (!h.RecurringYearly && h.HolidayDate == fecha)));
        }

        /// <summary>Resuelve el workerId (workers.id) asociado al usuario autenticado, vía person.user_id.</summary>
        private static async Task<int?> ResolveWorkerId(AppDbContext ctx, int userId)
        {
            return await (
                from w in ctx.Worker
                join p in ctx.Person on w.PersonId equals p.PersonId
                where p.UserId == userId
                select (int?)w.Id
            ).FirstOrDefaultAsync();
        }

        /// <summary>Catálogo de puestos, para el filtro de convocatoria masiva (ej. "Jefaturas", "Coordinador SSOMA").</summary>
        public async Task<List<CatalogoDto>> GetPuestos()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Puesto
                .Where(p => p.State && p.Active)
                .OrderBy(p => p.Nombre)
                .Select(p => new CatalogoDto { Id = p.PuestoId, Descripcion = p.Nombre })
                .ToListAsync();
        }

        /// <summary>
        /// Puestos que realmente tiene algún trabajador activo dentro de un área/gerencia (con toda
        /// su descendencia); null = cualquier trabajador de la organización. Evita que el filtro de
        /// convocatoria masiva muestre puestos ajenos al área elegida (ej. "Abogado" al filtrar por
        /// Gerencia de Proyectos).
        /// </summary>
        public async Task<List<CatalogoDto>> GetPuestosPorArea(int? areaScopeId)
        {
            using var ctx = _factory.CreateDbContext();

            HashSet<int>? descendientes = null;
            if (areaScopeId.HasValue)
                descendientes = await ctx.ResolveDescendantsAsync(areaScopeId.Value);

            var query = ctx.Worker.Where(w => w.PuestoId != null && w.WorkersEstadoId == WorkersEstadoIds.Activo);
            if (descendientes != null)
                query = query.Where(w => w.AreaScopeId != null && descendientes.Contains(w.AreaScopeId.Value));

            var puestoIds = await query.Select(w => w.PuestoId!.Value).Distinct().ToListAsync();

            return await ctx.Puesto
                .Where(p => p.State && p.Active && puestoIds.Contains(p.PuestoId))
                .OrderBy(p => p.Nombre)
                .Select(p => new CatalogoDto { Id = p.PuestoId, Descripcion = p.Nombre })
                .ToListAsync();
        }

        /// <summary>Temas predefinidos para el desplegable de "Tema de la reunión" al agendar.</summary>
        private static async Task<List<ReunionTemaOpcionDto>> GetTemas(AppDbContext ctx)
        {
            return await ctx.ReunionTema
                .Where(t => t.State && t.Active)
                .OrderBy(t => t.ReunionTemaId)
                .Select(t => new ReunionTemaOpcionDto { Id = t.ReunionTemaId, Descripcion = t.Descripcion, AreaScopeId = t.AreaScopeId })
                .ToListAsync();
        }

        /// <summary>
        /// Si un participante se eligió del desplegable de trabajadores (trae WorkerId) y su cargo
        /// se ingresó a mano porque el worker no tenía puesto, se completa la ficha del trabajador.
        /// No pisa datos existentes.
        ///
        /// El puesto ya no es texto libre: si el cargo escrito no existe en el catálogo se da de
        /// alta ahí (en MAYÚSCULAS y sin categoría, para que la asignen desde Configuración) y se
        /// apunta el trabajador a esa fila. Así el catálogo sigue siendo la única fuente.
        /// </summary>
        private static async Task BackfillPuestoTrabajadores(AppDbContext ctx, List<ReunionParticipanteInput> participantes)
        {
            var cargoPorWorker = participantes
                .Where(p => p.WorkerId.HasValue && !string.IsNullOrWhiteSpace(p.Cargo))
                .GroupBy(p => p.WorkerId!.Value)
                .ToDictionary(g => g.Key, g => g.First().Cargo!.Trim().ToUpperInvariant());
            if (cargoPorWorker.Count == 0) return;

            var ids = cargoPorWorker.Keys.ToList();
            var workers = await ctx.Worker
                .Where(w => ids.Contains(w.Id) && w.PuestoId == null)
                .ToListAsync();
            if (workers.Count == 0) return;

            var nombres = workers.Select(w => cargoPorWorker[w.Id]).Distinct().ToList();
            var existentes = await ctx.Puesto
                .Where(p => p.State && nombres.Contains(p.Nombre))
                .ToDictionaryAsync(p => p.Nombre, p => p);

            foreach (var worker in workers)
            {
                var nombre = cargoPorWorker[worker.Id];
                if (!existentes.TryGetValue(nombre, out var puesto))
                {
                    puesto = new Puesto { Nombre = nombre, CreatedDateTime = DateTime.UtcNow };
                    ctx.Puesto.Add(puesto);
                    existentes[nombre] = puesto;
                }
                worker.PuestoCatalogo = puesto;
                worker.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        private static async Task<Reunion> GetReunionOrThrow(AppDbContext ctx, int reunionId)
        {
            var reunion = await ctx.Reunion.FirstOrDefaultAsync(r => r.ReunionId == reunionId && r.State);
            if (reunion is null)
                throw new AbrilException("El acta de reunión no existe.", 404);
            return reunion;
        }

        /// <summary>true si el usuario autenticado puede editar el acta: es su creador, o es un
        /// participante marcado como coautor (ver ReunionParticipante.EsCoautor).</summary>
        private static async Task<bool> PuedeEditarActa(AppDbContext ctx, int reunionId, int createdUserId, int userId)
        {
            if (createdUserId == userId) return true;

            var workerId = await ResolveWorkerId(ctx, userId);
            if (workerId is null) return false;

            return await ctx.ReunionParticipante.AnyAsync(p =>
                p.ReunionId == reunionId && p.State && p.EsCoautor && p.WorkerId == workerId.Value);
        }

        /// <summary>Lanza 403 si el usuario autenticado no puede editar el acta (ver PuedeEditarActa).
        /// Usar en el CRUD del acta y sus acuerdos; no aplica a "marcar cumplido"/"reprogramar" un
        /// acuerdo propio, que se rigen por EnsurePuedeGestionarAcuerdo (solo el responsable).</summary>
        private static async Task EnsurePuedeEditarActa(AppDbContext ctx, int reunionId, int createdUserId, int userId)
        {
            if (!await PuedeEditarActa(ctx, reunionId, createdUserId, userId))
                throw new AbrilException("Solo el creador del acta o sus coautores pueden editarla.", 403);
        }

        /// <summary>Igual que EnsurePuedeEditarActa pero resolviendo primero el CreatedUserId del acta.
        /// Para usar en operaciones sobre un acuerdo/archivo puntual donde no se tiene la Reunion cargada.</summary>
        private static async Task EnsurePuedeEditarActaByReunionId(AppDbContext ctx, int reunionId, int userId)
        {
            var createdUserId = await ctx.Reunion
                .Where(r => r.ReunionId == reunionId)
                .Select(r => (int?)r.CreatedUserId)
                .FirstOrDefaultAsync();
            if (createdUserId is null) return; // ya validado por GetReunionOrThrow o la carga previa del acuerdo/archivo
            await EnsurePuedeEditarActa(ctx, reunionId, createdUserId.Value, userId);
        }

        /// <summary>Lanza 403 si el usuario autenticado no es responsable (worker) del acuerdo. Usado
        /// para "marcar cumplido" y "reprogramar" un acuerdo propio desde Mis Acuerdos.</summary>
        private static async Task EnsurePuedeGestionarAcuerdo(AppDbContext ctx, int reunionAcuerdoId, int userId)
        {
            var workerId = await ResolveWorkerId(ctx, userId);
            var esResponsable = workerId.HasValue && await ctx.ReunionAcuerdoResponsable.AnyAsync(r =>
                r.ReunionAcuerdoId == reunionAcuerdoId && r.State && r.WorkerId == workerId.Value);
            if (!esResponsable)
                throw new AbrilException("Solo un responsable de este acuerdo puede realizar esta acción.", 403);
        }

        private static async Task<int> GetEstadoReunionId(AppDbContext ctx, string descripcion)
        {
            var id = await ctx.ReunionEstado
                .Where(e => e.Descripcion == descripcion && e.State)
                .Select(e => (int?)e.ReunionEstadoId)
                .FirstOrDefaultAsync();
            if (id is null)
                throw new AbrilException($"El estado de reunión '{descripcion}' no está configurado.", 400);
            return id.Value;
        }

        private static async Task<int> GetEstadoAcuerdoId(AppDbContext ctx, string descripcion)
        {
            var id = await ctx.ReunionAcuerdoEstado
                .Where(e => e.Descripcion == descripcion && e.State)
                .Select(e => (int?)e.ReunionAcuerdoEstadoId)
                .FirstOrDefaultAsync();
            if (id is null)
                throw new AbrilException($"El estado de acuerdo '{descripcion}' no está configurado.", 400);
            return id.Value;
        }

        private static async Task<string> GetEstadoAcuerdoDescripcion(AppDbContext ctx, int reunionAcuerdoEstadoId)
        {
            var descripcion = await ctx.ReunionAcuerdoEstado
                .Where(e => e.ReunionAcuerdoEstadoId == reunionAcuerdoEstadoId)
                .Select(e => e.Descripcion)
                .FirstOrDefaultAsync();
            return descripcion ?? string.Empty;
        }

        /// <summary>
        /// Los responsables ahora son cualquier worker de la organización, sin importar si
        /// asistió a la reunión: solo se valida que la lista de workers exista y esté vigente.
        /// </summary>
        private static async Task ValidarResponsables(AppDbContext ctx, List<int> responsableWorkerIds)
        {
            if (responsableWorkerIds.Count == 0) return;
            var ids = responsableWorkerIds.Distinct().ToList();
            var validos = await (
                from w in ctx.Worker
                where ids.Contains(w.Id)
                join p in ctx.Person on w.PersonId equals p.PersonId
                where p.State
                select w.Id
            ).CountAsync();
            if (validos != ids.Count)
                throw new AbrilException("Uno o más responsables seleccionados no existen.", 400);
        }
    }
}
