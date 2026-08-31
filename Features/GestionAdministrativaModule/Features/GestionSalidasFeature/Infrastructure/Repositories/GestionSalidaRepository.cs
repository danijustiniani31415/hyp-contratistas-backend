using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Infrastructure.Models;
using Abril_Backend.Features.GestionAdministrativa.Shared.Services;
using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Constants;
using Abril_Backend.Shared.Services.Revisores.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Infrastructure.Repositories
{
    public class GestionSalidaRepository : IGestionSalidaRepository
    {
        private const int PageSize = 10;
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IJefeRevisorResolver _jefeResolver;

        public GestionSalidaRepository(
            IDbContextFactory<AppDbContext> factory,
            IJefeRevisorResolver jefeResolver)
        {
            _factory = factory;
            _jefeResolver = jefeResolver;
        }

        /// <summary>
        /// Tabla ordenada + paginada. Reutiliza <see cref="GetAll"/> (que ya resuelve motivo/origen/
        /// destino/horas y <c>PuedeRendirse</c>) y aplica el orden por columna en memoria. El orden es
        /// estable: los empates conservan el orden original (fecha de salida descendente, luego las
        /// registradas más recientemente).
        /// </summary>
        public async Task<PagedResult<GestionSalidaListItemDto>> GetPaged(GestionSalidaFiltersDto filters)
        {
            var all = await GetAll(filters);
            var sorted = ApplySort(all, filters);

            var totalRecords = sorted.Count;
            var page = filters.Page < 1 ? 1 : filters.Page;

            return new PagedResult<GestionSalidaListItemDto>
            {
                Page = page,
                PageSize = PageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)PageSize),
                Data = sorted.Skip((page - 1) * PageSize).Take(PageSize).ToList(),
            };
        }

        /// <summary>
        /// Ordena en memoria por la columna indicada en <paramref name="filters"/>. Si no se indica
        /// columna (o es desconocida) se conserva el orden original que trae <see cref="GetAll"/>.
        /// Las columnas de texto se ordenan ignorando mayúsculas/acentos según la cultura.
        /// </summary>
        private static List<GestionSalidaListItemDto> ApplySort(List<GestionSalidaListItemDto> items, GestionSalidaFiltersDto filters)
        {
            if (string.IsNullOrWhiteSpace(filters.SortBy)) return items;

            var asc = !string.Equals(filters.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

            IEnumerable<GestionSalidaListItemDto> OrderText(Func<GestionSalidaListItemDto, string?> sel) =>
                asc
                    ? items.OrderBy(x => sel(x) ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
                    : items.OrderByDescending(x => sel(x) ?? string.Empty, StringComparer.CurrentCultureIgnoreCase);

            IEnumerable<GestionSalidaListItemDto> OrderKey<TKey>(Func<GestionSalidaListItemDto, TKey> sel) =>
                asc ? items.OrderBy(sel) : items.OrderByDescending(sel);

            IEnumerable<GestionSalidaListItemDto>? ordered = filters.SortBy.Trim().ToLowerInvariant() switch
            {
                "trabajador"       => OrderText(s => s.Trabajador),
                "area"             => OrderText(s => s.Area),
                "revisornombre"    => OrderText(s => s.RevisorNombre),
                "motivo"           => OrderText(s => s.Motivo),
                "lugarorigen"      => OrderText(s => s.LugarOrigen),
                "lugardestino"     => OrderText(s => s.LugarDestino),
                "estadoaprobacion" => OrderText(s => s.EstadoAprobacion),
                "estadorendicion"  => OrderText(s => s.EstadoRendicion),
                "estadoreembolso"  => OrderText(s => s.EstadoReembolso),
                "fechasalida"      => OrderKey(s => s.FechaSalida),
                "horasalida"       => OrderKey(s => s.HoraSalida),
                "horaretorno"      => OrderKey(s => s.HoraRetorno),
                "createdat"        => OrderKey(s => s.CreatedAt),
                _                  => null,
            };

            return ordered?.ToList() ?? items;
        }

        public async Task<List<GestionSalidaListItemDto>> GetAll(GestionSalidaFiltersDto filters)
        {
            using var ctx = _factory.CreateDbContext();

            // 1. Filtrar solicitudes (cabecera)
            var solicitudQuery = ctx.GaSolicitudSalida.AsQueryable();

            if (filters.WorkerId.HasValue)
                solicitudQuery = solicitudQuery.Where(s => s.WorkerId == filters.WorkerId.Value);

            var rendId = EstadosSalida.Rendicion.IdFromNombre(filters.EstadoRendicion);
            if (rendId.HasValue)
                solicitudQuery = solicitudQuery.Where(s => s.EstadoRendicionId == rendId.Value);

            var aprobId = EstadosSalida.Aprobacion.IdFromNombre(filters.EstadoAprobacion);
            if (aprobId.HasValue)
                solicitudQuery = solicitudQuery.Where(s => s.EstadoAprobacionId == aprobId.Value);

            // Estado del reembolso. En modo TESORERÍA el filtro no es opcional: la bandeja del
            // tesorero son las salidas ya firmadas por la jefatura y las ya pagadas, y lo que pida
            // por el desplegable solo puede recortar ESE conjunto, nunca ampliarlo.
            var reembId = EstadosSalida.Reembolso.IdFromNombre(filters.EstadoReembolso);
            if (filters.EsTesorero)
            {
                var visiblesTesoreria = EstadosSalida.Reembolso.VisiblesParaTesoreria;
                if (reembId.HasValue && visiblesTesoreria.Contains(reembId.Value))
                    solicitudQuery = solicitudQuery.Where(s => s.EstadoReembolsoId == reembId.Value);
                else
                    solicitudQuery = solicitudQuery.Where(s => visiblesTesoreria.Contains(s.EstadoReembolsoId));
            }
            else if (reembId.HasValue)
            {
                solicitudQuery = solicitudQuery.Where(s => s.EstadoReembolsoId == reembId.Value);
            }

            // Filtro "Hoy" (encendido por defecto en Gestión de Salidas): solo las solicitudes cuya
            // fecha de salida es la de hoy. El día se toma en hora de Perú (UTC-5) y no la del
            // servidor, que corre en UTC: pasadas las 19:00 de Lima el UTC ya está en el día
            // siguiente y la pantalla mostraría el día equivocado justo al final de la jornada.
            if (filters.SoloHoy)
            {
                var hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
                solicitudQuery = solicitudQuery.Where(s => s.FechaSalida == hoy);
            }

            // Rango de fecha de salida (lo usa la rendición del mes anterior). Es independiente
            // del filtro "Hoy": quien manda el rango lo hace con SoloHoy apagado.
            if (filters.FechaSalidaDesde.HasValue)
            {
                var desde = filters.FechaSalidaDesde.Value;
                solicitudQuery = solicitudQuery.Where(s => s.FechaSalida >= desde);
            }
            if (filters.FechaSalidaHasta.HasValue)
            {
                var hasta = filters.FechaSalidaHasta.Value;
                solicitudQuery = solicitudQuery.Where(s => s.FechaSalida <= hasta);
            }

            // Visibilidad obligatoria (server-side): el usuario SIEMPRE ve sus propias solicitudes
            // (worker_id → su user), sin importar rol ni área — así un trabajador cualquiera puede
            // ver y rendir lo suyo. Además ve las que le fueron enviadas para revisar
            // (enviado_a_correo → su email_corporativo), las que él decidió (aprobador_worker_id →
            // su user; también cubre solicitudes antiguas donde ese campo guardaba al revisor al que
            // se envió), MÁS las de los trabajadores que pertenecen a las áreas (area_scope) que
            // tiene permitido ver. Si SeesAll = true no se aplica restricción por área. El servicio
            // ya resolvió el alcance (override o algoritmo).
            if (filters.CurrentUserId.HasValue && !filters.SeesAll && !filters.EsTesorero)
            {
                var uid = filters.CurrentUserId.Value;
                var areaIds = filters.VisibleAreaScopeIds ?? new List<int>();
                solicitudQuery = solicitudQuery.Where(s =>
                    ctx.Worker.Any(w => w.Id == s.WorkerId &&
                        ctx.Person.Any(p => p.PersonId == w.PersonId && p.UserId == uid))
                    ||
                    (s.AprobadorWorkerId != null &&
                     ctx.Worker.Any(w => w.Id == s.AprobadorWorkerId &&
                         ctx.Person.Any(p => p.PersonId == w.PersonId && p.UserId == uid)))
                    ||
                    (s.EnviadoACorreo != null &&
                     ctx.Worker.Any(w => w.EmailCorporativo != null &&
                         w.EmailCorporativo.Trim().ToLower() == s.EnviadoACorreo.Trim().ToLower() &&
                         ctx.Person.Any(p => p.PersonId == w.PersonId && p.UserId == uid)))
                    ||
                    ctx.Worker.Any(w => w.Id == s.WorkerId &&
                        w.AreaScopeId != null && areaIds.Contains(w.AreaScopeId.Value)));
            }

            // Filtro de área elegido por el usuario (cascada): trabajadores cuyo area_scope_id
            // esté dentro del nodo seleccionado + sus descendientes (ya expandidos en el frontend).
            if (filters.FilterAreaScopeIds is { Count: > 0 })
            {
                var areaFilter = filters.FilterAreaScopeIds;
                solicitudQuery = solicitudQuery.Where(s =>
                    ctx.Worker.Any(w => w.Id == s.WorkerId &&
                        w.AreaScopeId != null && areaFilter.Contains(w.AreaScopeId.Value)));
            }

            // Filtro por lugar proyecto: necesita pasar por trayectos
            if (filters.LugarProyectoId.HasValue)
            {
                var lugId = filters.LugarProyectoId.Value;
                solicitudQuery = solicitudQuery.Where(s => ctx.GaSolicitudTrayecto.Any(t =>
                    t.SolicitudId == s.Id &&
                    (t.LugarOrigenId == lugId || t.LugarDestinoId == lugId)));
            }

            var solicitudes = await (
                from s in solicitudQuery
                join w in ctx.Worker on s.WorkerId equals w.Id
                join per in ctx.Person on w.PersonId equals (int?)per.PersonId into perGroup
                from per in perGroup.DefaultIfEmpty()
                // Orden por defecto: por fecha de salida, de la más futura a la más antigua.
                // Antes se agrupaba primero lo Pendiente y luego el resto, pero ver la lista en
                // línea de tiempo (lo que viene primero, arriba) es lo que le sirve al revisor.
                // Empate en la fecha → la registrada más recientemente primero.
                orderby s.FechaSalida descending, s.CreatedAt descending
                select new
                {
                    s.Id, s.WorkerId, WorkerInternalId = w.Id, w.Subarea, w.AreaScopeId,
                    Trabajador = per != null ? (per.FullName ?? "[Sin nombre]") : "[Sin nombre]",
                    s.FechaSalida, s.EstadoAprobacionId, s.EstadoRendicionId, s.CreatedAt,
                    s.HoraSalidaReal, s.HoraRetornoReal, s.RendicionId,
                    s.EstadoReembolsoId, s.ObservacionReembolso, s.ReembolsoDecididoPorId,
                    s.ReembolsoDecididoAt, s.RevisorNotificadoAt
                }
            ).ToListAsync();

            if (solicitudes.Count == 0) return new();

            var solicitudIds = solicitudes.Select(s => s.Id).ToList();

            // 2. Trayectos para info agregada (motivo, origen, destino, horas).
            // Conservamos los IDs crudos de origen/destino para el match contra ga_trayecto.
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
                where solicitudIds.Contains(t.SolicitudId)
                orderby t.SolicitudId, t.Orden
                select new
                {
                    t.Id, t.SolicitudId, t.Orden, t.HoraSalida, t.HoraRetorno,
                    t.LugarOrigenId, t.LugarDestinoId,
                    Motivo = m != null ? m.Descripcion : (t.MotivoLibre ?? string.Empty),
                    // Motivo libre (sin catálogo) cuenta como hora exacta → se registra hora real.
                    EsHoraEstimada = m != null && m.EsHoraEstimada,
                    LugarOrigen = lo == null ? t.LugarOrigenLibre
                                : lo.Tipo == "proyecto" ? (po != null ? po.ProjectDescription : "[Sin proyecto]")
                                : lo.Nombre,
                    LugarDestino = ld == null ? t.LugarDestinoLibre
                                 : ld.Tipo == "proyecto" ? (pd != null ? pd.ProjectDescription : "[Sin proyecto]")
                                 : ld.Nombre,
                }
            ).ToListAsync();

            var trayectosBySol = trayectos.GroupBy(t => t.SolicitudId)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Orden).ToList());

            // 3. Trayectos con al menos 1 captura
            var trayectoIds = trayectos.Select(t => t.Id).ToList();
            var trayectosConCapturas = trayectoIds.Count == 0
                ? new HashSet<int>()
                : (await ctx.GaSolicitudCaptura
                    .Where(c => trayectoIds.Contains(c.TrayectoId))
                    .Select(c => c.TrayectoId)
                    .Distinct()
                    .ToListAsync()).ToHashSet();

            // 4. Catálogo si hay al menos un trabajador TI sin todas sus capturas — para evaluar la regla relajada.
            var hayWorkerTI = solicitudes.Any(s => string.Equals(s.Subarea, SubareaTi, StringComparison.OrdinalIgnoreCase));
            var catalogoMap = hayWorkerTI ? await CargarCatalogoTrayectosAsync(ctx) : new();

            // 4.b. Worker(s) del usuario actual + si es Gerente — para marcar por fila si puede
            //      aprobar/rechazar (nadie decide sus propias salidas, salvo los gerentes).
            var misWorkerIds = new HashSet<int>();
            var esGerente = false;
            if (filters.CurrentUserId.HasValue)
            {
                var uidDec = filters.CurrentUserId.Value;
                var misWorkers = await (
                    from w in ctx.Worker
                    join p in ctx.Person on w.PersonId equals p.PersonId
                    where p.UserId == uidDec
                    // La categoría sale del puesto: workers ya no la guarda.
                    select new
                    {
                        w.Id,
                        CategoriaId = w.PuestoCatalogo != null ? w.PuestoCatalogo.CategoriaId : (int?)null
                    }
                ).ToListAsync();
                misWorkerIds = misWorkers.Select(x => x.Id).ToHashSet();
                esGerente = misWorkers.Any(x => x.CategoriaId == CategoriaIds.Gerente);
            }

            // 4.c. Área del trabajador (nodo de workers.area_scope_id, el más bajo del árbol) y
            //       jefe/revisor de cada solicitante. El revisor se resuelve en UN lote para todos
            //       los trabajadores de la lista con la misma fuente que decide a quién se le manda
            //       la solicitud a aprobar (IJefeRevisorResolver).
            var arbolAreas = await CargarArbolAreasAsync(ctx);
            var revisorPorWorker = await _jefeResolver.ResolveManyAsync(
                solicitudes.Select(s => s.WorkerInternalId).Distinct().ToList());

            // 4.d. Consolidado del S10 vigente por solicitud (propio o heredado de su planilla).
            var consolidados = await ConsolidadoS10Loader.LoadAsync(
                ctx, solicitudes.ToDictionary(x => x.Id, x => x.RendicionId));

            // 4.e. Nombre de quien decidió el reembolso (1 query) y planilla firmada (1 query).
            //      Ambas se saltan si nadie decidió/firmó todavía, que es el caso normal al abrir
            //      la pantalla en "Hoy".
            var decisorIds = solicitudes
                .Where(x => x.ReembolsoDecididoPorId.HasValue)
                .Select(x => x.ReembolsoDecididoPorId!.Value)
                .Distinct()
                .ToList();
            var nombrePorUserId = decisorIds.Count == 0
                ? new Dictionary<int, string>()
                : await ctx.Person
                    .Where(pe => pe.UserId != null && decisorIds.Contains(pe.UserId.Value))
                    .Select(pe => new { UserId = pe.UserId!.Value, Nombre = pe.FullName ?? "" })
                    .ToDictionaryAsync(x => x.UserId, x => x.Nombre);

            var rendicionIds = solicitudes
                .Where(x => x.RendicionId.HasValue)
                .Select(x => x.RendicionId!.Value)
                .Distinct()
                .ToList();
            var planillaFirmadaPorRendicion = rendicionIds.Count == 0
                ? new Dictionary<int, string>()
                : await ctx.GaRendicion
                    .Where(r => rendicionIds.Contains(r.Id) && r.PdfFirmadoUrl != null)
                    .Select(r => new { r.Id, Url = r.PdfFirmadoUrl! })
                    .ToDictionaryAsync(x => x.Id, x => x.Url);

            // 5. Armar resultado
            var result = new List<GestionSalidaListItemDto>(solicitudes.Count);
            foreach (var s in solicitudes)
            {
                trayectosBySol.TryGetValue(s.Id, out var trList);
                trList ??= new();
                var first = trList.FirstOrDefault();
                var last  = trList.LastOrDefault();

                var esTI = string.Equals(s.Subarea, SubareaTi, StringComparison.OrdinalIgnoreCase);
                bool trayectoCubierto(dynamic t)
                {
                    if (trayectosConCapturas.Contains((int)t.Id)) return true;
                    if (!esTI) return false;
                    if (t.LugarOrigenId == null || t.LugarDestinoId == null) return false;
                    return catalogoMap.ContainsKey(((int)t.LugarOrigenId, (int)t.LugarDestinoId));
                }
                var puedeRendir = trList.Count > 0 && trList.All(t => trayectoCubierto(t));

                revisorPorWorker.TryGetValue(s.WorkerInternalId, out var revisor);

                result.Add(new GestionSalidaListItemDto
                {
                    Id               = s.Id,
                    WorkerId         = s.WorkerInternalId,
                    Trabajador       = s.Trabajador,
                    Area             = AreaMasBaja(s.AreaScopeId, arbolAreas),
                    RevisorNombre    = revisor?.Nombre,
                    FechaSalida      = s.FechaSalida,
                    HoraSalida       = first?.HoraSalida ?? default,
                    HoraRetorno      = last?.HoraRetorno,
                    Motivo           = first?.Motivo ?? string.Empty,
                    LugarOrigen      = first?.LugarOrigen,
                    LugarDestino     = last?.LugarDestino,
                    TrayectosCount   = trList.Count,
                    EstadoAprobacion = EstadosSalida.Aprobacion.Nombre(s.EstadoAprobacionId),
                    EstadoRendicion  = EstadosSalida.Rendicion.Nombre(s.EstadoRendicionId),
                    CreatedAt        = s.CreatedAt,
                    PuedeRendirse    = puedeRendir,
                    HoraSalidaReal   = s.HoraSalidaReal,
                    HoraRetornoReal  = s.HoraRetornoReal,
                    // Solo se omite la hora real si TODOS los trayectos son de hora estimada.
                    EsHoraEstimada   = trList.Count > 0 && trList.All(t => t.EsHoraEstimada),
                    PuedeDecidir     = esGerente || !misWorkerIds.Contains(s.WorkerId),
                    EsPropia         = misWorkerIds.Contains(s.WorkerId),

                    EstadoReembolso      = EstadosSalida.Reembolso.Nombre(s.EstadoReembolsoId),
                    ObservacionReembolso = s.ObservacionReembolso,
                    ReembolsoDecididoAt  = s.ReembolsoDecididoAt,
                    RevisorNotificadoAt  = s.RevisorNotificadoAt,
                    ReembolsoDecididoPor = s.ReembolsoDecididoPorId.HasValue
                        ? nombrePorUserId.GetValueOrDefault(s.ReembolsoDecididoPorId.Value)
                        : null,
                    PlanillaFirmadaUrl = s.RendicionId.HasValue
                        ? planillaFirmadaPorRendicion.GetValueOrDefault(s.RendicionId.Value)
                        : null,
                });

                if (consolidados.TryGetValue(s.Id, out var cons))
                {
                    var item = result[^1];
                    item.ConsolidadoS10Url      = cons.PdfUrl;
                    item.ConsolidadoS10Filename = cons.PdfFilename;
                    item.ConsolidadoS10Ambito   = cons.Ambito;
                }

                // El reembolso solo se revisa cuando la salida ya está rendida Y tiene el
                // Consolidado del S10: es el papel que el jefe mira para dar el visto bueno.
                result[^1].ReembolsoRevisable =
                    s.EstadoRendicionId == EstadosSalida.Rendicion.Rendido
                    && consolidados.ContainsKey(s.Id);
            }

            return result;
        }

        public async Task<GestionSalidaFilterDataDto> GetFilterData(bool seesAll, List<int> visibleAreaScopeIds, int? currentUserId)
        {
            using var ctx = _factory.CreateDbContext();

            var workerIds = await ctx.GaSolicitudSalida
                .Select(s => s.WorkerId)
                .Distinct()
                .ToListAsync();

            // Base: trabajadores con al menos una solicitud.
            var trabajadoresQuery = ctx.Worker.Where(w => workerIds.Contains(w.Id));

            // Recorte por visibilidad: solo trabajadores cuya área esté en el alcance del usuario
            // (área actual hacia abajo). El propio trabajador del usuario siempre entra, porque
            // siempre ve sus propias solicitudes. Si ve todo (recepción/GTH), no se recorta.
            if (!seesAll)
            {
                trabajadoresQuery = trabajadoresQuery.Where(w =>
                    (w.AreaScopeId != null && visibleAreaScopeIds.Contains(w.AreaScopeId.Value))
                    || (currentUserId != null &&
                        ctx.Person.Any(p => p.PersonId == w.PersonId && p.UserId == currentUserId)));
            }

            var trabajadores = await (
                from w   in trabajadoresQuery
                join per in ctx.Person on w.PersonId equals (int?)per.PersonId into perGroup
                from per in perGroup.DefaultIfEmpty()
                orderby per != null ? per.FullName : null
                select new TrabajadorOptionDto
                {
                    WorkerId       = w.Id,
                    NombreCompleto = per != null ? (per.FullName ?? "[Sin nombre]") : "[Sin nombre]",
                }
            ).ToListAsync();

            var lugaresProyecto = await (
                from g in ctx.GaLugar
                join p in ctx.Project on g.ProjectId equals p.ProjectId
                where g.Tipo == "proyecto" && g.Activo
                orderby p.ProjectDescription
                select new LugarProyectoOptionDto
                {
                    GaLugarId    = g.Id,
                    NombreDisplay = p.ProjectDescription,
                }
            ).ToListAsync();

            var areaTree = await (
                from s in ctx.AreaScope
                join ai in ctx.AreaItem on s.AreaItemId equals ai.AreaItemId
                join at in ctx.AreaType on ai.AreaTypeId equals at.AreaTypeId
                where s.State && ai.State && at.State
                   && (seesAll || visibleAreaScopeIds.Contains(s.AreaScopeId))
                orderby s.DisplayOrder
                select new AreaNodeDto
                {
                    AreaScopeId       = s.AreaScopeId,
                    AreaItemId        = s.AreaItemId,
                    AreaItemName      = ai.AreaItemName,
                    AreaTypeId        = ai.AreaTypeId,
                    AreaTypeName      = at.AreaTypeName,
                    AreaScopeParentId = s.AreaScopeParentId,
                    DisplayOrder      = s.DisplayOrder,
                }
            ).ToListAsync();

            return new GestionSalidaFilterDataDto
            {
                Trabajadores    = trabajadores,
                LugaresProyecto = lugaresProyecto,
                AreaTree        = areaTree,
            };
        }

        /// <summary>
        /// Regla de negocio: un usuario NO puede aprobar ni rechazar sus propias solicitudes de
        /// salida. Única excepción: si el usuario es Gerente (<see cref="CategoriaIds.Gerente"/>),
        /// sí puede decidir las suyas (los gerentes salen sin pedir permiso, pero se deja
        /// habilitado por si acaso).
        /// </summary>
        private static async Task EnsurePuedeDecidirAsync(AppDbContext ctx, GaSolicitudSalida s, int reviewerUserId)
        {
            var esPropia = await ctx.Worker.AnyAsync(w => w.Id == s.WorkerId &&
                ctx.Person.Any(p => p.PersonId == w.PersonId && p.UserId == reviewerUserId));
            if (!esPropia) return;

            // Un usuario puede tener más de una ficha de worker (reingreso): basta con que
            // alguna sea Gerente. Mismo criterio que SalidaVisibilityResolver.
            var esGerente = await (
                from w in ctx.Worker
                join p in ctx.Person on w.PersonId equals p.PersonId
                where p.UserId == reviewerUserId
                select w.PuestoCatalogo != null ? w.PuestoCatalogo.CategoriaId : (int?)null
            ).AnyAsync(id => id == CategoriaIds.Gerente);

            if (!esGerente)
                throw new AbrilException("No puedes aprobar ni rechazar tus propias solicitudes de salida.", 403);
        }

        public async Task Aprobar(int id, int reviewerUserId)
        {
            using var ctx = _factory.CreateDbContext();
            var s = await ctx.GaSolicitudSalida.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new AbrilException("Solicitud no encontrada.", 404);
            await EnsurePuedeDecidirAsync(ctx, s, reviewerUserId);
            if (s.EstadoAprobacionId != EstadosSalida.Aprobacion.Pendiente)
                throw new AbrilException("Solo se pueden aprobar solicitudes en estado Pendiente.", 400);
            s.EstadoAprobacionId = EstadosSalida.Aprobacion.Aprobado;
            s.FechaDecision      = DateTimeOffset.UtcNow;
            s.UpdatedAt          = DateTimeOffset.UtcNow;
            // Decisión desde la web: el aprobador real es el worker del usuario logueado.
            await SalidaAprobadorHelper.AsignarPorUsuarioAsync(ctx, s, reviewerUserId);
            await ctx.SaveChangesAsync();
        }

        public async Task Rechazar(int id, int reviewerUserId)
        {
            using var ctx = _factory.CreateDbContext();
            var s = await ctx.GaSolicitudSalida.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new AbrilException("Solicitud no encontrada.", 404);
            await EnsurePuedeDecidirAsync(ctx, s, reviewerUserId);

            // Se puede rechazar una solicitud Pendiente o una ya Aprobada que todavía NO haya sido
            // rendida: el revisor puede revertir una aprobación mientras no exista la rendición. Una
            // vez rendida (Rendido) la aprobación queda firme y ya no se puede rechazar.
            var esPendiente         = s.EstadoAprobacionId == EstadosSalida.Aprobacion.Pendiente;
            var esAprobadaNoRendida = s.EstadoAprobacionId == EstadosSalida.Aprobacion.Aprobado
                                   && s.EstadoRendicionId  == EstadosSalida.Rendicion.NoRendido;
            if (!esPendiente && !esAprobadaNoRendida)
            {
                if (s.EstadoAprobacionId == EstadosSalida.Aprobacion.Aprobado)
                    throw new AbrilException("No se puede rechazar una solicitud que ya fue rendida.", 400);
                throw new AbrilException("Solo se pueden rechazar solicitudes pendientes o aprobadas aún no rendidas.", 400);
            }

            s.EstadoAprobacionId = EstadosSalida.Aprobacion.Rechazado;
            s.FechaDecision      = DateTimeOffset.UtcNow;
            s.UpdatedAt          = DateTimeOffset.UtcNow;
            // Decisión desde la web: quien decide (rechaza) es el worker del usuario logueado.
            await SalidaAprobadorHelper.AsignarPorUsuarioAsync(ctx, s, reviewerUserId);
            await ctx.SaveChangesAsync();
        }

        public async Task<int> GetNextNumeroPlanillaAsync()
        {
            using var ctx = _factory.CreateDbContext();
            var values = await ctx.Database
                .SqlQuery<int>($"SELECT nextval('seq_planilla_numero')::int AS \"Value\"")
                .ToListAsync();
            return values.First();
        }

        public async Task<string?> GetRendicionFolderUrl()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.GaRendicionFolder
                .Where(f => f.State && f.Active)
                .OrderBy(f => f.GaRendicionFolderId)
                .Select(f => f.LinkUrl)
                .FirstOrDefaultAsync();
        }

        public async Task<List<int>> CrearRendicionYMarcarBulk(
            IEnumerable<int> ids,
            int userId,
            string pdfUrl,
            string? pdfItemId,
            string pdfFilename,
            int numeroPlanilla)
        {
            using var ctx = _factory.CreateDbContext();
            var idsList = ids?.Distinct().ToList() ?? new List<int>();
            if (idsList.Count == 0) return new();

            var solicitudes = await ctx.GaSolicitudSalida
                .Where(s => idsList.Contains(s.Id)
                         && s.EstadoAprobacionId == EstadosSalida.Aprobacion.Aprobado
                         && s.EstadoRendicionId  == EstadosSalida.Rendicion.NoRendido)
                .ToListAsync();

            if (solicitudes.Count == 0)
                throw new AbrilException("No hay solicitudes elegibles para rendir (deben estar aprobadas y no rendidas).", 400);

            var now = DateTimeOffset.UtcNow;

            var strategy = ctx.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await ctx.Database.BeginTransactionAsync();

                var rendicion = new GaRendicion
                {
                    PdfUrl         = pdfUrl,
                    PdfItemId      = pdfItemId,
                    PdfFilename    = pdfFilename,
                    RendidoPorId   = userId,
                    RendidoAt      = now,
                    NumeroPlanilla = numeroPlanilla,
                };
                ctx.GaRendicion.Add(rendicion);
                await ctx.SaveChangesAsync();

                foreach (var s in solicitudes)
                {
                    s.EstadoRendicionId = EstadosSalida.Rendicion.Rendido;
                    s.RendicionId       = rendicion.Id;
                    s.UpdatedAt         = now;
                }
                await ctx.SaveChangesAsync();
                await tx.CommitAsync();
            });

            return solicitudes.Select(s => s.Id).ToList();
        }

        public async Task<List<int>> GetEligibleIdsForRendicion(IEnumerable<int> ids)
        {
            using var ctx = _factory.CreateDbContext();
            var idsList = ids?.Distinct().ToList() ?? new List<int>();
            if (idsList.Count == 0) return new();

            return await ctx.GaSolicitudSalida
                .Where(s => idsList.Contains(s.Id)
                         && s.EstadoAprobacionId == EstadosSalida.Aprobacion.Aprobado
                         && s.EstadoRendicionId  == EstadosSalida.Rendicion.NoRendido)
                .Select(s => s.Id)
                .ToListAsync();
        }

        public async Task<List<int>> GetIdsNotOwnedByUser(IEnumerable<int> ids, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var idsList = ids?.Distinct().ToList() ?? new List<int>();
            if (idsList.Count == 0) return new();

            var owned = await (
                from s   in ctx.GaSolicitudSalida
                join w   in ctx.Worker on s.WorkerId equals w.Id
                join per in ctx.Person on w.PersonId equals (int?)per.PersonId
                where idsList.Contains(s.Id) && per.UserId == userId
                select s.Id
            ).ToListAsync();

            return idsList.Except(owned).ToList();
        }

        public async Task<List<int>> GetIdsConTrayectosSinCapturas(IEnumerable<int> ids)
        {
            using var ctx = _factory.CreateDbContext();
            var idsList = ids?.Distinct().ToList() ?? new List<int>();
            if (idsList.Count == 0) return new();

            // Cargar info de cada solicitud + subarea del worker
            var solicitudes = await (
                from s in ctx.GaSolicitudSalida
                join w in ctx.Worker on s.WorkerId equals w.Id
                where idsList.Contains(s.Id)
                select new { s.Id, w.Subarea }
            ).ToListAsync();

            if (solicitudes.Count == 0) return idsList;

            // Trayectos por solicitud (Id + lugares para match con catálogo)
            var trayectos = await ctx.GaSolicitudTrayecto
                .Where(t => idsList.Contains(t.SolicitudId))
                .Select(t => new { t.Id, t.SolicitudId, t.LugarOrigenId, t.LugarDestinoId })
                .ToListAsync();
            var trayectosBySol = trayectos.GroupBy(t => t.SolicitudId).ToDictionary(g => g.Key, g => g.ToList());

            // Trayectos con al menos 1 captura
            var trayectoIds = trayectos.Select(t => t.Id).ToList();
            var conCapturas = trayectoIds.Count == 0
                ? new HashSet<int>()
                : (await ctx.GaSolicitudCaptura
                    .Where(c => trayectoIds.Contains(c.TrayectoId))
                    .Select(c => c.TrayectoId)
                    .Distinct()
                    .ToListAsync()).ToHashSet();

            // Catálogo (cargado solo si algún worker es TI)
            var hayTI = solicitudes.Any(s => string.Equals(s.Subarea, SubareaTi, StringComparison.OrdinalIgnoreCase));
            var catalogoMap = hayTI ? await CargarCatalogoTrayectosAsync(ctx) : new();

            var incompletas = new List<int>();
            foreach (var s in solicitudes)
            {
                if (!trayectosBySol.TryGetValue(s.Id, out var trList) || trList.Count == 0)
                {
                    incompletas.Add(s.Id);
                    continue;
                }

                var esTI = string.Equals(s.Subarea, SubareaTi, StringComparison.OrdinalIgnoreCase);
                bool todosCubiertos = trList.All(t =>
                {
                    if (conCapturas.Contains(t.Id)) return true;
                    if (!esTI) return false;
                    if (!t.LugarOrigenId.HasValue || !t.LugarDestinoId.HasValue) return false;
                    return catalogoMap.ContainsKey((t.LugarOrigenId.Value, t.LugarDestinoId.Value));
                });

                if (!todosCubiertos) incompletas.Add(s.Id);
            }

            return incompletas;
        }

        public async Task<GestionSalidaDetalleDto?> GetDetalle(int id)
        {
            using var ctx = _factory.CreateDbContext();

            var head = await (
                from s in ctx.GaSolicitudSalida
                join w in ctx.Worker on s.WorkerId equals w.Id
                join per in ctx.Person on w.PersonId equals (int?)per.PersonId into perGroup
                from per in perGroup.DefaultIfEmpty()
                join r in ctx.GaRendicion on s.RendicionId equals (int?)r.Id into rGroup
                from r in rGroup.DefaultIfEmpty()
                where s.Id == id
                select new
                {
                    s.Id, WorkerInternalId = w.Id, w.Subarea, w.AreaScopeId,
                    Trabajador = per != null ? (per.FullName ?? "[Sin nombre]") : "[Sin nombre]",
                    s.FechaSalida, s.EstadoAprobacionId, s.EstadoRendicionId, s.CreatedAt, s.MotivoRechazo,
                    s.RendicionId,
                    s.EstadoReembolsoId, s.ObservacionReembolso,
                    s.ReembolsoDecididoPorId, s.ReembolsoDecididoAt,
                    s.FirmadoPorId, s.FirmadoAt, s.PagadoPorId, s.PagadoAt,
                    Rendicion = r == null ? null : new GestionSalidaRendicionDto
                    {
                        Id            = r.Id,
                        PdfUrl        = r.PdfUrl,
                        PdfFilename   = r.PdfFilename,
                        RendidoAt     = r.RendidoAt,
                        PdfFirmadoUrl = r.PdfFirmadoUrl,
                    },
                }
            ).FirstOrDefaultAsync();

            if (head == null) return null;

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
                where t.SolicitudId == id
                orderby t.Orden
                select new
                {
                    Dto = new GestionSalidaTrayectoDto
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

            var trayectos = trayectosRaw.Select(x => x.Dto).ToList();

            // Capturas por trayecto
            var trayectoIds = trayectos.Select(t => t.Id).ToList();

            // Adjuntos (tabla nueva ga_solicitud_trayecto_adjunto, N por trayecto) + legacy embebido.
            var adjuntosByTrayecto = new Dictionary<int, List<GestionSalidaAdjuntoDto>>();
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
                        g => g.Select(a => new GestionSalidaAdjuntoDto { Url = a.AdjuntoUrl, Filename = a.AdjuntoFilename }).ToList());
            }

            foreach (var raw in trayectosRaw)
            {
                var lista = new List<GestionSalidaAdjuntoDto>();
                if (!string.IsNullOrWhiteSpace(raw.AdjuntoUrl))
                    lista.Add(new GestionSalidaAdjuntoDto { Url = raw.AdjuntoUrl, Filename = raw.AdjuntoFilename ?? "Ver documento" });
                if (adjuntosByTrayecto.TryGetValue(raw.Dto.Id, out var nuevos))
                    lista.AddRange(nuevos);
                raw.Dto.Adjuntos = lista;
            }

            if (trayectoIds.Count > 0)
            {
                var capsRaw = await ctx.GaSolicitudCaptura
                    .Where(c => trayectoIds.Contains(c.TrayectoId))
                    .OrderBy(c => c.UploadedAt)
                    .Select(c => new
                    {
                        c.TrayectoId,
                        Dto = new GestionSalidaCapturaDto
                        {
                            Id         = c.Id,
                            ImageUrl   = c.ImageUrl,
                            Filename   = c.Filename,
                            Monto      = c.Monto,
                            UploadedAt = c.UploadedAt,
                        }
                    })
                    .ToListAsync();

                var capsByTr = capsRaw.GroupBy(x => x.TrayectoId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Dto).ToList());

                foreach (var tr in trayectos)
                {
                    if (capsByTr.TryGetValue(tr.Id, out var list))
                        tr.Capturas = list;
                }
            }

            // Catálogo (solo si el worker es TI)
            var esTI = string.Equals(head.Subarea, SubareaTi, StringComparison.OrdinalIgnoreCase);
            var catalogoMap = esTI ? await CargarCatalogoTrayectosAsync(ctx) : new();

            foreach (var raw in trayectosRaw)
            {
                var sumCapturas = raw.Dto.Capturas.Sum(c => c.Monto);
                if (esTI && raw.LugarOrigenId.HasValue && raw.LugarDestinoId.HasValue &&
                    catalogoMap.TryGetValue((raw.LugarOrigenId.Value, raw.LugarDestinoId.Value), out var montoCat))
                {
                    raw.Dto.MontoCatalogo = montoCat;
                }
                raw.Dto.MontoTotal = sumCapturas > 0 ? sumCapturas : (raw.Dto.MontoCatalogo ?? 0m);
            }

            // Área: aquí sí va la ruta completa (la tabla muestra solo el último nodo) y el
            // jefe/revisor con su correo — el mismo que recibió la solicitud para aprobar.
            var arbolAreas = await CargarArbolAreasAsync(ctx);
            var revisor    = await _jefeResolver.ResolveAsync(head.WorkerInternalId);

            // Quién decidió el reembolso, quién firmó y quién pagó: los tres son app_user, así que
            // salen de una sola consulta a person.
            var userIdsDecision = new[] { head.ReembolsoDecididoPorId, head.FirmadoPorId, head.PagadoPorId }
                .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
            var nombresDecision = userIdsDecision.Count == 0
                ? new Dictionary<int, string>()
                : await ctx.Person
                    .Where(pe => pe.UserId != null && userIdsDecision.Contains(pe.UserId.Value))
                    .Select(pe => new { UserId = pe.UserId!.Value, Nombre = pe.FullName ?? "" })
                    .ToDictionaryAsync(x => x.UserId, x => x.Nombre);

            return new GestionSalidaDetalleDto
            {
                Id               = head.Id,
                WorkerId         = head.WorkerInternalId,
                Trabajador       = head.Trabajador,
                Area             = AreaMasBaja(head.AreaScopeId, arbolAreas),
                AreaRuta         = RutaArea(head.AreaScopeId, arbolAreas),
                RevisorNombre    = revisor?.Nombre,
                RevisorEmail     = revisor?.Email,
                FechaSalida      = head.FechaSalida,
                EstadoAprobacion = EstadosSalida.Aprobacion.Nombre(head.EstadoAprobacionId),
                EstadoRendicion  = EstadosSalida.Rendicion.Nombre(head.EstadoRendicionId),
                CreatedAt        = head.CreatedAt,
                MotivoRechazo    = head.MotivoRechazo,

                EstadoReembolso      = EstadosSalida.Reembolso.Nombre(head.EstadoReembolsoId),
                ObservacionReembolso = head.ObservacionReembolso,
                ReembolsoDecididoPor = nombresDecision.GetValueOrDefault(head.ReembolsoDecididoPorId ?? 0),
                ReembolsoDecididoAt  = head.ReembolsoDecididoAt,
                FirmadoPor           = nombresDecision.GetValueOrDefault(head.FirmadoPorId ?? 0),
                FirmadoAt            = head.FirmadoAt,
                PagadoPor            = nombresDecision.GetValueOrDefault(head.PagadoPorId ?? 0),
                PagadoAt             = head.PagadoAt,

                Rendicion        = head.Rendicion,
                ConsolidadoS10   = (await ConsolidadoS10Loader.LoadAsync(
                                        ctx, new Dictionary<int, int?> { [head.Id] = head.RendicionId }))
                                    .GetValueOrDefault(head.Id),
                Trayectos        = trayectos,
            };
        }

        public async Task<List<RendicionItemDto>> GetRendicionData(List<int> solicitudIds)
        {
            using var ctx = _factory.CreateDbContext();
            if (solicitudIds.Count == 0) return new();

            // Una fila = un trayecto. Cargamos los IDs crudos de lugares + subarea del worker
            // para poder hacer match contra ga_trayecto cuando el trabajador es TI.
            var rowsRaw = await (
                from t   in ctx.GaSolicitudTrayecto
                join s   in ctx.GaSolicitudSalida on t.SolicitudId equals s.Id
                join w   in ctx.Worker on s.WorkerId equals w.Id
                join per in ctx.Person on w.PersonId equals (int?)per.PersonId into perGroup
                from per in perGroup.DefaultIfEmpty()
                join cont in ctx.Contributor on w.ContributorId equals (int?)cont.ContributorId into contGroup
                from cont in contGroup.DefaultIfEmpty()
                join m   in ctx.GaMotivoSalida on t.MotivoId equals m.Id into mGroup
                from m   in mGroup.DefaultIfEmpty()
                join lo  in ctx.GaLugar on t.LugarOrigenId equals lo.Id into loGroup
                from lo  in loGroup.DefaultIfEmpty()
                join po  in ctx.Project on lo.ProjectId equals (int?)po.ProjectId into poGroup
                from po  in poGroup.DefaultIfEmpty()
                join ld  in ctx.GaLugar on t.LugarDestinoId equals ld.Id into ldGroup
                from ld  in ldGroup.DefaultIfEmpty()
                join pd  in ctx.Project on ld.ProjectId equals (int?)pd.ProjectId into pdGroup
                from pd  in pdGroup.DefaultIfEmpty()
                where solicitudIds.Contains(s.Id)
                orderby w.Id, s.FechaSalida, t.Orden
                select new
                {
                    Item = new RendicionItemDto
                    {
                        Id               = t.Id,
                        SolicitudId      = s.Id,
                        WorkerId         = w.Id,
                        TrabajadorNombre = per != null ? (per.FullName ?? "") : "",
                        TrabajadorDni    = per != null ? per.DocumentIdentityCode : null,
                        TrabajadorDocumentTypeId = per != null ? per.DocumentIdentityTypeId : null,
                        Area             = w.Area,     // fallback; se sobrescribe abajo si hay area_scope_id
                        FechaSalida      = s.FechaSalida,
                        Motivo           = m != null ? m.Descripcion : (t.MotivoLibre ?? ""),
                        MotivoAdicional  = t.MotivoAdicional,
                        LugarOrigen      = lo == null ? t.LugarOrigenLibre
                                         : lo.Tipo == "proyecto" ? (po != null ? po.ProjectDescription : null)
                                         : lo.Nombre,
                        LugarDestino     = ld == null ? t.LugarDestinoLibre
                                         : ld.Tipo == "proyecto" ? (pd != null ? pd.ProjectDescription : null)
                                         : ld.Nombre,
                        RazonSocial      = cont != null ? cont.ContributorName : null,
                        Ruc              = cont != null ? cont.ContributorRuc  : null,
                    },
                    Subarea = w.Subarea,
                    WorkerAreaScopeId = w.AreaScopeId,
                    t.LugarOrigenId,
                    t.LugarDestinoId,
                }
            ).ToListAsync();

            // Resolver nombre del área navegando hacia arriba en area_scope hasta encontrar
            // el primer nodo cuyo tipo sea "Área Estándar" (saltando "Área de Gerencia", etc).
            // Una solicitud puede tener N trayectos → hay varias filas por worker; nos quedamos
            // con una sola entrada (worker → area_scope_id) para no duplicar la clave del diccionario.
            var workerScope = rowsRaw
                .Where(r => r.WorkerAreaScopeId.HasValue)
                .GroupBy(r => r.Item.WorkerId)
                .ToDictionary(g => g.Key, g => g.First().WorkerAreaScopeId!.Value);

            var areaResueltaPorWorker = await ResolverAreaPorWorkerAsync(
                ctx,
                workerScope.Keys.ToList(),
                workerScope);

            foreach (var r in rowsRaw)
            {
                if (areaResueltaPorWorker.TryGetValue(r.Item.WorkerId, out var nombreArea) && !string.IsNullOrWhiteSpace(nombreArea))
                    r.Item.Area = nombreArea;
            }

            // Importe por trayecto (suma de capturas)
            var trayectoIds = rowsRaw.Select(r => r.Item.Id).ToList();
            var importesCapturas = await ctx.GaSolicitudCaptura
                .Where(c => trayectoIds.Contains(c.TrayectoId))
                .GroupBy(c => c.TrayectoId)
                .Select(g => new { TrayectoId = g.Key, Total = g.Sum(x => x.Monto) })
                .ToDictionaryAsync(x => x.TrayectoId, x => x.Total);

            // Catálogo: necesario para los trayectos sin capturas cuyo worker es TI
            var necesitaCatalogo = rowsRaw.Any(r =>
                string.Equals(r.Subarea, SubareaTi, StringComparison.OrdinalIgnoreCase) &&
                !importesCapturas.ContainsKey(r.Item.Id));
            var catalogoMap = necesitaCatalogo ? await CargarCatalogoTrayectosAsync(ctx) : new();

            foreach (var r in rowsRaw)
            {
                if (importesCapturas.TryGetValue(r.Item.Id, out var sumCap) && sumCap > 0m)
                {
                    r.Item.Importe    = sumCap;
                    r.Item.EsCatalogo = false;
                }
                else if (string.Equals(r.Subarea, SubareaTi, StringComparison.OrdinalIgnoreCase) &&
                         r.LugarOrigenId.HasValue && r.LugarDestinoId.HasValue &&
                         catalogoMap.TryGetValue((r.LugarOrigenId.Value, r.LugarDestinoId.Value), out var montoCat))
                {
                    r.Item.Importe    = montoCat;
                    r.Item.EsCatalogo = true;
                }
                else
                {
                    r.Item.Importe    = 0m;
                    r.Item.EsCatalogo = false;
                }
            }

            return rowsRaw.Select(r => r.Item).ToList();
        }

        public async Task SetHoraSalidaReal(int solicitudId, TimeOnly? hora, int registradaPorUserId)
        {
            using var ctx = _factory.CreateDbContext();
            var s = await ctx.GaSolicitudSalida.FirstOrDefaultAsync(x => x.Id == solicitudId)
                ?? throw new AbrilException("Solicitud no encontrada.", 404);

            s.HoraSalidaReal                = hora;
            s.HoraSalidaRealRegistradaPorId = hora.HasValue ? registradaPorUserId : (int?)null;
            s.HoraSalidaRealRegistradaAt    = hora.HasValue ? DateTimeOffset.UtcNow : (DateTimeOffset?)null;
            await ctx.SaveChangesAsync();
        }

        public async Task SetHoraRetornoReal(int solicitudId, TimeOnly? hora, int registradaPorUserId)
        {
            using var ctx = _factory.CreateDbContext();
            var s = await ctx.GaSolicitudSalida.FirstOrDefaultAsync(x => x.Id == solicitudId)
                ?? throw new AbrilException("Solicitud no encontrada.", 404);

            s.HoraRetornoReal                = hora;
            s.HoraRetornoRealRegistradaPorId = hora.HasValue ? registradaPorUserId : (int?)null;
            s.HoraRetornoRealRegistradaAt    = hora.HasValue ? DateTimeOffset.UtcNow : (DateTimeOffset?)null;
            await ctx.SaveChangesAsync();
        }

        // ══ Reembolso ═══════════════════════════════════════════════════════

        /// <summary>
        /// Ids de la selección cuyo reembolso YA se puede decidir: rendidas, con Consolidado del
        /// S10 vigente (propio o heredado de su planilla) y con el reembolso todavía abierto
        /// (Pendiente o Rechazado — se puede reconsiderar un rechazo). Lo demás se ignora.
        /// </summary>
        private static async Task<List<int>> IdsConReembolsoRevisableAsync(AppDbContext ctx, List<int> ids)
        {
            if (ids.Count == 0) return new();

            var candidatas = await ctx.GaSolicitudSalida
                .Where(s => ids.Contains(s.Id)
                         && s.EstadoRendicionId == EstadosSalida.Rendicion.Rendido
                         && (s.EstadoReembolsoId == EstadosSalida.Reembolso.Pendiente
                          || s.EstadoReembolsoId == EstadosSalida.Reembolso.Rechazado))
                .Select(s => new { s.Id, s.RendicionId })
                .ToListAsync();

            if (candidatas.Count == 0) return new();

            var consolidados = await ConsolidadoS10Loader.LoadAsync(
                ctx, candidatas.ToDictionary(x => x.Id, x => x.RendicionId));

            return candidatas.Where(x => consolidados.ContainsKey(x.Id)).Select(x => x.Id).ToList();
        }

        public async Task<List<int>> DecidirReembolso(
            IEnumerable<int> ids, bool aprobar, string? observacion, int reviewerUserId)
        {
            var idsList = ids?.Distinct().ToList() ?? new List<int>();
            if (idsList.Count == 0) return new();

            if (!aprobar && string.IsNullOrWhiteSpace(observacion))
                throw new AbrilException("Para rechazar un reembolso hay que escribir la observación.", 400);

            using var ctx = _factory.CreateDbContext();

            var elegibles = await IdsConReembolsoRevisableAsync(ctx, idsList);
            if (elegibles.Count == 0)
                throw new AbrilException(
                    "Ninguna de las salidas seleccionadas tiene un reembolso por decidir: deben estar " +
                    "rendidas y con el Consolidado del S10 adjunto.", 400);

            var solicitudes = await ctx.GaSolicitudSalida
                .Where(s => elegibles.Contains(s.Id))
                .ToListAsync();

            // Nadie decide el reembolso de sus propias salidas (salvo Gerente), misma regla que la
            // aprobación de la salida. El chequeo se hace con UNA consulta para todo el lote y no
            // con EnsurePuedeDecidirAsync por fila: eso eran dos consultas por salida seleccionada.
            var misWorkers = await (
                from w in ctx.Worker
                join per in ctx.Person on w.PersonId equals per.PersonId
                where per.UserId == reviewerUserId
                select new
                {
                    w.Id,
                    CategoriaId = w.PuestoCatalogo != null ? w.PuestoCatalogo.CategoriaId : (int?)null
                }
            ).ToListAsync();

            var misWorkerIds = misWorkers.Select(x => x.Id).ToHashSet();
            var esGerente    = misWorkers.Any(x => x.CategoriaId == CategoriaIds.Gerente);

            if (!esGerente && solicitudes.Any(x => misWorkerIds.Contains(x.WorkerId)))
                throw new AbrilException(
                    "No puedes decidir el reembolso de tus propias salidas — deselecciónalas primero.", 403);

            var now  = DateTimeOffset.UtcNow;
            var obs  = aprobar ? null : observacion!.Trim();
            var next = aprobar ? EstadosSalida.Reembolso.Aprobado : EstadosSalida.Reembolso.Rechazado;

            foreach (var s in solicitudes)
            {
                s.EstadoReembolsoId      = next;
                s.ReembolsoDecididoPorId = reviewerUserId;
                s.ReembolsoDecididoAt    = now;
                s.UpdatedAt              = now;
                // Al aprobar se limpia la observación: ya no hay nada que subsanar. Al rechazar se
                // reemplaza por la nueva.
                s.ObservacionReembolso   = obs;
            }

            await ctx.SaveChangesAsync();
            return solicitudes.Select(s => s.Id).ToList();
        }

        public async Task<List<RendicionPorFirmarDto>> GetRendicionesPorFirmar(IEnumerable<int> ids)
        {
            var idsList = ids?.Distinct().ToList() ?? new List<int>();
            if (idsList.Count == 0) return new();

            using var ctx = _factory.CreateDbContext();

            var filas = await (
                from s in ctx.GaSolicitudSalida
                join r in ctx.GaRendicion on s.RendicionId equals r.Id
                where idsList.Contains(s.Id)
                   && s.EstadoReembolsoId == EstadosSalida.Reembolso.Aprobado
                   && s.EstadoRendicionId == EstadosSalida.Rendicion.Rendido
                select new
                {
                    SolicitudId = s.Id,
                    r.Id, r.PdfUrl, r.PdfFilename, r.PdfFirmadoUrl,
                }
            ).ToListAsync();

            return filas
                .GroupBy(x => x.Id)
                .Select(g => new RendicionPorFirmarDto
                {
                    RendicionId   = g.Key,
                    PdfUrl        = g.First().PdfUrl,
                    PdfFilename   = g.First().PdfFilename,
                    PdfFirmadoUrl = g.First().PdfFirmadoUrl,
                    SolicitudIds  = g.Select(x => x.SolicitudId).ToList(),
                })
                .ToList();
        }

        public async Task MarcarFirmadas(
            int rendicionId, IEnumerable<int> solicitudIds, int userId,
            string? pdfUrl, string? pdfItemId, string? pdfFilename)
        {
            var idsList = solicitudIds?.Distinct().ToList() ?? new List<int>();
            if (idsList.Count == 0) return;

            using var ctx = _factory.CreateDbContext();
            var now = DateTimeOffset.UtcNow;

            var rendicion = await ctx.GaRendicion.FirstOrDefaultAsync(r => r.Id == rendicionId)
                ?? throw new AbrilException("La planilla de rendición no existe.", 404);

            // Solo se guarda el archivo si esta firma lo generó. Si la planilla ya venía firmada no
            // se pisa: el PDF firmado que vale es el primero, y lo que falta es mover el estado de
            // las salidas que aún no estaban firmadas.
            if (!string.IsNullOrWhiteSpace(pdfUrl))
            {
                rendicion.PdfFirmadoUrl      = pdfUrl;
                rendicion.PdfFirmadoItemId   = pdfItemId;
                rendicion.PdfFirmadoFilename = pdfFilename;
                rendicion.FirmadoPorId       = userId;
                rendicion.FirmadoAt          = now;
            }

            var solicitudes = await ctx.GaSolicitudSalida
                .Where(s => idsList.Contains(s.Id)
                         && s.EstadoReembolsoId == EstadosSalida.Reembolso.Aprobado)
                .ToListAsync();

            foreach (var s in solicitudes)
            {
                s.EstadoReembolsoId = EstadosSalida.Reembolso.Firmado;
                s.FirmadoPorId      = userId;
                s.FirmadoAt         = now;
                s.UpdatedAt         = now;
            }

            await ctx.SaveChangesAsync();
        }

        public async Task<List<int>> MarcarPagadas(IEnumerable<int> ids, int tesoreroUserId)
        {
            var idsList = ids?.Distinct().ToList() ?? new List<int>();
            if (idsList.Count == 0) return new();

            using var ctx = _factory.CreateDbContext();

            var solicitudes = await ctx.GaSolicitudSalida
                .Where(s => idsList.Contains(s.Id)
                         && s.EstadoReembolsoId == EstadosSalida.Reembolso.Firmado)
                .ToListAsync();

            if (solicitudes.Count == 0)
                throw new AbrilException(
                    "Ninguna de las salidas seleccionadas está firmada: solo se puede marcar como pagado " +
                    "lo que la jefatura ya firmó.", 400);

            var now = DateTimeOffset.UtcNow;
            foreach (var s in solicitudes)
            {
                s.EstadoReembolsoId = EstadosSalida.Reembolso.Pagado;
                s.PagadoPorId       = tesoreroUserId;
                s.PagadoAt          = now;
                s.UpdatedAt         = now;
            }

            await ctx.SaveChangesAsync();
            return solicitudes.Select(s => s.Id).ToList();
        }

        public async Task<ReembolsoCorreoInfoDto?> GetReembolsoCorreoInfo(int solicitudId)
        {
            using var ctx = _factory.CreateDbContext();

            var head = await (
                from s in ctx.GaSolicitudSalida
                join w in ctx.Worker on s.WorkerId equals w.Id
                join per in ctx.Person on w.PersonId equals (int?)per.PersonId into perGroup
                from per in perGroup.DefaultIfEmpty()
                join u in ctx.User on (per != null ? per.UserId : null) equals (int?)u.UserId into uGroup
                from u in uGroup.DefaultIfEmpty()
                join r in ctx.GaRendicion on s.RendicionId equals (int?)r.Id into rGroup
                from r in rGroup.DefaultIfEmpty()
                where s.Id == solicitudId
                select new
                {
                    s.Id, WorkerInternalId = w.Id, w.AreaScopeId,
                    Trabajador = per != null ? (per.FullName ?? "Trabajador") : "Trabajador",
                    Email = u != null ? u.Email : null,
                    s.FechaSalida, s.EstadoReembolsoId, s.ObservacionReembolso, s.ReembolsoDecididoPorId,
                    NumeroPlanilla = r != null ? r.NumeroPlanilla : null,
                }
            ).FirstOrDefaultAsync();

            if (head == null) return null;

            var trayectoIds = await ctx.GaSolicitudTrayecto
                .Where(t => t.SolicitudId == solicitudId)
                .Select(t => t.Id)
                .ToListAsync();

            var monto = trayectoIds.Count == 0
                ? 0m
                : await ctx.GaSolicitudCaptura
                    .Where(c => trayectoIds.Contains(c.TrayectoId))
                    .SumAsync(c => (decimal?)c.Monto) ?? 0m;

            string? decididoPor = null;
            if (head.ReembolsoDecididoPorId.HasValue)
            {
                decididoPor = await ctx.Person
                    .Where(pe => pe.UserId == head.ReembolsoDecididoPorId.Value)
                    .Select(pe => pe.FullName)
                    .FirstOrDefaultAsync();
            }

            var arbolAreas = await CargarArbolAreasAsync(ctx);

            // Correlativo dentro del trabajador (el "#3" que ve él), no el id de la tabla: es el
            // mismo numero que usan los demas correos del flujo.
            var numeroUsuario = await ctx.GaSolicitudSalida
                .CountAsync(x => x.WorkerId == head.WorkerInternalId && x.Id <= head.Id);

            return new ReembolsoCorreoInfoDto
            {
                SolicitudId          = head.Id,
                WorkerId             = head.WorkerInternalId,
                NumeroUsuario        = numeroUsuario,
                Trabajador           = head.Trabajador,
                SolicitanteEmail     = head.Email,
                Area                 = AreaMasBaja(head.AreaScopeId, arbolAreas),
                FechaSalida          = head.FechaSalida,
                NumeroPlanilla       = head.NumeroPlanilla.HasValue ? $"TI: {head.NumeroPlanilla.Value:D6}" : null,
                TrayectosCount       = trayectoIds.Count,
                MontoTotal           = monto,
                EstadoReembolso      = EstadosSalida.Reembolso.Nombre(head.EstadoReembolsoId),
                ObservacionReembolso = head.ObservacionReembolso,
                DecididoPor          = decididoPor,
            };
        }

        private const string SubareaTi              = "Tecnología de la Información";
        private const string TipoAreaEstandar       = "Área Estándar";

        /// <summary>
        /// Para cada workerId dado (con area_scope_id), camina hacia arriba en el árbol
        /// area_scope y devuelve el nombre del primer nodo cuyo tipo (area_type.area_type_name)
        /// sea "Área Estándar". Si no encuentra uno, devuelve null para ese worker.
        /// </summary>
        private static async Task<Dictionary<int, string?>> ResolverAreaPorWorkerAsync(
            AppDbContext ctx,
            List<int> workerIds,
            Dictionary<int, int> workerToScope)
        {
            var resultado = new Dictionary<int, string?>();
            if (workerIds.Count == 0) return resultado;

            // Topología: scopeId → (nombre, tipo, padre)
            var nodos = await (
                from sc in ctx.AreaScope
                join it in ctx.AreaItem on sc.AreaItemId equals it.AreaItemId
                join tp in ctx.AreaType on it.AreaTypeId equals tp.AreaTypeId
                select new
                {
                    sc.AreaScopeId,
                    sc.AreaScopeParentId,
                    Nombre = it.AreaItemName,
                    Tipo   = tp.AreaTypeName,
                }
            ).ToDictionaryAsync(
                x => x.AreaScopeId,
                x => (Padre: x.AreaScopeParentId, Nombre: x.Nombre, Tipo: x.Tipo));

            foreach (var workerId in workerIds)
            {
                if (!workerToScope.TryGetValue(workerId, out var startScope)) continue;

                string? nombre = null;
                var seen = new HashSet<int>();
                int? curr = startScope;
                while (curr.HasValue && seen.Add(curr.Value) && nodos.TryGetValue(curr.Value, out var nodo))
                {
                    if (string.Equals(nodo.Tipo, TipoAreaEstandar, StringComparison.OrdinalIgnoreCase))
                    {
                        nombre = nodo.Nombre;
                        break;
                    }
                    curr = nodo.Padre;
                }
                resultado[workerId] = nombre;
            }
            return resultado;
        }

        /// <summary>
        /// Topología del árbol de áreas indexada por <c>area_scope_id</c>: nombre del nodo y su
        /// padre. Es una tabla chica (decenas de filas), se trae completa y se camina en memoria.
        /// No se filtra por <c>state</c> a propósito: un trabajador que quedó en un nodo dado de
        /// baja igual tiene que mostrar su área en vez de una celda vacía.
        /// </summary>
        private static async Task<Dictionary<int, (int? Padre, string Nombre)>> CargarArbolAreasAsync(AppDbContext ctx)
        {
            return await (
                from sc in ctx.AreaScope
                join it in ctx.AreaItem on sc.AreaItemId equals it.AreaItemId
                select new { sc.AreaScopeId, sc.AreaScopeParentId, Nombre = it.AreaItemName }
            ).ToDictionaryAsync(
                x => x.AreaScopeId,
                x => (Padre: x.AreaScopeParentId, Nombre: x.Nombre));
        }

        /// <summary>
        /// El área más baja a la que pertenece el trabajador: el nodo al que apunta directamente
        /// <c>workers.area_scope_id</c> (el último de <see cref="RutaArea"/>). Es lo único que se
        /// muestra en la tabla; el detalle muestra además la ruta completa.
        /// </summary>
        private static string? AreaMasBaja(
            int? areaScopeId,
            IReadOnlyDictionary<int, (int? Padre, string Nombre)> arbol)
            => areaScopeId.HasValue && arbol.TryGetValue(areaScopeId.Value, out var nodo) ? nodo.Nombre : null;

        /// <summary>
        /// Ruta del área desde la raíz hasta <paramref name="areaScopeId"/> (el nodo del propio
        /// trabajador), para el detalle. Vacía si el trabajador no tiene área. Si el árbol tuviera
        /// un ciclo (no debería) se corta solo.
        /// </summary>
        private static List<string> RutaArea(
            int? areaScopeId,
            IReadOnlyDictionary<int, (int? Padre, string Nombre)> arbol)
        {
            var ruta   = new List<string>();
            var vistos = new HashSet<int>();
            var actual = areaScopeId;
            while (actual.HasValue && vistos.Add(actual.Value) && arbol.TryGetValue(actual.Value, out var nodo))
            {
                ruta.Insert(0, nodo.Nombre);
                actual = nodo.Padre;
            }
            return ruta;
        }

        /// <summary>
        /// Carga el catálogo de trayectos activos en memoria. Llave: (lugar_origen_id, lugar_destino_id).
        /// </summary>
        private static async Task<Dictionary<(int, int), decimal>> CargarCatalogoTrayectosAsync(AppDbContext ctx)
        {
            var rows = await ctx.GaTrayecto
                .Where(g => g.Activo)
                .Select(g => new { g.LugarOrigenId, g.LugarDestinoId, g.Monto })
                .ToListAsync();
            return rows.ToDictionary(r => (r.LugarOrigenId, r.LugarDestinoId), r => r.Monto);
        }
    }
}
