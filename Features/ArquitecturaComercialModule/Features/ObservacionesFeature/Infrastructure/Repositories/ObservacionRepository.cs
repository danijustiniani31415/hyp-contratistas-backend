using Abril_Backend.Shared.Constants;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Dtos;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Infrastructure.Repositories;

public class ObservacionRepository : IObservacionRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public ObservacionRepository(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<ObservacionListResponseDTO> GetObservaciones(int? proyectoId, string? estado, string? partida, DateTime? desde, DateTime? hasta, string? search, int pagina, int porPagina)
    {
        using var ctx = _factory.CreateDbContext();
        var query = ctx.AcObservaciones.Include(o => o.Proyecto).Include(o => o.Fotos)
            .Include(o => o.LevantaPor).AsQueryable();

        if (proyectoId.HasValue) query = query.Where(o => o.ProyectoId == proyectoId.Value);
        if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(o => o.Estado == estado);
        if (!string.IsNullOrWhiteSpace(partida)) query = query.Where(o => o.PartidaReportada == partida);
        if (desde.HasValue) query = query.Where(o => o.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(o => o.Fecha <= hasta.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(o => o.Codigo.Contains(search) || o.Descripcion.Contains(search) || (o.PersonaReporta != null && o.PersonaReporta.Contains(search)));

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.Fecha)
            .Skip((pagina - 1) * porPagina)
            .Take(porPagina)
            .Select(o => new ObservacionListItemDTO
            {
                Id = o.Id,
                ProyectoId = o.ProyectoId,
                ProyectoNombre = o.Proyecto != null ? o.Proyecto.ProjectDescription : null,
                Codigo = o.Codigo,
                Fecha = o.Fecha,
                PersonaReporta = o.PersonaReporta,
                EmpresaReporta = o.EmpresaReporta,
                Lugar = o.Lugar,
                Descripcion = o.Descripcion,
                PlazoLevantamiento = o.PlazoLevantamiento,
                PartidaReportada = o.PartidaReportada,
                Estado = o.Estado,
                TipoObservacion = o.TipoObservacion,
                AreaResponsable = o.AreaResponsable,
                Ejecutor = o.Ejecutor,
                Origen = o.Origen,
                LevantaPorWorkerId = o.LevantaPorWorkerId,
                LevantaPorNombre = o.LevantaPor != null ? o.LevantaPor.ApellidoNombre : null,
                Fotos = o.Fotos.Select(f => new ObservacionFotoDTO { Id = f.Id, Tipo = f.Tipo, Url = f.Url, Orden = f.Orden }).ToList()
            })
            .ToListAsync();

        return new ObservacionListResponseDTO { Total = total, Pagina = pagina, PorPagina = porPagina, Items = items };
    }

    public async Task<ObservacionListItemDTO?> GetObservacionById(int id)
    {
        using var ctx = _factory.CreateDbContext();
        var o = await ctx.AcObservaciones.Include(x => x.Proyecto).Include(x => x.Fotos).Include(x => x.LevantaPor).FirstOrDefaultAsync(x => x.Id == id);
        if (o == null) return null;

        return new ObservacionListItemDTO
        {
            Id = o.Id,
            ProyectoId = o.ProyectoId,
            ProyectoNombre = o.Proyecto?.ProjectDescription,
            Codigo = o.Codigo,
            Fecha = o.Fecha,
            PersonaReporta = o.PersonaReporta,
            EmpresaReporta = o.EmpresaReporta,
            Lugar = o.Lugar,
            Descripcion = o.Descripcion,
            PlazoLevantamiento = o.PlazoLevantamiento,
            PartidaReportada = o.PartidaReportada,
            Estado = o.Estado,
            TipoObservacion = o.TipoObservacion,
            AreaResponsable = o.AreaResponsable,
            Ejecutor = o.Ejecutor,
            Origen = o.Origen,
            LevantaPorWorkerId = o.LevantaPorWorkerId,
            LevantaPorNombre = o.LevantaPor?.ApellidoNombre,
            Fotos = o.Fotos.Select(f => new ObservacionFotoDTO { Id = f.Id, Tipo = f.Tipo, Url = f.Url, Orden = f.Orden }).ToList()
        };
    }

    public async Task<ObservacionFiltrosDTO> GetFiltros()
    {
        using var ctx = _factory.CreateDbContext();

        // Antes se derivaba de observaciones ya existentes (un proyecto sin observaciones
        // previas nunca aparecía). Ahora sale del flag TieneArquitecturaComercial en Project,
        // que el usuario controla desde el ícono de activar/desactivar en la lista.
        var proyectos = await ctx.Project
            .Where(p => p.TieneArquitecturaComercial && p.State && p.Active && !ctx.ProyectoFiltro.Any(f => f.ProjectId == p.ProjectId && f.FuncionalidadId == ProyectoFiltroFuncionalidades.AcObservaciones && !f.Active))
            .Select(p => new ProyectoFiltroDTO { Id = p.ProjectId, Nombre = p.ProjectDescription })
            .OrderBy(p => p.Nombre)
            .ToListAsync();

        var partidas = await ctx.AcObservaciones
            .Where(o => o.PartidaReportada != null)
            .Select(o => o.PartidaReportada!)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync();

        var estados = await ctx.AcObservaciones
            .Select(o => o.Estado)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync();

        return new ObservacionFiltrosDTO { Proyectos = proyectos, Partidas = partidas, Estados = estados };
    }

    public async Task<ObservacionDashboardDTO> GetDashboard(DateTime? desde, DateTime? hasta, int? proyectoId)
    {
        using var ctx = _factory.CreateDbContext();
        var query = ctx.AcObservaciones.Include(o => o.Proyecto).AsQueryable();

        if (desde.HasValue) query = query.Where(o => o.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(o => o.Fecha <= hasta.Value);
        if (proyectoId.HasValue) query = query.Where(o => o.ProyectoId == proyectoId.Value);

        var registros = await query.ToListAsync();

        var supervisores = registros
            .GroupBy(o => new { Persona = o.PersonaReporta ?? "Sin asignar", Proyecto = o.Proyecto?.ProjectDescription })
            .Select(g => new ObservacionDashboardSupervisorDTO
            {
                PersonaReporta = g.Key.Persona,
                ProyectoNombre = g.Key.Proyecto,
                TotalReportadas = g.Count(),
                TotalCompletadas = g.Count(o => o.Estado == "Completado"),
                TotalPendientes = g.Count(o => o.Estado == "Pendiente"),
                TotalEnProceso = g.Count(o => o.Estado == "En Proceso"),
                PctAvance = g.Count() == 0 ? 0 : Math.Round(g.Count(o => o.Estado == "Completado") * 100m / g.Count(), 1),
                PorPartida = g.GroupBy(o => o.PartidaReportada ?? "Otros")
                    .Select(pg => new ObservacionPorPartidaDTO
                    {
                        Partida = pg.Key,
                        Completado = pg.Count(o => o.Estado == "Completado"),
                        Pendiente = pg.Count(o => o.Estado != "Completado")
                    }).ToList()
            })
            .OrderByDescending(s => s.TotalReportadas)
            .ToList();

        return new ObservacionDashboardDTO { Supervisores = supervisores };
    }

    /// <summary>
    /// Los 4 totales de las cards, calculados con COUNT en SQL (sin traer filas a memoria).
    /// La Lista llama a este endpoint en vez de GetDashboard: no necesita el desglose por
    /// supervisor/partida, solo estos 4 números.
    /// </summary>
    public async Task<ObservacionStatsDTO> GetStats(DateTime? desde, DateTime? hasta, int? proyectoId)
    {
        using var ctx = _factory.CreateDbContext();
        var query = ctx.AcObservaciones.AsQueryable();

        if (desde.HasValue) query = query.Where(o => o.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(o => o.Fecha <= hasta.Value);
        if (proyectoId.HasValue) query = query.Where(o => o.ProyectoId == proyectoId.Value);

        // Un solo round-trip con agregación condicional en vez de 4 CountAsync()
        // secuenciales (antes cada card esperaba a la anterior).
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

        return new ObservacionStatsDTO
        {
            Reportados = porEstado?.Total ?? 0,
            Completados = porEstado?.Completados ?? 0,
            Pendientes = porEstado?.Pendientes ?? 0,
            EnProceso = porEstado?.EnProceso ?? 0
        };
    }

    public async Task<int> GetProximoCorrelativo(string prefijoProyecto, int anio)
    {
        using var ctx = _factory.CreateDbContext();
        var sufijo = $"-{anio}";
        var existentes = await ctx.AcObservaciones
            .Where(o => o.Codigo.StartsWith(prefijoProyecto + "-") && o.Codigo.EndsWith(sufijo))
            .Select(o => o.Codigo)
            .ToListAsync();

        var max = 0;
        foreach (var c in existentes)
        {
            var partes = c.Split('-');
            if (partes.Length == 3 && int.TryParse(partes[1], out var n) && n > max) max = n;
        }
        return max + 1;
    }

    public async Task<string> GetProyectoAbbreviation(int proyectoId)
    {
        using var ctx = _factory.CreateDbContext();
        var proyecto = await ctx.Set<Abril_Backend.Shared.Models.Project>().FirstOrDefaultAsync(p => p.ProjectId == proyectoId);
        return proyecto?.Abbreviation ?? proyecto?.Codigo ?? "OBS";
    }

    public async Task<AcObservacion> CreateObservacion(CreateObservacionDTO body, string codigo)
    {
        using var ctx = _factory.CreateDbContext();
        var entity = new AcObservacion
        {
            ProyectoId = body.ProyectoId,
            Codigo = codigo,
            Fecha = DateTime.SpecifyKind(body.Fecha, DateTimeKind.Utc),
            PersonaReporta = body.PersonaReporta,
            EmpresaReporta = body.EmpresaReporta,
            Lugar = body.Lugar,
            Descripcion = body.Descripcion,
            PlazoLevantamiento = body.PlazoLevantamiento.HasValue
                ? DateTime.SpecifyKind(body.PlazoLevantamiento.Value, DateTimeKind.Utc)
                : (DateTime?)null,
            PartidaReportada = body.PartidaReportada,
            TipoObservacion = body.TipoObservacion,
            AreaResponsable = body.AreaResponsable,
            Ejecutor = body.Ejecutor,
            CreadoPor = body.CreadoPor,
            Estado = "Pendiente",
            Origen = "Nuevo",
            CreatedAt = DateTime.UtcNow
        };

        ctx.AcObservaciones.Add(entity);
        await ctx.SaveChangesAsync();
        return entity;
    }

    public async Task<AcObservacionFoto> AgregarFoto(int observacionId, string tipo, string url, int orden)
    {
        using var ctx = _factory.CreateDbContext();
        var foto = new AcObservacionFoto { ObservacionId = observacionId, Tipo = tipo, Url = url, Orden = orden, CreatedAt = DateTime.UtcNow };
        ctx.AcObservacionFotos.Add(foto);
        await ctx.SaveChangesAsync();
        return foto;
    }

    public async Task<AcObservacionFoto?> GetFotoById(int fotoId)
    {
        using var ctx = _factory.CreateDbContext();
        return await ctx.AcObservacionFotos.Include(f => f.Observacion).FirstOrDefaultAsync(f => f.Id == fotoId);
    }

    public async Task ActualizarFoto(int fotoId, string url)
    {
        using var ctx = _factory.CreateDbContext();
        var foto = await ctx.AcObservacionFotos.FirstOrDefaultAsync(f => f.Id == fotoId);
        if (foto == null) return;
        foto.Url = url;
        await ctx.SaveChangesAsync();
    }

    public async Task<ObservacionListItemDTO?> LevantarObservacion(int id, int? levantaPorWorkerId)
    {
        using var ctx = _factory.CreateDbContext();
        var o = await ctx.AcObservaciones.FirstOrDefaultAsync(x => x.Id == id);
        if (o == null) return null;

        o.Estado = "Completado";
        o.FechaLevantamiento = DateTime.UtcNow;
        if (levantaPorWorkerId.HasValue) o.LevantaPorWorkerId = levantaPorWorkerId.Value;
        await ctx.SaveChangesAsync();

        return await GetObservacionById(id);
    }

    public async Task<ObservacionListItemDTO?> UpdateObservacion(int id, UpdateObservacionDTO body)
    {
        using var ctx = _factory.CreateDbContext();
        var o = await ctx.AcObservaciones.FirstOrDefaultAsync(x => x.Id == id);
        if (o == null) return null;

        // null = no tocar; string vacío si se quiere vaciar el campo (mismo criterio que UpdateEmails de Project).
        if (body.Lugar != null) o.Lugar = string.IsNullOrWhiteSpace(body.Lugar) ? null : body.Lugar.Trim();
        if (body.Descripcion != null && !string.IsNullOrWhiteSpace(body.Descripcion)) o.Descripcion = body.Descripcion.Trim();
        if (body.PartidaReportada != null) o.PartidaReportada = string.IsNullOrWhiteSpace(body.PartidaReportada) ? null : body.PartidaReportada.Trim();
        if (body.AreaResponsable != null) o.AreaResponsable = string.IsNullOrWhiteSpace(body.AreaResponsable) ? null : body.AreaResponsable.Trim();
        if (body.PersonaReporta != null) o.PersonaReporta = string.IsNullOrWhiteSpace(body.PersonaReporta) ? null : body.PersonaReporta.Trim();

        await ctx.SaveChangesAsync();

        return await GetObservacionById(id);
    }
}
