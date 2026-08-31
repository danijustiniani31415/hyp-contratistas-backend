using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.SctrVidaley;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Helpers;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Abril_Backend.Shared.Constants;
using Abril_Backend.Shared.Helpers;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Repositories
{
    public class SctrVidaLeyRepository : ISctrVidaLeyRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly ILogger<SctrVidaLeyRepository> _logger;
        private readonly ISharePointHabService _sharePoint;

        public SctrVidaLeyRepository(
            IDbContextFactory<AppDbContext> factory,
            ILogger<SctrVidaLeyRepository> logger,
            ISharePointHabService sharePoint)
        {
            _factory = factory;
            _logger = logger;
            _sharePoint = sharePoint;
        }

        public async Task<(List<SctrVidaLeyDto> Items, int Total)> GetPagedAsync(
            int? empresaId, int? proyectoId, string? tipo,
            int? mes, int? anio, string? estado, int page, int pageSize)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ctx.SsSctrVidaley.AsQueryable();

            if (empresaId.HasValue) query = query.Where(s => s.EmpresaId == empresaId.Value);
            if (proyectoId.HasValue) query = query.Where(s => s.ProyectoId == proyectoId.Value);
            if (!string.IsNullOrWhiteSpace(tipo)) query = query.Where(s => s.Tipo == tipo);
            if (mes.HasValue) query = query.Where(s => s.Mes == mes.Value);
            if (anio.HasValue) query = query.Where(s => s.Anio == anio.Value);
            if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(s => s.Estado == estado);

            var total = await query.CountAsync();

            var pageRows = await query
                .OrderByDescending(s => s.Anio)
                .ThenByDescending(s => s.Mes)
                .ThenByDescending(s => s.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = await BuildDtosAsync(ctx, pageRows);
            return (items, total);
        }

        public async Task<SctrVidaLeyDto?> GetByIdAsync(int id)
        {
            using var ctx = _factory.CreateDbContext();
            var entity = await ctx.SsSctrVidaley.FirstOrDefaultAsync(s => s.Id == id);
            if (entity is null) return null;
            var list = await BuildDtosAsync(ctx, new List<SsSctrVidaley> { entity });
            return list.FirstOrDefault();
        }

        public async Task<SctrVidaLeyDto> CreateAsync(SctrVidaLeyCreateDto dto, int empresaId)
        {
            using var ctx = _factory.CreateDbContext();

            var fechaRef = dto.FechaInicio ?? DateTime.UtcNow;
            var mes = dto.Mes != 0 ? dto.Mes : fechaRef.Month;
            var anio = dto.Anio != 0 ? dto.Anio : fechaRef.Year;

            var entity = new SsSctrVidaley
            {
                EmpresaId = empresaId,
                ProyectoId = dto.ProyectoId,
                Tipo = dto.Tipo,
                TipoPoliza = string.IsNullOrWhiteSpace(dto.TipoPoliza) ? "Renovacion" : dto.TipoPoliza,
                FechaInicio = dto.FechaInicio.HasValue ? DateTime.SpecifyKind(dto.FechaInicio.Value, DateTimeKind.Utc) : null,
                Mes = mes,
                Anio = anio,
                ArchivoUrl = dto.ArchivoUrl,
                ArchivoUrl2 = dto.ArchivoUrl2,
                Vigencia = dto.Vigencia.HasValue ? DateTime.SpecifyKind(dto.Vigencia.Value, DateTimeKind.Utc) : null,
                Estado = "Enviado",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            ctx.SsSctrVidaley.Add(entity);
            await ctx.SaveChangesAsync();

            var workersDistinct = dto.Workers.GroupBy(w => w.WorkerId).Select(g => g.First()).ToList();

            foreach (var workerInput in workersDistinct)
            {
                ctx.SsSctrVidaLeyWorker.Add(new SsSctrVidaLeyWorker
                {
                    SctrVidaLeyId = entity.Id,
                    WorkerId = workerInput.WorkerId,
                    FechaInicioCobertura = workerInput.FechaInicioCobertura.HasValue
                        ? DateTime.SpecifyKind(workerInput.FechaInicioCobertura.Value, DateTimeKind.Utc)
                        : null
                });
            }
            await ctx.SaveChangesAsync();

            var esAbril = await ctx.Contributor
                .Where(c => c.ContributorId == empresaId)
                .Select(c => c.EsAbril)
                .FirstOrDefaultAsync();

            var estadoHab = esAbril ? "Aprobado" : "Enviado";
            var vigenciaHab = dto.Vigencia.HasValue
                ? DateTime.SpecifyKind(dto.Vigencia.Value, DateTimeKind.Utc)
                : (DateTime?)null;

            var itemNombreBuscar = dto.Tipo == "VIDA_LEY" ? "Vida" : "SCTR";
            var sctrItems = await ctx.SsItemTrabajador
                .Where(i => i.EsSctrVidaley && i.Activo)
                .ToListAsync();
            var item = sctrItems.FirstOrDefault(i =>
                i.Nombre.Contains(itemNombreBuscar, StringComparison.OrdinalIgnoreCase));

            if (item is not null)
            {
                var hoyUtc = DateTime.UtcNow.Date;
                var estadosResultantes = new List<string>(workersDistinct.Count);
                foreach (var workerInput in workersDistinct)
                {
                    if (!esAbril && dto.TipoPoliza == "Renovacion")
                    {
                        var habVigente = await ctx.SsHabTrabajador
                            .FirstOrDefaultAsync(h => h.WorkerId == workerInput.WorkerId
                                                   && h.ItemId == item.Id
                                                   && h.Estado == "Aprobado"
                                                   && h.Vigencia >= hoyUtc);
                        if (habVigente != null)
                        {
                            habVigente.Estado = "En revision";
                            habVigente.Vigencia = dto.Vigencia.HasValue
                                ? DateTime.SpecifyKind(dto.Vigencia.Value, DateTimeKind.Utc)
                                : habVigente.Vigencia;
                            habVigente.ArchivoUrl = dto.ArchivoUrl;
                            habVigente.UpdatedAt = DateTime.UtcNow;
                            estadosResultantes.Add("En revision");
                            continue;
                        }
                    }

                    var hab = await ctx.SsHabTrabajador
                        .FirstOrDefaultAsync(h => h.WorkerId == workerInput.WorkerId && h.ItemId == item.Id);

                    if (hab is null)
                    {
                        hab = new SsHabTrabajador
                        {
                            WorkerId = workerInput.WorkerId,
                            ItemId = item.Id,
                            Estado = estadoHab,
                            Vigencia = vigenciaHab,
                            ArchivoUrl = dto.ArchivoUrl,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        ctx.SsHabTrabajador.Add(hab);
                        estadosResultantes.Add(estadoHab);
                    }
                    else
                    {
                        // Para empresas Abril siempre actualizar (auto-aprobado, renovación directa).
                        // Para contratistas solo sobreescribir si el estado es inferior.
                        if (esAbril || hab.Estado == "Falta" || hab.Estado == "Rechazado" || string.IsNullOrEmpty(hab.Estado))
                        {
                            hab.Estado = estadoHab;
                            hab.Vigencia = vigenciaHab;
                            hab.ArchivoUrl = dto.ArchivoUrl;
                            hab.UpdatedAt = DateTime.UtcNow;
                        }
                        estadosResultantes.Add(hab.Estado);
                    }
                }

                if (!esAbril)
                {
                    var hayEnviados   = estadosResultantes.Any(e => e == "Enviado");
                    var hayEnRevision = estadosResultantes.Any(e => e == "En revision");
                    entity.Estado = hayEnviados   ? "Enviado"
                                  : hayEnRevision ? "En revision"
                                  : "Aprobado";
                }

                await ctx.SaveChangesAsync();
            }

            if (esAbril)
            {
                entity.Estado = "Aprobado";

                if (entity.ProyectoId.HasValue)
                {
                    var itemEmpresaId = dto.Tipo == "VIDA_LEY" ? 16 : 15;
                    var habEmpresa = await ctx.SsHabEmpresa
                        .FirstOrDefaultAsync(h => h.EmpresaId == entity.EmpresaId
                                               && h.ProyectoId == entity.ProyectoId.Value
                                               && h.ItemId == itemEmpresaId);
                    if (habEmpresa is null)
                    {
                        ctx.SsHabEmpresa.Add(new SsHabEmpresa
                        {
                            EmpresaId = entity.EmpresaId,
                            ProyectoId = entity.ProyectoId.Value,
                            ItemId = itemEmpresaId,
                            Mes = entity.Mes,
                            Anio = entity.Anio,
                            Estado = "Aprobado",
                            Vigencia = vigenciaHab,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        habEmpresa.Estado = "Aprobado";
                        habEmpresa.Vigencia = vigenciaHab;
                        habEmpresa.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await ctx.SaveChangesAsync();
            }

            var dtos = await BuildDtosAsync(ctx, new List<SsSctrVidaley> { entity });
            return dtos.First();
        }

        public async Task<SctrVidaLeyDto> UpdateAsync(int id, SctrVidaLeyCreateDto dto, int empresaId)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = await ctx.SsSctrVidaley.FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new AbrilException("SCTR/VidaLey no encontrado.", 404);

            if (entity.EmpresaId != empresaId)
                throw new AbrilException("No tiene permiso para editar esta póliza.", 403);

            var esAbril = await ctx.Contributor
                .Where(c => c.ContributorId == empresaId)
                .Select(c => c.EsAbril)
                .FirstOrDefaultAsync();

            var archivoReemplazado = !string.IsNullOrWhiteSpace(dto.ArchivoUrl)
                && dto.ArchivoUrl != entity.ArchivoUrl;

            entity.Tipo = dto.Tipo;
            entity.TipoPoliza = string.IsNullOrWhiteSpace(dto.TipoPoliza) ? entity.TipoPoliza : dto.TipoPoliza;
            entity.FechaInicio = dto.FechaInicio.HasValue
                ? DateTime.SpecifyKind(dto.FechaInicio.Value, DateTimeKind.Utc)
                : entity.FechaInicio;
            entity.Mes = dto.Mes;
            entity.Anio = dto.Anio;
            entity.ArchivoUrl = dto.ArchivoUrl;
            entity.ArchivoUrl2 = dto.ArchivoUrl2;
            entity.UpdatedAt = DateTime.UtcNow;

            if (archivoReemplazado && entity.Estado == "Aprobado" && !esAbril)
                entity.Estado = "Enviado";

            // Workers: calcular delta
            var workersDistinct = dto.Workers.GroupBy(w => w.WorkerId).Select(g => g.First()).ToList();
            var workerIdsNuevos = workersDistinct.Select(w => w.WorkerId).ToHashSet();

            var workersActuales = await ctx.SsSctrVidaLeyWorker
                .Where(w => w.SctrVidaLeyId == id)
                .ToListAsync();

            var workerIdsActuales = workersActuales.Select(w => w.WorkerId).ToHashSet();
            var workerIdsAgregar = workerIdsNuevos.Except(workerIdsActuales).ToList();
            var workerIdsQuitar = workerIdsActuales.Except(workerIdsNuevos).ToList();

            // Quitar workers
            if (workerIdsQuitar.Count > 0)
            {
                var aEliminar = workersActuales.Where(w => workerIdsQuitar.Contains(w.WorkerId)).ToList();
                ctx.SsSctrVidaLeyWorker.RemoveRange(aEliminar);
            }

            // Agregar workers nuevos
            foreach (var workerInput in workersDistinct.Where(w => workerIdsAgregar.Contains(w.WorkerId)))
            {
                ctx.SsSctrVidaLeyWorker.Add(new SsSctrVidaLeyWorker
                {
                    SctrVidaLeyId = id,
                    WorkerId = workerInput.WorkerId,
                    FechaInicioCobertura = workerInput.FechaInicioCobertura.HasValue
                        ? DateTime.SpecifyKind(workerInput.FechaInicioCobertura.Value, DateTimeKind.Utc)
                        : null
                });
            }

            // Actualizar FechaInicioCobertura de workers existentes si cambió
            foreach (var workerActual in workersActuales.Where(w => workerIdsNuevos.Contains(w.WorkerId)))
            {
                var input = workersDistinct.First(w => w.WorkerId == workerActual.WorkerId);
                workerActual.FechaInicioCobertura = input.FechaInicioCobertura.HasValue
                    ? DateTime.SpecifyKind(input.FechaInicioCobertura.Value, DateTimeKind.Utc)
                    : workerActual.FechaInicioCobertura;
            }

            await ctx.SaveChangesAsync();

            var itemNombreBuscar = dto.Tipo == "VIDA_LEY" ? "Vida" : "SCTR";
            var sctrItems = await ctx.SsItemTrabajador
                .Where(i => i.EsSctrVidaley && i.Activo)
                .ToListAsync();
            var item = sctrItems.FirstOrDefault(i =>
                i.Nombre.Contains(itemNombreBuscar, StringComparison.OrdinalIgnoreCase));

            if (item is not null)
            {
                // Propagar estado a ss_hab_trabajador para nuevos workers
                foreach (var workerInput in workersDistinct.Where(w => workerIdsAgregar.Contains(w.WorkerId)))
                {
                    var hab = await ctx.SsHabTrabajador
                        .FirstOrDefaultAsync(h => h.WorkerId == workerInput.WorkerId && h.ItemId == item.Id);

                    var estadoNuevoWorker = esAbril ? "Aprobado" : "Enviado";
                    if (hab is null)
                    {
                        ctx.SsHabTrabajador.Add(new SsHabTrabajador
                        {
                            WorkerId = workerInput.WorkerId,
                            ItemId = item.Id,
                            Estado = estadoNuevoWorker,
                            ArchivoUrl = dto.ArchivoUrl,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        // Solo sobreescribir si el estado actual es inferior
                        // Nunca degradar "Aprobado", "En revision", "En plazo"
                        if (hab.Estado == "Falta" || hab.Estado == "Rechazado" || string.IsNullOrEmpty(hab.Estado))
                        {
                            hab.Estado = estadoNuevoWorker;
                            hab.ArchivoUrl = dto.ArchivoUrl;
                            hab.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                // Quitar workers → Estado=Falta + limpiar ArchivoUrl
                if (workerIdsQuitar.Count > 0)
                {
                    var habsQuitar = await ctx.SsHabTrabajador
                        .Where(h => h.ItemId == item.Id && workerIdsQuitar.Contains(h.WorkerId))
                        .ToListAsync();

                    foreach (var hab in habsQuitar)
                    {
                        hab.Estado = "Falta";
                        hab.ArchivoUrl = null;
                        hab.Vigencia = null;
                        hab.UpdatedAt = DateTime.UtcNow;
                    }
                }

                // Si se reemplazó archivo y la póliza volvió a Enviado, actualizar estado en ss_hab_trabajador
                if (archivoReemplazado && !esAbril)
                {
                    var habsExistentes = await ctx.SsHabTrabajador
                        .Where(h => h.ItemId == item.Id && workerIdsNuevos.Contains(h.WorkerId))
                        .ToListAsync();

                    foreach (var hab in habsExistentes)
                    {
                        if (hab.Estado == "Aprobado")
                        {
                            hab.Estado = "Enviado";
                            hab.ArchivoUrl = dto.ArchivoUrl;
                            hab.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                await ctx.SaveChangesAsync();

                // Los workers agregados/quitados/reenviados pueden pertenecer también a
                // otras pólizas (otros meses); recalcular esas para que no queden con
                // Estado desactualizado.
                var workersAfectadosHab = workerIdsAgregar
                    .Concat(workerIdsQuitar)
                    .Concat(archivoReemplazado ? workerIdsNuevos : Enumerable.Empty<int>())
                    .Distinct()
                    .ToList();
                if (workersAfectadosHab.Count > 0)
                {
                    await RecalcularPolizasPorWorkersAsync(ctx, workersAfectadosHab);
                }

                // Si la póliza se quedó sin trabajadores, no hay nada pendiente:
                // no debe quedar en "Enviado"/"En revision".
                if (workerIdsNuevos.Count == 0 && (entity.Estado == "Enviado" || entity.Estado == "En revision"))
                {
                    entity.Estado = "Aprobado";
                    entity.UpdatedAt = DateTime.UtcNow;
                    await ctx.SaveChangesAsync();
                }
            }

            if (esAbril)
            {
                entity.Estado = "Aprobado";

                if (entity.ProyectoId.HasValue)
                {
                    var vigenciaHab = dto.Vigencia.HasValue
                        ? DateTime.SpecifyKind(dto.Vigencia.Value, DateTimeKind.Utc)
                        : (DateTime?)null;
                    var itemEmpresaId = dto.Tipo == "VIDA_LEY" ? 16 : 15;
                    var habEmpresa = await ctx.SsHabEmpresa
                        .FirstOrDefaultAsync(h => h.EmpresaId == entity.EmpresaId
                                               && h.ProyectoId == entity.ProyectoId.Value
                                               && h.ItemId == itemEmpresaId);
                    if (habEmpresa is null)
                    {
                        ctx.SsHabEmpresa.Add(new SsHabEmpresa
                        {
                            EmpresaId = entity.EmpresaId,
                            ProyectoId = entity.ProyectoId.Value,
                            ItemId = itemEmpresaId,
                            Mes = entity.Mes,
                            Anio = entity.Anio,
                            Estado = "Aprobado",
                            Vigencia = vigenciaHab,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        habEmpresa.Estado = "Aprobado";
                        habEmpresa.Vigencia = vigenciaHab;
                        habEmpresa.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await ctx.SaveChangesAsync();
            }

            var dtos = await BuildDtosAsync(ctx, new List<SsSctrVidaley> { entity });
            return dtos.First();
        }

        public async Task<SctrVidaLeyDto> AprobarAsync(int id, SctrVidaLeyAprobarDto dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = await ctx.SsSctrVidaley.FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new AbrilException("SCTR/VidaLey no encontrado.", 404);

            var itemNombreBuscar = entity.Tipo == "VIDA_LEY" ? "Vida" : "SCTR";
            var sctrItems = await ctx.SsItemTrabajador
                .Where(i => i.EsSctrVidaley && i.Activo)
                .ToListAsync();
            var item = sctrItems.FirstOrDefault(i =>
                i.Nombre.Contains(itemNombreBuscar, StringComparison.OrdinalIgnoreCase));

            var aprobados = dto.WorkerIdsAprobados.Distinct().ToList();
            var rechazados = dto.WorkerIdsRechazados.Distinct().ToList();
            var afectados = aprobados.Concat(rechazados).Distinct().ToList();

            if (item is not null && afectados.Count > 0)
            {
                var habs = await ctx.SsHabTrabajador
                    .Where(h => h.ItemId == item.Id && afectados.Contains(h.WorkerId))
                    .ToListAsync();

                foreach (var workerId in aprobados)
                {
                    var hab = habs.FirstOrDefault(h => h.WorkerId == workerId);
                    if (hab is null)
                    {
                        hab = new SsHabTrabajador
                        {
                            WorkerId = workerId,
                            ItemId = item.Id,
                            CreatedAt = DateTime.UtcNow
                        };
                        ctx.SsHabTrabajador.Add(hab);
                        habs.Add(hab);
                    }
                    hab.Estado = "Aprobado";
                    hab.Vigencia = HabilitacionDateHelper.AsUtc(dto.Vigencia);
                    hab.ObsAbril = dto.ObsAbril;
                    hab.AprobadoPor = userId;
                    hab.FechaAprobacion = DateTime.UtcNow;
                    hab.UpdatedAt = DateTime.UtcNow;
                }

                foreach (var workerId in rechazados)
                {
                    var hab = habs.FirstOrDefault(h => h.WorkerId == workerId);
                    if (hab is null)
                    {
                        hab = new SsHabTrabajador
                        {
                            WorkerId = workerId,
                            ItemId = item.Id,
                            CreatedAt = DateTime.UtcNow
                        };
                        ctx.SsHabTrabajador.Add(hab);
                        habs.Add(hab);
                    }
                    hab.Estado = "Rechazado";
                    hab.ObsAbril = dto.ObsAbril;
                    hab.UpdatedAt = DateTime.UtcNow;
                }
            }

            await ctx.SaveChangesAsync();

            // Estado de la póliza — prioridad: Enviado > En revision > Aprobado
            var workersDePoliza = await ctx.SsSctrVidaLeyWorker
                .Where(w => w.SctrVidaLeyId == id)
                .Select(w => w.WorkerId)
                .ToListAsync();

            string nuevoEstado = "Aprobado";
            if (item is not null && workersDePoliza.Count > 0)
            {
                var countEnviado = await ctx.SsHabTrabajador
                    .Where(h => h.ItemId == item.Id
                             && workersDePoliza.Contains(h.WorkerId)
                             && h.Estado == "Enviado")
                    .CountAsync();

                var countEnRevision = await ctx.SsHabTrabajador
                    .Where(h => h.ItemId == item.Id
                             && workersDePoliza.Contains(h.WorkerId)
                             && h.Estado == "En revision")
                    .CountAsync();

                nuevoEstado = countEnviado > 0 ? "Enviado"
                            : countEnRevision > 0 ? "En revision"
                            : "Aprobado";
            }

            entity.Estado = nuevoEstado;
            entity.Vigencia = HabilitacionDateHelper.AsUtc(dto.Vigencia);
            entity.ObsAbril = dto.ObsAbril;
            entity.UpdatedAt = DateTime.UtcNow;

            await ctx.SaveChangesAsync();

            // Los trabajadores afectados pueden pertenecer también a otras pólizas
            // (otros meses). Recalcular esas también para que no queden con Estado
            // desactualizado (p.ej. "Enviado" cuando ya no hay pendientes reales).
            if (item is not null && afectados.Count > 0)
            {
                await RecalcularPolizasPorWorkersAsync(ctx, afectados);
            }

            var dtos = await BuildDtosAsync(ctx, new List<SsSctrVidaley> { entity });
            return dtos.First();
        }

        public async Task<List<SctrVidaLeyDto>> GetPorTrabajadorAsync(int workerId)
        {
            using var ctx = _factory.CreateDbContext();

            var sctrIds = await ctx.SsSctrVidaLeyWorker
                .Where(w => w.WorkerId == workerId)
                .Select(w => w.SctrVidaLeyId)
                .Distinct()
                .ToListAsync();

            var entities = await ctx.SsSctrVidaley
                .Where(s => sctrIds.Contains(s.Id))
                .OrderByDescending(s => s.Anio)
                .ThenByDescending(s => s.Mes)
                .ToListAsync();

            return await BuildDtosAsync(ctx, entities);
        }

        public async Task<List<SctrVidaLeyDto>> GetProximosVencerAsync(int dias)
        {
            using var ctx = _factory.CreateDbContext();

            var hoy = DateTime.UtcNow.Date;
            var limite = hoy.AddDays(dias);

            var entities = await ctx.SsSctrVidaley
                .Where(s => s.Estado == "Aprobado"
                            && s.Vigencia.HasValue
                            && s.Vigencia.Value >= hoy
                            && s.Vigencia.Value <= limite)
                .OrderBy(s => s.Vigencia)
                .ToListAsync();

            return await BuildDtosAsync(ctx, entities);
        }

        public async Task<List<SctrTrabajadorEstadoDto>> GetTrabajadoresPorEmpresaAsync(
            int? empresaId, int? proyectoId, string? tipo, string? tipoPoliza,
            string? estadoSctr, string? estadoVidaLey, string? obraOficina = null,
            int? areaScopeId = null)
        {
            using var ctx = _factory.CreateDbContext();

            var idsArea = areaScopeId.HasValue ? await ctx.ResolveDescendantsAsync(areaScopeId.Value) : null;

            // Obtener workerIds desde WorkerVinculacion, aplicando solo los filtros que vienen
            var vinculacionQuery = ctx.WorkerVinculacion.Where(v => v.FechaFin == null);
            if (empresaId.HasValue)   vinculacionQuery = vinculacionQuery.Where(v => v.EmpresaId == empresaId.Value);
            if (proyectoId.HasValue)  vinculacionQuery = vinculacionQuery.Where(v => v.ProyectoId == proyectoId.Value);

            var workerIds = await vinculacionQuery
                .Select(v => v.WorkerId)
                .Distinct()
                .ToListAsync();

            // Suplemento: WorkerProyecto (multi-proyecto Casa) solo cuando hay filtro de proyecto
            if (proyectoId.HasValue)
            {
                var wpQuery = ctx.WorkerProyecto
                    .Where(wp => wp.ProyectoId == proyectoId.Value && wp.FechaFin == null);
                if (empresaId.HasValue) wpQuery = wpQuery.Where(wp => wp.EmpresaId == empresaId.Value);

                var idsProyecto = await wpQuery
                    .Select(wp => wp.WorkerId)
                    .Distinct()
                    .ToListAsync();

                workerIds = workerIds.Union(idsProyecto).ToList();
            }

            // Filtro por obraOficina: "OficinaStaff" → solo Oficina Central y Staff
            if (obraOficina == "OficinaStaff" && workerIds.Count > 0)
            {
                var idsOficinaStaff = await ctx.Worker
                    .Where(w => workerIds.Contains(w.Id)
                             && (w.ObraOficinaStaffId == ObraOficinaStaffIds.OficinaCentral
                                 || w.ObraOficinaStaffId == ObraOficinaStaffIds.Staff))
                    .Select(w => w.Id)
                    .ToListAsync();
                workerIds = idsOficinaStaff;
            }

            if (workerIds.Count == 0)
            {
                _logger.LogWarning("[GetTrabajadoresPorEmpresa] Sin workers para empresaId={EmpresaId} proyectoId={ProyectoId}. Retornando lista vacía.", empresaId, proyectoId);
                return [];
            }

            // Items SCTR y VidaLey del catálogo
            var sctrItems = await ctx.SsItemTrabajador
                .Where(i => i.EsSctrVidaley && i.Activo)
                .ToListAsync();

            var itemSctr    = sctrItems.FirstOrDefault(i => i.Nombre.Contains("SCTR", StringComparison.OrdinalIgnoreCase));
            var itemVidaLey = sctrItems.FirstOrDefault(i => i.Nombre.Contains("Vida", StringComparison.OrdinalIgnoreCase));
            int? itemSctrId    = itemSctr?.Id;
            int? itemVidaLeyId = itemVidaLey?.Id;

            _logger.LogInformation("itemSctr: {item}, itemVidaLey: {item2}", itemSctr?.Nombre, itemVidaLey?.Nombre);

            // Parse filtros de estado antes de la query
            var valoresSctr = string.IsNullOrWhiteSpace(estadoSctr) ? null
                : estadoSctr.Replace("%2C", ",").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var valoresVidaLey = string.IsNullOrWhiteSpace(estadoVidaLey) ? null
                : estadoVidaLey.Replace("%2C", ",").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // LEFT JOIN workers + ss_hab_trabajador SCTR + ss_hab_trabajador VidaLey en BD
            // COALESCE equivalente: habX != null ? habX.Estado : "Falta"
            var habQuery =
                from w in ctx.Worker.Where(w => workerIds.Contains(w.Id)
                    && (idsArea == null || (w.AreaScopeId != null && idsArea.Contains(w.AreaScopeId.Value))))
                join p in ctx.Person on w.PersonId equals (int?)p.PersonId into pg
                from person in pg.DefaultIfEmpty()
                join hs in ctx.SsHabTrabajador.Where(h => h.ItemId == itemSctrId)
                    on w.Id equals hs.WorkerId into hsg
                from habSctr in hsg.DefaultIfEmpty()
                join hv in ctx.SsHabTrabajador.Where(h => h.ItemId == itemVidaLeyId)
                    on w.Id equals hv.WorkerId into hvg
                from habVida in hvg.DefaultIfEmpty()
                select new
                {
                    WorkerId       = w.Id,
                    ApellidoNombre = person != null ? person.FullName : null,
                    Dni            = person != null ? person.DocumentIdentityCode : null,
                    ObraOficinaStaffId = w.ObraOficinaStaffId,
                    EstadoSctr     = habSctr != null ? habSctr.Estado : "Falta",
                    EstadoVidaLey  = habVida != null ? habVida.Estado : "Falta",
                    SctrHabId      = habSctr != null ? (int?)habSctr.Id : null,
                    FechaVencimiento = habSctr != null ? habSctr.Vigencia
                                     : habVida != null ? habVida.Vigencia
                                     : (DateTime?)null,
                };

            if (valoresSctr != null)    habQuery = habQuery.Where(x => valoresSctr.Contains(x.EstadoSctr));
            if (valoresVidaLey != null) habQuery = habQuery.Where(x => valoresVidaLey.Contains(x.EstadoVidaLey));

            var rows = await habQuery.ToListAsync();
            if (rows.Count == 0) return [];

            // sctrId de la póliza activa (Enviado/Parcial) más reciente — sobre workers ya filtrados
            var filteredWorkerIds = rows.Select(r => r.WorkerId).Distinct().ToList();
            var sctrIdPorWorker = await (
                from svw in ctx.SsSctrVidaLeyWorker
                join s in ctx.SsSctrVidaley on svw.SctrVidaLeyId equals s.Id
                where filteredWorkerIds.Contains(svw.WorkerId)
                    && (s.Estado == "Enviado" || s.Estado == "Parcial")
                    && (tipo == null || s.Tipo == tipo)
                group svw by svw.WorkerId into g
                select new { WorkerId = g.Key, SctrId = g.Max(x => x.SctrVidaLeyId) }
            ).ToDictionaryAsync(x => x.WorkerId, x => x.SctrId);

            var rowWorkerIds = rows.Select(r => r.WorkerId).ToList();

            var vinculaciones = await ctx.WorkerVinculacion
                .Where(wv => rowWorkerIds.Contains(wv.WorkerId) && wv.FechaFin == null)
                .GroupBy(wv => wv.WorkerId)
                .Select(g => g.OrderByDescending(wv => wv.Id).First())
                .ToListAsync();

            var vinMap = vinculaciones.ToDictionary(v => v.WorkerId);

            var empIds  = vinculaciones.Where(v => v.EmpresaId  != null).Select(v => v.EmpresaId!.Value).Distinct().ToList();
            var proyIds = vinculaciones.Where(v => v.ProyectoId != null).Select(v => v.ProyectoId!.Value).Distinct().ToList();

            var empMap = await ctx.Contributor
                .Where(c => empIds.Contains(c.ContributorId))
                .ToDictionaryAsync(c => c.ContributorId, c => c.ContributorName);

            var proyMap = await ctx.Project
                .Where(p => proyIds.Contains(p.ProjectId))
                .ToDictionaryAsync(p => p.ProjectId, p => p.ProjectDescription);

            var result = rows.Select(r =>
            {
                vinMap.TryGetValue(r.WorkerId, out var vin);
                var empNombre  = vin?.EmpresaId  != null && empMap.TryGetValue(vin.EmpresaId.Value,  out var en) ? en : null;
                var proyNombre = vin?.ProyectoId != null && proyMap.TryGetValue(vin.ProyectoId.Value, out var pn) ? pn : null;

                return new SctrTrabajadorEstadoDto
                {
                    WorkerId         = r.WorkerId,
                    ApellidoNombre   = r.ApellidoNombre ?? string.Empty,
                    Dni              = r.Dni ?? string.Empty,
                    ObraOficinaStaffId = r.ObraOficinaStaffId,
                    ObraOficina      = ObraOficinaStaffIds.Nombre(r.ObraOficinaStaffId),
                    SctrId           = sctrIdPorWorker.TryGetValue(r.WorkerId, out var sid) ? sid : null,
                    SctrHabId        = r.SctrHabId,
                    EstadoSctr       = r.EstadoSctr,
                    EstadoVidaLey    = r.EstadoVidaLey,
                    EmpresaNombre    = empNombre,
                    ProyectoNombre   = proyNombre,
                    FechaVencimiento = r.FechaVencimiento,
                };
            }).ToList();

            return result;
        }

        public async Task RecalcularEstadoPolizasAsync()
        {
            using var ctx = _factory.CreateDbContext();

            var polizas = await ctx.SsSctrVidaley.ToListAsync();
            var sctrItems = await ctx.SsItemTrabajador
                .Where(i => i.Nombre.Contains("SCTR") || i.Nombre.Contains("Vida"))
                .ToListAsync();

            foreach (var poliza in polizas)
            {
                var item = sctrItems.FirstOrDefault(i =>
                    poliza.Tipo == "VIDA_LEY" ? i.Nombre.Contains("Vida") : i.Nombre.Contains("SCTR"));
                if (item is null) continue;

                var workerIds = await ctx.SsSctrVidaLeyWorker
                    .Where(w => w.SctrVidaLeyId == poliza.Id)
                    .Select(w => w.WorkerId)
                    .ToListAsync();

                if (!workerIds.Any())
                {
                    // Sin trabajadores no hay nada pendiente: no debe quedar en "Enviado".
                    if (poliza.Estado == "Enviado" || poliza.Estado == "En revision")
                    {
                        poliza.Estado = "Aprobado";
                        poliza.UpdatedAt = DateTime.UtcNow;
                    }
                    continue;
                }

                var pendientes = await ctx.SsHabTrabajador
                    .Where(h => h.ItemId == item.Id
                             && workerIds.Contains(h.WorkerId)
                             && (h.Estado == "Enviado" || h.Estado == "En revision"))
                    .CountAsync();

                poliza.Estado = pendientes > 0 ? "Enviado" : "Aprobado";
                poliza.UpdatedAt = DateTime.UtcNow;
            }

            await ctx.SaveChangesAsync();
        }

        // Recalcula el Estado de TODAS las pólizas (de cualquier mes) que incluyan
        // alguno de estos workers, no solo la póliza que se está editando.
        // Sin esto, aprobar/rechazar un trabajador en la póliza del mes actual deja
        // pólizas viejas (de meses anteriores) con Estado="Enviado" desactualizado,
        // aunque ya no tengan trabajadores realmente pendientes.
        //
        // IMPORTANTE: el item (SCTR vs Vida Ley) se resuelve SEGÚN EL TIPO DE CADA
        // PÓLIZA. Antes se recibía un único itemId y se aplicaba a todas las pólizas
        // por igual; como SCTR y Vida Ley comparten los mismos trabajadores, aprobar
        // una póliza SCTR recalculaba también la póliza Vida Ley (y viceversa) mirando
        // el item equivocado y la marcaba "Aprobado" indebidamente, haciéndola
        // desaparecer de la bandeja "Enviado"/"En revisión".
        private async Task RecalcularPolizasPorWorkersAsync(AppDbContext ctx, List<int> workerIds)
        {
            if (workerIds.Count == 0) return;

            var polizaIds = await ctx.SsSctrVidaLeyWorker
                .Where(w => workerIds.Contains(w.WorkerId))
                .Select(w => w.SctrVidaLeyId)
                .Distinct()
                .ToListAsync();

            if (polizaIds.Count == 0) return;

            var polizas = await ctx.SsSctrVidaley
                .Where(s => polizaIds.Contains(s.Id))
                .ToListAsync();

            var sctrItems = await ctx.SsItemTrabajador
                .Where(i => i.EsSctrVidaley && i.Activo)
                .ToListAsync();

            foreach (var poliza in polizas)
            {
                var item = sctrItems.FirstOrDefault(i =>
                    poliza.Tipo == "VIDA_LEY"
                        ? i.Nombre.Contains("Vida", StringComparison.OrdinalIgnoreCase)
                        : i.Nombre.Contains("SCTR", StringComparison.OrdinalIgnoreCase));
                if (item is null) continue;

                var workersDePoliza = await ctx.SsSctrVidaLeyWorker
                    .Where(w => w.SctrVidaLeyId == poliza.Id)
                    .Select(w => w.WorkerId)
                    .ToListAsync();

                if (workersDePoliza.Count == 0) continue;

                var countEnviado = await ctx.SsHabTrabajador
                    .Where(h => h.ItemId == item.Id
                             && workersDePoliza.Contains(h.WorkerId)
                             && h.Estado == "Enviado")
                    .CountAsync();

                var countEnRevision = await ctx.SsHabTrabajador
                    .Where(h => h.ItemId == item.Id
                             && workersDePoliza.Contains(h.WorkerId)
                             && h.Estado == "En revision")
                    .CountAsync();

                poliza.Estado = countEnviado > 0 ? "Enviado"
                              : countEnRevision > 0 ? "En revision"
                              : "Aprobado";
                poliza.UpdatedAt = DateTime.UtcNow;
            }

            await ctx.SaveChangesAsync();
        }

        private async Task<List<SctrVidaLeyDto>> BuildDtosAsync(
            AppDbContext ctx, List<SsSctrVidaley> entities)
        {
            if (entities.Count == 0) return new List<SctrVidaLeyDto>();

            var ids = entities.Select(e => e.Id).ToList();
            var empresaIds = entities.Select(e => e.EmpresaId).Distinct().ToList();
            var proyectoIds = entities.Select(e => e.ProyectoId).Distinct().ToList();

            var empresaMap = await ctx.Contributor
                .Where(c => empresaIds.Contains(c.ContributorId))
                .ToDictionaryAsync(c => c.ContributorId, c => c.ContributorName);

            var proyectoMap = await ctx.Project
                .Where(p => proyectoIds.Contains(p.ProjectId))
                .ToDictionaryAsync(p => p.ProjectId, p => p.ProjectDescription);

            var workersData = await (from svw in ctx.SsSctrVidaLeyWorker
                                     where ids.Contains(svw.SctrVidaLeyId)
                                     join w in ctx.Worker.Include(x => x.Person) on svw.WorkerId equals w.Id
                                     select new
                                     {
                                         svw.SctrVidaLeyId,
                                         svw.WorkerId,
                                         svw.FechaInicioCobertura,
                                         ApellidoNombre = w.Person != null ? w.Person.FullName : null,
                                         Dni = w.Person != null ? w.Person.DocumentIdentityCode : null
                                     }).ToListAsync();

            foreach (var w in workersData.Where(x => x.ApellidoNombre == null).Take(3))
                _logger.LogWarning("Worker sin nombre: workerId={Id} sctrId={SctrId}", w.WorkerId, w.SctrVidaLeyId);

            var sctrItem = await ctx.SsItemTrabajador
                .Where(i => i.EsSctrVidaley)
                .ToListAsync();

            var workerIds = workersData.Select(w => w.WorkerId).Distinct().ToList();
            var sctrItemIds = sctrItem.Select(i => i.Id).ToList();

            var habs = await ctx.SsHabTrabajador
                .Where(h => workerIds.Contains(h.WorkerId) && sctrItemIds.Contains(h.ItemId))
                .ToListAsync();

            var tasks = entities.Select(async e =>
            {
                var workersDeEste = workersData.Where(x => x.SctrVidaLeyId == e.Id).ToList();
                var itemTipo = sctrItem.FirstOrDefault(i =>
                    e.Tipo == "VIDA_LEY" ? i.Nombre.Contains("Vida") : i.Nombre.Contains("SCTR"));

                var workersDto = workersDeEste.Select(w =>
                {
                    var aprobado = false;
                    var estadoWorker = "Falta";
                    int? sctrHabId = null;
                    DateTime? fechaVencimiento = null;
                    if (itemTipo is not null)
                    {
                        var hab = habs.FirstOrDefault(h => h.WorkerId == w.WorkerId && h.ItemId == itemTipo.Id);
                        if (hab is not null)
                        {
                            aprobado = hab.Estado == "Aprobado";
                            estadoWorker = hab.Estado ?? "Falta";
                            sctrHabId = hab.Id;
                            fechaVencimiento = hab.Vigencia;
                        }
                    }
                    return new SctrWorkerDto
                    {
                        WorkerId = w.WorkerId,
                        ApellidoNombre = w.ApellidoNombre ?? string.Empty,
                        Dni = w.Dni ?? string.Empty,
                        Aprobado = aprobado,
                        Estado = estadoWorker,
                        SctrHabId = sctrHabId,
                        FechaInicioCobertura = w.FechaInicioCobertura,
                        FechaVencimiento = fechaVencimiento
                    };
                }).OrderBy(w => w.ApellidoNombre).ToList();

                string? archivoUrl = null;
                string? archivoUrl2 = null;

                if (!string.IsNullOrEmpty(e.ArchivoUrl))
                {
                    try { archivoUrl = await _sharePoint.GetDownloadUrlAsync(e.ArchivoUrl); }
                    catch (Exception ex) { _logger.LogError(ex, "Error resolviendo URL: {Path}", e.ArchivoUrl); archivoUrl = null; }
                }

                if (!string.IsNullOrEmpty(e.ArchivoUrl2))
                {
                    try { archivoUrl2 = await _sharePoint.GetDownloadUrlAsync(e.ArchivoUrl2); }
                    catch (Exception ex) { _logger.LogError(ex, "Error resolviendo URL: {Path}", e.ArchivoUrl2); archivoUrl2 = null; }
                }

                return new SctrVidaLeyDto
                {
                    Id = e.Id,
                    EmpresaId = e.EmpresaId,
                    EmpresaNombre = empresaMap.TryGetValue(e.EmpresaId, out var en) ? en : string.Empty,
                    ProyectoId = e.ProyectoId,
                    ProyectoNombre = e.ProyectoId.HasValue && proyectoMap.TryGetValue(e.ProyectoId.Value, out var pn) ? pn ?? string.Empty : string.Empty,
                    Tipo = e.Tipo,
                    TipoPoliza = e.TipoPoliza,
                    FechaInicio = e.FechaInicio,
                    Mes = e.Mes,
                    Anio = e.Anio,
                    ArchivoUrl = archivoUrl,
                    ArchivoUrl2 = archivoUrl2,
                    Estado = e.Estado,
                    Vigencia = e.Vigencia,
                    ObsAbril = e.ObsAbril,
                    Workers = workersDto
                };
            });

            return (await Task.WhenAll(tasks)).ToList();
        }
    }
}
