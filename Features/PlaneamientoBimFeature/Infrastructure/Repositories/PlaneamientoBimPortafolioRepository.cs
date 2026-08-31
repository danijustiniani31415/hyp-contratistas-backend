using Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos;
using Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Repositories
{
    public class PlaneamientoBimPortafolioRepository : IPlaneamientoBimPortafolioRepository
    {
        private static readonly TimeZoneInfo LimaZone = TimeZoneInfo.FindSystemTimeZoneById("America/Lima");

        private readonly IDbContextFactory<AppDbContext> _factory;

        public PlaneamientoBimPortafolioRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        private static DateOnly HoyLima()
            => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, LimaZone));

        /// <summary>Alcance del portafolio: proyectos con al menos una zona configurada en Planeamiento BIM.</summary>
        private static Task<List<int>> GetProyectosEnAlcance(AppDbContext ctx)
            => ctx.BimProyectoZona.Select(z => z.ProjectId).Distinct().ToListAsync();

        public async Task<PortafolioKpisDto> GetKpis()
        {
            using var ctx = _factory.CreateDbContext();

            var proyectosScope = await GetProyectosEnAlcance(ctx);
            var hoy = HoyLima();

            // KPI 1: PPC promedio del portafolio, últimos 7 días.
            var desdeSemana = hoy.AddDays(-6);
            var sumaPorcentajeSemana = await ctx.BimRegistroDiario
                .Where(r => proyectosScope.Contains(r.ProjectId) && r.Fecha >= desdeSemana && r.Fecha <= hoy)
                .Select(r => (decimal?)r.PorcentajeAvance)
                .SumAsync() ?? 0m;
            var totalSemana = await ctx.BimRegistroDiario
                .CountAsync(r => proyectosScope.Contains(r.ProjectId) && r.Fecha >= desdeSemana && r.Fecha <= hoy);
            var ppcPromedio = PorcentajeDe(sumaPorcentajeSemana, totalSemana);

            // KPI 2: cantidad de proyectos por fase actual (fase sin fecha_fin_real con fecha_inicio más reciente).
            var fases = await ctx.BimProyectoFase
                .Where(f => proyectosScope.Contains(f.ProjectId))
                .Select(f => new { f.ProjectId, f.FaseId, FaseNombre = f.Fase.Nombre, f.FechaInicio, f.FechaFinReal })
                .ToListAsync();

            var proyectosPorFase = proyectosScope
                .Select(pid =>
                {
                    var actual = fases
                        .Where(f => f.ProjectId == pid && f.FechaFinReal == null && f.FechaInicio != null)
                        .OrderByDescending(f => f.FechaInicio)
                        .FirstOrDefault();
                    return actual != null
                        ? (FaseId: actual.FaseId, FaseNombre: actual.FaseNombre)
                        : (FaseId: 0, FaseNombre: "Sin fase iniciada");
                })
                .GroupBy(f => new { f.FaseId, f.FaseNombre })
                .Select(g => new ProyectosPorFaseDto
                {
                    FaseId = g.Key.FaseId,
                    FaseNombre = g.Key.FaseNombre,
                    CantidadProyectos = g.Count(),
                })
                .OrderBy(x => x.FaseId == 0 ? int.MaxValue : x.FaseId)
                .ToList();

            // KPI 3: proyectos con restricciones abiertas hace más de 3 días.
            var limiteRestriccion = DateTimeOffset.UtcNow.AddDays(-3);
            var proyectosConRestriccionesVencidas = await ctx.BimRestriccion
                .Where(b => proyectosScope.Contains(b.ProjectId) && b.FechaCierre == null && b.FechaCreacion < limiteRestriccion)
                .Select(b => b.ProjectId)
                .Distinct()
                .CountAsync();

            // KPI 4: top causas de incumplimiento del mes calendario en curso, cross-proyecto.
            var inicioMes = new DateOnly(hoy.Year, hoy.Month, 1);
            var causasMes = await ctx.BimRegistroDiario
                .Where(r => proyectosScope.Contains(r.ProjectId) && r.PorcentajeAvance < 100 && r.CausaId != null
                    && r.Fecha >= inicioMes && r.Fecha <= hoy)
                .GroupBy(r => new { r.CausaId, CausaNombre = r.Causa!.Nombre })
                .Select(g => new { g.Key.CausaId, g.Key.CausaNombre, Cantidad = g.Count() })
                .OrderByDescending(c => c.Cantidad)
                .ToListAsync();
            var totalCausasMes = causasMes.Sum(c => c.Cantidad);

            return new PortafolioKpisDto
            {
                PpcPromedioUltimaSemana = ppcPromedio,
                ProyectosPorFase = proyectosPorFase,
                ProyectosConRestriccionesVencidas = proyectosConRestriccionesVencidas,
                CausasTopMes = new CausasParetoDto
                {
                    TotalNoCumplidas = totalCausasMes,
                    Causas = causasMes.Select(c => new CausaParetoDto
                    {
                        CausaId = c.CausaId!.Value,
                        CausaNombre = c.CausaNombre,
                        Cantidad = c.Cantidad,
                        Porcentaje = PorcentajeDe(c.Cantidad, totalCausasMes),
                    }).ToList(),
                },
            };
        }

        public async Task<List<ProyectoPortafolioDto>> GetProyectos()
        {
            using var ctx = _factory.CreateDbContext();

            var proyectosScope = await GetProyectosEnAlcance(ctx);

            var proyectos = await ctx.Project
                .Where(p => proyectosScope.Contains(p.ProjectId))
                .Select(p => new { p.ProjectId, p.ProjectDescription })
                .ToListAsync();

            var avances = await ctx.BimRegistroDiario
                .Where(r => proyectosScope.Contains(r.ProjectId))
                .GroupBy(r => r.ProjectId)
                .Select(g => new { ProjectId = g.Key, Total = g.Count(), SumaPorcentaje = g.Sum(x => x.PorcentajeAvance) })
                .ToListAsync();

            return proyectos
                .Select(p =>
                {
                    var avance = avances.FirstOrDefault(a => a.ProjectId == p.ProjectId);
                    var total = avance?.Total ?? 0;
                    var sumaPorcentaje = avance?.SumaPorcentaje ?? 0m;
                    decimal? porcentaje = total == 0 ? null : PorcentajeDe(sumaPorcentaje, total);

                    return new ProyectoPortafolioDto
                    {
                        ProjectId = p.ProjectId,
                        ProjectNombre = p.ProjectDescription,
                        TotalRegistros = total,
                        CumplidosRegistros = sumaPorcentaje,
                        PorcentajeAvance = porcentaje,
                        Semaforo = CalcularSemaforo(porcentaje),
                    };
                })
                .OrderBy(p => p.ProjectNombre)
                .ToList();
        }

        public async Task<(string ProjectNombre, string FaseActualNombre)?> GetContextoProyecto(int projectId)
        {
            using var ctx = _factory.CreateDbContext();

            var nombre = await ctx.Project
                .Where(p => p.ProjectId == projectId)
                .Select(p => p.ProjectDescription)
                .FirstOrDefaultAsync();
            if (nombre == null)
                return null;

            var faseActual = await ctx.BimProyectoFase
                .Where(f => f.ProjectId == projectId && f.FechaFinReal == null && f.FechaInicio != null)
                .OrderByDescending(f => f.FechaInicio)
                .Select(f => f.Fase.Nombre)
                .FirstOrDefaultAsync();

            return (nombre, faseActual ?? "Sin fase iniciada");
        }

        /// <summary>parte es una SUMA de porcentajes (0-100 por registro), no un conteo.</summary>
        private static decimal PorcentajeDe(decimal parte, int total)
            => total == 0 ? 0 : Math.Round(parte / total, 2);

        private static string CalcularSemaforo(decimal? porcentaje)
        {
            if (porcentaje == null) return "GRIS";
            if (porcentaje >= 90) return "VERDE";
            if (porcentaje >= 70) return "AMARILLO";
            return "ROJO";
        }
    }
}
