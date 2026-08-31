using Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Infrastructure.Repositories
{
    public class InduccionProgramacionRepository : IInduccionProgramacionRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public InduccionProgramacionRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        // ── Rotación ──────────────────────────────────────────────────────

        public async Task<List<(int ProyectoId, string Nombre)>> GetProyectosActivosAsync()
        {
            using var ctx = _factory.CreateDbContext();
            var proyectos = await ctx.Project
                .Where(p => p.State)
                .OrderBy(p => p.ProjectDescription)
                .Select(p => new { p.ProjectId, p.ProjectDescription })
                .ToListAsync();

            return proyectos.Select(p => (p.ProjectId, p.ProjectDescription)).ToList();
        }

        public async Task<List<SsInduccionRotacionProyecto>> GetRotacionAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Set<SsInduccionRotacionProyecto>()
                .Include(r => r.Proyecto)
                .OrderBy(r => r.Orden)
                .ToListAsync();
        }

        public async Task<SsInduccionRotacionProyecto> AgregarARotacionAsync(int proyectoId, int? responsableWorkerId)
        {
            using var ctx = _factory.CreateDbContext();
            var maxOrden = await ctx.Set<SsInduccionRotacionProyecto>()
                .Select(r => (int?)r.Orden)
                .MaxAsync() ?? 0;

            var entity = new SsInduccionRotacionProyecto
            {
                ProyectoId = proyectoId,
                ResponsableWorkerId = responsableWorkerId,
                Orden = maxOrden + 1,
                Activo = true,
                CreatedAt = DateTime.UtcNow,
            };
            ctx.Set<SsInduccionRotacionProyecto>().Add(entity);
            await ctx.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> SetResponsableAsync(int id, int? responsableWorkerId)
        {
            using var ctx = _factory.CreateDbContext();
            var entity = await ctx.Set<SsInduccionRotacionProyecto>().FirstOrDefaultAsync(r => r.Id == id);
            if (entity is null) return false;

            entity.ResponsableWorkerId = responsableWorkerId;
            entity.UpdatedAt = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
            return true;
        }

        public async Task<List<ResponsableProyectoDto>> GetResponsablesDisponiblesAsync(int proyectoId)
        {
            using var ctx = _factory.CreateDbContext();

            var vinculos = await ctx.WorkerVinculacion
                .Where(v => v.ProyectoId == proyectoId
                         && v.FechaFin == null
                         && (v.CategoriaId == CategoriaIds.CoordinadorSsoma || v.CategoriaId == CategoriaIds.Prevencionista))
                .Select(v => new { v.WorkerId, v.CategoriaId })
                .Distinct()
                .ToListAsync();

            var resultado = new List<ResponsableProyectoDto>();
            if (vinculos.Count > 0)
            {
                var workerIds = vinculos.Select(v => v.WorkerId).Distinct().ToList();
                var nombres = await GetWorkerNombresInternalAsync(ctx, workerIds);

                resultado.AddRange(vinculos.Select(v => new ResponsableProyectoDto
                {
                    WorkerId = v.WorkerId,
                    Nombre = nombres.TryGetValue(v.WorkerId, out var n) ? n : $"Trabajador {v.WorkerId}",
                    Rol = v.CategoriaId == CategoriaIds.CoordinadorSsoma ? "Coordinador SSOMA" : "Prevencionista",
                }));
            }

            // El Jefe SSOMA es un rol de acceso corporativo (no una vinculación por proyecto,
            // a diferencia de Coordinador/Prevencionista) — solo aplica como candidato para
            // "Oficina Central", el único "proyecto" que en realidad representa a la propia
            // gerencia de SSOMA.
            var esOficinaCentral = await ctx.Project
                .Where(p => p.ProjectId == proyectoId)
                .Select(p => p.ProjectDescription)
                .FirstOrDefaultAsync();

            if (string.Equals(esOficinaCentral?.Trim(), "Oficina Central", StringComparison.OrdinalIgnoreCase))
            {
                var jefeSsomaRoleId = int.Parse(Roles.AdministradorSsoma);
                var jefeUserIds = await ctx.UserRole
                    .Where(ur => ur.RoleId == jefeSsomaRoleId && ur.Active && ur.State)
                    .Select(ur => ur.UserId)
                    .ToListAsync();

                var jefes = await ctx.Person
                    .Where(p => p.UserId.HasValue && jefeUserIds.Contains(p.UserId.Value))
                    .Join(ctx.Worker, p => p.PersonId, w => w.PersonId, (p, w) => new { w.Id, p.FullName })
                    .ToListAsync();

                var yaListados = resultado.Select(r => r.WorkerId).ToHashSet();
                resultado.AddRange(jefes
                    .Where(j => !yaListados.Contains(j.Id))
                    .Select(j => new ResponsableProyectoDto
                    {
                        WorkerId = j.Id,
                        Nombre = j.FullName ?? $"Trabajador {j.Id}",
                        Rol = "Jefe SSOMA",
                    }));
            }

            return resultado
                .OrderBy(r => r.Rol)
                .ThenBy(r => r.Nombre)
                .ToList();
        }

        public async Task<bool> ReordenarAsync(List<(int Id, int Orden)> items)
        {
            using var ctx = _factory.CreateDbContext();
            var ids = items.Select(i => i.Id).ToList();
            var entidades = await ctx.Set<SsInduccionRotacionProyecto>()
                .Where(r => ids.Contains(r.Id))
                .ToListAsync();

            foreach (var e in entidades)
            {
                var nuevoOrden = items.First(i => i.Id == e.Id).Orden;
                e.Orden = nuevoOrden;
                e.UpdatedAt = DateTime.UtcNow;
            }

            await ctx.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetActivoAsync(int id, bool activo)
        {
            using var ctx = _factory.CreateDbContext();
            var entity = await ctx.Set<SsInduccionRotacionProyecto>().FirstOrDefaultAsync(r => r.Id == id);
            if (entity is null) return false;

            entity.Activo = activo;
            entity.UpdatedAt = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
            return true;
        }

        // ── Cursor ──────────────────────────────────────────────────────────

        public async Task<SsInduccionRotacionCursor> GetOrCreateCursorAsync()
        {
            using var ctx = _factory.CreateDbContext();
            var cursor = await ctx.Set<SsInduccionRotacionCursor>().FirstOrDefaultAsync(c => c.Id == 1);
            if (cursor is not null) return cursor;

            cursor = new SsInduccionRotacionCursor { Id = 1 };
            ctx.Set<SsInduccionRotacionCursor>().Add(cursor);
            await ctx.SaveChangesAsync();
            return cursor;
        }

        public async Task GuardarCursorAsync(int? ultimoProyectoRotacionId, DateOnly ultimaFechaGenerada)
        {
            using var ctx = _factory.CreateDbContext();
            var cursor = await ctx.Set<SsInduccionRotacionCursor>().FirstOrDefaultAsync(c => c.Id == 1);
            if (cursor is null)
            {
                cursor = new SsInduccionRotacionCursor { Id = 1 };
                ctx.Set<SsInduccionRotacionCursor>().Add(cursor);
            }

            cursor.UltimoProyectoRotacionId = ultimoProyectoRotacionId;
            cursor.UltimaFechaGenerada = ultimaFechaGenerada;
            cursor.UpdatedAt = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }

        // ── Feriados ────────────────────────────────────────────────────────

        /// <summary>
        /// Mismo criterio que <c>LessonReminderRepository.GetHolidayDatesAsync</c> (feriados
        /// activos/vivos, resolviendo los "recurring_yearly" contra cada año del rango), pero
        /// sobre un rango de fechas arbitrario en vez de un solo mes.
        /// </summary>
        public async Task<HashSet<DateOnly>> GetFeriadosAsync(DateOnly desde, DateOnly hasta)
        {
            using var ctx = _factory.CreateDbContext();
            var holidays = await ctx.Holiday
                .Where(h => h.State && h.Active)
                .Select(h => new { h.HolidayDate, h.RecurringYearly })
                .ToListAsync();

            var result = new HashSet<DateOnly>();
            for (var year = desde.Year; year <= hasta.Year; year++)
            {
                foreach (var h in holidays)
                {
                    if (h.RecurringYearly)
                    {
                        var day = Math.Min(h.HolidayDate.Day, DateTime.DaysInMonth(year, h.HolidayDate.Month));
                        var fecha = new DateOnly(year, h.HolidayDate.Month, day);
                        if (fecha >= desde && fecha <= hasta) result.Add(fecha);
                    }
                    else if (h.HolidayDate.Year == year && h.HolidayDate >= desde && h.HolidayDate <= hasta)
                    {
                        result.Add(h.HolidayDate);
                    }
                }
            }

            return result;
        }

        // ── Programación ──────────────────────────────────────────────────

        public async Task<List<SsInduccionProgramacion>> GetProgramacionAsync(DateOnly desde, DateOnly hasta)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Set<SsInduccionProgramacion>()
                .Where(p => p.Fecha >= desde && p.Fecha <= hasta)
                .OrderBy(p => p.Fecha)
                .ToListAsync();
        }

        public async Task<SsInduccionProgramacion?> GetProgramacionByIdAsync(int id)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Set<SsInduccionProgramacion>().FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<SsInduccionProgramacion> CrearProgramacionAsync(DateOnly fecha, int proyectoId, int? responsableWorkerId)
        {
            using var ctx = _factory.CreateDbContext();
            var entity = new SsInduccionProgramacion
            {
                Fecha = fecha,
                ProyectoId = proyectoId,
                ResponsableWorkerId = responsableWorkerId,
                Estado = "Programada",
                CreatedAt = DateTime.UtcNow,
            };
            ctx.Set<SsInduccionProgramacion>().Add(entity);
            await ctx.SaveChangesAsync();
            return entity;
        }

        public async Task GuardarProgramacionAsync(SsInduccionProgramacion programacion)
        {
            using var ctx = _factory.CreateDbContext();

            // La entidad llega "detached" (se leyó en OTRO DbContext) — Update() la marca
            // COMPLETA como modificada, incluidas columnas que no cambiaron (ej. created_at).
            // Postgres exige Kind=Utc para "timestamp with time zone"; lo que vuelve de una
            // query anterior puede traer Kind=Unspecified y Npgsql lo rechaza al reenviarlo.
            if (programacion.CreatedAt.Kind != DateTimeKind.Utc)
                programacion.CreatedAt = DateTime.SpecifyKind(programacion.CreatedAt, DateTimeKind.Utc);
            if (programacion.FechaAvisoEnviado.HasValue && programacion.FechaAvisoEnviado.Value.Kind != DateTimeKind.Utc)
                programacion.FechaAvisoEnviado = DateTime.SpecifyKind(programacion.FechaAvisoEnviado.Value, DateTimeKind.Utc);

            ctx.Set<SsInduccionProgramacion>().Update(programacion);
            programacion.UpdatedAt = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }

        public async Task<List<SsInduccionProgramacion>> GetPendientesDeAvisoAsync(DateOnly hasta)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Set<SsInduccionProgramacion>()
                .Where(p => p.Estado == "Programada" && !p.AvisoEnviado && p.Fecha <= hasta)
                .ToListAsync();
        }

        // ── Destinatarios ───────────────────────────────────────────────────

        public async Task<InduccionDestinatariosDto> GetDestinatariosAsync(int proyectoId)
        {
            using var ctx = _factory.CreateDbContext();

            var proyecto = await ctx.Project
                .Where(p => p.ProjectId == proyectoId)
                .Select(p => new { p.EmailCoordAdmin, p.EmailCoordSsoma, p.ResidenteWorkersId })
                .FirstOrDefaultAsync();

            var residenteEmail = proyecto?.ResidenteWorkersId is int workerId
                ? await ctx.Worker
                    .Where(w => w.Id == workerId)
                    .Select(w => w.EmailCorporativo)
                    .FirstOrDefaultAsync()
                : null;

            var prevencionistas = await ctx.WorkerVinculacion
                .Where(v => v.ProyectoId == proyectoId
                         && v.FechaFin == null
                         && v.CategoriaId == CategoriaIds.Prevencionista)
                .Join(ctx.Worker, v => v.WorkerId, w => w.Id, (v, w) => w.EmailCorporativo)
                .Where(email => email != null)
                .Distinct()
                .ToListAsync();

            return new InduccionDestinatariosDto
            {
                EmailCoordAdmin = proyecto?.EmailCoordAdmin,
                EmailCoordSsoma = proyecto?.EmailCoordSsoma,
                EmailResidente = residenteEmail,
                EmailsPrevencionistas = prevencionistas!,
            };
        }

        public async Task<string> GetProyectoNombreAsync(int proyectoId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Project
                .Where(p => p.ProjectId == proyectoId)
                .Select(p => p.ProjectDescription)
                .FirstOrDefaultAsync() ?? $"Proyecto {proyectoId}";
        }

        public async Task<Dictionary<int, string>> GetProyectoNombresAsync(IEnumerable<int> proyectoIds)
        {
            using var ctx = _factory.CreateDbContext();
            var ids = proyectoIds.Distinct().ToList();
            return await ctx.Project
                .Where(p => ids.Contains(p.ProjectId))
                .ToDictionaryAsync(p => p.ProjectId, p => p.ProjectDescription);
        }

        public async Task<Dictionary<int, string>> GetWorkerNombresAsync(IEnumerable<int> workerIds)
        {
            using var ctx = _factory.CreateDbContext();
            return await GetWorkerNombresInternalAsync(ctx, workerIds);
        }

        /// <summary>Worker no tiene navegación directa a Person (mismo patrón que el resto del
        /// código: join manual por PersonId) — de ahí sale el nombre completo.</summary>
        private static async Task<Dictionary<int, string>> GetWorkerNombresInternalAsync(
            Abril_Backend.Infrastructure.Data.AppDbContext ctx, IEnumerable<int> workerIds)
        {
            var ids = workerIds.Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<int, string>();

            return await ctx.Worker
                .Where(w => ids.Contains(w.Id))
                .Join(ctx.Person, w => w.PersonId, p => p.PersonId, (w, p) => new { w.Id, p.FullName })
                .Where(x => x.FullName != null)
                .ToDictionaryAsync(x => x.Id, x => x.FullName!);
        }
    }
}
