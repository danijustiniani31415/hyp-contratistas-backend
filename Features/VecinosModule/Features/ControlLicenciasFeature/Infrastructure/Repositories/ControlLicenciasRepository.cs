using Abril_Backend.Features.VecinosModule.Features.ControlLicenciasFeature.Application.Dtos;
using Abril_Backend.Features.VecinosModule.Features.ControlLicenciasFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.VecinosModule.Features.ControlLicenciasFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.VecinosModule.Features.ControlLicenciasFeature.Infrastructure.Repositories
{
    public class ControlLicenciasRepository : IControlLicenciasRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public ControlLicenciasRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        private static DateOnly Hoy() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));

        public async Task<List<ProjectOptionDto>> GetProyectos()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Project
                .Where(p => p.State && p.Active
                    && !ctx.ProyectoFiltro.Any(f => f.ProjectId == p.ProjectId
                        && f.FuncionalidadId == ProyectoFiltroFuncionalidades.ControlLicencias && !f.Active))
                .OrderBy(p => p.ProjectDescription)
                .Select(p => new ProjectOptionDto { ProjectId = p.ProjectId, ProjectDescription = p.ProjectDescription })
                .ToListAsync();
        }

        public async Task<VecinoLicenciaPlantillaResponseDto> GetPlantilla(int projectId)
        {
            using var ctx = _factory.CreateDbContext();
            var hoy = Hoy();

            var tipos = await ctx.VecinoLicenciaControlTipo
                .Where(t => t.State && t.Active && (t.ProjectId == null || t.ProjectId == projectId))
                .OrderBy(t => t.Orden)
                .ToListAsync();

            var estados = await ctx.VecinoLicenciaControlEstado
                .Where(e => e.State && e.Active)
                .OrderBy(e => e.VecinoLicenciaControlEstadoId)
                .ToListAsync();
            var estadoDesc = estados.ToDictionary(e => e.VecinoLicenciaControlEstadoId, e => e.Descripcion);
            int pendienteId = estados.FirstOrDefault(e => e.Descripcion == "Pendiente")?.VecinoLicenciaControlEstadoId ?? 0;

            var rows = await ctx.VecinoLicenciaControl
                .Where(r => r.ProjectId == projectId && r.State)
                .ToListAsync();
            var rowIds = rows.Select(r => r.VecinoLicenciaControlId).ToList();

            var historialCounts = await ctx.VecinoLicenciaControlHistorial
                .Where(h => h.State && rowIds.Contains(h.VecinoLicenciaControlId))
                .GroupBy(h => h.VecinoLicenciaControlId)
                .Select(g => new { VecinoLicenciaControlId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.VecinoLicenciaControlId, g => g.Count);

            var recordatorios = await ctx.VecinoLicenciaControlRecordatorio
                .Where(r => r.State && r.Active && rowIds.Contains(r.VecinoLicenciaControlId))
                .OrderBy(r => r.DiasAntes)
                .ToListAsync();
            var recordatoriosPorLicencia = recordatorios
                .GroupBy(r => r.VecinoLicenciaControlId)
                .ToDictionary(g => g.Key, g => g.Select(r => new VecinoLicenciaRecordatorioDto
                {
                    VecinoLicenciaControlRecordatorioId = r.VecinoLicenciaControlRecordatorioId,
                    DiasAntes = r.DiasAntes,
                    FechaRecordatorio = r.FechaRecordatorio,
                    Enviado = r.EnviadoDateTime != null,
                }).ToList());

            var items = tipos.Select(t =>
            {
                var row = rows.FirstOrDefault(r => r.VecinoLicenciaControlTipoId == t.VecinoLicenciaControlTipoId);
                var estadoId = row?.VecinoLicenciaControlEstadoId ?? pendienteId;
                var descripcion = row != null
                    ? (estadoDesc.TryGetValue(estadoId, out var desc) ? desc : "Pendiente")
                    : "Pendiente";
                var recordatoriosDeEsta = row != null && recordatoriosPorLicencia.TryGetValue(row.VecinoLicenciaControlId, out var rs)
                    ? rs : new List<VecinoLicenciaRecordatorioDto>();

                // Vencido/Por vencer se muestran solo como indicador visual; el estado guardado sigue siendo Cargado.
                if (descripcion == "Cargado" && row?.FechaVencimiento != null)
                {
                    if (row.FechaVencimiento < hoy)
                        descripcion = "Vencido";
                    else if (recordatoriosDeEsta.Any(r => FechaEfectivaEnvio(r.FechaRecordatorio) <= hoy))
                        descripcion = "Por vencer";
                }

                return new VecinoLicenciaItemDto
                {
                    VecinoLicenciaControlId = row?.VecinoLicenciaControlId,
                    VecinoLicenciaControlTipoId = t.VecinoLicenciaControlTipoId,
                    TipoDescripcion = t.Descripcion,
                    Orden = t.Orden,
                    EsBase = t.ProjectId == null,
                    VecinoLicenciaControlEstadoId = estadoId,
                    EstadoDescripcion = descripcion,
                    ArchivoUrl = row?.ArchivoUrl,
                    OriginalFileName = row?.OriginalFileName,
                    FechaVencimiento = row?.FechaVencimiento,
                    DiasAntesDefault = t.DiasAntesDefault,
                    Recordatorios = recordatoriosDeEsta,
                    VersionesHistorial = row != null && historialCounts.TryGetValue(row.VecinoLicenciaControlId, out var c) ? c : 0,
                };
            }).ToList();

            return new VecinoLicenciaPlantillaResponseDto
            {
                Items = items,
                Estados = estados.Select(e => new CatalogOptionDto { Id = e.VecinoLicenciaControlEstadoId, Descripcion = e.Descripcion }).ToList(),
            };
        }

        public async Task<bool> TipoAplicaAProyecto(int projectId, int tipoId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.VecinoLicenciaControlTipo
                .AnyAsync(t => t.VecinoLicenciaControlTipoId == tipoId && t.State && t.Active
                    && (t.ProjectId == null || t.ProjectId == projectId));
        }

        public Task<VecinoLicenciaTipoDto> AddTipo(int projectId, string descripcion, int? diasAntesDefault, int userId)
            => CrearTipo(projectId, descripcion, diasAntesDefault, userId);

        public Task<VecinoLicenciaTipoDto> AddTipoBase(string descripcion, int? diasAntesDefault, int userId)
            => CrearTipo(null, descripcion, diasAntesDefault, userId);

        private async Task<VecinoLicenciaTipoDto> CrearTipo(int? projectId, string descripcion, int? diasAntesDefault, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var now = DateTime.UtcNow;

            var maxOrden = await ctx.VecinoLicenciaControlTipo
                .Where(t => t.State && (t.ProjectId == null || t.ProjectId == projectId))
                .Select(t => (int?)t.Orden)
                .MaxAsync() ?? 0;

            var tipo = new VecinoLicenciaControlTipo
            {
                ProjectId = projectId,
                Descripcion = descripcion,
                Orden = maxOrden + 1,
                DiasAntesDefault = diasAntesDefault,
                CreatedDateTime = now,
                CreatedUserId = userId,
                Active = true,
                State = true,
            };
            ctx.VecinoLicenciaControlTipo.Add(tipo);
            await ctx.SaveChangesAsync();

            return new VecinoLicenciaTipoDto
            {
                VecinoLicenciaControlTipoId = tipo.VecinoLicenciaControlTipoId,
                Descripcion = tipo.Descripcion,
                Orden = tipo.Orden,
                EsBase = tipo.ProjectId == null,
                DiasAntesDefault = tipo.DiasAntesDefault,
            };
        }

        public async Task<List<VecinoLicenciaTipoDto>> GetCatalogoBase()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.VecinoLicenciaControlTipo
                .Where(t => t.State && t.Active && t.ProjectId == null)
                .OrderBy(t => t.Orden)
                .Select(t => new VecinoLicenciaTipoDto
                {
                    VecinoLicenciaControlTipoId = t.VecinoLicenciaControlTipoId,
                    Descripcion = t.Descripcion,
                    Orden = t.Orden,
                    EsBase = true,
                    DiasAntesDefault = t.DiasAntesDefault,
                })
                .ToListAsync();
        }

        public async Task<VecinoLicenciaTipoDto> UpdateTipo(int tipoId, string descripcion, int? diasAntesDefault, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var tipo = await ctx.VecinoLicenciaControlTipo
                .FirstOrDefaultAsync(t => t.VecinoLicenciaControlTipoId == tipoId && t.State);
            if (tipo is null)
                throw new InvalidOperationException("El tipo de licencia no existe.");

            tipo.Descripcion = descripcion;
            tipo.DiasAntesDefault = diasAntesDefault;
            tipo.UpdatedDateTime = DateTime.UtcNow;
            tipo.UpdatedUserId = userId;
            await ctx.SaveChangesAsync();

            return new VecinoLicenciaTipoDto
            {
                VecinoLicenciaControlTipoId = tipo.VecinoLicenciaControlTipoId,
                Descripcion = tipo.Descripcion,
                Orden = tipo.Orden,
                EsBase = tipo.ProjectId == null,
                DiasAntesDefault = tipo.DiasAntesDefault,
            };
        }

        public async Task DeleteTipo(int tipoId, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var tipo = await ctx.VecinoLicenciaControlTipo
                .FirstOrDefaultAsync(t => t.VecinoLicenciaControlTipoId == tipoId);
            if (tipo is null) return;

            tipo.State = false;
            tipo.UpdatedDateTime = DateTime.UtcNow;
            tipo.UpdatedUserId = userId;
            await ctx.SaveChangesAsync();
        }

        /// <summary>Obtiene la fila vigente (proyecto + tipo) o crea una nueva en el contexto.</summary>
        private static async Task<VecinoLicenciaControl> GetOrCreateRow(AppDbContext ctx, int projectId, int tipoId, int userId, int estadoIdSiNueva)
        {
            var row = await ctx.VecinoLicenciaControl
                .FirstOrDefaultAsync(r => r.ProjectId == projectId && r.VecinoLicenciaControlTipoId == tipoId && r.State);

            if (row == null)
            {
                row = new VecinoLicenciaControl
                {
                    ProjectId = projectId,
                    VecinoLicenciaControlTipoId = tipoId,
                    VecinoLicenciaControlEstadoId = estadoIdSiNueva,
                    CreatedDateTime = DateTime.UtcNow,
                    CreatedUserId = userId,
                    Active = true,
                    State = true,
                };
                ctx.VecinoLicenciaControl.Add(row);
            }
            else
            {
                row.UpdatedDateTime = DateTime.UtcNow;
                row.UpdatedUserId = userId;
            }

            return row;
        }

        public async Task UploadLicencia(int projectId, int tipoId, string archivoUrl, string? originalFileName,
            DateOnly fechaVencimiento, List<int> diasAntesRecordatorio, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var now = DateTime.UtcNow;

            var cargadoId = await ctx.VecinoLicenciaControlEstado
                .Where(e => e.Descripcion == "Cargado" && e.State)
                .Select(e => e.VecinoLicenciaControlEstadoId)
                .FirstOrDefaultAsync();

            var row = await GetOrCreateRow(ctx, projectId, tipoId, userId, cargadoId);
            var esReemplazo = row.VecinoLicenciaControlId != 0 && !string.IsNullOrEmpty(row.ArchivoUrl);

            // Si ya había un archivo vigente, se archiva como versión anterior antes de sobrescribir
            // (el recordatorio de esa versión se guarda como referencia: el primero de los que tenía).
            if (esReemplazo)
            {
                var recordatorioPrevio = await ctx.VecinoLicenciaControlRecordatorio
                    .Where(r => r.VecinoLicenciaControlId == row.VecinoLicenciaControlId && r.State)
                    .OrderBy(r => r.DiasAntes)
                    .FirstOrDefaultAsync();

                ctx.VecinoLicenciaControlHistorial.Add(new VecinoLicenciaControlHistorial
                {
                    VecinoLicenciaControlId = row.VecinoLicenciaControlId,
                    ArchivoUrl = row.ArchivoUrl!,
                    OriginalFileName = row.OriginalFileName,
                    FechaVencimiento = row.FechaVencimiento,
                    FechaRecordatorio = recordatorioPrevio?.FechaRecordatorio,
                    DiasAntes = recordatorioPrevio?.DiasAntes,
                    Motivo = "Reemplazado por un documento actualizado",
                    CreatedDateTime = now,
                    CreatedUserId = userId,
                    Active = true,
                    State = true,
                });
            }

            row.ArchivoUrl = archivoUrl;
            row.OriginalFileName = originalFileName;
            row.FechaVencimiento = fechaVencimiento;
            row.VecinoLicenciaControlEstadoId = cargadoId; // subir archivo ⇒ Cargado (sobrescribe "No aplica").
            await ctx.SaveChangesAsync(); // asegura VecinoLicenciaControlId si la fila es nueva.

            // Documento nuevo ⇒ los recordatorios anteriores quedan obsoletos; se reemplazan por los nuevos.
            var previos = await ctx.VecinoLicenciaControlRecordatorio
                .Where(r => r.VecinoLicenciaControlId == row.VecinoLicenciaControlId && r.State)
                .ToListAsync();
            foreach (var previo in previos)
                previo.State = false;

            foreach (var dias in diasAntesRecordatorio.Distinct())
            {
                ctx.VecinoLicenciaControlRecordatorio.Add(new VecinoLicenciaControlRecordatorio
                {
                    VecinoLicenciaControlId = row.VecinoLicenciaControlId,
                    DiasAntes = dias,
                    FechaRecordatorio = fechaVencimiento.AddDays(-dias),
                    CreatedDateTime = now,
                    CreatedUserId = userId,
                    Active = true,
                    State = true,
                });
            }

            await ctx.SaveChangesAsync();
        }

        public async Task<VecinoLicenciaRecordatorioDto> AddRecordatorio(int projectId, int tipoId, int diasAntes, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var row = await ctx.VecinoLicenciaControl
                .FirstOrDefaultAsync(r => r.ProjectId == projectId && r.VecinoLicenciaControlTipoId == tipoId && r.State);
            if (row is null || row.FechaVencimiento is null)
                throw new InvalidOperationException("Primero sube el documento con su fecha de vencimiento.");

            var recordatorio = new VecinoLicenciaControlRecordatorio
            {
                VecinoLicenciaControlId = row.VecinoLicenciaControlId,
                DiasAntes = diasAntes,
                FechaRecordatorio = row.FechaVencimiento.Value.AddDays(-diasAntes),
                CreatedDateTime = DateTime.UtcNow,
                CreatedUserId = userId,
                Active = true,
                State = true,
            };
            ctx.VecinoLicenciaControlRecordatorio.Add(recordatorio);
            await ctx.SaveChangesAsync();

            return new VecinoLicenciaRecordatorioDto
            {
                VecinoLicenciaControlRecordatorioId = recordatorio.VecinoLicenciaControlRecordatorioId,
                DiasAntes = recordatorio.DiasAntes,
                FechaRecordatorio = recordatorio.FechaRecordatorio,
                Enviado = false,
            };
        }

        public async Task DeleteRecordatorio(int recordatorioId, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var recordatorio = await ctx.VecinoLicenciaControlRecordatorio
                .FirstOrDefaultAsync(r => r.VecinoLicenciaControlRecordatorioId == recordatorioId);
            if (recordatorio is null) return;

            recordatorio.State = false;
            await ctx.SaveChangesAsync();
        }

        public async Task SetNoAplica(int projectId, int tipoId, bool noAplica, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var estados = await ctx.VecinoLicenciaControlEstado
                .Where(e => e.State)
                .Select(e => new { e.VecinoLicenciaControlEstadoId, e.Descripcion })
                .ToListAsync();
            int noAplicaId = estados.First(e => e.Descripcion == "No aplica").VecinoLicenciaControlEstadoId;
            int cargadoId = estados.First(e => e.Descripcion == "Cargado").VecinoLicenciaControlEstadoId;
            int pendienteId = estados.First(e => e.Descripcion == "Pendiente").VecinoLicenciaControlEstadoId;

            var row = await GetOrCreateRow(ctx, projectId, tipoId, userId, pendienteId);
            row.VecinoLicenciaControlEstadoId = noAplica
                ? noAplicaId
                : (string.IsNullOrEmpty(row.ArchivoUrl) ? pendienteId : cargadoId);

            await ctx.SaveChangesAsync();
        }

        public async Task<List<VecinoLicenciaHistorialItemDto>> GetHistorial(int projectId, int tipoId)
        {
            using var ctx = _factory.CreateDbContext();

            var row = await ctx.VecinoLicenciaControl
                .FirstOrDefaultAsync(r => r.ProjectId == projectId && r.VecinoLicenciaControlTipoId == tipoId && r.State);
            if (row == null) return new List<VecinoLicenciaHistorialItemDto>();

            var historial = await ctx.VecinoLicenciaControlHistorial
                .Where(h => h.VecinoLicenciaControlId == row.VecinoLicenciaControlId && h.State)
                .OrderByDescending(h => h.CreatedDateTime)
                .ToListAsync();

            var userIds = historial.Select(h => h.CreatedUserId).Distinct().ToList();
            var nombres = await ctx.Person
                .Where(p => p.UserId != null && userIds.Contains(p.UserId!.Value))
                .Select(p => new { p.UserId, p.FullName })
                .ToListAsync();
            var nombreDeUser = nombres
                .Where(n => n.UserId.HasValue)
                .GroupBy(n => n.UserId!.Value)
                .ToDictionary(g => g.Key, g => g.First().FullName);

            return historial.Select(h => new VecinoLicenciaHistorialItemDto
            {
                VecinoLicenciaControlHistorialId = h.VecinoLicenciaControlHistorialId,
                ArchivoUrl = h.ArchivoUrl,
                OriginalFileName = h.OriginalFileName,
                FechaVencimiento = h.FechaVencimiento,
                FechaRecordatorio = h.FechaRecordatorio,
                DiasAntes = h.DiasAntes,
                Motivo = h.Motivo,
                CreatedDateTime = h.CreatedDateTime,
                CreatedUserName = nombreDeUser.TryGetValue(h.CreatedUserId, out var n) ? n : null,
            }).ToList();
        }

        /// <summary>
        /// Residente, Coordinador SSOMA y Administración de un proyecto, resueltos desde su
        /// propia ficha (project.residente_workers_id → workers.email_corporativo,
        /// project.email_coord_ssoma, project.email_coord_admin) — mismo criterio que EMOs:
        /// nunca un correo escrito a mano, siempre el dato maestro del proyecto.
        /// </summary>
        private async Task<List<VecinoLicenciaDestinatarioAutomaticoDto>> ResolverAutomaticos(AppDbContext ctx, int projectId)
        {
            var proyecto = await (
                from p in ctx.Project.AsNoTracking()
                where p.ProjectId == projectId
                join rw in ctx.Worker.AsNoTracking() on p.ResidenteWorkersId equals rw.Id into rwj
                from residente in rwj.DefaultIfEmpty()
                select new
                {
                    EmailResidente = residente != null ? residente.EmailCorporativo : null,
                    p.EmailCoordSsoma,
                    p.EmailCoordAdmin,
                })
                .FirstOrDefaultAsync();

            return new List<VecinoLicenciaDestinatarioAutomaticoDto>
            {
                new() { Rol = "Residente", Email = proyecto?.EmailResidente },
                new() { Rol = "Coordinador SSOMA", Email = proyecto?.EmailCoordSsoma },
                new() { Rol = "Administración", Email = proyecto?.EmailCoordAdmin },
            };
        }

        public async Task<List<string>> ResolverDestinatariosAutomaticos(int projectId)
        {
            using var ctx = _factory.CreateDbContext();
            var automaticos = await ResolverAutomaticos(ctx, projectId);
            return automaticos
                .Where(a => !string.IsNullOrWhiteSpace(a.Email))
                .Select(a => a.Email!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<List<string>> GetDestinatariosAdicionales(int projectId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.VecinoLicenciaControlDestinatario
                .Where(d => d.ProjectId == projectId && d.State && d.Active)
                .Select(d => d.Email)
                .ToListAsync();
        }

        public async Task<VecinoLicenciaDestinatariosResponseDto> GetDestinatarios(int projectId)
        {
            using var ctx = _factory.CreateDbContext();

            var automaticos = await ResolverAutomaticos(ctx, projectId);

            var adicionales = await ctx.VecinoLicenciaControlDestinatario
                .Where(d => d.ProjectId == projectId && d.State && d.Active)
                .OrderBy(d => d.Rol)
                .Select(d => new VecinoLicenciaDestinatarioDto
                {
                    VecinoLicenciaControlDestinatarioId = d.VecinoLicenciaControlDestinatarioId,
                    Rol = d.Rol,
                    Email = d.Email,
                })
                .ToListAsync();

            return new VecinoLicenciaDestinatariosResponseDto { Automaticos = automaticos, Adicionales = adicionales };
        }

        public async Task<VecinoLicenciaDestinatarioDto> AddDestinatario(int projectId, string rol, string email, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var now = DateTime.UtcNow;

            var entity = new VecinoLicenciaControlDestinatario
            {
                ProjectId = projectId,
                Rol = rol,
                Email = email,
                CreatedDateTime = now,
                CreatedUserId = userId,
                Active = true,
                State = true,
            };
            ctx.VecinoLicenciaControlDestinatario.Add(entity);
            await ctx.SaveChangesAsync();

            return new VecinoLicenciaDestinatarioDto
            {
                VecinoLicenciaControlDestinatarioId = entity.VecinoLicenciaControlDestinatarioId,
                Rol = entity.Rol,
                Email = entity.Email,
            };
        }

        public async Task DeleteDestinatario(int destinatarioId, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var entity = await ctx.VecinoLicenciaControlDestinatario
                .FirstOrDefaultAsync(d => d.VecinoLicenciaControlDestinatarioId == destinatarioId);
            if (entity is null) return;

            entity.State = false;
            entity.UpdatedDateTime = DateTime.UtcNow;
            entity.UpdatedUserId = userId;
            await ctx.SaveChangesAsync();
        }

        /// <summary>
        /// Si la fecha de recordatorio cae sábado o domingo, se adelanta al viernes anterior:
        /// nadie revisa el correo en fin de semana, así que el aviso debe salir en día hábil.
        /// </summary>
        private static DateOnly FechaEfectivaEnvio(DateOnly fecha) => fecha.DayOfWeek switch
        {
            DayOfWeek.Saturday => fecha.AddDays(-1),
            DayOfWeek.Sunday => fecha.AddDays(-2),
            _ => fecha,
        };

        public async Task<List<VecinoLicenciaRecordatorioPendienteDto>> GetPendientesRecordatorio(DateOnly hoy)
        {
            using var ctx = _factory.CreateDbContext();

            // Se trae un rango un poco más amplio (hasta 2 días adelante) porque un recordatorio
            // del sábado o domingo próximo ya debe salir hoy si hoy es el viernes anterior.
            var candidatos = await (
                from rec in ctx.VecinoLicenciaControlRecordatorio
                where rec.State && rec.Active && rec.EnviadoDateTime == null
                    && rec.FechaRecordatorio <= hoy.AddDays(2)
                join lic in ctx.VecinoLicenciaControl on rec.VecinoLicenciaControlId equals lic.VecinoLicenciaControlId
                where lic.State && lic.Active && lic.ArchivoUrl != null
                join t in ctx.VecinoLicenciaControlTipo on lic.VecinoLicenciaControlTipoId equals t.VecinoLicenciaControlTipoId
                select new
                {
                    rec.VecinoLicenciaControlRecordatorioId,
                    rec.DiasAntes,
                    rec.FechaRecordatorio,
                    lic.ProjectId,
                    t.Descripcion,
                    FechaVencimiento = lic.FechaVencimiento!.Value,
                })
                .ToListAsync();

            return candidatos
                .Where(p => FechaEfectivaEnvio(p.FechaRecordatorio) <= hoy)
                .Select(p => new VecinoLicenciaRecordatorioPendienteDto
                {
                    VecinoLicenciaControlRecordatorioId = p.VecinoLicenciaControlRecordatorioId,
                    ProjectId = p.ProjectId,
                    TipoDescripcion = p.Descripcion,
                    FechaVencimiento = p.FechaVencimiento,
                    DiasAntes = p.DiasAntes,
                })
                .ToList();
        }

        public async Task MarcarRecordatorioEnviado(int recordatorioId)
        {
            using var ctx = _factory.CreateDbContext();
            var recordatorio = await ctx.VecinoLicenciaControlRecordatorio
                .FirstOrDefaultAsync(r => r.VecinoLicenciaControlRecordatorioId == recordatorioId);
            if (recordatorio is null) return;
            recordatorio.EnviadoDateTime = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }
    }
}
