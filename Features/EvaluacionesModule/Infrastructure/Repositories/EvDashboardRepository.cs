using Abril_Backend.Features.Evaluaciones.Application.Dtos;
using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Repositories
{
    public class EvDashboardRepository : IEvDashboardRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public EvDashboardRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<EvAreaPromedioDto>> GetPromediosPorAreaAsync(int periodoId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.EvEvaluacionesResidente
                .Where(e => e.PeriodoId == periodoId && !e.NoAplica && e.Nota.HasValue)
                .GroupBy(e => e.AreaNombre)
                .Select(g => new EvAreaPromedioDto
                {
                    AreaNombre = g.Key,
                    Promedio = Math.Round(g.Average(e => e.Nota!.Value), 1),
                    TotalEvaluaciones = g.Count()
                })
                .OrderByDescending(a => a.Promedio)
                .ToListAsync();
        }

        public async Task<List<EvTendenciaDto>> GetTendenciaAsync()
        {
            using var ctx = _factory.CreateDbContext();
            var raw = await ctx.EvEvaluacionesResidente
                .Where(e => !e.NoAplica && e.Nota.HasValue)
                .Include(e => e.Periodo)
                .ToListAsync();

            var userIds = raw.Select(e => e.EvaluadoUserId).Distinct().ToList();
            var persons = await ctx.Person
                .Where(p => p.UserId.HasValue && userIds.Contains(p.UserId.Value))
                .ToDictionaryAsync(p => p.UserId!.Value, p => p.FullName ?? "");

            return raw
                .GroupBy(e => new { e.Periodo!.Mes, e.Periodo.Anio, e.EvaluadoUserId })
                .Select(g => new EvTendenciaDto
                {
                    Mes = g.Key.Mes,
                    Anio = g.Key.Anio,
                    NombreMes = new DateTime(g.Key.Anio, g.Key.Mes, 1)
                        .ToString("MMM", new System.Globalization.CultureInfo("es-PE")),
                    UserId = g.Key.EvaluadoUserId,
                    Nombre = persons.GetValueOrDefault(g.Key.EvaluadoUserId, ""),
                    Promedio = Math.Round(g.Average(e => e.Nota!.Value), 1)
                })
                .OrderBy(t => t.Anio)
                .ThenBy(t => t.Mes)
                .ToList();
        }

        public async Task<List<EvPendienteDto>> GetPendientesAsync(int periodoId)
        {
            using var ctx = _factory.CreateDbContext();

            var completadas = await ctx.EvEvaluacionesResidente
                .Where(e => e.PeriodoId == periodoId)
                .GroupBy(e => e.EvaluadorUserId)
                .Select(g => new { UserId = g.Key, Areas = g.Select(e => e.AreaNombre).ToList() })
                .ToListAsync();

            // Trae todos los usuarios con proyecto asignado (evaluadores potenciales)
            var evaluadores = await (
                from up in ctx.UserProject
                join u in ctx.User on up.UserId equals u.UserId
                join p in ctx.Person on u.UserId equals p.UserId
                select new { UserId = u.UserId, u.Email, Nombre = p.FullName ?? "" }
            ).Distinct().ToListAsync();

            return evaluadores
                .Select(e =>
                {
                    var comp = completadas.FirstOrDefault(c => c.UserId == e.UserId);
                    return new EvPendienteDto
                    {
                        UserId = e.UserId,
                        Nombre = e.Nombre,
                        Email = e.Email ?? "",
                        AreasCompletadas = comp?.Areas ?? [],
                        AreasPendientes = comp == null ? ["Sin evaluaciones"] : []
                    };
                })
                .Where(e => e.AreasPendientes.Count != 0)
                .ToList();
        }

        public async Task<List<EvResidenteResumenDto>> GetResidentesResumenAsync(int periodoId)
        {
            using var ctx = _factory.CreateDbContext();

            var evals = await ctx.EvEvaluacionesResidente
                .Where(e => e.PeriodoId == periodoId && !e.NoAplica && e.Nota.HasValue)
                .Include(e => e.Project)
                .Include(e => e.Detalles)
                .ToListAsync();

            var userIds = evals.Select(e => e.EvaluadoUserId).Distinct().ToList();
            var persons = await ctx.Person
                .Where(p => p.UserId.HasValue && userIds.Contains(p.UserId.Value))
                .ToDictionaryAsync(p => p.UserId!.Value, p => p.FullName ?? "");

            var periodoActual = await ctx.EvPeriodos
                .FirstAsync(p => p.Id == periodoId);

            var periodoAnterior = await ctx.EvPeriodos
                .Where(p => (p.Anio == periodoActual.Anio && p.Mes < periodoActual.Mes)
                         || p.Anio < periodoActual.Anio)
                .OrderByDescending(p => p.Anio)
                .ThenByDescending(p => p.Mes)
                .FirstOrDefaultAsync();

            var evalsAnt = periodoAnterior != null
                ? await ctx.EvEvaluacionesResidente
                    .Where(e => e.PeriodoId == periodoAnterior.Id && e.Nota.HasValue)
                    .Select(e => new { e.EvaluadoUserId, e.Nota })
                    .ToListAsync()
                : [];

            var evaluadorIds = evals
                .Select(e => e.EvaluadorUserId ?? 0)
                .Where(id => id != 0)
                .Distinct()
                .ToList();
            var evaluadores = await ctx.Person
                .Where(p => p.UserId.HasValue && evaluadorIds.Contains(p.UserId.Value))
                .ToDictionaryAsync(p => p.UserId!.Value, p => p.FullName ?? "");

            return evals
                .GroupBy(e => e.EvaluadoUserId)
                .Select(g => new EvResidenteResumenDto
                {
                    UserId = g.Key,
                    Nombre = persons.GetValueOrDefault(g.Key, ""),
                    ProjectId = g.First().ProjectId,
                    ProjectNombre = g.First().Project?.ProjectDescription,
                    PromedioGeneral = Math.Round(g.Average(e => e.Nota!.Value), 1),
                    PromedioMesAnterior = evalsAnt.Any(a => a.EvaluadoUserId == g.Key)
                        ? Math.Round(evalsAnt
                            .Where(a => a.EvaluadoUserId == g.Key)
                            .Average(a => a.Nota!.Value), 1)
                        : null,
                    PromediosPorArea = g
                        .GroupBy(e => e.AreaNombre)
                        .Select(ag => new EvAreaPromedioDto
                        {
                            AreaNombre = ag.Key,
                            Promedio = Math.Round(ag.Average(e => e.Nota!.Value), 1),
                            TotalEvaluaciones = ag.Count()
                        })
                        .OrderByDescending(a => a.Promedio)
                        .ToList(),
                    Evaluaciones = g.Select(e => new EvEvaluacionResidenteResponseDto
                    {
                        Id = e.Id,
                        AreaNombre = e.AreaNombre,
                        Nota = e.Nota,
                        Comentario = e.Comentario,
                        NoAplica = e.NoAplica,
                        EvaluadorUserId = e.EvaluadorUserId ?? 0,
                        EvaluadorNombre = e.EvaluadorUserId.HasValue
                            ? evaluadores.GetValueOrDefault(e.EvaluadorUserId.Value, "")
                            : "",
                        CreatedAt = e.CreatedAt,
                        Detalles = e.Detalles.Select(d => new EvDetalleResponseDto
                        {
                            Id = d.Id,
                            Criterio = d.Criterio,
                            Puntaje = d.Puntaje,
                            EsNa = d.EsNa
                        }).ToList()
                    }).ToList()
                })
                .OrderByDescending(r => r.PromedioGeneral)
                .ToList();
        }

        public async Task<EvDashboardGerenciaDto> GetDashboardGerenciaAsync(int periodoId)
        {
            var residentes = await GetResidentesResumenAsync(periodoId);
            var areas = await GetPromediosPorAreaAsync(periodoId);
            var tendencia = await GetTendenciaAsync();

            using var ctx = _factory.CreateDbContext();

            var comentarios = await ctx.EvEvaluacionesResidente
                .Where(e => e.PeriodoId == periodoId && !string.IsNullOrEmpty(e.Comentario))
                .Include(e => e.Project)
                .OrderByDescending(e => e.CreatedAt)
                .Take(10)
                .ToListAsync();

            var evaluadorIds = comentarios
                .Select(e => e.EvaluadorUserId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();
            var persons = await ctx.Person
                .Where(p => p.UserId.HasValue && evaluadorIds.Contains(p.UserId.Value))
                .ToDictionaryAsync(p => p.UserId!.Value, p => p.FullName ?? "");

            var ultimosComentarios = comentarios.Select(e => new EvEvaluacionResidenteResponseDto
            {
                Id = e.Id,
                PeriodoId = e.PeriodoId,
                EvaluadorUserId = e.EvaluadorUserId ?? 0,
                EvaluadorNombre = e.EvaluadorUserId.HasValue ? persons.GetValueOrDefault(e.EvaluadorUserId.Value, "") : "",
                EvaluadoUserId = e.EvaluadoUserId,
                ProjectId = e.ProjectId,
                ProjectNombre = e.Project?.ProjectDescription,
                AreaNombre = e.AreaNombre,
                Nota = e.Nota,
                Comentario = e.Comentario,
                CreatedAt = e.CreatedAt
            }).ToList();

            return new EvDashboardGerenciaDto
            {
                PromedioGeneral = residentes.Count != 0
                    ? Math.Round(residentes.Average(r => r.PromedioGeneral ?? 0), 1)
                    : null,
                TotalResidentes = residentes.Select(r => r.UserId).Distinct().Count(),
                EvaluacionesCompletadas = await ctx.EvEvaluacionesResidente.CountAsync(e => e.PeriodoId == periodoId),
                EvaluacionesEsperadas = residentes.Count * 8,
                BajoRendimiento = residentes.Count(r => r.PromedioGeneral < 12),
                Residentes = residentes,
                PromediosPorArea = areas,
                UltimosComentarios = ultimosComentarios,
                Tendencia = tendencia
            };
        }
    }
}
