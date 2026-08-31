using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Inducciones;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Helpers;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Repositories
{
    public class InduccionRepository : IInduccionRepository
    {
        private const int ItemInduccionObra = 12;
        private const int ItemRegistroEpp = 5;
        private const int ItemRisst = 6;
        private const int ItemEntregaRecomendaciones = 8;
        private const int ItemDifusionPts = 10;

        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly ITrabajadorRestringidoService _restringidoService;

        public InduccionRepository(
            IDbContextFactory<AppDbContext> factory,
            ITrabajadorRestringidoService restringidoService)
        {
            _factory = factory;
            _restringidoService = restringidoService;
        }

        public async Task<List<int>> CreateAsync(InduccionCreateDto dto, int programadoPor)
        {
            using var ctx = _factory.CreateDbContext();

            // Validar restricciones antes de programar
            foreach (var workerId in dto.WorkerIds)
            {
                var w = await ctx.Worker
                    .Include(x => x.Person)
                    .FirstOrDefaultAsync(x => x.Id == workerId)
                    ?? throw new AbrilException($"Trabajador {workerId} no encontrado.", 404);
                if (await _restringidoService.EstaRestringidoPorDniAsync(w.Person?.DocumentIdentityCode))
                    throw new AbrilException(
                        $"El trabajador {w.Person?.FullName} está restringido. Comuníquese con el área de Administración o SSOMA.", 400);
            }

            var limaZone = TimeZoneInfo.FindSystemTimeZoneById("America/Lima");
            var fechaLima = DateTime.SpecifyKind(dto.FechaProgramada, DateTimeKind.Unspecified);
            var fecha = TimeZoneInfo.ConvertTimeToUtc(fechaLima, limaZone);
            var hoyLima = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, limaZone).Date;

            // IDs de workers que ya tienen una inducción PROGRAMADA que sigue bloqueando el reagendado:
            // vigente (fecha aún no vencida) o ya con ingreso confirmado (asistió y está pendiente de
            // aprobación, no de reprogramación). Solo se libera cuando es un no-show real: vencida y
            // sin ingreso confirmado — sin depender de que el cron reset-falta ya la haya pasado a FALTA.
            var yaExisten = await ctx.SsInduccion
                .Where(i => dto.WorkerIds.Contains(i.WorkerId)
                    && i.ProyectoId == dto.ProyectoId
                    && i.Estado == "PROGRAMADA")
                .Select(i => new { i.WorkerId, i.FechaProgramada, i.IngresoConfirmado })
                .ToListAsync();

            var yaExistenVigentesSet = yaExisten
                .Where(i => i.IngresoConfirmado
                    || TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(i.FechaProgramada, DateTimeKind.Utc), limaZone).Date >= hoyLima)
                .Select(i => i.WorkerId)
                .ToHashSet();

            var nuevos = dto.WorkerIds.Distinct()
                .Where(wId => !yaExistenVigentesSet.Contains(wId))
                .ToList();

            if (nuevos.Count == 0) return [];

            var empresaId = dto.EmpresaId ?? 0;
            var now = DateTime.UtcNow;

            var inducciones = nuevos.Select(wId => new SsInduccion
            {
                WorkerId = wId,
                ProyectoId = dto.ProyectoId,
                EmpresaId = empresaId,
                FechaProgramada = fecha,
                TrabajoAltura = dto.TrabajoAltura,
                EquipoElectrico = dto.EquipoElectrico,
                Estado = "PROGRAMADA",
                ProgramadoPor = programadoPor,
                CreatedAt = now,
                UpdatedAt = now
            }).ToList();

            ctx.SsInduccion.AddRange(inducciones);
            await ctx.SaveChangesAsync();

            return inducciones.Select(i => i.Id).ToList();
        }

        public async Task<List<InduccionListDto>> GetAsync(
            int? proyectoId, int? empresaId, string? estado,
            DateTime? fechaDesde, DateTime? fechaHasta)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ctx.SsInduccion.AsQueryable();

            if (proyectoId.HasValue)
                query = query.Where(i => i.ProyectoId == proyectoId.Value);
            if (empresaId.HasValue)
                query = query.Where(i => i.EmpresaId == empresaId.Value);
            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(i => i.Estado == estado);
            var desde = DateTime.SpecifyKind(fechaDesde ?? DateTime.MinValue, DateTimeKind.Utc);
            var hasta = DateTime.SpecifyKind(fechaHasta ?? DateTime.MaxValue, DateTimeKind.Utc);
            query = query.Where(i => i.FechaProgramada >= desde && i.FechaProgramada <= hasta);

            var rows = await query
                .OrderByDescending(i => i.FechaProgramada)
                .ThenByDescending(i => i.Id)
                .ToListAsync();

            if (rows.Count == 0) return [];

            var workerIds = rows.Select(r => r.WorkerId).Distinct().ToList();
            var proyectoIds = rows.Select(r => r.ProyectoId).Distinct().ToList();
            var empresaIds = rows.Select(r => r.EmpresaId).Distinct().ToList();

            var workers = await ctx.Worker
                .Where(w => workerIds.Contains(w.Id))
                .Select(w => new
                {
                    w.Id,
                    ApellidoNombre = w.Person != null ? w.Person.FullName : null,
                    Dni = w.Person != null ? w.Person.DocumentIdentityCode : null
                })
                .ToDictionaryAsync(w => w.Id);

            var proyectos = await ctx.Project
                .Where(p => proyectoIds.Contains(p.ProjectId))
                .Select(p => new { p.ProjectId, p.ProjectDescription })
                .ToDictionaryAsync(p => p.ProjectId);

            var empresas = await ctx.Contributor
                .Where(c => empresaIds.Contains(c.ContributorId))
                .Select(c => new { c.ContributorId, c.ContributorName })
                .ToDictionaryAsync(c => c.ContributorId);

            return rows.Select(r =>
            {
                workers.TryGetValue(r.WorkerId, out var w);
                proyectos.TryGetValue(r.ProyectoId, out var p);
                empresas.TryGetValue(r.EmpresaId, out var e);

                return new InduccionListDto
                {
                    Id = r.Id,
                    WorkerId = r.WorkerId,
                    ApellidoNombre = w?.ApellidoNombre ?? string.Empty,
                    Dni = w?.Dni ?? string.Empty,
                    ProyectoId = r.ProyectoId,
                    ProyectoNombre = p?.ProjectDescription ?? string.Empty,
                    EmpresaId = r.EmpresaId,
                    EmpresaNombre = e?.ContributorName ?? string.Empty,
                    FechaProgramada = r.FechaProgramada,
                    TrabajoAltura = r.TrabajoAltura,
                    EquipoElectrico = r.EquipoElectrico,
                    Estado = r.Estado,
                    IngresoConfirmado = r.IngresoConfirmado,
                    FechaIngreso = r.FechaIngreso
                };
            }).ToList();
        }

        public async Task<List<InduccionTrabajadorDto>> GetTrabajadoresPorProgramarAsync(int? empresaId, int proyectoId, string? search = null)
        {
            using var ctx = _factory.CreateDbContext();

            // Workers asignados al proyecto con inducción pendiente (sin filtro fecha_fin)
            var workerIds = await ctx.WorkerProyecto
                .Where(wp => wp.ProyectoId == proyectoId && !wp.InduccionCompletada)
                .Select(wp => wp.WorkerId)
                .Distinct()
                .ToListAsync();

            if (workerIds.Count == 0) return [];

            var limaZone = TimeZoneInfo.FindSystemTimeZoneById("America/Lima");
            var hoyLima = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, limaZone).Date;

            // Solo bloquea una PROGRAMADA vigente (fecha >= hoy) o con ingreso ya confirmado (asistió,
            // está pendiente de aprobación, no de reprogramación). Una PROGRAMADA vencida sin ingreso
            // confirmado es un no-show real y no bloquea, aunque el cron reset-falta aún no la haya
            // pasado a FALTA.
            var workerIdsConProgramacion = (await ctx.SsInduccion
                .Where(i => i.ProyectoId == proyectoId
                         && workerIds.Contains(i.WorkerId)
                         && i.Estado == "PROGRAMADA")
                .Select(i => new { i.WorkerId, i.FechaProgramada, i.IngresoConfirmado })
                .ToListAsync())
                .Where(i => i.IngresoConfirmado
                    || TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(i.FechaProgramada, DateTimeKind.Utc), limaZone).Date >= hoyLima)
                .Select(i => i.WorkerId)
                .Distinct()
                .ToList();

            workerIds = workerIds
                .Where(id => !workerIdsConProgramacion.Contains(id))
                .ToList();

            if (workerIds.Count == 0) return [];

            // Si se filtra por empresa, reducir a workers con vinculación con esa empresa
            if (empresaId.HasValue)
            {
                var conEmpresa = await ctx.WorkerVinculacion
                    .Where(v => workerIds.Contains(v.WorkerId) && v.EmpresaId == empresaId.Value)
                    .Select(v => v.WorkerId)
                    .Distinct()
                    .ToListAsync();
                workerIds = conEmpresa;
                if (workerIds.Count == 0) return [];
            }

            // Datos del worker con filtro de búsqueda aplicado en la query
            var workersQuery = ctx.Worker.Where(w => workerIds.Contains(w.Id));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                if (s.Length == 8 && s.All(char.IsDigit))
                    workersQuery = workersQuery.Where(w => w.Person != null && w.Person.DocumentIdentityCode == s);
                else
                    workersQuery = workersQuery.Where(w =>
                        w.Person != null && w.Person.FullName != null && w.Person.FullName.ToLower().Contains(s.ToLower()));
            }

            var workers = await workersQuery
                .Select(w => new
                {
                    w.Id,
                    ApellidoNombre = w.Person != null ? w.Person.FullName : null,
                    Dni = w.Person != null ? w.Person.DocumentIdentityCode : null,
                    w.ObraOficinaStaffId
                })
                .ToDictionaryAsync(w => w.Id);

            workerIds = workers.Keys.ToList();
            if (workerIds.Count == 0) return [];

            // Última vinculación de cada worker para resolver empresa
            var todasVinculaciones = await ctx.WorkerVinculacion
                .Where(v => workerIds.Contains(v.WorkerId))
                .OrderByDescending(v => v.CreatedAt)
                .ThenByDescending(v => v.Id)
                .ToListAsync();

            var ultimaVinculacion = todasVinculaciones
                .GroupBy(v => v.WorkerId)
                .ToDictionary(g => g.Key, g => g.First());

            var empresaIds = ultimaVinculacion.Values
                .Where(v => v.EmpresaId.HasValue)
                .Select(v => v.EmpresaId!.Value)
                .Distinct()
                .ToList();

            var empresaMap = await ctx.Contributor
                .Where(c => empresaIds.Contains(c.ContributorId))
                .ToDictionaryAsync(c => c.ContributorId, c => c.ContributorName);

            // Badge futuro: workers que ya indujeron en este proyecto
            var yaIndujeroSet = (await ctx.WorkerProyecto
                .Where(wp => wp.ProyectoId == proyectoId && wp.InduccionCompletada)
                .Select(wp => wp.WorkerId)
                .ToListAsync()).ToHashSet();

            return workerIds
                .Where(workers.ContainsKey)
                .Select(wId =>
                {
                    var w = workers[wId];
                    var empId = ultimaVinculacion.TryGetValue(wId, out var vin) ? vin.EmpresaId : null;
                    empresaMap.TryGetValue(empId ?? 0, out var empNombre);
                    return new InduccionTrabajadorDto
                    {
                        WorkerId = wId,
                        ApellidoNombre = w.ApellidoNombre ?? string.Empty,
                        Dni = w.Dni ?? string.Empty,
                        ObraOficinaStaffId = w.ObraOficinaStaffId,
                        ObraOficina = ObraOficinaStaffIds.Nombre(w.ObraOficinaStaffId),
                        EmpresaId = empId,
                        EmpresaNombre = empNombre ?? string.Empty,
                        YaIndujo = yaIndujeroSet.Contains(wId)
                    };
                })
                .OrderBy(d => d.ApellidoNombre)
                .ToList();
        }

        public async Task AprobarAsync(int id)
        {
            using var ctx = _factory.CreateDbContext();
            var induccion = await ctx.SsInduccion.FirstOrDefaultAsync(i => i.Id == id)
                ?? throw new AbrilException("Inducción no encontrada.", 404);

            await AprobarInduccionAsync(ctx, induccion);
            await ctx.SaveChangesAsync();
        }

        public async Task AprobarBatchAsync(List<int> ids)
        {
            using var ctx = _factory.CreateDbContext();

            var inducciones = await ctx.SsInduccion
                .Where(i => ids.Contains(i.Id))
                .ToListAsync();

            foreach (var induccion in inducciones)
                await AprobarInduccionAsync(ctx, induccion);

            await ctx.SaveChangesAsync();
        }

        public async Task RechazarAsync(int id)
        {
            using var ctx = _factory.CreateDbContext();
            var induccion = await ctx.SsInduccion.FirstOrDefaultAsync(i => i.Id == id)
                ?? throw new AbrilException("Inducción no encontrada.", 404);

            // RECHAZADA no bloquea el reagendado (a diferencia de PROGRAMADA vigente o con
            // ingreso confirmado): el trabajador queda libre para volver a programarse.
            induccion.Estado = "RECHAZADA";
            induccion.UpdatedAt = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }

        public async Task<int> ResetFaltaAsync()
        {
            using var ctx = _factory.CreateDbContext();
            var ahora = DateTime.UtcNow;
            var limaZone = TimeZoneInfo.FindSystemTimeZoneById("America/Lima");
            var hoyLima = TimeZoneInfo.ConvertTimeFromUtc(ahora, limaZone).Date;

            var candidatas = await ctx.SsInduccion
                .Where(i => i.Estado == "PROGRAMADA" && !i.IngresoConfirmado)
                .ToListAsync();

            var aMarcar = candidatas.Where(i =>
            {
                var fechaLima = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(i.FechaProgramada, DateTimeKind.Utc),
                    limaZone).Date;
                return fechaLima < hoyLima;
            }).ToList();

            foreach (var ind in aMarcar)
            {
                ind.Estado = "FALTA";
                ind.UpdatedAt = ahora;
            }

            await ctx.SaveChangesAsync();
            return aMarcar.Count;
        }

        private async Task AprobarInduccionAsync(AppDbContext ctx, SsInduccion induccion)
        {
            induccion.Estado = "REALIZADA";
            induccion.UpdatedAt = DateTime.UtcNow;

            // Determinar si el worker pertenece a empresa Casa
            var esAbril = await ctx.Contributor
                .Where(c => c.ContributorId == induccion.EmpresaId)
                .Select(c => c.EsAbril)
                .FirstOrDefaultAsync();

            // Items a aprobar: InduccionObra siempre + items Casa si corresponde
            var itemIds = new HashSet<int> { ItemInduccionObra };
            if (esAbril)
            {
                itemIds.Add(ItemRegistroEpp);
                itemIds.Add(ItemRisst);
                itemIds.Add(ItemEntregaRecomendaciones);
                itemIds.Add(ItemDifusionPts);
            }

            var now = DateTime.UtcNow;
            var sentinel = HabilitacionDateHelper.ResolverVigencia(false, "Aprobado", null);

            // Actualizar ss_hab_trabajador para cada item
            var habs = await ctx.SsHabTrabajador
                .Where(h => h.WorkerId == induccion.WorkerId && itemIds.Contains(h.ItemId))
                .ToListAsync();

            var habItemIds = habs.Select(h => h.ItemId).ToHashSet();

            foreach (var itemId in itemIds)
            {
                var hab = habs.FirstOrDefault(h => h.ItemId == itemId);
                if (hab is not null)
                {
                    hab.Estado = "Aprobado";
                    hab.Vigencia = sentinel;
                    hab.UpdatedAt = now;
                }
                else if (!habItemIds.Contains(itemId))
                {
                    ctx.SsHabTrabajador.Add(new SsHabTrabajador
                    {
                        WorkerId = induccion.WorkerId,
                        ItemId = itemId,
                        Estado = "Aprobado",
                        Vigencia = sentinel,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
            }

            // Marcar InduccionCompletada en ss_hab_worker_proyecto
            var workerProyecto = await ctx.WorkerProyecto
                .Where(wp => wp.WorkerId == induccion.WorkerId
                    && wp.ProyectoId == induccion.ProyectoId)
                .OrderByDescending(wp => wp.CreatedAt)
                .ThenByDescending(wp => wp.Id)
                .FirstOrDefaultAsync();

            if (workerProyecto is not null)
            {
                workerProyecto.InduccionCompletada = true;
                workerProyecto.FechaInduccion = DateOnly.FromDateTime(DateTime.UtcNow);
                workerProyecto.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
    }
}
