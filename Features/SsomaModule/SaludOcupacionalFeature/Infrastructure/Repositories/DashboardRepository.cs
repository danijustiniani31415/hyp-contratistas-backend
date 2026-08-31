using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Dashboard;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private static readonly string[] AbrilCategorias = { "Casa", "Staff" };
        private const string ContratistaCategoria = "Contratista";

        private readonly IDbContextFactory<AppDbContext> _factory;

        public DashboardRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<DashboardSaludOcupacionalDto> GetDashboard()
        {
            using var ctx = _factory.CreateDbContext();
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var hoy7 = hoy.AddDays(7);
            var hoy15 = hoy.AddDays(15);
            var hoy30 = hoy.AddDays(30);

            var workerIdsAbril = await ctx.WorkerVinculacion
                .Where(v => v.FechaFin == null)
                .Join(ctx.Contributor.Where(c => c.EsAbril),
                      v => v.EmpresaId, c => c.ContributorId, (v, c) => v.WorkerId)
                .Distinct()
                .ToListAsync();

            var totalTrabajadores = await ctx.Worker
                .CountAsync(w => workerIdsAbril.Contains(w.Id));
            var totalAbril = await ctx.Worker
                .CountAsync(w => workerIdsAbril.Contains(w.Id)
                              && w.ContrataCasa != null && AbrilCategorias.Contains(w.ContrataCasa));
            var totalContratistas = await ctx.Worker
                .CountAsync(w => workerIdsAbril.Contains(w.Id)
                              && w.ContrataCasa == ContratistaCategoria);

            // Load all active EMOs for Abril workers into memory once.
            // Avoids complex correlated subquery translation issues with EF Core + Npgsql.
            var todosEmos = await ctx.WorkerEmo
                .Where(e => e.Activo && workerIdsAbril.Contains(e.WorkerId))
                .Select(e => new
                {
                    e.Id, e.WorkerId, e.Aptitud, e.FechaEmo,
                    Vence = e.FechaVencimientoCalculada ?? e.FechaVencimiento
                })
                .ToListAsync();

            // Latest EMO per worker (fecha desc, id desc as tiebreaker)
            var ultimosEmos = todosEmos
                .GroupBy(e => e.WorkerId)
                .Select(g => g.OrderByDescending(e => e.FechaEmo).ThenByDescending(e => e.Id).First())
                .ToList();

            int GetCount(string aptitud) => ultimosEmos.Count(e => e.Aptitud == aptitud);
            var workersConEmo = ultimosEmos.Count;

            var aptitud = new AptitudResumenDto
            {
                Apto = GetCount("Apto"),
                AptoConRestricciones = GetCount("Apto con Restricciones"),
                NoApto = GetCount("No Apto"),
                Observado = GetCount("Observado"),
                SinEmo = Math.Max(0, totalTrabajadores - workersConEmo)
            };

            var emosVencidos = todosEmos.Count(e => e.Vence != null && e.Vence < hoy);

            var vencer = new VencimientoResumenDto
            {
                Dias30 = todosEmos.Count(e => e.Vence != null && e.Vence >= hoy && e.Vence <= hoy30),
                Dias15 = todosEmos.Count(e => e.Vence != null && e.Vence >= hoy && e.Vence <= hoy15),
                Dias7  = todosEmos.Count(e => e.Vence != null && e.Vence >= hoy && e.Vence <= hoy7)
            };

            var interconsultasPendientes = await ctx.SsInterconsulta
                .CountAsync(i => i.Estado == "Pendiente" && workerIdsAbril.Contains(i.WorkerId));

            var programacionesSemana = await ctx.SsProgramacionEmo
                .CountAsync(p => p.State
                              && p.FechaProgramada >= hoy && p.FechaProgramada <= hoy7
                              && p.Estado != "Cancelado");

            var trabajadoresInhabilitados = await ctx.Worker
                .CountAsync(w => workerIdsAbril.Contains(w.Id) && w.HabilitadoObra == false);

            // Get top-10 proximos a vencer: look up worker names from the EMO ids already loaded
            var proximosWorkerIds = todosEmos
                .Where(e => e.Vence != null && e.Vence >= hoy)
                .OrderBy(e => e.Vence)
                .Select(e => e.WorkerId)
                .Distinct()
                .Take(10)
                .ToList();

            var proximosVenceFechas = todosEmos
                .Where(e => e.Vence != null && e.Vence >= hoy && proximosWorkerIds.Contains(e.WorkerId))
                .GroupBy(e => e.WorkerId)
                .ToDictionary(g => g.Key, g => g.Min(e => e.Vence)!.Value);

            var proximosWorkers = await (
                from w in ctx.Worker
                join per in ctx.Person on w.PersonId equals per.PersonId into perj
                from per in perj.DefaultIfEmpty()
                join v in ctx.WorkerVinculacion.Where(x => x.FechaFin == null) on w.Id equals v.WorkerId into vj
                from v in vj.DefaultIfEmpty()
                join em in ctx.Contributor on v.EmpresaId equals em.ContributorId into ej
                from em in ej.DefaultIfEmpty()
                where proximosWorkerIds.Contains(w.Id)
                select new
                {
                    w.Id,
                    Nombre = per != null ? per.FullName : string.Empty,
                    Dni = per != null ? per.DocumentIdentityCode : string.Empty,
                    Empresa = em != null ? (em.ContributorName ?? string.Empty) : string.Empty
                })
                .ToListAsync();

            var proximos = proximosWorkers
                .Where(w => proximosVenceFechas.ContainsKey(w.Id))
                .Select(w => new { w.Id, w.Nombre, w.Dni, FechaVencimiento = proximosVenceFechas[w.Id], w.Empresa })
                .OrderBy(w => w.FechaVencimiento)
                .ToList();

            var proximosDto = proximos.Select(p => new ProximoVencerDto
            {
                WorkerId = p.Id,
                Nombre = p.Nombre,
                Dni = p.Dni,
                FechaVencimiento = p.FechaVencimiento,
                DiasParaVencer = p.FechaVencimiento.DayNumber - hoy.DayNumber,
                Empresa = p.Empresa
            }).ToList();

            return new DashboardSaludOcupacionalDto
            {
                TotalTrabajadores = totalTrabajadores,
                TotalAbril = totalAbril,
                TotalContratistas = totalContratistas,
                EmosPorAptitud = aptitud,
                EmosPorVencer = vencer,
                EmosVencidos = emosVencidos,
                InterconsultasPendientes = interconsultasPendientes,
                ProgramacionesSemana = programacionesSemana,
                TrabajadoresInhabilitados = trabajadoresInhabilitados,
                ProximosVencer = proximosDto
            };
        }
    }
}
