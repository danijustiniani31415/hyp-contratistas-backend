using System.Security.Cryptography;
using System.Text;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Convalidacion;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Constants;
using Abril_Backend.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Repositories
{
    public class ConvalidacionRepository : IConvalidacionRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public ConvalidacionRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<PagedResponseDto<ConvalidacionListDto>> List(ConvalidacionFilterDto filter)
        {
            using var ctx = _factory.CreateDbContext();
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            var q = from cv in ctx.WorkerEmoConvalidacion
                    join e in ctx.WorkerEmo on cv.EmoId equals e.Id
                    join w in ctx.Worker on e.WorkerId equals w.Id
                    join per in ctx.Person on w.PersonId equals per.PersonId into perj
                    from per in perj.DefaultIfEmpty()
                    join et in ctx.SsEmoTipo on e.TipoEmoId equals et.Id into etj
                    from et in etj.DefaultIfEmpty()
                    join med in ctx.SsMedicoOcupacional on cv.MedicoId equals med.Id into medj
                    from med in medj.DefaultIfEmpty()
                    join eo in ctx.Contributor on e.EmpresaOrigenId equals eo.ContributorId into eoj
                    from eo in eoj.DefaultIfEmpty()
                    join ed in ctx.Contributor on cv.EmpresaDestinoId equals ed.ContributorId into edj
                    from ed in edj.DefaultIfEmpty()
                    join oso in ctx.WorkersObraOficinaStaff on cv.ObraOficinaStaffOrigenId equals (int?)oso.WorkersObraOficinaStaffId into osoj
                    from oso in osoj.DefaultIfEmpty()
                    join osd in ctx.WorkersObraOficinaStaff on cv.ObraOficinaStaffDestinoId equals (int?)osd.WorkersObraOficinaStaffId into osdj
                    from osd in osdj.DefaultIfEmpty()
                    select new { cv, e, per, et, med, eo, ed, w, oso, osd };

            if (filter.WorkerId.HasValue)
                q = q.Where(x => x.e.WorkerId == filter.WorkerId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Resultado))
                q = q.Where(x => x.cv.Resultado == filter.Resultado);

            if (filter.EmpresaDestinoId.HasValue)
                q = q.Where(x => x.cv.EmpresaDestinoId == filter.EmpresaDestinoId.Value);

            if (filter.TipoEmoId.HasValue)
                q = q.Where(x => x.e.TipoEmoId == filter.TipoEmoId.Value);

            if (filter.ProyectoId.HasValue)
                q = q.Where(x => ctx.WorkerVinculacion
                    .Any(v => v.WorkerId == x.e.WorkerId && v.FechaFin == null && v.ProyectoId == filter.ProyectoId.Value));

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.ToLower().Trim();
                q = q.Where(x =>
                    (x.per != null && x.per.FullName.ToLower().Contains(term)) ||
                    (x.per != null && x.per.DocumentIdentityCode.ToLower().Contains(term)));
            }

            var total = await q.CountAsync();
            var page = Math.Max(filter.Page, 1);
            var pageSize = Math.Max(filter.PageSize, 1);

            var items = await q
                .OrderByDescending(x => x.cv.FechaConvalidacion)
                .ThenByDescending(x => x.cv.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ConvalidacionListDto
                {
                    Id = x.cv.Id,
                    EmoOrigenId = x.cv.EmoId,
                    WorkerId = x.e.WorkerId,
                    WorkerNombre = x.per != null ? x.per.FullName : null,
                    WorkerDni = x.per != null ? x.per.DocumentIdentityCode : null,
                    EmpresaOrigen = x.eo != null ? x.eo.ContributorName : null,
                    EmpresaDestinoId = x.cv.EmpresaDestinoId,
                    EmpresaDestino = x.ed != null ? x.ed.ContributorName : null,
                    Proyecto = (from v in ctx.WorkerVinculacion
                                join pr in ctx.Project on v.ProyectoId equals (int?)pr.ProjectId
                                where v.WorkerId == x.e.WorkerId && v.FechaFin == null
                                orderby v.CreatedAt descending
                                select (string?)pr.ProjectDescription)
                               .FirstOrDefault(),
                    TipoEmo = x.et != null ? x.et.Nombre : null,
                    Medico = x.med != null ? x.med.ApellidoNombre : null,
                    FechaEmoOrigen = x.e.FechaEmo,
                    FechaConvalidacion = x.cv.FechaConvalidacion,
                    Resultado = x.cv.Resultado,
                    FechaVencimiento = x.cv.FechaVencimiento,
                    Notas = x.cv.Observaciones,
                    UrlDocumento = x.cv.UrlDocumento,
                    EmoFechaVencimiento = x.e.FechaVencimientoCalculada ?? x.e.FechaVencimiento,
                    UrlResultado = x.e.UrlResultado,
                    UrlAptitud = x.e.UrlAptitud,
                    UrlEmoCompleto = x.e.UrlEmoCompleto,
                    InterconsultaEstado = ctx.SsInterconsulta
                        .Where(i => i.EmoId == x.e.Id)
                        .OrderByDescending(i => i.FechaDerivacion)
                        .Select(i => (string?)i.Estado)
                        .FirstOrDefault(),
                    InterconsultaEspecialidad = ctx.SsInterconsulta
                        .Where(i => i.EmoId == x.e.Id)
                        .OrderByDescending(i => i.FechaDerivacion)
                        .Select(i => (string?)i.Especialidad)
                        .FirstOrDefault(),
                    InterconsultaUrlInforme = ctx.SsInterconsulta
                        .Where(i => i.EmoId == x.e.Id)
                        .OrderByDescending(i => i.FechaDerivacion)
                        .Select(i => (string?)i.UrlInforme)
                        .FirstOrDefault(),
                    PuestoOrigen = x.cv.PuestoOrigen ?? (x.w.PuestoCatalogo == null ? null : x.w.PuestoCatalogo.Nombre),
                    PuestoDestino = x.cv.PuestoDestino,
                    CategoriaOrigen = x.cv.CategoriaOrigen ?? (x.w.PuestoCatalogo == null || x.w.PuestoCatalogo.Categoria == null ? null : x.w.PuestoCatalogo.Categoria.Nombre),
                    CategoriaDestino = x.cv.CategoriaDestino ?? (x.w.PuestoCatalogo == null || x.w.PuestoCatalogo.Categoria == null ? null : x.w.PuestoCatalogo.Categoria.Nombre),
                    ObraOficinaStaffOrigenId = x.cv.ObraOficinaStaffOrigenId ?? x.w.ObraOficinaStaffId,
                    ObraOficinaStaffOrigenNombre = x.oso != null ? x.oso.Name
                        : (x.w.ObraOficinaStaff != null ? x.w.ObraOficinaStaff.Name : null),
                    ObraOficinaStaffDestinoId = x.cv.ObraOficinaStaffDestinoId,
                    ObraOficinaStaffDestinoNombre = x.osd != null ? x.osd.Name : null,
                    CambioRiesgo = x.cv.CambioRiesgo
                })
                .ToListAsync();

            foreach (var it in items)
            {
                if (it.FechaVencimiento.HasValue)
                    it.DiasParaVencer = it.FechaVencimiento.Value.DayNumber - hoy.DayNumber;
                it.RiesgoOrigen = ObraOficinaStaffIds.RiesgoEmo(it.ObraOficinaStaffOrigenId);
                it.RiesgoDestino = ObraOficinaStaffIds.RiesgoEmo(it.ObraOficinaStaffDestinoId);
            }

            return new PagedResponseDto<ConvalidacionListDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Data = items
            };
        }

        public async Task<ConvalidacionDetalleDto?> GetDetalleAsync(int id)
        {
            using var ctx = _factory.CreateDbContext();

            var row = await (
                from cv in ctx.WorkerEmoConvalidacion
                join e in ctx.WorkerEmo on cv.EmoId equals e.Id
                join w in ctx.Worker on e.WorkerId equals w.Id
                join per in ctx.Person on w.PersonId equals per.PersonId into perj
                from per in perj.DefaultIfEmpty()
                join et in ctx.SsEmoTipo on e.TipoEmoId equals et.Id into etj
                from et in etj.DefaultIfEmpty()
                join med in ctx.SsMedicoOcupacional on cv.MedicoId equals med.Id into medj
                from med in medj.DefaultIfEmpty()
                join eo in ctx.Contributor on e.EmpresaOrigenId equals eo.ContributorId into eoj
                from eo in eoj.DefaultIfEmpty()
                join ed in ctx.Contributor on cv.EmpresaDestinoId equals ed.ContributorId into edj
                from ed in edj.DefaultIfEmpty()
                join oso in ctx.WorkersObraOficinaStaff on cv.ObraOficinaStaffOrigenId equals (int?)oso.WorkersObraOficinaStaffId into osoj
                from oso in osoj.DefaultIfEmpty()
                join osd in ctx.WorkersObraOficinaStaff on cv.ObraOficinaStaffDestinoId equals (int?)osd.WorkersObraOficinaStaffId into osdj
                from osd in osdj.DefaultIfEmpty()
                where cv.Id == id
                select new ConvalidacionDetalleDto
                {
                    Id = cv.Id,
                    WorkerId = e.WorkerId,
                    WorkerNombre = per != null ? per.FullName : "",
                    WorkerDni = per != null ? per.DocumentIdentityCode : "",
                    WorkerPuesto = w.PuestoCatalogo == null ? null : w.PuestoCatalogo.Nombre,
                    TipoEmo = et != null ? et.Nombre : null,
                    FechaEmoOrigen = e.FechaEmo,
                    AptitudOrigen = e.Aptitud,
                    EmpresaOrigen = eo != null ? eo.ContributorName : null,
                    EmpresaDestino = ed != null ? ed.ContributorName : null,
                    MedicoNombre = med != null ? med.ApellidoNombre : null,
                    MedicoEspecialidad = med != null ? med.Especialidad : null,
                    MedicoRegistroCmp = med != null ? med.Cmp : null,
                    FechaConvalidacion = cv.FechaConvalidacion,
                    FechaVencimiento = cv.FechaVencimiento,
                    Resultado = cv.Resultado,
                    Notas = cv.Observaciones,
                    PuestoOrigen = cv.PuestoOrigen ?? (w.PuestoCatalogo == null ? null : w.PuestoCatalogo.Nombre),
                    PuestoDestino = cv.PuestoDestino,
                    CategoriaOrigen = cv.CategoriaOrigen ?? (w.PuestoCatalogo == null || w.PuestoCatalogo.Categoria == null ? null : w.PuestoCatalogo.Categoria.Nombre),
                    CategoriaDestino = cv.CategoriaDestino ?? (w.PuestoCatalogo == null || w.PuestoCatalogo.Categoria == null ? null : w.PuestoCatalogo.Categoria.Nombre),
                    ObraOficinaStaffOrigenId = cv.ObraOficinaStaffOrigenId ?? w.ObraOficinaStaffId,
                    ObraOficinaStaffOrigenNombre = oso != null ? oso.Name
                        : (w.ObraOficinaStaff != null ? w.ObraOficinaStaff.Name : null),
                    ObraOficinaStaffDestinoId = cv.ObraOficinaStaffDestinoId,
                    ObraOficinaStaffDestinoNombre = osd != null ? osd.Name : null,
                    CambioRiesgo = cv.CambioRiesgo
                }
            ).FirstOrDefaultAsync();

            if (row != null)
            {
                row.RiesgoOrigen = ObraOficinaStaffIds.RiesgoEmo(row.ObraOficinaStaffOrigenId);
                row.RiesgoDestino = ObraOficinaStaffIds.RiesgoEmo(row.ObraOficinaStaffDestinoId);
            }

            return row;
        }

        public async Task<MedicoFirmaEstadoDto?> GetMedicoFirmaEstadoAsync(int medicoId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsMedicoOcupacional
                .Where(m => m.Id == medicoId)
                .Select(m => new MedicoFirmaEstadoDto
                {
                    PinFirmaHash = m.PinFirmaHash,
                    Email = m.Email,
                    PinFirmaIntentosFallidos = m.PinFirmaIntentosFallidos,
                    PinFirmaBloqueadoHasta = m.PinFirmaBloqueadoHasta
                })
                .FirstOrDefaultAsync();
        }

        public async Task RegistrarIntentoFirmaAsync(int medicoId, bool exito)
        {
            using var ctx = _factory.CreateDbContext();
            var med = await ctx.SsMedicoOcupacional.FirstOrDefaultAsync(m => m.Id == medicoId);
            if (med == null) return;

            if (exito)
            {
                med.PinFirmaIntentosFallidos = 0;
                med.PinFirmaBloqueadoHasta = null;
            }
            else
            {
                med.PinFirmaIntentosFallidos++;
                if (med.PinFirmaIntentosFallidos >= 5)
                {
                    med.PinFirmaBloqueadoHasta = DateTimeOffset.UtcNow.AddMinutes(15);
                    med.PinFirmaIntentosFallidos = 0;
                }
            }
            await ctx.SaveChangesAsync();
        }

        private static readonly HashSet<string> ResultadosAprobados = new()
        {
            "Aprobada", "Aprobada con Observaciones"
        };

        /// <summary>
        /// Valida el cambio de puesto (riesgo origen vs destino) y bloquea la convalidación
        /// cuando sube de riesgo bajo a alto (Oficina Central → Staff/Obra): en ese caso la
        /// R.M. 312-2011/MINSA exige un EMO nuevo, no es un caso convalidable.
        /// </summary>
        private static bool ValidarCambioRiesgo(int? origenId, int? destinoId, string resultado)
        {
            var cambioRiesgo = ObraOficinaStaffIds.EsCambioRiesgoCritico(origenId, destinoId);
            if (cambioRiesgo && ResultadosAprobados.Contains(resultado))
                throw new AbrilException(
                    "No se puede convalidar: el puesto destino (Staff/Obra, riesgo alto) exige exámenes " +
                    "que el EMO de origen (Oficina Central, riesgo bajo) no evaluó. Corresponde generar un " +
                    "EMO nuevo conforme al protocolo del puesto destino.", 400);
            return cambioRiesgo;
        }

        /// <summary>
        /// SHA-256 de los datos exactos que el médico autorizó, para poder demostrar más
        /// adelante que la convalidación no fue alterada después de firmada.
        /// </summary>
        private static string CalcularDocumentoHash(WorkerEmoConvalidacion ent)
        {
            var raw = string.Join('|',
                ent.EmoId, ent.EmpresaDestinoId, ent.FechaConvalidacion, ent.FechaVencimiento,
                ent.MedicoId, ent.Resultado, ent.PuestoOrigen, ent.PuestoDestino,
                ent.ObraOficinaStaffOrigenId, ent.ObraOficinaStaffDestinoId, ent.Observaciones);
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes);
        }

        private static void RegistrarFirma(
            AppDbContext ctx, int convalidacionId, int? medicoId, string resultado,
            string documentoHash, string? ip, string? userAgent)
        {
            ctx.SsConvalidacionFirmaLog.Add(new SsConvalidacionFirmaLog
            {
                ConvalidacionId = convalidacionId,
                MedicoId = medicoId,
                FechaHora = DateTimeOffset.UtcNow,
                Ip = ip,
                UserAgent = userAgent,
                DocumentoHash = documentoHash,
                Resultado = resultado,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        /// <summary>
        /// Puesto/clasificación "de origen" = lo que el trabajador tenía en la empresa de
        /// origen del EMO, a la fecha del EMO (histórico, vía WorkerVinculacion — ya no el
        /// registro actual del trabajador, que desde que Habilitación gestiona el puesto
        /// correctamente representa el DESTINO, no el origen).
        /// </summary>
        private static async Task<(string? Puesto, int? ObraOficinaStaffId)> ResolverPuestoOrigenAsync(
            AppDbContext ctx, WorkerEmo emo)
        {
            var vinculacionOrigen = await ctx.WorkerVinculacion
                .Where(v => v.WorkerId == emo.WorkerId
                         && v.EmpresaId == emo.EmpresaOrigenId
                         && v.FechaInicio <= emo.FechaEmo)
                .OrderByDescending(v => v.FechaInicio)
                .FirstOrDefaultAsync();

            return (
                vinculacionOrigen?.Puesto ?? emo.Worker?.PuestoCatalogo?.Nombre,
                vinculacionOrigen?.ObraOficinaStaffId ?? emo.Worker?.ObraOficinaStaffId
            );
        }

        /// <summary>
        /// Puesto/clasificación "de destino" = el registro VIGENTE del trabajador (vinculación
        /// activa), que ya llega listo si el administrador hizo el cambio en Habilitación →
        /// Cambiar obra / puesto de trabajo antes de convalidar.
        /// </summary>
        private static async Task<(string? Puesto, int? ObraOficinaStaffId)> ResolverPuestoDestinoAsync(
            AppDbContext ctx, WorkerEmo emo)
        {
            var vinculacionActual = await ctx.WorkerVinculacion
                .Where(v => v.WorkerId == emo.WorkerId && v.FechaFin == null)
                .OrderByDescending(v => v.FechaInicio)
                .FirstOrDefaultAsync();

            return (
                vinculacionActual?.Puesto ?? emo.Worker?.PuestoCatalogo?.Nombre,
                vinculacionActual?.ObraOficinaStaffId ?? emo.Worker?.ObraOficinaStaffId
            );
        }

        /// <summary>
        /// Categoría "de origen" = la que el trabajador tenía congelada en la vinculación con la
        /// empresa de origen del EMO, a la fecha del EMO — igual criterio que
        /// <see cref="ResolverPuestoOrigenAsync"/>. Solo existe con precisión histórica para
        /// vinculaciones creadas después de que <c>worker_vinculaciones.categoria_id</c> empezó a
        /// congelarse; para registros previos cae al valor ACTUAL del trabajador como mejor esfuerzo.
        /// </summary>
        private static async Task<string?> ResolverCategoriaOrigenAsync(AppDbContext ctx, WorkerEmo emo)
        {
            var vinculacionOrigen = await ctx.WorkerVinculacion
                .Include(v => v.Categoria)
                .Where(v => v.WorkerId == emo.WorkerId
                         && v.EmpresaId == emo.EmpresaOrigenId
                         && v.FechaInicio <= emo.FechaEmo)
                .OrderByDescending(v => v.FechaInicio)
                .FirstOrDefaultAsync();

            return vinculacionOrigen?.Categoria?.Nombre ?? emo.Worker?.PuestoCatalogo?.Categoria?.Nombre;
        }

        /// <summary>Categoría "de destino" = el registro VIGENTE del trabajador (vinculación activa).</summary>
        private static async Task<string?> ResolverCategoriaDestinoAsync(AppDbContext ctx, WorkerEmo emo)
        {
            var vinculacionActual = await ctx.WorkerVinculacion
                .Include(v => v.Categoria)
                .Where(v => v.WorkerId == emo.WorkerId && v.FechaFin == null)
                .OrderByDescending(v => v.FechaInicio)
                .FirstOrDefaultAsync();

            return vinculacionActual?.Categoria?.Nombre ?? emo.Worker?.PuestoCatalogo?.Categoria?.Nombre;
        }

        public async Task<int> Create(ConvalidacionCreateDto dto, int? userId, string? ip, string? userAgent)
        {
            using var ctx = _factory.CreateDbContext();
            var emo = await ctx.WorkerEmo
                .Include(e => e.Worker).ThenInclude(w => w!.PuestoCatalogo).ThenInclude(pu => pu!.Categoria)
                .FirstOrDefaultAsync(e => e.Id == dto.EmoOrigenId)
                ?? throw new AbrilException("EMO no encontrado.", 404);

            var (puestoOrigenAuto, obraOficinaOrigenAuto) = await ResolverPuestoOrigenAsync(ctx, emo);
            var (puestoDestinoAuto, obraOficinaDestinoAuto) = await ResolverPuestoDestinoAsync(ctx, emo);
            var categoriaOrigenAuto = await ResolverCategoriaOrigenAsync(ctx, emo);
            var categoriaDestinoAuto = await ResolverCategoriaDestinoAsync(ctx, emo);

            var puestoOrigen = dto.PuestoOrigen ?? puestoOrigenAuto;
            var origenId = dto.ObraOficinaStaffOrigenId ?? obraOficinaOrigenAuto;
            var puestoDestino = dto.PuestoDestino ?? puestoDestinoAuto;
            var destinoId = dto.ObraOficinaStaffDestinoId ?? obraOficinaDestinoAuto;

            var cambioRiesgo = ValidarCambioRiesgo(origenId, destinoId, dto.Resultado);

            var ent = new WorkerEmoConvalidacion
            {
                EmoId = dto.EmoOrigenId,
                EmpresaDestinoId = dto.EmpresaDestinoId,
                FechaConvalidacion = dto.FechaConvalidacion,
                MedicoId = dto.MedicoId,
                Resultado = dto.Resultado,
                FechaVencimiento = dto.FechaVencimiento,
                UrlDocumento = dto.UrlDocumento,
                Observaciones = dto.Notas,
                PuestoOrigen = puestoOrigen,
                PuestoDestino = puestoDestino,
                CategoriaOrigen = categoriaOrigenAuto,
                CategoriaDestino = categoriaDestinoAuto,
                ObraOficinaStaffOrigenId = origenId,
                ObraOficinaStaffDestinoId = destinoId,
                CambioRiesgo = cambioRiesgo,
                RegistradoPorId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            ctx.WorkerEmoConvalidacion.Add(ent);

            await SincronizarHabilitacionAsync(ctx, emo, dto.Resultado, dto.FechaVencimiento);

            await ctx.SaveChangesAsync();

            RegistrarFirma(ctx, ent.Id, dto.MedicoId, dto.Resultado, CalcularDocumentoHash(ent), ip, userAgent);
            await ctx.SaveChangesAsync();

            return ent.Id;
        }

        public async Task Update(int id, ConvalidacionUpdateDto dto, int? userId, string? ip, string? userAgent)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = await ctx.WorkerEmoConvalidacion.FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new AbrilException("Convalidación no encontrada.", 404);

            var emo = await ctx.WorkerEmo
                .Include(e => e.Worker).ThenInclude(w => w!.PuestoCatalogo).ThenInclude(pu => pu!.Categoria)
                .FirstOrDefaultAsync(e => e.Id == ent.EmoId)
                ?? throw new AbrilException("EMO de origen no encontrado.", 404);

            string? puestoOrigenAuto = null, puestoDestinoAuto = null;
            int? obraOficinaOrigenAuto = null, obraOficinaDestinoAuto = null;
            if (ent.ObraOficinaStaffOrigenId is null || string.IsNullOrWhiteSpace(ent.PuestoOrigen))
                (puestoOrigenAuto, obraOficinaOrigenAuto) = await ResolverPuestoOrigenAsync(ctx, emo);
            if (ent.ObraOficinaStaffDestinoId is null || string.IsNullOrWhiteSpace(ent.PuestoDestino))
                (puestoDestinoAuto, obraOficinaDestinoAuto) = await ResolverPuestoDestinoAsync(ctx, emo);

            string? categoriaOrigenAuto = null, categoriaDestinoAuto = null;
            if (string.IsNullOrWhiteSpace(ent.CategoriaOrigen))
                categoriaOrigenAuto = await ResolverCategoriaOrigenAsync(ctx, emo);
            if (string.IsNullOrWhiteSpace(ent.CategoriaDestino))
                categoriaDestinoAuto = await ResolverCategoriaDestinoAsync(ctx, emo);

            var origenId = dto.ObraOficinaStaffOrigenId ?? ent.ObraOficinaStaffOrigenId ?? obraOficinaOrigenAuto;
            var destinoId = dto.ObraOficinaStaffDestinoId ?? ent.ObraOficinaStaffDestinoId ?? obraOficinaDestinoAuto;
            var cambioRiesgo = ValidarCambioRiesgo(origenId, destinoId, dto.Resultado);

            ent.EmpresaDestinoId = dto.EmpresaDestinoId;
            ent.FechaConvalidacion = dto.FechaConvalidacion;
            ent.MedicoId = dto.MedicoId;
            ent.Resultado = dto.Resultado;
            ent.FechaVencimiento = dto.FechaVencimiento;
            ent.UrlDocumento = dto.UrlDocumento;
            ent.Observaciones = dto.Notas;
            ent.PuestoOrigen = dto.PuestoOrigen ?? ent.PuestoOrigen ?? puestoOrigenAuto;
            ent.PuestoDestino = dto.PuestoDestino ?? ent.PuestoDestino ?? puestoDestinoAuto;
            ent.CategoriaOrigen = ent.CategoriaOrigen ?? categoriaOrigenAuto;
            ent.CategoriaDestino = ent.CategoriaDestino ?? categoriaDestinoAuto;
            ent.ObraOficinaStaffOrigenId = origenId;
            ent.ObraOficinaStaffDestinoId = destinoId;
            ent.CambioRiesgo = cambioRiesgo;
            ent.UpdatedAt = DateTimeOffset.UtcNow;

            await SincronizarHabilitacionAsync(ctx, emo, dto.Resultado, dto.FechaVencimiento);

            await ctx.SaveChangesAsync();

            RegistrarFirma(ctx, ent.Id, ent.MedicoId, ent.Resultado, CalcularDocumentoHash(ent), ip, userAgent);
            await ctx.SaveChangesAsync();
        }

        private static async Task SincronizarHabilitacionAsync(
            AppDbContext ctx, WorkerEmo emo, string resultado, DateOnly? fechaVencimiento)
        {
            var workerId = emo.WorkerId;

            var hab = await ctx.SsHabTrabajador
                .FirstOrDefaultAsync(h => h.WorkerId == workerId && h.ItemId == HabItemIds.CertAptitud);

            if (hab == null)
            {
                hab = new SsHabTrabajador
                {
                    WorkerId = workerId,
                    ItemId = HabItemIds.CertAptitud,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                ctx.SsHabTrabajador.Add(hab);
            }

            switch (resultado)
            {
                case "Aprobada":
                case "Aprobada con Observaciones":
                    emo.Estado = "Convalidado";
                    emo.UpdatedAt = DateTimeOffset.UtcNow;
                    hab.Estado = "Aprobado";
                    if (fechaVencimiento.HasValue)
                    {
                        hab.Vigencia = DateTime.SpecifyKind(
                            fechaVencimiento.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
                        // La convalidación reemplaza la vigencia del EMO de origen por la nueva
                        // fecha que el médico acaba de aprobar — sin esto, worker_emos se queda
                        // con la fecha vieja (a menudo ya vencida) para siempre, y cualquier
                        // filtro/proceso que lea el EMO directo (p.ej. "EMO Vencido" en la lista
                        // de Trabajadores) contradice al ítem visible, que sí queda con la fecha
                        // nueva. Caso Huarcaya Mesajil: ítem "Aprobado" con vigencia 2027, pero el
                        // EMO seguía marcado vencido porque su fecha nunca se actualizó.
                        emo.FechaVencimientoCalculada = fechaVencimiento;
                        emo.FechaVencimiento = fechaVencimiento;
                    }
                    break;
                case "Rechazada":
                    hab.Estado = "Falta";
                    break;
                case "Pendiente":
                    hab.Estado = "Pendiente";
                    break;
                case "Descartada":
                    // No es una decisión médica sobre aptitud (no exige firma): descarta un
                    // registro que no correspondía, p.ej. porque el trabajador ya estaba
                    // convalidado hacia el mismo destino. No se fuerza "Aprobado" a ciegas —
                    // se recalcula según la vigencia REAL del EMO de origen, como si la
                    // convalidación nunca se hubiera disparado.
                    var vencimientoReal = emo.FechaVencimientoCalculada ?? emo.FechaVencimiento;
                    var vigente = vencimientoReal.HasValue
                        && vencimientoReal.Value.ToDateTime(TimeOnly.MinValue) > DateTime.UtcNow;
                    hab.Estado = vigente ? "Aprobado" : "Vencido";
                    hab.Vigencia = vencimientoReal.HasValue
                        ? DateTime.SpecifyKind(vencimientoReal.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
                        : null;
                    emo.Estado = vigente ? "Vigente" : "Vencido";
                    emo.UpdatedAt = DateTimeOffset.UtcNow;
                    break;
            }

            hab.UpdatedAt = DateTime.UtcNow;
        }
    }
}
