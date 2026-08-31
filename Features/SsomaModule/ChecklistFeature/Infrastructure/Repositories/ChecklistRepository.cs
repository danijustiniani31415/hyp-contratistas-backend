using Abril_Backend.Features.SsomaModule.ChecklistFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.ChecklistFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.ChecklistFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.SsomaModule.ChecklistFeature.Infrastructure.Repositories
{
    public class ChecklistRepository : IChecklistRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public ChecklistRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        // ─────────────────────────────────────────────────────────────────
        // PLANTILLAS
        // ─────────────────────────────────────────────────────────────────

        public async Task<List<ChecklistPlantillaListDto>> GetPlantillasAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsChecklistPlantilla
                .OrderBy(p => p.Orden)
                .ThenBy(p => p.Nombre)
                .Select(p => new ChecklistPlantillaListDto
                {
                    Id               = p.Id,
                    Nombre           = p.Nombre,
                    Descripcion      = p.Descripcion,
                    TipoActivacion   = p.TipoActivacion,
                    EventoActivacion = p.EventoActivacion,
                    EsObligatorio    = p.EsObligatorio,
                    Orden            = p.Orden,
                    Activo           = p.Activo,
                    TotalItems       = p.Items.Count(i => i.Activo)
                })
                .ToListAsync();
        }

        public async Task<ChecklistPlantillaDetalleDto?> GetPlantillaDetalleAsync(int plantillaId)
        {
            using var ctx = _factory.CreateDbContext();
            var p = await ctx.SsChecklistPlantilla
                .Include(x => x.Items.OrderBy(i => i.Orden))
                .FirstOrDefaultAsync(x => x.Id == plantillaId);

            if (p == null) return null;

            return new ChecklistPlantillaDetalleDto
            {
                Id               = p.Id,
                Nombre           = p.Nombre,
                Descripcion      = p.Descripcion,
                TipoActivacion   = p.TipoActivacion,
                EventoActivacion = p.EventoActivacion,
                EsObligatorio    = p.EsObligatorio,
                Orden            = p.Orden,
                Activo           = p.Activo,
                TotalItems       = p.Items.Count(i => i.Activo),
                Items = p.Items.Select(i => new ChecklistPlantillaItemDto
                {
                    Id               = i.Id,
                    Descripcion      = i.Descripcion,
                    Orden            = i.Orden,
                    TieneAdjuntoRef  = i.TieneAdjuntoRef,
                    Activo           = i.Activo
                }).ToList()
            };
        }

        public async Task<SsChecklistPlantilla> CreatePlantillaAsync(ChecklistPlantillaUpsertDto dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var now = DateTimeOffset.UtcNow;
            var entity = new SsChecklistPlantilla
            {
                Nombre           = dto.Nombre,
                Descripcion      = dto.Descripcion,
                TipoActivacion   = dto.TipoActivacion,
                EventoActivacion = dto.EventoActivacion,
                EsObligatorio    = dto.EsObligatorio,
                Orden            = dto.Orden,
                Activo           = true,
                CreatedAt        = now,
                UpdatedAt        = now
            };
            ctx.SsChecklistPlantilla.Add(entity);
            await ctx.SaveChangesAsync();
            return entity;
        }

        public async Task UpdatePlantillaAsync(int plantillaId, ChecklistPlantillaUpsertDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var entity = await ctx.SsChecklistPlantilla.FindAsync(plantillaId)
                ?? throw new KeyNotFoundException($"Plantilla {plantillaId} no encontrada.");

            entity.Nombre           = dto.Nombre;
            entity.Descripcion      = dto.Descripcion;
            entity.TipoActivacion   = dto.TipoActivacion;
            entity.EventoActivacion = dto.EventoActivacion;
            entity.EsObligatorio    = dto.EsObligatorio;
            entity.Orden            = dto.Orden;
            entity.UpdatedAt        = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }

        public async Task<SsChecklistPlantillaItem> AddItemToPlantillaAsync(int plantillaId, ChecklistPlantillaItemCreateDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var now = DateTimeOffset.UtcNow;

            // Orden: al final de los items existentes
            var maxOrden = await ctx.SsChecklistPlantillaItem
                .Where(i => i.PlantillaId == plantillaId)
                .Select(i => (int?)i.Orden)
                .MaxAsync() ?? 0;

            var item = new SsChecklistPlantillaItem
            {
                PlantillaId     = plantillaId,
                Descripcion     = dto.Descripcion,
                TieneAdjuntoRef = dto.TieneAdjuntoRef,
                Orden           = maxOrden + 1,
                Activo          = true,
                CreatedAt       = now,
                UpdatedAt       = now
            };
            ctx.SsChecklistPlantillaItem.Add(item);
            await ctx.SaveChangesAsync();

            // Propagar a todos los proyectos que ya tienen esta plantilla activa
            var checklistsActivos = await ctx.SsChecklistProyecto
                .Where(c => c.PlantillaId == plantillaId)
                .ToListAsync();

            if (checklistsActivos.Count > 0)
            {
                var nuevosItems = checklistsActivos.Select(c => new SsChecklistProyectoItem
                {
                    ChecklistProyectoId = c.Id,
                    PlantillaItemId     = item.Id,
                    Completado          = false,
                    CreatedAt           = now,
                    UpdatedAt           = now
                }).ToList();

                ctx.SsChecklistProyectoItem.AddRange(nuevosItems);
                await ctx.SaveChangesAsync();
            }

            return item;
        }

        public async Task UpdatePlantillaItemAsync(int itemId, ChecklistPlantillaItemEditDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var item = await ctx.SsChecklistPlantillaItem.FindAsync(itemId)
                ?? throw new KeyNotFoundException($"Item {itemId} no encontrado.");

            item.Descripcion     = dto.Descripcion;
            item.TieneAdjuntoRef = dto.TieneAdjuntoRef;
            item.Activo          = dto.Activo;
            item.UpdatedAt       = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────────────────────────
        // CHECKLISTS DE PROYECTO
        // ─────────────────────────────────────────────────────────────────

        public async Task<ChecklistProyectoResumenDto> GetResumenProyectoAsync(int proyectoId)
        {
            using var ctx = _factory.CreateDbContext();

            var checklists = await ctx.SsChecklistProyecto
                .Include(c => c.Plantilla)
                .Include(c => c.Items)
                .Where(c => c.ProyectoId == proyectoId)
                .OrderBy(c => c.Plantilla!.Orden)
                .ToListAsync();

            // Resolver nombres de quienes activaron
            var activadoPorIds = checklists
                .Where(c => c.ActivadoPorId.HasValue)
                .Select(c => c.ActivadoPorId!.Value)
                .Distinct()
                .ToList();

            var usuarios = await ctx.User
                .Where(u => activadoPorIds.Contains(u.UserId))
                .Select(u => new { u.UserId, u.Person.FullName })
                .ToDictionaryAsync(u => u.UserId, u => u.FullName);

            var cards = checklists.Select(c => new ChecklistProyectoCardDto
            {
                ChecklistProyectoId  = c.Id,
                PlantillaId          = c.PlantillaId,
                NombrePlantilla      = c.Plantilla?.Nombre ?? "",
                EsObligatorio        = c.Plantilla?.EsObligatorio ?? false,
                Estado               = c.Estado,
                PorcentajeCompletado = c.PorcentajeCompletado,
                TotalItems           = c.Items.Count,
                ItemsCompletados     = c.Items.Count(i => i.Completado),
                FechaActivacion      = c.FechaActivacion,
                FechaCompletado      = c.FechaCompletado,
                ActivadoPor          = c.ActivadoPorId.HasValue && usuarios.TryGetValue(c.ActivadoPorId.Value, out var nombre) ? nombre : null
            }).ToList();

            return new ChecklistProyectoResumenDto
            {
                ProyectoId  = proyectoId,
                Checklists  = cards
            };
        }

        public async Task<ChecklistProyectoDetalleDto?> GetChecklistDetalleAsync(int checklistProyectoId)
        {
            using var ctx = _factory.CreateDbContext();

            var c = await ctx.SsChecklistProyecto
                .Include(x => x.Plantilla)
                .Include(x => x.Proyecto)
                .Include(x => x.Items)
                    .ThenInclude(i => i.PlantillaItem)
                .Include(x => x.Items)
                    .ThenInclude(i => i.CompletadoPor)
                        .ThenInclude(u => u!.Person)
                .FirstOrDefaultAsync(x => x.Id == checklistProyectoId);

            if (c == null) return null;

            return new ChecklistProyectoDetalleDto
            {
                Id                   = c.Id,
                ProyectoId           = c.ProyectoId,
                ProyectoNombre       = c.Proyecto?.ProjectDescription ?? "",
                PlantillaId          = c.PlantillaId,
                NombrePlantilla      = c.Plantilla?.Nombre ?? "",
                EsObligatorio        = c.Plantilla?.EsObligatorio ?? false,
                Estado               = c.Estado,
                PorcentajeCompletado = c.PorcentajeCompletado,
                FechaActivacion      = c.FechaActivacion,
                FechaCompletado      = c.FechaCompletado,
                Items = c.Items
                    .OrderBy(i => i.PlantillaItem?.Orden ?? 0)
                    .Select(i => new ChecklistProyectoItemDto
                    {
                        Id              = i.Id,
                        PlantillaItemId = i.PlantillaItemId,
                        Descripcion     = i.PlantillaItem?.Descripcion ?? "",
                        Orden           = i.PlantillaItem?.Orden ?? 0,
                        TieneAdjuntoRef = i.PlantillaItem?.TieneAdjuntoRef ?? false,
                        Completado      = i.Completado,
                        FechaCompletado = i.FechaCompletado,
                        CompletadoPor   = i.CompletadoPor?.Person?.FullName,
                        Observacion     = i.Observacion,
                        UrlAdjunto      = i.UrlAdjunto
                    }).ToList()
            };
        }

        public async Task<SsChecklistProyecto> ActivarChecklistAsync(int proyectoId, int plantillaId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            // Idempotente: si ya existe, lo retorna
            var existente = await ctx.SsChecklistProyecto
                .FirstOrDefaultAsync(c => c.ProyectoId == proyectoId && c.PlantillaId == plantillaId);
            if (existente != null)
                return existente;

            var items = await ctx.SsChecklistPlantillaItem
                .Where(i => i.PlantillaId == plantillaId && i.Activo)
                .OrderBy(i => i.Orden)
                .ToListAsync();

            var now = DateTimeOffset.UtcNow;
            var checklist = new SsChecklistProyecto
            {
                ProyectoId       = proyectoId,
                PlantillaId      = plantillaId,
                Estado           = "pendiente",
                PorcentajeCompletado = 0,
                FechaActivacion  = now,
                ActivadoPorId    = userId,
                NotificacionEnviada = false,
                CreatedAt        = now,
                UpdatedAt        = now
            };
            ctx.SsChecklistProyecto.Add(checklist);
            await ctx.SaveChangesAsync();

            var proyectoItems = items.Select(i => new SsChecklistProyectoItem
            {
                ChecklistProyectoId = checklist.Id,
                PlantillaItemId     = i.Id,
                Completado          = false,
                CreatedAt           = now,
                UpdatedAt           = now
            }).ToList();

            ctx.SsChecklistProyectoItem.AddRange(proyectoItems);
            await ctx.SaveChangesAsync();

            return checklist;
        }

        public async Task SeedChecklistsObligatoriosAsync(int proyectoId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var plantillasObligatorias = await ctx.SsChecklistPlantilla
                .Where(p => p.EsObligatorio && p.Activo && p.TipoActivacion == "automatico")
                .ToListAsync();

            var now = DateTimeOffset.UtcNow;
            foreach (var plantilla in plantillasObligatorias)
            {
                var yaExiste = await ctx.SsChecklistProyecto
                    .AnyAsync(c => c.ProyectoId == proyectoId && c.PlantillaId == plantilla.Id);
                if (yaExiste) continue;

                var items = await ctx.SsChecklistPlantillaItem
                    .Where(i => i.PlantillaId == plantilla.Id && i.Activo)
                    .OrderBy(i => i.Orden)
                    .ToListAsync();

                var checklist = new SsChecklistProyecto
                {
                    ProyectoId          = proyectoId,
                    PlantillaId         = plantilla.Id,
                    Estado              = "pendiente",
                    PorcentajeCompletado = 0,
                    FechaActivacion     = now,
                    ActivadoPorId       = userId,
                    NotificacionEnviada = false,
                    CreatedAt           = now,
                    UpdatedAt           = now
                };
                ctx.SsChecklistProyecto.Add(checklist);
                await ctx.SaveChangesAsync();

                var proyectoItems = items.Select(i => new SsChecklistProyectoItem
                {
                    ChecklistProyectoId = checklist.Id,
                    PlantillaItemId     = i.Id,
                    Completado          = false,
                    CreatedAt           = now,
                    UpdatedAt           = now
                }).ToList();

                ctx.SsChecklistProyectoItem.AddRange(proyectoItems);
                await ctx.SaveChangesAsync();
            }
        }

        public async Task<(decimal porcentaje, bool recienCompletado)> ToggleItemAsync(
            int checklistProyectoItemId, ChecklistItemToggleDto dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var now = DateTimeOffset.UtcNow;

            var item = await ctx.SsChecklistProyectoItem
                .Include(i => i.ChecklistProyecto)
                .FirstOrDefaultAsync(i => i.Id == checklistProyectoItemId)
                ?? throw new KeyNotFoundException($"Item {checklistProyectoItemId} no encontrado.");

            item.Completado     = dto.Completado;
            item.FechaCompletado = dto.Completado ? now : null;
            item.CompletadoPorId = dto.Completado ? userId : null;
            item.Observacion    = dto.Observacion;
            item.UrlAdjunto     = dto.UrlAdjunto;
            item.UpdatedAt      = now;

            await ctx.SaveChangesAsync();

            // Recalcular porcentaje del checklist padre
            var checklist = item.ChecklistProyecto!;
            var todos = await ctx.SsChecklistProyectoItem
                .Where(i => i.ChecklistProyectoId == checklist.Id)
                .ToListAsync();

            var total = todos.Count;
            var completados = todos.Count(i => i.Completado);
            var porcentaje = total == 0 ? 0m : Math.Round((decimal)completados / total * 100, 2);

            var estabaCompletado = checklist.Estado == "completado";
            checklist.PorcentajeCompletado = porcentaje;
            checklist.Estado = porcentaje == 0 ? "pendiente"
                             : porcentaje < 100 ? "en_progreso"
                             : "completado";

            if (porcentaje == 100 && checklist.FechaCompletado == null)
                checklist.FechaCompletado = now;
            else if (porcentaje < 100)
                checklist.FechaCompletado = null;

            checklist.UpdatedAt = now;
            await ctx.SaveChangesAsync();

            // recienCompletado = acaba de llegar al 100% y notificación aún no enviada
            var recienCompletado = porcentaje == 100 && !estabaCompletado && !checklist.NotificacionEnviada;
            return (porcentaje, recienCompletado);
        }

        public async Task<(string? emailGerente, string nombreProyecto, string nombreChecklist)> GetDatosNotificacionAsync(int checklistProyectoId)
        {
            using var ctx = _factory.CreateDbContext();
            var data = await ctx.SsChecklistProyecto
                .Include(c => c.Proyecto)
                .Include(c => c.Plantilla)
                .Where(c => c.Id == checklistProyectoId)
                .Select(c => new
                {
                    EmailGerente    = c.Proyecto != null ? c.Proyecto.EmailResponsable : null,
                    NombreProyecto  = c.Proyecto != null ? c.Proyecto.ProjectDescription : "",
                    NombreChecklist = c.Plantilla != null ? c.Plantilla.Nombre : ""
                })
                .FirstOrDefaultAsync();

            if (data == null) return (null, "", "");
            return (data.EmailGerente, data.NombreProyecto, data.NombreChecklist);
        }

        public async Task<int> GetChecklistProyectoIdByItemAsync(int checklistProyectoItemId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsChecklistProyectoItem
                .Where(i => i.Id == checklistProyectoItemId)
                .Select(i => i.ChecklistProyectoId)
                .FirstOrDefaultAsync();
        }

        public async Task MarcarNotificacionEnviadaAsync(int checklistProyectoId)
        {
            using var ctx = _factory.CreateDbContext();
            var c = await ctx.SsChecklistProyecto.FindAsync(checklistProyectoId);
            if (c == null) return;
            c.NotificacionEnviada = true;
            c.UpdatedAt = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }
    }
}
