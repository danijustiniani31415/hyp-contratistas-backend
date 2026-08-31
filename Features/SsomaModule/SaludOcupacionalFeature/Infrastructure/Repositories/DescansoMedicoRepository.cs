using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.DescansoMedico;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Infrastructure;
using Abril_Backend.Features.SsomaModule.Shared;
using Abril_Backend.Features.SsomaModule.Shared.DescansoCertificados;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Repositories
{
    public class DescansoMedicoRepository : IDescansoMedicoRepository
    {
        private const int PageSize = 20;
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly ReactivosCacheVersion _reactivosCacheVersion;

        public DescansoMedicoRepository(IDbContextFactory<AppDbContext> factory, ReactivosCacheVersion reactivosCacheVersion)
        {
            _factory = factory;
            _reactivosCacheVersion = reactivosCacheVersion;
        }

        /// <summary>
        /// Catálogo ss_descanso_tipo. <paramref name="soloMiSalud"/> = true devuelve únicamente los
        /// tipos que el trabajador puede elegir desde Mi Salud (los "común").
        /// </summary>
        public async Task<List<DescansoTipoDto>> GetTipos(bool soloMiSalud = false)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsDescansoTipo
                .Where(t => t.State && t.Active && (!soloMiSalud || t.DisponibleMiSalud))
                .OrderBy(t => t.Orden).ThenBy(t => t.Nombre)
                .Select(t => new DescansoTipoDto
                {
                    Id          = t.Id,
                    Nombre      = t.Nombre,
                    NombreCorto = t.NombreCorto ?? t.Nombre,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Resuelve el id de un tipo por su nombre de catálogo. Para los descansos que crea el
        /// sistema (p. ej. los que genera el Tópico Médico), que no pasan por un desplegable.
        /// </summary>
        public async Task<int> GetTipoIdPorNombre(string nombre)
        {
            using var ctx = _factory.CreateDbContext();
            var id = await ctx.SsDescansoTipo
                .Where(t => t.State && t.Nombre == nombre)
                .Select(t => (int?)t.Id)
                .FirstOrDefaultAsync();
            return id ?? throw new AbrilException($"No se encontró el tipo de descanso '{nombre}' en el catálogo.", 500);
        }

        public async Task<PagedResult<DescansoMedicoListItemDto>> ListPaged(DescansoMedicoFilterDto filter)
        {
            using var ctx = _factory.CreateDbContext();

            var q =
                from d in ctx.SsDescansoMedico
                where d.State
                join w in ctx.Worker on d.WorkerId equals w.Id
                join t in ctx.SsDescansoTipo on d.TipoId equals t.Id
                join em in ctx.Contributor on d.EmpresaId equals em.ContributorId into emj
                from em in emj.DefaultIfEmpty()
                select new { d, w, t, em };

            if (filter.WorkerId.HasValue)
                q = q.Where(x => x.d.WorkerId == filter.WorkerId.Value);
            if (!string.IsNullOrWhiteSpace(filter.Estado))
                q = q.Where(x => x.d.Estado == filter.Estado);
            if (filter.TipoId.HasValue)
                q = q.Where(x => x.d.TipoId == filter.TipoId.Value);
            if (filter.EmpresaId.HasValue)
                q = q.Where(x => x.d.EmpresaId == filter.EmpresaId.Value);
            if (filter.FechaDesde.HasValue)
                q = q.Where(x => x.d.FechaInicio >= filter.FechaDesde.Value);
            if (filter.FechaHasta.HasValue)
                q = q.Where(x => x.d.FechaInicio <= filter.FechaHasta.Value);

            var total = await q.CountAsync();
            var page = filter.Page < 1 ? 1 : filter.Page;

            var items = await q
                .OrderByDescending(x => x.d.FechaInicio)
                .ThenByDescending(x => x.d.CreatedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(x => new DescansoMedicoListItemDto
                {
                    Id = x.d.Id,
                    CasoId = x.d.CasoId,
                    WorkerId = x.d.WorkerId,
                    WorkerNombre = x.w.Person != null ? x.w.Person.FullName : null,
                    WorkerDni = x.w.Person != null ? x.w.Person.DocumentIdentityCode : null,
                    EmpresaNombre = x.em != null ? x.em.ContributorName : null,
                    TipoId = x.d.TipoId,
                    Tipo = x.t.Nombre,
                    FechaInicio = x.d.FechaInicio,
                    FechaFin = x.d.FechaFin,
                    Dias = x.d.Dias,
                    Estado = x.d.Estado,
                    TopicoOrigenId = x.d.TopicoOrigenId,
                    TrabajadorBloqueado = ctx.SsTrabajadorRestringido.Any(r => r.WorkerId == x.d.WorkerId && r.Activo),
                    ReportadoPorTrabajador = x.d.ReportadoPorTrabajador,
                    CreatedAt = x.d.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<DescansoMedicoListItemDto>
            {
                Page = page,
                PageSize = PageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)PageSize),
                Data = items
            };
        }

        public async Task<DescansoMedicoDetalleDto> GetById(int id)
        {
            using var ctx = _factory.CreateDbContext();

            var row = await (
                from d in ctx.SsDescansoMedico
                where d.Id == id && d.State
                join w in ctx.Worker on d.WorkerId equals w.Id
                join t in ctx.SsDescansoTipo on d.TipoId equals t.Id
                join em in ctx.Contributor on d.EmpresaId equals em.ContributorId into emj
                from em in emj.DefaultIfEmpty()
                join c10 in ctx.Cie10Catalogo on d.DiagnosticoCie10Codigo equals c10.Codigo into c10j
                from c10 in c10j.DefaultIfEmpty()
                select new { d, w, t, em, c10 }
            ).FirstOrDefaultAsync()
              ?? throw new AbrilException("Descanso médico no encontrado.", 404);

            var adjuntos = await ctx.SsDescansoMedicoAdjunto
                .Where(a => a.State && a.DescansoId == id)
                .OrderBy(a => a.Id)
                .Select(a => new DescansoAdjuntoDto { Id = a.Id, Url = a.Url, Nombre = a.NombreArchivo })
                .ToListAsync();

            return new DescansoMedicoDetalleDto
            {
                Id = row.d.Id,
                CasoId = row.d.CasoId,
                WorkerId = row.d.WorkerId,
                WorkerNombre = row.w.Person != null ? row.w.Person.FullName : null,
                WorkerDni = row.w.Person != null ? row.w.Person.DocumentIdentityCode : null,
                ProyectoId = row.d.ProyectoId,
                EmpresaId = row.d.EmpresaId,
                EmpresaNombre = row.em != null ? row.em.ContributorName : null,
                TipoId = row.d.TipoId,
                Tipo = row.t.Nombre,
                FechaInicio = row.d.FechaInicio,
                FechaFin = row.d.FechaFin,
                Dias = row.d.Dias,
                Diagnostico = row.d.Diagnostico,
                DiagnosticoCie10 = row.d.DiagnosticoCie10,
                DiagnosticoCie10Codigo = row.d.DiagnosticoCie10Codigo,
                DiagnosticoCie10Descripcion = row.c10 != null ? row.c10.Descripcion : null,
                UrlCertificado = row.d.UrlCertificado,
                Adjuntos = adjuntos,
                UrlDocumento = row.d.UrlDocumento,
                Estado = row.d.Estado,
                MotivoRechazo = row.d.MotivoRechazo,
                AprobadoPorId = row.d.AprobadoPorId,
                FechaAprobacion = row.d.FechaAprobacion,
                AccidenteId = row.d.AccidenteId,
                EsRecaida = row.d.EsRecaida,
                NotificadoGth = row.d.NotificadoGth,
                NotificadoJefe = row.d.NotificadoJefe,
                ReportadoPorTrabajador = row.d.ReportadoPorTrabajador,
                Observaciones = row.d.Observaciones,
                TopicoOrigenId = row.d.TopicoOrigenId,
                ProrrogaDelId = row.d.ProrrogaDelId,
                FechaAlta = row.d.FechaAlta,
                AltaPorId = row.d.AltaPorId,
                AltaObservaciones = row.d.AltaObservaciones,
                RegistradoPorId = row.d.RegistradoPorId,
                CreatedAt = row.d.CreatedAt,
                UpdatedAt = row.d.UpdatedAt
            };
        }

        /// <param name="adjuntos">Certificados médicos ya subidos a la carpeta configurada en SharePoint.</param>
        public async Task<int> Create(DescansoMedicoCreateDto dto, int registradoPorId, List<DescansoCertificadoSubidoDto> adjuntos)
        {
            using var ctx = _factory.CreateDbContext();

            // El tipo tiene que existir en el catálogo: es el único clasificador del descanso.
            var tipoValido = await ctx.SsDescansoTipo.AnyAsync(t => t.Id == dto.TipoId && t.State && t.Active);
            if (!tipoValido)
                throw new AbrilException("El tipo de descanso seleccionado no es válido.", 400);

            if (dto.DiagnosticoCie10Codigo != null)
            {
                var cie10Valido = await ctx.Cie10Catalogo.AnyAsync(c => c.Codigo == dto.DiagnosticoCie10Codigo && c.Activo);
                if (!cie10Valido)
                    throw new AbrilException("El código CIE-10 indicado no es válido.", 400);
            }

            // Resuelve a qué caso pertenece este descanso — ver comentarios de CasoId/ProrrogaDelId
            // en DescansoMedicoCreateDto.
            SsDescansoCaso? nuevoCaso = null;
            int casoIdResuelto;

            if (dto.CasoId.HasValue)
            {
                var caso = await ctx.SsDescansoCaso.FirstOrDefaultAsync(c => c.Id == dto.CasoId.Value && c.State)
                    ?? throw new AbrilException("El caso indicado no existe.", 404);
                if (caso.Estado != "Abierto")
                    throw new AbrilException("Solo se puede agregar un descanso a un caso abierto.", 400);
                casoIdResuelto = caso.Id;
            }
            else if (dto.ProrrogaDelId.HasValue)
            {
                var origen = await ctx.SsDescansoMedico.FirstOrDefaultAsync(d => d.Id == dto.ProrrogaDelId.Value && d.State)
                    ?? throw new AbrilException("El descanso que se intenta extender no existe.", 404);
                casoIdResuelto = origen.CasoId;
            }
            else
            {
                nuevoCaso = new SsDescansoCaso
                {
                    WorkerId = dto.WorkerId,
                    FechaApertura = dto.FechaInicio,
                    Estado = "Abierto",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    State = true
                };
                ctx.SsDescansoCaso.Add(nuevoCaso);
                casoIdResuelto = 0; // se resuelve vía la navegación Caso al guardar.
            }

            var dias = dto.FechaFin.DayNumber - dto.FechaInicio.DayNumber + 1;

            var entity = new SsDescansoMedico
            {
                WorkerId = dto.WorkerId,
                CasoId = casoIdResuelto,
                Caso = nuevoCaso,
                TipoId = dto.TipoId,
                FechaInicio = dto.FechaInicio,
                FechaFin = dto.FechaFin,
                Dias = dias,
                Diagnostico = dto.Diagnostico,
                DiagnosticoCie10 = dto.DiagnosticoCie10,
                DiagnosticoCie10Codigo = dto.DiagnosticoCie10Codigo,
                // El primer adjunto también va a url_certificado para no romper las vistas
                // antiguas que muestran un único certificado.
                UrlCertificado = adjuntos.Count > 0 ? adjuntos[0].Url : null,
                Estado = "Pendiente",
                // Registrado por SSOMA, no autorreportado: Revisión de Descansos solo revisa
                // los que reporta el propio trabajador desde Mi Salud.
                ReportadoPorTrabajador = false,
                AccidenteId = dto.AccidenteId,
                EsRecaida = dto.EsRecaida,
                TopicoOrigenId = dto.TopicoOrigenId,
                ProrrogaDelId = dto.ProrrogaDelId,
                ProyectoId = dto.ProyectoId,
                EmpresaId = dto.EmpresaId,
                RegistradoPorId = registradoPorId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                State = true
            };

            ctx.SsDescansoMedico.Add(entity);

            foreach (var adjunto in adjuntos)
            {
                ctx.SsDescansoMedicoAdjunto.Add(new SsDescansoMedicoAdjunto
                {
                    Descanso      = entity,
                    Url           = adjunto.Url,
                    NombreArchivo = adjunto.Nombre,
                    DriveId       = adjunto.DriveId,
                    ItemId        = adjunto.ItemId,
                    State         = true,
                    CreatedAt     = DateTimeOffset.UtcNow,
                    UpdatedAt     = DateTimeOffset.UtcNow,
                });
            }

            await ctx.SaveChangesAsync();
            return entity.Id;
        }

        public async Task<DescansoAdjuntoArchivoDto?> GetAdjunto(int adjuntoId)
        {
            using var ctx = _factory.CreateDbContext();

            return await (
                from a in ctx.SsDescansoMedicoAdjunto
                join d in ctx.SsDescansoMedico on a.DescansoId equals d.Id
                where a.Id == adjuntoId && a.State && d.State
                select new DescansoAdjuntoArchivoDto
                {
                    Url           = a.Url,
                    NombreArchivo = a.NombreArchivo,
                    DriveId       = a.DriveId,
                    ItemId        = a.ItemId,
                }
            ).FirstOrDefaultAsync();
        }

        public async Task Update(int id, DescansoMedicoUpdateDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = await ctx.SsDescansoMedico.FirstOrDefaultAsync(d => d.Id == id && d.State)
                ?? throw new AbrilException("Descanso médico no encontrado.", 404);

            if (entity.Estado != "Pendiente")
                throw new AbrilException("Solo se puede editar un descanso en estado Pendiente.", 400);

            var tipoValido = await ctx.SsDescansoTipo.AnyAsync(t => t.Id == dto.TipoId && t.State && t.Active);
            if (!tipoValido)
                throw new AbrilException("El tipo de descanso seleccionado no es válido.", 400);

            if (dto.DiagnosticoCie10Codigo != null)
            {
                var cie10Valido = await ctx.Cie10Catalogo.AnyAsync(c => c.Codigo == dto.DiagnosticoCie10Codigo && c.Activo);
                if (!cie10Valido)
                    throw new AbrilException("El código CIE-10 indicado no es válido.", 400);
            }

            entity.TipoId = dto.TipoId;
            entity.FechaInicio = dto.FechaInicio;
            entity.FechaFin = dto.FechaFin;
            entity.Dias = dto.FechaFin.DayNumber - dto.FechaInicio.DayNumber + 1;
            entity.Diagnostico = dto.Diagnostico;
            entity.DiagnosticoCie10 = dto.DiagnosticoCie10;
            entity.DiagnosticoCie10Codigo = dto.DiagnosticoCie10Codigo;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            await ctx.SaveChangesAsync();
        }

        public async Task AsignarDiagnosticoCie10(int id, string? codigo)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = await ctx.SsDescansoMedico.FirstOrDefaultAsync(d => d.Id == id && d.State)
                ?? throw new AbrilException("Descanso médico no encontrado.", 404);

            if (codigo != null)
            {
                var cie10Valido = await ctx.Cie10Catalogo.AnyAsync(c => c.Codigo == codigo && c.Activo);
                if (!cie10Valido)
                    throw new AbrilException("El código CIE-10 indicado no es válido.", 400);
            }

            entity.DiagnosticoCie10Codigo = codigo;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }

        public async Task Aprobar(int id, DescansoAprobarDto dto, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = await ctx.SsDescansoMedico
                .Include(d => d.Worker)
                    .ThenInclude(w => w!.Person)
                .FirstOrDefaultAsync(d => d.Id == id && d.State)
                ?? throw new AbrilException("Descanso médico no encontrado.", 404);

            if (entity.Estado == "Aprobado")
                throw new AbrilException("El descanso ya está aprobado.", 400);

            entity.Estado = "Aprobado";
            entity.AprobadoPorId = userId;
            entity.FechaAprobacion = DateTimeOffset.UtcNow;
            if (dto.Observaciones != null)
                entity.Observaciones = dto.Observaciones;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            // Bloqueo automático en Control de Acceso para TODOS los descansos aprobados
            {
                var dni = entity.Worker?.Person?.DocumentIdentityCode;
                var nombre = entity.Worker?.Person?.FullName;

                var yaRestringido = await ctx.SsTrabajadorRestringido
                    .AnyAsync(r => r.WorkerId == entity.WorkerId && r.Activo);

                if (!yaRestringido)
                {
                    var anterior = await ctx.SsTrabajadorRestringido
                        .FirstOrDefaultAsync(r => r.WorkerId == entity.WorkerId && !r.Activo);

                    if (anterior is not null)
                    {
                        anterior.Activo = true;
                        anterior.Motivo = $"Descanso médico aprobado (ID {id})";
                        anterior.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        ctx.SsTrabajadorRestringido.Add(new SsTrabajadorRestringido
                        {
                            WorkerId = entity.WorkerId,
                            Dni = dni,
                            ApellidoNombre = nombre,
                            Motivo = $"Descanso médico aprobado (ID {id})",
                            FechaRestriccion = DateOnly.FromDateTime(DateTime.UtcNow),
                            Activo = true,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            await ctx.SaveChangesAsync();

            // Recalcular dias_descanso_reales en ss_accidente_trabajo
            if (entity.AccidenteId.HasValue)
            {
                using var ctx2 = _factory.CreateDbContext();
                var totalDias = await ctx2.SsDescansoMedico
                    .Where(d => d.AccidenteId == entity.AccidenteId && d.Estado == "Aprobado" && d.State)
                    .SumAsync(d => d.Dias);

                var accidente = await ctx2.SsAccidenteTrabajo.FindAsync(entity.AccidenteId.Value);
                if (accidente != null)
                {
                    accidente.DiasDescansoReales = totalDias;
                    accidente.UpdatedAt = DateTimeOffset.UtcNow;
                    await ctx2.SaveChangesAsync();
                    _reactivosCacheVersion.Bump();
                }
            }
        }

        public async Task Rechazar(int id, DescansoRechazarDto dto, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = await ctx.SsDescansoMedico.FirstOrDefaultAsync(d => d.Id == id && d.State)
                ?? throw new AbrilException("Descanso médico no encontrado.", 404);

            if (entity.Estado == "Rechazado")
                throw new AbrilException("El descanso ya está rechazado.", 400);

            entity.Estado = "Rechazado";
            entity.MotivoRechazo = dto.MotivoRechazo;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            await ctx.SaveChangesAsync();
        }

        /// <summary>Da de alta el CASO (no un descanso individual): lo cierra, cierra todos sus
        /// descansos Aprobados como Completado, y desbloquea al trabajador.</summary>
        public async Task DarAlta(int casoId, DarAltaDto dto, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var caso = await ctx.SsDescansoCaso.FirstOrDefaultAsync(c => c.Id == casoId && c.State)
                ?? throw new AbrilException("Caso no encontrado.", 404);

            if (caso.Estado == "Cerrado")
                throw new AbrilException("El caso ya está cerrado.", 400);

            // Si el caso fue reabierto alguna vez, exige que haya un descanso nuevo registrado
            // después de esa reapertura antes de poder volver a cerrarlo — evita dar de alta un
            // caso reabierto sin que se haya documentado nada nuevo.
            if (caso.FechaReapertura.HasValue)
            {
                var hayDescansoPosterior = await ctx.SsDescansoMedico.AnyAsync(d =>
                    d.CasoId == casoId && d.State && d.FechaInicio >= caso.FechaReapertura.Value);
                if (!hayDescansoPosterior)
                    throw new AbrilException(
                        "Debe registrar un nuevo descanso médico antes de poder dar el alta de este caso reabierto.", 400);
            }

            var pendientes = await ctx.SsDescansoMedico
                .Where(d => d.CasoId == casoId && d.State && d.Estado != "Aprobado" && d.Estado != "Completado")
                .ToListAsync();
            if (pendientes.Count > 0)
                throw new AbrilException("Todos los descansos del caso deben estar aprobados antes de dar el alta.", 400);

            caso.Estado = "Cerrado";
            caso.FechaCierre = DateOnly.FromDateTime(DateTime.UtcNow);
            caso.AltaPorId = userId;
            if (!string.IsNullOrWhiteSpace(dto.Observaciones))
                caso.AltaObservaciones = dto.Observaciones;
            caso.UpdatedAt = DateTimeOffset.UtcNow;

            var descansosAprobados = await ctx.SsDescansoMedico
                .Where(d => d.CasoId == casoId && d.State && d.Estado == "Aprobado")
                .ToListAsync();
            foreach (var d in descansosAprobados)
            {
                d.Estado = "Completado";
                d.UpdatedAt = DateTimeOffset.UtcNow;
            }

            // Desbloquear al trabajador en Control de Acceso
            var restriccion = await ctx.SsTrabajadorRestringido
                .FirstOrDefaultAsync(r => r.WorkerId == caso.WorkerId && r.Activo);
            if (restriccion is not null)
            {
                restriccion.Activo = false;
                restriccion.UpdatedAt = DateTime.UtcNow;
            }

            await ctx.SaveChangesAsync();
        }

        public async Task ReabrirCaso(int casoId, ReabrirCasoDto dto, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var caso = await ctx.SsDescansoCaso.FirstOrDefaultAsync(c => c.Id == casoId && c.State)
                ?? throw new AbrilException("Caso no encontrado.", 404);

            if (caso.Estado != "Cerrado")
                throw new AbrilException("Solo se puede reabrir un caso cerrado.", 400);

            // No se borra el alta anterior (fecha_cierre/alta_por_id/alta_observaciones quedan
            // como evidencia histórica) — solo se reabre y se marca cuándo, para exigir un
            // descanso nuevo antes de poder volver a cerrar (ver DarAlta).
            caso.Estado = "Abierto";
            caso.FechaReapertura = DateOnly.FromDateTime(DateTime.UtcNow);
            caso.UpdatedAt = DateTimeOffset.UtcNow;

            await ctx.SaveChangesAsync();
        }

        public async Task<CasoDetalleDto> GetCasoDetalle(int casoId)
        {
            using var ctx = _factory.CreateDbContext();

            var caso = await ctx.SsDescansoCaso.FirstOrDefaultAsync(c => c.Id == casoId && c.State)
                ?? throw new AbrilException("Caso no encontrado.", 404);

            var worker = await ctx.Worker
                .Where(w => w.Id == caso.WorkerId)
                .Select(w => new { Nombre = w.Person != null ? w.Person.FullName : null, Dni = w.Person != null ? w.Person.DocumentIdentityCode : null })
                .FirstOrDefaultAsync();

            var descansos = await (
                from d in ctx.SsDescansoMedico
                where d.CasoId == casoId && d.State
                join t in ctx.SsDescansoTipo on d.TipoId equals t.Id
                orderby d.FechaInicio
                select new DescansoMedicoListItemDto
                {
                    Id = d.Id,
                    CasoId = d.CasoId,
                    WorkerId = d.WorkerId,
                    TipoId = d.TipoId,
                    Tipo = t.Nombre,
                    FechaInicio = d.FechaInicio,
                    FechaFin = d.FechaFin,
                    Dias = d.Dias,
                    Estado = d.Estado,
                    ReportadoPorTrabajador = d.ReportadoPorTrabajador,
                    TopicoOrigenId = d.TopicoOrigenId,
                    CreatedAt = d.CreatedAt
                }
            ).ToListAsync();

            var seguimientos = await GetSeguimientosPorCaso(casoId, puedeVerDetalleClinico: true);

            return new CasoDetalleDto
            {
                Id = caso.Id,
                WorkerId = caso.WorkerId,
                WorkerNombre = worker?.Nombre,
                WorkerDni = worker?.Dni,
                FechaApertura = caso.FechaApertura,
                Estado = caso.Estado,
                FechaCierre = caso.FechaCierre,
                AltaPorId = caso.AltaPorId,
                AltaObservaciones = caso.AltaObservaciones,
                FechaReapertura = caso.FechaReapertura,
                Descansos = descansos,
                Seguimientos = seguimientos
            };
        }

        /// <param name="puedeVerDetalleClinico">Si es false, el campo Nota se oculta (null) en
        /// los seguimientos marcados Confidencial=true. La resolución de quién tiene este
        /// permiso queda pendiente de definir (depende de cómo entre el médico al sistema) —
        /// por ahora se llama siempre con true desde el controller.</param>
        public async Task<List<DescansoSeguimientoDto>> GetSeguimientosPorCaso(int casoId, bool puedeVerDetalleClinico)
        {
            using var ctx = _factory.CreateDbContext();
            var items = await (
                from s in ctx.SsDescansoSeguimiento
                where s.CasoId == casoId && s.State
                join tt in ctx.SsSeguimientoTipo on s.TipoId equals tt.Id into ttj
                from tt in ttj.DefaultIfEmpty()
                join c10 in ctx.Cie10Catalogo on s.DiagnosticoCie10Codigo equals c10.Codigo into c10j
                from c10 in c10j.DefaultIfEmpty()
                orderby s.FechaSeguimiento
                select new DescansoSeguimientoDto
                {
                    Id = s.Id,
                    DescansoId = s.DescansoId,
                    CasoId = s.CasoId,
                    FechaSeguimiento = s.FechaSeguimiento,
                    Tipo = s.Tipo,
                    TipoId = s.TipoId,
                    TipoNombre = tt != null ? tt.Nombre : s.Tipo,
                    RealizadoPorRol = s.RealizadoPorRol,
                    RealizadoPorId = s.RealizadoPorId,
                    Nota = s.Nota,
                    ProximaCita = s.ProximaCita,
                    UrlEvidencia = s.UrlEvidencia,
                    DiagnosticoCie10Codigo = s.DiagnosticoCie10Codigo,
                    DiagnosticoCie10Descripcion = c10 != null ? c10.Descripcion : null,
                    PuestoTrabajoSnapshot = s.PuestoTrabajoSnapshot,
                    Confidencial = s.Confidencial,
                    CreatedAt = s.CreatedAt
                }
            ).ToListAsync();

            if (!puedeVerDetalleClinico)
                foreach (var s in items.Where(s => s.Confidencial))
                    s.Nota = null;

            return items;
        }

        public async Task<int> CreateSeguimiento(int casoId, DescansoSeguimientoCreateDto dto, int registradoPorId, string? rolUsuario)
        {
            using var ctx = _factory.CreateDbContext();

            var caso = await ctx.SsDescansoCaso.FirstOrDefaultAsync(c => c.Id == casoId && c.State)
                ?? throw new AbrilException("Caso no encontrado.", 404);

            if (dto.DiagnosticoCie10Codigo != null)
            {
                var cie10Valido = await ctx.Cie10Catalogo.AnyAsync(c => c.Codigo == dto.DiagnosticoCie10Codigo && c.Activo);
                if (!cie10Valido)
                    throw new AbrilException("El código CIE-10 indicado no es válido.", 400);
            }

            // Sobre cuál descanso puntual se hace la nota: el indicado, o si no, el más reciente
            // del caso — siempre debe existir al menos uno porque el caso nace con su descanso.
            var descansoId = dto.DescansoId
                ?? await ctx.SsDescansoMedico
                    .Where(d => d.CasoId == casoId && d.State)
                    .OrderByDescending(d => d.FechaInicio).ThenByDescending(d => d.Id)
                    .Select(d => d.Id)
                    .FirstOrDefaultAsync();

            if (descansoId == 0)
                throw new AbrilException("El caso no tiene ningún descanso registrado.", 400);

            // Snapshot del puesto de trabajo del paciente — contexto para que el médico evalúe
            // aptitud para ESE puesto, congelado al momento del seguimiento (no se recalcula
            // después, igual criterio que worker_vinculaciones.puesto).
            var puestoActual = await ctx.Worker
                .Where(w => w.Id == caso.WorkerId)
                .Select(w => w.PuestoCatalogo != null ? w.PuestoCatalogo.Nombre : null)
                .FirstOrDefaultAsync();

            var entity = new SsDescansoSeguimiento
            {
                DescansoId = descansoId,
                CasoId = casoId,
                FechaSeguimiento = DateTimeOffset.UtcNow,
                TipoId = dto.TipoId,
                RealizadoPorRol = rolUsuario,
                RealizadoPorId = registradoPorId,
                Nota = dto.Nota,
                ProximaCita = dto.ProximaCita,
                UrlEvidencia = dto.UrlEvidencia,
                DiagnosticoCie10Codigo = dto.DiagnosticoCie10Codigo,
                PuestoTrabajoSnapshot = puestoActual,
                Confidencial = dto.Confidencial,
                CreatedAt = DateTimeOffset.UtcNow
            };

            ctx.SsDescansoSeguimiento.Add(entity);
            await ctx.SaveChangesAsync();
            return entity.Id;
        }

        public async Task<List<CasoCandidatoDto>> GetCasosCandidatos(int workerId, int excluirCasoId)
        {
            using var ctx = _factory.CreateDbContext();

            return await (
                from c in ctx.SsDescansoCaso
                where c.WorkerId == workerId && c.State && c.Estado == "Abierto" && c.Id != excluirCasoId
                let primero = ctx.SsDescansoMedico
                    .Where(d => d.CasoId == c.Id && d.State)
                    .OrderBy(d => d.FechaInicio).ThenBy(d => d.Id)
                    .Select(d => new { d.FechaInicio, d.FechaFin, Tipo = d.TipoCatalogo!.Nombre })
                    .FirstOrDefault()
                where primero != null
                orderby c.FechaApertura descending
                select new CasoCandidatoDto
                {
                    Id = c.Id,
                    FechaApertura = c.FechaApertura,
                    PrimerDescansoInicio = primero!.FechaInicio,
                    PrimerDescansoFin = primero.FechaFin,
                    PrimerDescansoTipo = primero.Tipo,
                }
            ).ToListAsync();
        }

        public async Task VincularCaso(int descansoId, int casoDestinoId)
        {
            using var ctx = _factory.CreateDbContext();

            var descanso = await ctx.SsDescansoMedico.FirstOrDefaultAsync(d => d.Id == descansoId && d.State)
                ?? throw new AbrilException("Descanso médico no encontrado.", 404);

            var casoOrigenId = descanso.CasoId;
            if (casoOrigenId == casoDestinoId)
                throw new AbrilException("Este descanso ya pertenece a ese caso.", 400);

            var casoDestino = await ctx.SsDescansoCaso.FirstOrDefaultAsync(c => c.Id == casoDestinoId && c.State)
                ?? throw new AbrilException("Caso destino no encontrado.", 404);
            if (casoDestino.Estado != "Abierto")
                throw new AbrilException("Solo se puede vincular a un caso abierto.", 400);
            if (casoDestino.WorkerId != descanso.WorkerId)
                throw new AbrilException("El caso destino no es del mismo trabajador.", 400);

            // Solo se permite vincular un descanso "suelto" (el que nace solo al subirse desde
            // Mi Salud) — si el caso de origen ya tiene más de un descanso, vincularlo mezclaría
            // dos historiales clínicos distintos, y eso ya no es una operación de un clic.
            var descansosEnOrigen = await ctx.SsDescansoMedico
                .Where(d => d.CasoId == casoOrigenId && d.State)
                .CountAsync();
            if (descansosEnOrigen > 1)
                throw new AbrilException(
                    "Este descanso ya forma parte de un caso con más registros — no se puede vincular automáticamente.", 400);

            descanso.CasoId = casoDestinoId;
            descanso.UpdatedAt = DateTimeOffset.UtcNow;

            // Los seguimientos que ya se hayan hecho sobre este descanso viajan con él.
            var seguimientos = await ctx.SsDescansoSeguimiento
                .Where(s => s.CasoId == casoOrigenId && s.State)
                .ToListAsync();
            foreach (var s in seguimientos)
                s.CasoId = casoDestinoId;

            // El caso de origen se queda sin descansos — se da de baja (auditoría conservada).
            var casoOrigen = await ctx.SsDescansoCaso.FirstOrDefaultAsync(c => c.Id == casoOrigenId);
            if (casoOrigen != null)
            {
                casoOrigen.State = false;
                casoOrigen.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await ctx.SaveChangesAsync();
        }

        public async Task<List<SeguimientoTipoDto>> GetSeguimientoTipos()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsSeguimientoTipo
                .Where(t => t.Active)
                .OrderBy(t => t.Orden)
                .Select(t => new SeguimientoTipoDto { Id = t.Id, Nombre = t.Nombre })
                .ToListAsync();
        }

        public async Task<List<Cie10Dto>> BuscarCie10(string? search, int limite)
        {
            using var ctx = _factory.CreateDbContext();
            var q = ctx.Cie10Catalogo.Where(c => c.Activo);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(c => EF.Functions.ILike(c.Codigo, $"%{search}%") || EF.Functions.ILike(c.Descripcion, $"%{search}%"));

            return await q
                .OrderBy(c => c.Codigo)
                .Take(limite)
                .Select(c => new Cie10Dto { Codigo = c.Codigo, Descripcion = c.Descripcion })
                .ToListAsync();
        }

        public async Task Delete(int id)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = await ctx.SsDescansoMedico.FirstOrDefaultAsync(d => d.Id == id && d.State)
                ?? throw new AbrilException("Descanso médico no encontrado.", 404);

            entity.State = false;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            await ctx.SaveChangesAsync();
        }
    }
}
