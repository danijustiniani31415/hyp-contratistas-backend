using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionAdministrativa.Shared.Services;
using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Infrastructure.Repositories
{
    public class SolicitudSalidaRepository : ISolicitudSalidaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public SolicitudSalidaRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<SolicitudSalidaFormDataDto> GetFormData()
        {
            using var ctx = _factory.CreateDbContext();

            var motivos = await ctx.GaMotivoSalida
                .Where(m => m.Activo)
                .OrderBy(m => m.Descripcion)
                .Select(m => new MotivoSalidaDto
                {
                    Id                      = m.Id,
                    Descripcion             = m.Descripcion,
                    RequiereAdjunto         = m.RequiereAdjunto,
                    EsHoraEstimada          = m.EsHoraEstimada,
                    RequiereMotivoAdicional = m.RequiereMotivoAdicional,
                })
                .ToListAsync();

            var lugares = await (
                from l in ctx.GaLugar
                join p in ctx.Project on l.ProjectId equals p.ProjectId into pGroup
                from p in pGroup.DefaultIfEmpty()
                where l.Activo
                orderby l.Orden
                select new LugarSalidaDto
                {
                    Id            = l.Id,
                    NombreDisplay = l.Tipo == "proyecto" ? (p != null ? p.ProjectDescription : "[Sin proyecto]")
                                  : l.Tipo == "libre"    ? "Otro lugar"
                                  : l.Nombre ?? string.Empty,
                    EsLibre       = l.Tipo == "libre"
                }
            ).ToListAsync();

            return new SolicitudSalidaFormDataDto
            {
                Motivos = motivos,
                Lugares = lugares
            };
        }

        public async Task<List<SolicitudSalidaListItemDto>> GetByUserId(int userId, SolicitudSalidaFiltersDto? filters = null)
        {
            using var ctx = _factory.CreateDbContext();

            var workerInfo = await ctx.Worker
                .Where(w => w.Person != null && w.Person.UserId == userId)
                .Select(w => new { w.Id, w.Subarea })
                .FirstOrDefaultAsync();
            if (workerInfo == null) return new();

            var query = ctx.GaSolicitudSalida
                .Where(s => s.WorkerId == workerInfo.Id);

            if (filters != null)
            {
                var aprobId = EstadosSalida.Aprobacion.IdFromNombre(filters.EstadoAprobacion);
                if (aprobId.HasValue)
                    query = query.Where(s => s.EstadoAprobacionId == aprobId.Value);

                var rendId = EstadosSalida.Rendicion.IdFromNombre(filters.EstadoRendicion);
                if (rendId.HasValue)
                    query = query.Where(s => s.EstadoRendicionId == rendId.Value);

                if (filters.FechaSalidaDesde.HasValue)
                    query = query.Where(s => s.FechaSalida >= filters.FechaSalidaDesde.Value);

                if (filters.FechaSalidaHasta.HasValue)
                    query = query.Where(s => s.FechaSalida <= filters.FechaSalidaHasta.Value);

                if (filters.LugarProyectoId.HasValue)
                {
                    var lugId = filters.LugarProyectoId.Value;
                    query = query.Where(s => ctx.GaSolicitudTrayecto.Any(t =>
                        t.SolicitudId == s.Id &&
                        (t.LugarOrigenId == lugId || t.LugarDestinoId == lugId)));
                }
            }

            var solicitudes = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            if (solicitudes.Count == 0) return new();

            var ids = solicitudes.Select(s => s.Id).ToList();

            // Cargar todos los trayectos en una query, ordenados.
            var trayectos = await (
                from t  in ctx.GaSolicitudTrayecto
                join m  in ctx.GaMotivoSalida on t.MotivoId equals m.Id into mGroup
                from m  in mGroup.DefaultIfEmpty()
                join lo in ctx.GaLugar on t.LugarOrigenId equals lo.Id into loGroup
                from lo in loGroup.DefaultIfEmpty()
                join po in ctx.Project on lo.ProjectId equals (int?)po.ProjectId into poGroup
                from po in poGroup.DefaultIfEmpty()
                join ld in ctx.GaLugar on t.LugarDestinoId equals ld.Id into ldGroup
                from ld in ldGroup.DefaultIfEmpty()
                join pd in ctx.Project on ld.ProjectId equals (int?)pd.ProjectId into pdGroup
                from pd in pdGroup.DefaultIfEmpty()
                where ids.Contains(t.SolicitudId)
                orderby t.SolicitudId, t.Orden
                select new
                {
                    t.Id, t.SolicitudId, t.Orden, t.HoraSalida, t.HoraRetorno,
                    t.LugarOrigenId, t.LugarDestinoId,
                    Motivo       = m != null ? m.Descripcion : (t.MotivoLibre ?? string.Empty),
                    LugarOrigen  = lo == null ? t.LugarOrigenLibre
                                 : lo.Tipo == "proyecto" ? (po != null ? po.ProjectDescription : "[Sin proyecto]")
                                 : lo.Nombre,
                    LugarDestino = ld == null ? t.LugarDestinoLibre
                                 : ld.Tipo == "proyecto" ? (pd != null ? pd.ProjectDescription : "[Sin proyecto]")
                                 : ld.Nombre,
                }
            ).ToListAsync();

            var trayectosBySolicitud = trayectos.GroupBy(t => t.SolicitudId)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Orden).ToList());

            // Trayectos con al menos 1 captura — misma regla de cobertura que Gestión de Salidas.
            var trayectoIds = trayectos.Select(t => t.Id).ToList();
            var trayectosConCapturas = trayectoIds.Count == 0
                ? new HashSet<int>()
                : (await ctx.GaSolicitudCaptura
                    .Where(c => trayectoIds.Contains(c.TrayectoId))
                    .Select(c => c.TrayectoId)
                    .Distinct()
                    .ToListAsync()).ToHashSet();

            // Regla relajada para TI: un trayecto también se considera cubierto si su
            // (origen, destino) está en el catálogo ga_trayecto.
            var esTI = string.Equals(workerInfo.Subarea, SubareaTi, StringComparison.OrdinalIgnoreCase);
            var catalogoMap = esTI ? await CargarCatalogoTrayectosAsync(ctx) : new();

            // Consolidado del S10 vigente por solicitud (propio o heredado de su planilla).
            var consolidados = await ConsolidadoS10Loader.LoadAsync(
                ctx, solicitudes.ToDictionary(x => x.Id, x => x.RendicionId));

            var result = new List<SolicitudSalidaListItemDto>(solicitudes.Count);
            foreach (var s in solicitudes)
            {
                trayectosBySolicitud.TryGetValue(s.Id, out var trList);
                trList ??= new();
                var first = trList.FirstOrDefault();
                var last  = trList.LastOrDefault();

                bool trayectoCubierto(int trayectoId, int? origenId, int? destinoId)
                {
                    if (trayectosConCapturas.Contains(trayectoId)) return true;
                    if (!esTI) return false;
                    if (!origenId.HasValue || !destinoId.HasValue) return false;
                    return catalogoMap.ContainsKey((origenId.Value, destinoId.Value));
                }
                var puedeRendir = trList.Count > 0
                    && trList.All(t => trayectoCubierto(t.Id, t.LugarOrigenId, t.LugarDestinoId));

                result.Add(new SolicitudSalidaListItemDto
                {
                    Id           = s.Id,
                    FechaSalida  = s.FechaSalida,
                    HoraSalida   = first?.HoraSalida ?? default,
                    HoraRetorno  = last?.HoraRetorno,
                    Motivo       = first?.Motivo ?? string.Empty,
                    LugarOrigen  = first?.LugarOrigen,
                    LugarDestino = last?.LugarDestino,
                    TrayectosCount   = trList.Count,
                    EstadoAprobacion = EstadosSalida.Aprobacion.Nombre(s.EstadoAprobacionId),
                    EstadoRendicion  = EstadosSalida.Rendicion.Nombre(s.EstadoRendicionId),
                    CreatedAt        = s.CreatedAt,
                    PuedeRendirse    = puedeRendir,

                    EstadoReembolso      = EstadosSalida.Reembolso.Nombre(s.EstadoReembolsoId),
                    ObservacionReembolso = s.ObservacionReembolso,
                    RevisorNotificadoAt  = s.RevisorNotificadoAt,
                });

                if (consolidados.TryGetValue(s.Id, out var cons))
                {
                    var item = result[^1];
                    item.ConsolidadoS10Url      = cons.PdfUrl;
                    item.ConsolidadoS10Filename = cons.PdfFilename;
                    item.ConsolidadoS10Ambito   = cons.Ambito;
                }

                // Avisar al revisor tiene sentido solo cuando ya hay algo que revisar: la salida
                // rendida, el Consolidado del S10 adjunto y el reembolso todavía abierto. Después
                // de aprobado (o firmado, o pagado) el botón no aporta nada.
                result[^1].PuedeNotificarRevisor =
                    s.EstadoRendicionId == EstadosSalida.Rendicion.Rendido
                    && consolidados.ContainsKey(s.Id)
                    && (s.EstadoReembolsoId == EstadosSalida.Reembolso.Pendiente
                     || s.EstadoReembolsoId == EstadosSalida.Reembolso.Rechazado);
            }
            return result;
        }

        public async Task<(GaSolicitudSalida Solicitud, List<GaSolicitudTrayecto> Trayectos, Worker Solicitante)> Create(SolicitudSalidaCreateDto dto, int? userId, Dictionary<int, List<TrayectoAdjuntoSubidoDto>>? adjuntosPorIndice = null)
        {
            using var ctx = _factory.CreateDbContext();

            var solicitante = await ctx.Worker
                .Where(w => w.Person != null && w.Person.UserId == userId)
                .FirstOrDefaultAsync()
                ?? throw new AbrilException("No se encontró un trabajador asociado a su usuario.", 404);

            // Validación BD: motivos referenciados deben existir
            var motivoIds = dto.Trayectos.Where(t => t.MotivoId.HasValue).Select(t => t.MotivoId!.Value).Distinct().ToList();
            if (motivoIds.Count > 0)
            {
                var existentes = await ctx.GaMotivoSalida.Where(m => motivoIds.Contains(m.Id)).Select(m => m.Id).ToListAsync();
                var faltantes = motivoIds.Except(existentes).ToList();
                if (faltantes.Count > 0)
                    throw new AbrilException($"Motivo(s) no encontrado(s): {string.Join(", ", faltantes)}.", 404);
            }

            var now = DateTimeOffset.UtcNow;
            var solicitud = new GaSolicitudSalida
            {
                WorkerId           = solicitante.Id,
                FechaSalida        = dto.FechaSalida,
                EstadoAprobacionId = EstadosSalida.Aprobacion.Pendiente,
                EstadoRendicionId  = EstadosSalida.Rendicion.NoRendido,
                RegistradoPorId    = userId,
                CreatedAt        = now,
                UpdatedAt        = now,
            };

            var trayectosEnts = dto.Trayectos.Select((t, idx) =>
                new GaSolicitudTrayecto
                {
                    Orden             = idx,
                    HoraSalida        = t.HoraSalida,
                    HoraRetorno       = t.HoraRetorno,
                    MotivoId          = t.MotivoId,
                    MotivoLibre       = string.IsNullOrWhiteSpace(t.MotivoLibre) ? null : t.MotivoLibre.Trim(),
                    MotivoAdicional   = string.IsNullOrWhiteSpace(t.MotivoAdicional) ? null : t.MotivoAdicional.Trim(),
                    LugarOrigenId     = t.LugarOrigenId,
                    LugarOrigenLibre  = string.IsNullOrWhiteSpace(t.LugarOrigenLibre) ? null : t.LugarOrigenLibre.Trim(),
                    LugarDestinoId    = t.LugarDestinoId,
                    LugarDestinoLibre = string.IsNullOrWhiteSpace(t.LugarDestinoLibre) ? null : t.LugarDestinoLibre.Trim(),
                    // Los adjuntos nuevos se guardan en ga_solicitud_trayecto_adjunto (ver más abajo),
                    // no en estas columnas embebidas (legacy, se conservan para históricos).
                }).ToList();

            // Transacción explícita (Npgsql retry strategy compatible)
            var strategy = ctx.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await ctx.Database.BeginTransactionAsync();
                ctx.GaSolicitudSalida.Add(solicitud);
                await ctx.SaveChangesAsync();

                foreach (var tr in trayectosEnts)
                {
                    tr.SolicitudId = solicitud.Id;
                }
                ctx.GaSolicitudTrayecto.AddRange(trayectosEnts);
                await ctx.SaveChangesAsync();

                // Adjuntos por trayecto (N por trayecto). Ya subidos a SharePoint por el service.
                if (adjuntosPorIndice != null && adjuntosPorIndice.Count > 0)
                {
                    var adjuntoEnts = new List<GaSolicitudTrayectoAdjunto>();
                    for (int idx = 0; idx < trayectosEnts.Count; idx++)
                    {
                        if (!adjuntosPorIndice.TryGetValue(idx, out var subidos) || subidos.Count == 0)
                            continue;
                        foreach (var a in subidos)
                        {
                            adjuntoEnts.Add(new GaSolicitudTrayectoAdjunto
                            {
                                TrayectoId      = trayectosEnts[idx].Id,
                                AdjuntoUrl      = a.Url,
                                AdjuntoItemId   = a.ItemId,
                                AdjuntoDriveId  = a.DriveId,
                                AdjuntoFilename = a.Filename,
                                UploadedById    = userId,
                                UploadedAt      = now,
                            });
                        }
                    }
                    if (adjuntoEnts.Count > 0)
                    {
                        ctx.GaSolicitudTrayectoAdjunto.AddRange(adjuntoEnts);
                        await ctx.SaveChangesAsync();
                    }
                }

                await tx.CommitAsync();
            });

            return (solicitud, trayectosEnts, solicitante);
        }

        public async Task SetEnviadoACorreo(int solicitudId, string correo)
        {
            using var ctx = _factory.CreateDbContext();
            var s = await ctx.GaSolicitudSalida.FirstOrDefaultAsync(x => x.Id == solicitudId);
            if (s == null) return;
            s.EnviadoACorreo = correo.Trim();
            s.UpdatedAt = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }

        public async Task<GaSolicitudSalida?> Aprobar(int solicitudId)
        {
            using var ctx = _factory.CreateDbContext();
            var s = await ctx.GaSolicitudSalida.FirstOrDefaultAsync(x => x.Id == solicitudId);
            if (s == null || s.EstadoAprobacionId != EstadosSalida.Aprobacion.Pendiente) return null;
            s.EstadoAprobacionId = EstadosSalida.Aprobacion.Aprobado;
            s.FechaDecision    = DateTimeOffset.UtcNow;
            s.UpdatedAt        = DateTimeOffset.UtcNow;
            // Decisión vía email: atribuir al dueño del correo destinatario (worker o área GTH).
            await SalidaAprobadorHelper.AsignarPorCorreoEnviadoAsync(ctx, s);
            await ctx.SaveChangesAsync();
            return s;
        }

        public async Task<GaSolicitudSalida?> Rechazar(int solicitudId, string? motivoRechazo)
        {
            using var ctx = _factory.CreateDbContext();
            var s = await ctx.GaSolicitudSalida.FirstOrDefaultAsync(x => x.Id == solicitudId);
            if (s == null || s.EstadoAprobacionId != EstadosSalida.Aprobacion.Pendiente) return null;
            s.EstadoAprobacionId = EstadosSalida.Aprobacion.Rechazado;
            s.MotivoRechazo    = string.IsNullOrWhiteSpace(motivoRechazo) ? null : motivoRechazo.Trim();
            s.FechaDecision    = DateTimeOffset.UtcNow;
            s.UpdatedAt        = DateTimeOffset.UtcNow;
            // Decisión vía email: atribuir al dueño del correo destinatario (worker o área GTH).
            await SalidaAprobadorHelper.AsignarPorCorreoEnviadoAsync(ctx, s);
            await ctx.SaveChangesAsync();
            return s;
        }

        public async Task Cancelar(int solicitudId, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var s = await ctx.GaSolicitudSalida.FirstOrDefaultAsync(x => x.Id == solicitudId)
                ?? throw new AbrilException("Solicitud no encontrada.", 404);

            // Solo el propio trabajador puede cancelar SU solicitud (nunca la de otro).
            var esPropia = await ctx.Worker.AnyAsync(w => w.Id == s.WorkerId &&
                ctx.Person.Any(p => p.PersonId == w.PersonId && p.UserId == userId));
            if (!esPropia)
                throw new AbrilException("Solo puedes cancelar tus propias solicitudes de salida.", 403);

            if (s.EstadoAprobacionId != EstadosSalida.Aprobacion.Pendiente)
                throw new AbrilException("Solo se pueden cancelar solicitudes en estado Pendiente.", 400);

            var now = DateTimeOffset.UtcNow;
            s.EstadoAprobacionId = EstadosSalida.Aprobacion.Cancelado;
            s.CanceladaPorId     = userId;
            s.CanceladaAt        = now;
            s.UpdatedAt          = now;
            await ctx.SaveChangesAsync();
        }

        public async Task<SolicitudSalidaDetalleDto?> GetDetalleForUser(int solicitudId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            // Carga worker + subarea para regla TI ("Tecnología de la Información")
            var workerInfo = await ctx.Worker
                .Where(w => w.Person != null && w.Person.UserId == userId)
                .Select(w => new { w.Id, w.Subarea })
                .FirstOrDefaultAsync();
            if (workerInfo == null) return null;

            var solicitud = await (
                from s in ctx.GaSolicitudSalida
                join r in ctx.GaRendicion on s.RendicionId equals (int?)r.Id into rGroup
                from r in rGroup.DefaultIfEmpty()
                where s.Id == solicitudId && s.WorkerId == workerInfo.Id
                select new
                {
                    s.Id, s.FechaSalida, s.EstadoAprobacionId, s.EstadoRendicionId,
                    s.CreatedAt, s.MotivoRechazo, s.RendicionId,
                    Rendicion = r == null ? null : new SolicitudSalidaRendicionDto
                    {
                        Id          = r.Id,
                        PdfUrl      = r.PdfUrl,
                        PdfFilename = r.PdfFilename,
                        RendidoAt   = r.RendidoAt,
                    },
                })
                .FirstOrDefaultAsync();
            if (solicitud == null) return null;

            // Trayectos con su info resuelta. Cargamos LugarOrigenId/LugarDestinoId crudos
            // para poder hacer el match contra ga_trayecto después.
            var trayectosRaw = await (
                from t  in ctx.GaSolicitudTrayecto
                join m  in ctx.GaMotivoSalida on t.MotivoId equals m.Id into mGroup
                from m  in mGroup.DefaultIfEmpty()
                join lo in ctx.GaLugar on t.LugarOrigenId equals lo.Id into loGroup
                from lo in loGroup.DefaultIfEmpty()
                join po in ctx.Project on lo.ProjectId equals (int?)po.ProjectId into poGroup
                from po in poGroup.DefaultIfEmpty()
                join ld in ctx.GaLugar on t.LugarDestinoId equals ld.Id into ldGroup
                from ld in ldGroup.DefaultIfEmpty()
                join pd in ctx.Project on ld.ProjectId equals (int?)pd.ProjectId into pdGroup
                from pd in pdGroup.DefaultIfEmpty()
                where t.SolicitudId == solicitudId
                orderby t.Orden
                select new
                {
                    Dto = new TrayectoDetalleDto
                    {
                        Id          = t.Id,
                        Orden       = t.Orden,
                        HoraSalida  = t.HoraSalida,
                        HoraRetorno = t.HoraRetorno,
                        Motivo      = m != null ? m.Descripcion : (t.MotivoLibre ?? string.Empty),
                        MotivoAdicional = t.MotivoAdicional,
                        LugarOrigen = lo == null ? t.LugarOrigenLibre
                                    : lo.Tipo == "proyecto" ? (po != null ? po.ProjectDescription : "[Sin proyecto]")
                                    : lo.Nombre,
                        LugarDestino = ld == null ? t.LugarDestinoLibre
                                    : ld.Tipo == "proyecto" ? (pd != null ? pd.ProjectDescription : "[Sin proyecto]")
                                    : ld.Nombre,
                    },
                    t.LugarOrigenId,
                    t.LugarDestinoId,
                    // Adjunto legacy embebido (modelo anterior 1:1). Se combina con la tabla nueva.
                    t.AdjuntoUrl,
                    t.AdjuntoFilename,
                }
            ).ToListAsync();

            var trayectosListado = trayectosRaw.Select(x => x.Dto).ToList();

            // Capturas
            var trayectoIds = trayectosListado.Select(t => t.Id).ToList();

            // Adjuntos (tabla nueva ga_solicitud_trayecto_adjunto, N por trayecto).
            var adjuntosByTrayecto = new Dictionary<int, List<TrayectoAdjuntoDto>>();
            if (trayectoIds.Count > 0)
            {
                var adjRaw = await ctx.GaSolicitudTrayectoAdjunto
                    .Where(a => trayectoIds.Contains(a.TrayectoId))
                    .OrderBy(a => a.UploadedAt).ThenBy(a => a.Id)
                    .Select(a => new { a.TrayectoId, a.AdjuntoUrl, a.AdjuntoFilename })
                    .ToListAsync();

                adjuntosByTrayecto = adjRaw.GroupBy(a => a.TrayectoId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(a => new TrayectoAdjuntoDto { Url = a.AdjuntoUrl, Filename = a.AdjuntoFilename }).ToList());
            }

            // Combinar: adjunto legacy embebido (si existe) + adjuntos de la tabla nueva.
            foreach (var raw in trayectosRaw)
            {
                var lista = new List<TrayectoAdjuntoDto>();
                if (!string.IsNullOrWhiteSpace(raw.AdjuntoUrl))
                    lista.Add(new TrayectoAdjuntoDto { Url = raw.AdjuntoUrl, Filename = raw.AdjuntoFilename ?? "Ver documento" });
                if (adjuntosByTrayecto.TryGetValue(raw.Dto.Id, out var nuevos))
                    lista.AddRange(nuevos);
                raw.Dto.Adjuntos = lista;
            }
            var capsByTrayecto = new Dictionary<int, List<SolicitudSalidaCapturaDto>>();
            if (trayectoIds.Count > 0)
            {
                var capsRaw = await ctx.GaSolicitudCaptura
                    .Where(c => trayectoIds.Contains(c.TrayectoId))
                    .OrderBy(c => c.UploadedAt)
                    .Select(c => new
                    {
                        c.TrayectoId,
                        Dto = new SolicitudSalidaCapturaDto
                        {
                            Id         = c.Id,
                            ImageUrl   = c.ImageUrl,
                            Filename   = c.Filename,
                            Monto      = c.Monto,
                            UploadedAt = c.UploadedAt,
                        }
                    })
                    .ToListAsync();

                capsByTrayecto = capsRaw.GroupBy(x => x.TrayectoId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Dto).ToList());
            }

            // Catálogo de trayectos (solo si TI). Cargamos todo el catálogo activo y mapeamos por (origen, destino).
            var esTI = string.Equals(workerInfo.Subarea, SubareaTi, StringComparison.OrdinalIgnoreCase);
            var catalogoMap = esTI ? await CargarCatalogoTrayectosAsync(ctx) : new();

            foreach (var raw in trayectosRaw)
            {
                if (capsByTrayecto.TryGetValue(raw.Dto.Id, out var list))
                    raw.Dto.Capturas = list;

                var sumCapturas = raw.Dto.Capturas.Sum(c => c.Monto);

                if (esTI && raw.LugarOrigenId.HasValue && raw.LugarDestinoId.HasValue &&
                    catalogoMap.TryGetValue((raw.LugarOrigenId.Value, raw.LugarDestinoId.Value), out var montoCat))
                {
                    raw.Dto.MontoCatalogo = montoCat;
                }

                raw.Dto.MontoTotal = sumCapturas > 0
                    ? sumCapturas
                    : (raw.Dto.MontoCatalogo ?? 0m);
            }

            return new SolicitudSalidaDetalleDto
            {
                Id               = solicitud.Id,
                FechaSalida      = solicitud.FechaSalida,
                EstadoAprobacion = EstadosSalida.Aprobacion.Nombre(solicitud.EstadoAprobacionId),
                EstadoRendicion  = EstadosSalida.Rendicion.Nombre(solicitud.EstadoRendicionId),
                CreatedAt        = solicitud.CreatedAt,
                MotivoRechazo    = solicitud.MotivoRechazo,
                Rendicion        = solicitud.Rendicion,
                ConsolidadoS10   = (await ConsolidadoS10Loader.LoadAsync(
                                        ctx, new Dictionary<int, int?> { [solicitud.Id] = solicitud.RendicionId }))
                                    .GetValueOrDefault(solicitud.Id),
                Trayectos        = trayectosListado,
            };
        }

        public async Task<SolicitudSalidaFilterDataDto> GetFilterData(int userId)
        {
            using var ctx = _factory.CreateDbContext();

            // Solo lugares activos de tipo "proyecto" (los útiles para filtrar — los fijos suelen ser oficinas, no distinguen)
            var lugaresProyecto = await (
                from l in ctx.GaLugar
                join p in ctx.Project on l.ProjectId equals p.ProjectId
                where l.Tipo == "proyecto" && l.Activo
                orderby p.ProjectDescription
                select new LugarProyectoOptionDto
                {
                    Id            = l.Id,
                    NombreDisplay = p.ProjectDescription,
                }
            ).ToListAsync();

            return new SolicitudSalidaFilterDataDto { LugaresProyecto = lugaresProyecto };
        }

        private const string SubareaTi = "Tecnología de la Información";

        /// <summary>
        /// Carga el catálogo de trayectos activos como un mapa (lugar_origen_id, lugar_destino_id) -> monto.
        /// </summary>
        private static async Task<Dictionary<(int, int), decimal>> CargarCatalogoTrayectosAsync(AppDbContext ctx)
        {
            var rows = await ctx.GaTrayecto
                .Where(g => g.Activo)
                .Select(g => new { g.LugarOrigenId, g.LugarDestinoId, g.Monto })
                .ToListAsync();
            return rows.ToDictionary(r => (r.LugarOrigenId, r.LugarDestinoId), r => r.Monto);
        }

        public async Task<GaSolicitudTrayecto?> GetTrayectoForUploadingCapturas(int trayectoId, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            return await (
                from t in ctx.GaSolicitudTrayecto
                join s in ctx.GaSolicitudSalida on t.SolicitudId equals s.Id
                join w in ctx.Worker            on s.WorkerId    equals w.Id
                join per in ctx.Person          on w.PersonId    equals (int?)per.PersonId
                where t.Id == trayectoId
                   && per.UserId == userId
                   && s.EstadoAprobacionId == EstadosSalida.Aprobacion.Aprobado
                   && s.EstadoRendicionId  == EstadosSalida.Rendicion.NoRendido
                select t
            ).FirstOrDefaultAsync();
        }

        public async Task<List<SolicitudSalidaCapturaDto>> InsertCapturas(
            int trayectoId,
            IEnumerable<(string Url, string? ItemId, string Filename, decimal Monto)> items,
            int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var now = DateTimeOffset.UtcNow;
            var entities = items.Select(it => new GaSolicitudCaptura
            {
                TrayectoId   = trayectoId,
                ImageUrl     = it.Url,
                ImageItemId  = it.ItemId,
                Filename     = it.Filename,
                Monto        = it.Monto,
                UploadedById = userId,
                UploadedAt   = now,
            }).ToList();

            ctx.GaSolicitudCaptura.AddRange(entities);
            await ctx.SaveChangesAsync();

            return entities.Select(c => new SolicitudSalidaCapturaDto
            {
                Id         = c.Id,
                ImageUrl   = c.ImageUrl,
                Filename   = c.Filename,
                Monto      = c.Monto,
                UploadedAt = c.UploadedAt,
            }).ToList();
        }
    }
}
