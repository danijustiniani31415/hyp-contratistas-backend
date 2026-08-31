using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Alerta;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    public class AlertaLoginSsomaService : IAlertaLoginSsomaService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public AlertaLoginSsomaService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<AlertaLoginSsomaResultDto> GetResumen(int userId)
        {
            var vacio = new AlertaLoginSsomaResultDto();
            using var ctx = _factory.CreateDbContext();

            var email = await ctx.User
                .Where(u => u.UserId == userId && u.State)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(email)) return vacio;

            var emailNorm = email.Trim().ToLower();

            // Proyectos donde el usuario logueado es Administrador o Coordinador SSOMA —
            // mismos campos que usa Gestión de Responsables (EmailCoordAdmin/EmailCoordSsoma),
            // no hay tabla de roles por proyecto: la responsabilidad se define por ese correo.
            var proyectos = await ctx.Project
                .Where(p => p.State &&
                    ((p.EmailCoordAdmin != null && p.EmailCoordAdmin.ToLower() == emailNorm) ||
                     (p.EmailCoordSsoma != null && p.EmailCoordSsoma.ToLower() == emailNorm)))
                .Select(p => new { p.ProjectId, p.ProjectDescription })
                .ToListAsync();
            if (proyectos.Count == 0) return vacio;

            var proyectoIds = proyectos.Select(p => p.ProjectId).ToList();
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            // Trabajadores activos + su proyecto/empresa actual (misma resolución que
            // InterconsultaRepository.List: asignación activa en ss_hab_worker_proyecto,
            // si no hay ninguna se cae a la última vinculación activa).
            var asignaciones = await ctx.Worker
                .Where(w => WorkersEstadoIds.NoRetirados.Contains(w.WorkersEstadoId))
                .Select(w => new
                {
                    WorkerId = w.Id,
                    WorkerNombre = w.Person != null ? w.Person.FullName : null,
                    ProyAsignada = ctx.WorkerProyecto
                        .Where(wp => wp.WorkerId == w.Id && wp.FechaFin == null)
                        .OrderByDescending(wp => wp.FechaInicio).ThenByDescending(wp => wp.Id)
                        .FirstOrDefault(),
                    VincActiva = ctx.WorkerVinculacion
                        .Where(v => v.WorkerId == w.Id && v.FechaFin == null)
                        .OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.Id)
                        .FirstOrDefault()
                })
                .ToDictionaryAsync(x => x.WorkerId);

            var empresaIds = asignaciones.Values
                .Select(x => x.ProyAsignada?.EmpresaId ?? x.VincActiva?.EmpresaId)
                .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
            var empresaMap = await ctx.Contributor
                .Where(c => empresaIds.Contains(c.ContributorId))
                .ToDictionaryAsync(c => c.ContributorId, c => c.ContributorName);

            int? ProyectoDe(int workerId) =>
                asignaciones.TryGetValue(workerId, out var a) ? (a.ProyAsignada?.ProyectoId ?? a.VincActiva?.ProyectoId) : null;
            string? RazonSocialDe(int workerId)
            {
                if (!asignaciones.TryGetValue(workerId, out var a)) return null;
                var empresaId = a.ProyAsignada?.EmpresaId ?? a.VincActiva?.EmpresaId;
                return empresaId.HasValue && empresaMap.TryGetValue(empresaId.Value, out var n) ? n : null;
            }

            var resultado = proyectos.Select(p => new AlertaLoginProyectoDto
            {
                ProyectoId = p.ProjectId,
                ProyectoNombre = p.ProjectDescription
            }).ToDictionary(p => p.ProyectoId);

            // Interconsultas pendientes
            var interconsultas = await ctx.SsInterconsulta
                .Where(i => i.Estado == "Pendiente")
                .Select(i => new { i.WorkerId, i.FechaDerivacion })
                .ToListAsync();

            foreach (var ic in interconsultas)
            {
                var proyectoId = ProyectoDe(ic.WorkerId);
                if (!proyectoId.HasValue || !resultado.TryGetValue(proyectoId.Value, out var proy)) continue;
                if (!asignaciones.TryGetValue(ic.WorkerId, out var a)) continue;

                proy.Interconsultas.Add(new AlertaLoginItemDto
                {
                    WorkerNombre = a.WorkerNombre ?? "—",
                    RazonSocial = RazonSocialDe(ic.WorkerId),
                    Dias = hoy.DayNumber - ic.FechaDerivacion.DayNumber
                });
            }

            // EMOs vencidos — solo si el EMO MÁS RECIENTE del trabajador ya venció (mismo
            // criterio que el Dashboard de Salud Ocupacional: un vencimiento viejo tapado por un
            // EMO nuevo vigente no cuenta).
            var emosActivos = await ctx.WorkerEmo
                .Where(e => e.Activo)
                .Select(e => new
                {
                    e.WorkerId,
                    e.FechaEmo,
                    e.Id,
                    Vence = e.FechaVencimientoCalculada ?? e.FechaVencimiento
                })
                .ToListAsync();

            var ultimoEmoPorWorker = emosActivos
                .GroupBy(e => e.WorkerId)
                .Select(g => g.OrderByDescending(e => e.FechaEmo).ThenByDescending(e => e.Id).First())
                .Where(e => e.Vence.HasValue && e.Vence.Value < hoy);

            foreach (var emo in ultimoEmoPorWorker)
            {
                var proyectoId = ProyectoDe(emo.WorkerId);
                if (!proyectoId.HasValue || !resultado.TryGetValue(proyectoId.Value, out var proy)) continue;
                if (!asignaciones.TryGetValue(emo.WorkerId, out var a)) continue;

                proy.EmosVencidos.Add(new AlertaLoginItemDto
                {
                    WorkerNombre = a.WorkerNombre ?? "—",
                    RazonSocial = RazonSocialDe(emo.WorkerId),
                    Dias = hoy.DayNumber - emo.Vence!.Value.DayNumber
                });
            }

            var proyectosConAlertas = resultado.Values
                .Where(p => p.Interconsultas.Count > 0 || p.EmosVencidos.Count > 0)
                .OrderByDescending(p => p.Interconsultas.Count + p.EmosVencidos.Count)
                .ToList();

            return new AlertaLoginSsomaResultDto
            {
                TieneAlertas = proyectosConAlertas.Count > 0,
                Proyectos = proyectosConAlertas
            };
        }
    }
}
