using Abril_Backend.Shared.Constants;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Application.Dtos;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Infrastructure.Repositories;

public class RevisionRepository : IRevisionRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public RevisionRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<List<RevisionDTO>> GetRevisiones(int? proyectoId, bool soloActivas)
    {
        using var ctx = _factory.CreateDbContext();
        var query = ctx.AcRevisiones.Include(r => r.Proyecto).AsQueryable();

        if (proyectoId.HasValue) query = query.Where(r => r.ProyectoId == proyectoId.Value);
        if (soloActivas) query = query.Where(r => r.Activo);

        return await query
            .OrderBy(r => r.Nombre)
            .Select(r => new RevisionDTO
            {
                Id = r.Id,
                ProyectoId = r.ProyectoId,
                ProyectoNombre = r.Proyecto != null ? r.Proyecto.ProjectDescription : null,
                Tipo = r.Tipo,
                Lugar = r.Lugar,
                Nombre = r.Nombre,
                Activo = r.Activo
            })
            .ToListAsync();
    }

    public async Task<string?> GetProyectoNombre(int proyectoId)
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.Project.Where(p => p.ProjectId == proyectoId).Select(p => p.ProjectDescription).FirstOrDefaultAsync();
    }

    public async Task<AcRevision?> GetRevisionEntityById(int id)
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.AcRevisiones.Include(r => r.Proyecto).FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<AcRevision> CreateRevision(int proyectoId, string tipo, string lugar, string nombre)
    {
        using var ctx = _factory.CreateDbContext();
        var revision = new AcRevision { ProyectoId = proyectoId, Tipo = tipo, Lugar = lugar, Nombre = nombre, Activo = true };
        ctx.AcRevisiones.Add(revision);
        await ctx.SaveChangesAsync();
        return revision;
    }

    public async Task<bool> DeleteRevision(int id)
    {
        using var ctx = _factory.CreateDbContext();
        var revision = await ctx.AcRevisiones.FindAsync(id);
        if (revision == null) return false;
        ctx.AcRevisiones.Remove(revision);
        await ctx.SaveChangesAsync();
        return true;
    }

    public async Task<RevisionObservacionListResponseDTO> GetObservaciones(int? revisionId, int? proyectoId, string? estado, string? partida, DateTime? desde, DateTime? hasta, string? search, int pagina, int porPagina)
    {
        using var ctx = _factory.CreateDbContext();
        var query = ctx.AcRevisionObservaciones
            .Include(o => o.Revision).ThenInclude(r => r!.Proyecto)
            .Include(o => o.Fotos)
            .Include(o => o.LevantaPor)
            .AsQueryable();

        if (revisionId.HasValue) query = query.Where(o => o.RevisionId == revisionId.Value);
        if (proyectoId.HasValue) query = query.Where(o => o.Revision != null && o.Revision.ProyectoId == proyectoId.Value);
        if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(o => o.Estado == estado);
        if (!string.IsNullOrWhiteSpace(partida)) query = query.Where(o => o.PartidaReportada == partida);
        if (desde.HasValue) query = query.Where(o => o.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(o => o.Fecha <= hasta.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o => o.Descripcion.Contains(search) || (o.PersonaReporta != null && o.PersonaReporta.Contains(search)));

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.Fecha)
            .Skip((pagina - 1) * porPagina)
            .Take(porPagina)
            .Select(o => new RevisionObservacionListItemDTO
            {
                Id = o.Id,
                RevisionId = o.RevisionId,
                RevisionNombre = o.Revision != null ? o.Revision.Nombre : null,
                ProyectoId = o.Revision != null ? o.Revision.ProyectoId : 0,
                ProyectoNombre = o.Revision != null && o.Revision.Proyecto != null ? o.Revision.Proyecto.ProjectDescription : null,
                Fecha = o.Fecha,
                PersonaReporta = o.PersonaReporta,
                ZonaAmbiente = o.ZonaAmbiente,
                PartidaReportada = o.PartidaReportada,
                Descripcion = o.Descripcion,
                PlazoLevantamiento = o.PlazoLevantamiento,
                Estado = o.Estado,
                Origen = o.Origen,
                LevantaPorWorkerId = o.LevantaPorWorkerId,
                LevantaPorNombre = o.LevantaPor != null ? o.LevantaPor.ApellidoNombre : null,
                Fotos = o.Fotos.Select(f => new RevisionObservacionFotoDTO { Id = f.Id, Tipo = f.Tipo, Url = f.Url, Orden = f.Orden }).ToList()
            })
            .ToListAsync();

        return new RevisionObservacionListResponseDTO { Total = total, Pagina = pagina, PorPagina = porPagina, Items = items };
    }

    public async Task<RevisionObservacionListItemDTO?> GetObservacionById(int id)
    {
        using var ctx = _factory.CreateDbContext();
        var o = await ctx.AcRevisionObservaciones
            .Include(x => x.Revision).ThenInclude(r => r!.Proyecto)
            .Include(x => x.Fotos)
            .Include(x => x.LevantaPor)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (o == null) return null;

        return new RevisionObservacionListItemDTO
        {
            Id = o.Id,
            RevisionId = o.RevisionId,
            RevisionNombre = o.Revision?.Nombre,
            ProyectoId = o.Revision?.ProyectoId ?? 0,
            ProyectoNombre = o.Revision?.Proyecto?.ProjectDescription,
            Fecha = o.Fecha,
            PersonaReporta = o.PersonaReporta,
            ZonaAmbiente = o.ZonaAmbiente,
            PartidaReportada = o.PartidaReportada,
            Descripcion = o.Descripcion,
            PlazoLevantamiento = o.PlazoLevantamiento,
            Estado = o.Estado,
            Origen = o.Origen,
            LevantaPorWorkerId = o.LevantaPorWorkerId,
            LevantaPorNombre = o.LevantaPor?.ApellidoNombre,
            Fotos = o.Fotos.Select(f => new RevisionObservacionFotoDTO { Id = f.Id, Tipo = f.Tipo, Url = f.Url, Orden = f.Orden }).ToList()
        };
    }

    public async Task<RevisionFiltrosDTO> GetFiltros()
    {
        using var ctx = _factory.CreateDbContext();

        // Igual criterio que Observaciones.GetFiltros: sale del flag TieneArquitecturaComercial
        // en Project, no de revisiones ya creadas — si no, un proyecto sin revisiones todavía
        // nunca aparecería en el combo para poder crear su primera revisión.
        var proyectos = await ctx.Project
            .Where(p => p.TieneArquitecturaComercial && p.State && p.Active && !ctx.ProyectoFiltro.Any(f => f.ProjectId == p.ProjectId && f.FuncionalidadId == ProyectoFiltroFuncionalidades.AcRevisiones && !f.Active))
            .Select(p => new ProyectoRevisionFiltroDTO { Id = p.ProjectId, Nombre = p.ProjectDescription })
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        var partidas = await ctx.AcRevisionObservaciones
            .Where(o => o.PartidaReportada != null)
            .Select(o => o.PartidaReportada!)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();

        var estados = await ctx.AcRevisionObservaciones
            .Select(o => o.Estado)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync();

        return new RevisionFiltrosDTO
        {
            Proyectos = proyectos,
            Partidas = partidas,
            Estados = estados,
            Tipos = TipoRevision.Valores.ToList()
        };
    }

    public async Task<RevisionDashboardDTO> GetDashboard(DateTime? desde, DateTime? hasta, int? proyectoId)
    {
        using var ctx = _factory.CreateDbContext();
        var query = ctx.AcRevisionObservaciones.Include(o => o.Revision).AsQueryable();

        if (desde.HasValue) query = query.Where(o => o.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(o => o.Fecha <= hasta.Value);
        if (proyectoId.HasValue) query = query.Where(o => o.Revision != null && o.Revision.ProyectoId == proyectoId.Value);

        var registros = await query.ToListAsync();

        var grupos = registros
            .GroupBy(o => new { Persona = o.PersonaReporta ?? "Sin asignar", Revision = o.Revision?.Nombre })
            .Select(g => new RevisionDashboardGrupoDTO
            {
                PersonaReporta = g.Key.Persona,
                RevisionNombre = g.Key.Revision,
                TotalReportadas = g.Count(),
                TotalCompletadas = g.Count(o => o.Estado == "Completado"),
                TotalPendientes = g.Count(o => o.Estado == "Pendiente"),
                TotalEnProceso = g.Count(o => o.Estado == "En Proceso"),
                PctAvance = g.Count() == 0 ? 0 : Math.Round(g.Count(o => o.Estado == "Completado") * 100m / g.Count(), 1),
                PorPartida = g.GroupBy(o => o.PartidaReportada ?? "Otros")
                    .Select(pg => new RevisionObservacionPorPartidaDTO
                    {
                        Partida = pg.Key,
                        Completado = pg.Count(o => o.Estado == "Completado"),
                        Pendiente = pg.Count(o => o.Estado != "Completado")
                    }).ToList()
            })
            .OrderByDescending(g => g.TotalReportadas)
            .ToList();

        return new RevisionDashboardDTO { Grupos = grupos };
    }

    public async Task<RevisionObservacionStatsDTO> GetStats(DateTime? desde, DateTime? hasta, int? proyectoId)
    {
        using var ctx = _factory.CreateDbContext();
        var query = ctx.AcRevisionObservaciones.Include(o => o.Revision).AsQueryable();

        if (desde.HasValue) query = query.Where(o => o.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(o => o.Fecha <= hasta.Value);
        if (proyectoId.HasValue) query = query.Where(o => o.Revision != null && o.Revision.ProyectoId == proyectoId.Value);

        var porEstado = await query
            .GroupBy(o => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Completados = g.Count(o => o.Estado == "Completado"),
                Pendientes = g.Count(o => o.Estado == "Pendiente"),
                EnProceso = g.Count(o => o.Estado == "En Proceso")
            })
            .FirstOrDefaultAsync();

        return new RevisionObservacionStatsDTO
        {
            Reportados = porEstado?.Total ?? 0,
            Completados = porEstado?.Completados ?? 0,
            Pendientes = porEstado?.Pendientes ?? 0,
            EnProceso = porEstado?.EnProceso ?? 0
        };
    }

    public async Task<AcRevisionObservacion> CreateObservacion(CreateRevisionObservacionDTO body)
    {
        using var ctx = _factory.CreateDbContext();
        var entity = new AcRevisionObservacion
        {
            RevisionId = body.RevisionId,
            Fecha = body.Fecha,
            PersonaReporta = body.PersonaReporta,
            ZonaAmbiente = body.ZonaAmbiente,
            PartidaReportada = body.PartidaReportada,
            Descripcion = body.Descripcion,
            PlazoLevantamiento = body.PlazoLevantamiento,
            CreadoPor = body.CreadoPor,
            Estado = "Pendiente",
            Origen = "Nuevo"
        };
        ctx.AcRevisionObservaciones.Add(entity);
        await ctx.SaveChangesAsync();
        return entity;
    }

    public async Task<AcRevisionObservacionFoto> AgregarFoto(int revisionObservacionId, string tipo, string url, int orden)
    {
        using var ctx = _factory.CreateDbContext();
        var foto = new AcRevisionObservacionFoto { RevisionObservacionId = revisionObservacionId, Tipo = tipo, Url = url, Orden = orden };
        ctx.AcRevisionObservacionFotos.Add(foto);
        await ctx.SaveChangesAsync();
        return foto;
    }

    public async Task<AcRevisionObservacionFoto?> GetFotoById(int fotoId)
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.AcRevisionObservacionFotos.Include(f => f.RevisionObservacion).ThenInclude(o => o!.Revision)
            .FirstOrDefaultAsync(f => f.Id == fotoId);
    }

    public async Task ActualizarFoto(int fotoId, string url)
    {
        using var ctx = _factory.CreateDbContext();
        var foto = await ctx.AcRevisionObservacionFotos.FindAsync(fotoId);
        if (foto == null) return;
        foto.Url = url;
        await ctx.SaveChangesAsync();
    }

    public async Task<RevisionObservacionListItemDTO?> LevantarObservacion(int id, int? levantaPorWorkerId)
    {
        using var ctx = _factory.CreateDbContext();
        var entity = await ctx.AcRevisionObservaciones.FindAsync(id);
        if (entity == null) return null;

        entity.Estado = "Completado";
        entity.FechaLevantamiento = DateTime.UtcNow;
        entity.LevantaPorWorkerId = levantaPorWorkerId;
        await ctx.SaveChangesAsync();

        return await GetObservacionById(id);
    }

    public async Task<RevisionObservacionListItemDTO?> UpdateObservacion(int id, UpdateRevisionObservacionDTO body)
    {
        using var ctx = _factory.CreateDbContext();
        var entity = await ctx.AcRevisionObservaciones.FindAsync(id);
        if (entity == null) return null;

        if (body.ZonaAmbiente != null) entity.ZonaAmbiente = body.ZonaAmbiente;
        if (body.Descripcion != null) entity.Descripcion = body.Descripcion;
        if (body.PartidaReportada != null) entity.PartidaReportada = body.PartidaReportada;
        if (body.PersonaReporta != null) entity.PersonaReporta = body.PersonaReporta;
        await ctx.SaveChangesAsync();

        return await GetObservacionById(id);
    }
}
