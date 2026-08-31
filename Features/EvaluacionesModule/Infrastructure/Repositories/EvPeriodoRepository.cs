using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Repositories
{
    public class EvPeriodoRepository : IEvPeriodoRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public EvPeriodoRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<EvPeriodo?> GetActivoAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.EvPeriodos.FirstOrDefaultAsync(p => p.Activo);
        }

        public async Task<EvPeriodo?> GetUltimoAsync()
        {
            using var ctx = _factory.CreateDbContext();
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            var mesAnterior = hoy.AddMonths(-1);

            // El resumen siempre muestra el mes calendario anterior al actual (el último que ya
            // cerró por completo) — no el registro con mayor (anio, mes) de la tabla, que puede
            // incluir períodos futuros o de prueba sembrados de antemano (p. ej. para poblar la
            // tendencia histórica de gráficos) y que aún no tienen evaluaciones reales.
            var periodo = await ctx.EvPeriodos
                .FirstOrDefaultAsync(p => p.Mes == mesAnterior.Month && p.Anio == mesAnterior.Year);
            if (periodo != null) return periodo;

            // Si por algún motivo no existe el período del mes anterior, cae al más reciente
            // que ya haya empezado (nunca uno futuro).
            return await ctx.EvPeriodos
                .Where(p => p.Anio < hoy.Year || (p.Anio == hoy.Year && p.Mes <= hoy.Month))
                .OrderByDescending(p => p.Anio)
                .ThenByDescending(p => p.Mes)
                .FirstOrDefaultAsync();
        }

        public async Task<List<EvPeriodo>> GetAllAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.EvPeriodos
                .OrderByDescending(p => p.Anio)
                .ThenByDescending(p => p.Mes)
                .ToListAsync();
        }

        public async Task<EvPeriodo?> GetByIdAsync(int id)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.EvPeriodos.FindAsync(id);
        }

        public async Task<EvPeriodo> CreateAsync(EvPeriodo periodo)
        {
            using var ctx = _factory.CreateDbContext();
            ctx.EvPeriodos.Add(periodo);
            await ctx.SaveChangesAsync();
            return periodo;
        }

        public async Task UpdateAsync(EvPeriodo periodo)
        {
            using var ctx = _factory.CreateDbContext();
            ctx.EvPeriodos.Update(periodo);
            await ctx.SaveChangesAsync();
        }

        public async Task SincronizarVigenciaAsync()
        {
            using var ctx = _factory.CreateDbContext();
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

            // Desactivar cualquier período activo cuya ventana ya cerró.
            var vencidos = await ctx.EvPeriodos.Where(p => p.Activo && p.FechaCierre < hoy).ToListAsync();
            foreach (var v in vencidos) v.Activo = false;
            if (vencidos.Count > 0) await ctx.SaveChangesAsync();

            // Determinar a qué ciclo (apertura día 25 -> cierre día 4 del mes siguiente)
            // pertenece la fecha de hoy. Fuera de esa ventana no hay nada que gestionar.
            int cicloMes, cicloAnio;
            if (hoy.Day >= 25)
            {
                cicloMes = hoy.Month;
                cicloAnio = hoy.Year;
            }
            else if (hoy.Day <= 4)
            {
                var mesAnterior = hoy.AddMonths(-1);
                cicloMes = mesAnterior.Month;
                cicloAnio = mesAnterior.Year;
            }
            else
            {
                return;
            }

            var apertura = new DateOnly(cicloAnio, cicloMes, 25);
            var finMesApertura = apertura.AddMonths(1);
            var cierre = new DateOnly(finMesApertura.Year, finMesApertura.Month, 4);

            var vigente = await ctx.EvPeriodos.FirstOrDefaultAsync(p => p.Mes == cicloMes && p.Anio == cicloAnio);
            if (vigente == null)
            {
                vigente = new EvPeriodo
                {
                    Mes = cicloMes,
                    Anio = cicloAnio,
                    FechaApertura = apertura,
                    FechaCierre = cierre,
                    Activo = true
                };
                ctx.EvPeriodos.Add(vigente);
                await ctx.SaveChangesAsync();
            }
            else if (!vigente.Activo)
            {
                vigente.Activo = true;
                await ctx.SaveChangesAsync();
            }

            // Garantizar que no quede ningún otro período activo por errores manuales previos.
            var otrosActivos = await ctx.EvPeriodos.Where(p => p.Activo && p.Id != vigente.Id).ToListAsync();
            foreach (var o in otrosActivos) o.Activo = false;
            if (otrosActivos.Count > 0) await ctx.SaveChangesAsync();
        }
    }
}
