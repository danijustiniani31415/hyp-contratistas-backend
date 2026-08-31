using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.AccidenteTrabajo;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.AltaMedica;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.CitaMedica;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.DescansoMedico;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.EquipoPrestado;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Infrastructure;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Repositories
{
    public class AccidenteTrabajoRepository : IAccidenteTrabajoRepository
    {
        private const int PageSize = 20;
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly ReactivosCacheVersion _reactivosCacheVersion;

        public AccidenteTrabajoRepository(IDbContextFactory<AppDbContext> factory, ReactivosCacheVersion reactivosCacheVersion)
        {
            _factory = factory;
            _reactivosCacheVersion = reactivosCacheVersion;
        }

        public async Task<PagedResult<AccidenteTrabajoListItemDto>> ListPaged(AccidenteFilterDto filter)
        {
            using var ctx = _factory.CreateDbContext();

            var q =
                from a in ctx.SsAccidenteTrabajo
                join w in ctx.Worker on a.WorkerId equals w.Id
                join em in ctx.Contributor on a.EmpresaId equals em.ContributorId into emj
                from em in emj.DefaultIfEmpty()
                join p in ctx.Project on a.ProyectoId equals p.ProjectId into pj
                from p in pj.DefaultIfEmpty()
                select new { a, w, em, p };

            if (filter.WorkerId.HasValue)
                q = q.Where(x => x.a.WorkerId == filter.WorkerId.Value);
            if (!string.IsNullOrWhiteSpace(filter.Estado))
                q = q.Where(x => x.a.Estado == filter.Estado);
            if (!string.IsNullOrWhiteSpace(filter.TipoAccidente))
                q = q.Where(x => x.a.TipoAccidente == filter.TipoAccidente);
            if (filter.EmpresaId.HasValue)
                q = q.Where(x => x.a.EmpresaId == filter.EmpresaId.Value);
            if (filter.ProyectoId.HasValue)
                q = q.Where(x => x.a.ProyectoId == filter.ProyectoId.Value);
            if (filter.FechaDesde.HasValue)
                q = q.Where(x => x.a.FechaAccidente >= filter.FechaDesde.Value);
            if (filter.FechaHasta.HasValue)
                q = q.Where(x => x.a.FechaAccidente <= filter.FechaHasta.Value);

            var total = await q.CountAsync();
            var page = filter.Page < 1 ? 1 : filter.Page;

            var items = await q
                .OrderByDescending(x => x.a.FechaAccidente)
                .ThenByDescending(x => x.a.CreatedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(x => new AccidenteTrabajoListItemDto
                {
                    Id = x.a.Id,
                    WorkerId = x.a.WorkerId,
                    WorkerNombre = x.w.Person != null ? x.w.Person.FullName : null,
                    WorkerDni = x.w.Person != null ? x.w.Person.DocumentIdentityCode : null,
                    EmpresaNombre = x.em != null ? x.em.ContributorName : null,
                    ProyectoNombre = x.p != null ? x.p.ProjectDescription : null,
                    FechaAccidente = x.a.FechaAccidente,
                    TipoAccidente = x.a.TipoAccidente,
                    LugarAccidente = x.a.LugarAccidente,
                    Estado = x.a.Estado,
                    NotificadoSunafil = x.a.NotificadoSunafil,
                    TotalSeguimientos = ctx.SsAccidenteSeguimiento.Count(s => s.AccidenteId == x.a.Id),
                    FlashReportId = x.a.FlashReportId,
                    TieneAlta = ctx.SsAltaMedica.Any(al => al.AccidenteId == x.a.Id && al.State),
                    RequiereReinduccion = x.a.RequiereReinduccion,
                    ReinduccionCompletada = x.a.ReinduccionCompletada,
                    CreatedAt = x.a.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<AccidenteTrabajoListItemDto>
            {
                Page = page,
                PageSize = PageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)PageSize),
                Data = items
            };
        }

        public async Task<AccidenteTrabajoDetalleDto> GetById(int id)
        {
            using var ctx = _factory.CreateDbContext();

            var row = await (
                from a in ctx.SsAccidenteTrabajo
                where a.Id == id
                join w in ctx.Worker on a.WorkerId equals w.Id
                join em in ctx.Contributor on a.EmpresaId equals em.ContributorId into emj
                from em in emj.DefaultIfEmpty()
                join p in ctx.Project on a.ProyectoId equals p.ProjectId into pj
                from p in pj.DefaultIfEmpty()
                join ag in ctx.SsAgenteRiesgo on a.AgenteRiesgoId equals ag.Id into agj
                from ag in agj.DefaultIfEmpty()
                select new { a, w, em, p, ag }
            ).FirstOrDefaultAsync()
              ?? throw new AbrilException("Accidente de trabajo no encontrado.", 404);

            var seguimientos = await ctx.SsAccidenteSeguimiento
                .Where(s => s.AccidenteId == id)
                .OrderByDescending(s => s.Fecha)
                .Select(s => new AccidenteSeguimientoDto
                {
                    Id = s.Id,
                    AccidenteId = s.AccidenteId,
                    Fecha = s.Fecha,
                    Tipo = s.Tipo,
                    Descripcion = s.Descripcion,
                    ProximaCita = s.ProximaCita,
                    RegistradoPorId = s.RegistradoPorId,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();

            var descansos = await ctx.SsDescansoMedico
                .Where(d => d.AccidenteId == id && d.State)
                .OrderByDescending(d => d.FechaInicio)
                .Select(d => new DescansoMedicoListItemDto
                {
                    Id = d.Id,
                    WorkerId = d.WorkerId,
                    TipoId = d.TipoId,
                    Tipo = d.TipoCatalogo != null ? d.TipoCatalogo.Nombre : string.Empty,
                    FechaInicio = d.FechaInicio,
                    FechaFin = d.FechaFin,
                    Dias = d.Dias,
                    Estado = d.Estado,
                    CreatedAt = d.CreatedAt
                })
                .ToListAsync();

            var citas = await ctx.SsCitaMedica
                .Where(c => c.AccidenteId == id && c.State)
                .OrderByDescending(c => c.FechaCita)
                .Select(c => new CitaMedicaListItemDto
                {
                    Id = c.Id,
                    AccidenteId = c.AccidenteId,
                    TipoId = c.TipoId,
                    TipoNombre = c.Tipo != null ? c.Tipo.Nombre : string.Empty,
                    FechaCita = c.FechaCita,
                    HoraCita = c.HoraCita,
                    Clinica = c.Clinica,
                    Medico = c.Medico,
                    Diagnostico = c.Diagnostico,
                    Indicaciones = c.Indicaciones,
                    ProximaCita = c.ProximaCita,
                    UrlEvidencia = c.UrlEvidencia,
                    Observaciones = c.Observaciones,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            var equipos = await ctx.SsEquipoPrestado
                .Where(e => e.AccidenteId == id && e.State)
                .OrderByDescending(e => e.FechaPrestamo)
                .Select(e => new EquipoPrestadoListItemDto
                {
                    Id = e.Id,
                    AccidenteId = e.AccidenteId,
                    TipoEquipoId = e.TipoEquipoId,
                    TipoEquipoNombre = e.TipoEquipo != null ? e.TipoEquipo.Nombre : string.Empty,
                    Cantidad = e.Cantidad,
                    FechaPrestamo = e.FechaPrestamo,
                    FechaDevolucion = e.FechaDevolucion,
                    Devuelto = e.Devuelto,
                    Observaciones = e.Observaciones,
                    UrlEvidencia = e.UrlEvidencia,
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync();

            var altaMedica = await ctx.SsAltaMedica
                .Where(a => a.AccidenteId == id && a.State)
                .Select(a => new AltaMedicaDto
                {
                    Id = a.Id,
                    AccidenteId = a.AccidenteId,
                    TipoId = a.TipoId,
                    TipoNombre = a.Tipo != null ? a.Tipo.Nombre : string.Empty,
                    FechaAlta = a.FechaAlta,
                    Medico = a.Medico,
                    DiagnosticoFinal = a.DiagnosticoFinal,
                    TieneRestriccion = a.TieneRestriccion,
                    DescripcionRestriccion = a.DescripcionRestriccion,
                    FechaFinRestriccion = a.FechaFinRestriccion,
                    UrlCertificado = a.UrlCertificado,
                    Observaciones = a.Observaciones,
                    CreatedAt = a.CreatedAt
                })
                .FirstOrDefaultAsync();

            return new AccidenteTrabajoDetalleDto
            {
                Id = row.a.Id,
                WorkerId = row.a.WorkerId,
                WorkerNombre = row.w.Person != null ? row.w.Person.FullName : null,
                WorkerDni = row.w.Person != null ? row.w.Person.DocumentIdentityCode : null,
                WorkerTelefono = row.w.Person != null && row.w.Person.PhoneNumber.HasValue
                    ? row.w.Person.PhoneNumber.Value.ToString()
                    : null,
                ProyectoId = row.a.ProyectoId,
                ProyectoNombre = row.p != null ? row.p.ProjectDescription : null,
                EmpresaId = row.a.EmpresaId,
                EmpresaNombre = row.em != null ? row.em.ContributorName : null,
                FechaAccidente = row.a.FechaAccidente,
                HoraAccidente = row.a.HoraAccidente,
                LugarAccidente = row.a.LugarAccidente,
                TipoAccidente = row.a.TipoAccidente,
                Mecanismo = row.a.Mecanismo,
                ParteCuerpoAfectada = row.a.ParteCuerpoAfectada,
                AgenteRiesgoId = row.a.AgenteRiesgoId,
                AgenteRiesgoNombre = row.ag != null ? row.ag.Nombre : null,
                Descripcion = row.a.Descripcion,
                DescripcionLesion = row.a.DescripcionLesion,
                DiagnosticoCie10 = row.a.DiagnosticoCie10,
                RequiereHospitalizacion = row.a.RequiereHospitalizacion,
                HospitalNombre = row.a.HospitalNombre,
                AtencionTopicoId = row.a.AtencionTopicoId,
                DiasDescansoEstimados = row.a.DiasDescansoEstimados,
                DiasDescansoReales = row.a.DiasDescansoReales,
                Estado = row.a.Estado,
                FechaAlta = row.a.FechaAlta,
                RestriccionesReintegro = row.a.RestriccionesReintegro,
                NotificadoSunafil = row.a.NotificadoSunafil,
                FechaNotificacionSunafil = row.a.FechaNotificacionSunafil,
                NumeroNotificacionSunafil = row.a.NumeroNotificacionSunafil,
                UrlInforme = row.a.UrlInforme,
                RequiereReinduccion = row.a.RequiereReinduccion,
                ReinduccionCompletada = row.a.ReinduccionCompletada,
                FechaReinduccion = row.a.FechaReinduccion,
                FlashReportId = row.a.FlashReportId,
                TieneAlta = altaMedica != null,
                CasoSocialId = row.a.CasoSocialId,
                RegistradoPorId = row.a.RegistradoPorId,
                CerradoPorId = row.a.CerradoPorId,
                FechaCierre = row.a.FechaCierre,
                CreatedAt = row.a.CreatedAt,
                Seguimientos = seguimientos,
                Descansos = descansos,
                Citas = citas,
                Equipos = equipos,
                AltaMedica = altaMedica
            };
        }

        public async Task<int> Create(AccidenteTrabajoCreateDto dto, int registradoPorId)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = new SsAccidenteTrabajo
            {
                WorkerId = dto.WorkerId,
                FechaAccidente = dto.FechaAccidente,
                HoraAccidente = dto.HoraAccidente,
                ProyectoId = dto.ProyectoId,
                EmpresaId = dto.EmpresaId,
                LugarAccidente = dto.LugarAccidente,
                TipoAccidente = dto.TipoAccidente,
                Mecanismo = dto.Mecanismo,
                ParteCuerpoAfectada = dto.ParteCuerpoAfectada,
                AgenteRiesgoId = dto.AgenteRiesgoId,
                Descripcion = dto.Descripcion,
                DescripcionLesion = dto.DescripcionLesion,
                DiagnosticoCie10 = dto.DiagnosticoCie10,
                RequiereHospitalizacion = dto.RequiereHospitalizacion,
                HospitalNombre = dto.HospitalNombre,
                DiasDescansoEstimados = dto.DiasDescansoEstimados,
                Estado = "Registrado",
                RegistradoPorId = registradoPorId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            ctx.SsAccidenteTrabajo.Add(entity);
            await ctx.SaveChangesAsync();
            _reactivosCacheVersion.Bump();
            return entity.Id;
        }

        public async Task Update(int id, AccidenteTrabajoUpdateDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = await ctx.SsAccidenteTrabajo.FirstOrDefaultAsync(a => a.Id == id)
                ?? throw new AbrilException("Accidente de trabajo no encontrado.", 404);

            entity.LugarAccidente = dto.LugarAccidente;
            entity.TipoAccidente = dto.TipoAccidente;
            entity.Mecanismo = dto.Mecanismo;
            entity.ParteCuerpoAfectada = dto.ParteCuerpoAfectada;
            entity.AgenteRiesgoId = dto.AgenteRiesgoId;
            entity.Descripcion = dto.Descripcion;
            entity.DescripcionLesion = dto.DescripcionLesion;
            entity.DiagnosticoCie10 = dto.DiagnosticoCie10;
            entity.RequiereHospitalizacion = dto.RequiereHospitalizacion;
            entity.HospitalNombre = dto.HospitalNombre;
            entity.DiasDescansoEstimados = dto.DiasDescansoEstimados;
            entity.DiasDescansoReales = dto.DiasDescansoReales;
            entity.NotificadoSunafil = dto.NotificadoSunafil;
            entity.FechaNotificacionSunafil = dto.FechaNotificacionSunafil;
            entity.NumeroNotificacionSunafil = dto.NumeroNotificacionSunafil;
            entity.RestriccionesReintegro = dto.RestriccionesReintegro;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            await ctx.SaveChangesAsync();
            _reactivosCacheVersion.Bump();
        }

        public async Task Cerrar(int id, AccidenteCerrarDto dto, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = await ctx.SsAccidenteTrabajo.FirstOrDefaultAsync(a => a.Id == id)
                ?? throw new AbrilException("Accidente de trabajo no encontrado.", 404);

            if (entity.Estado == "Cerrado" || entity.Estado == "Dado de Alta")
                throw new AbrilException("El accidente ya está cerrado.", 400);

            var tieneAlta = await ctx.SsAltaMedica.AnyAsync(a => a.AccidenteId == id && a.State);
            if (!tieneAlta)
                throw new AbrilException("Debe registrar el alta médica antes de cerrar el accidente.", 400);

            var tieneDescansosPendientes = await ctx.SsDescansoMedico
                .AnyAsync(d => d.AccidenteId == id && d.Estado == "Pendiente" && d.State);
            if (tieneDescansosPendientes)
                throw new AbrilException("Existen descansos médicos pendientes de aprobación. Apruébelos o rechácelos antes de cerrar.", 400);

            entity.Estado = "Dado de Alta";
            entity.FechaAlta = dto.FechaAlta;
            entity.RestriccionesReintegro = dto.RestriccionesReintegro;
            entity.DiasDescansoReales = dto.DiasDescansoReales;
            entity.CerradoPorId = userId;
            entity.FechaCierre = DateTimeOffset.UtcNow;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            await ctx.SaveChangesAsync();
            _reactivosCacheVersion.Bump();
        }

        public async Task Delete(int id)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = await ctx.SsAccidenteTrabajo.FirstOrDefaultAsync(a => a.Id == id)
                ?? throw new AbrilException("Accidente de trabajo no encontrado.", 404);

            ctx.SsAccidenteTrabajo.Remove(entity);
            await ctx.SaveChangesAsync();
            _reactivosCacheVersion.Bump();
        }

        public async Task<int> CreateSeguimiento(int accidenteId, AccidenteSeguimientoCreateDto dto, int registradoPorId)
        {
            using var ctx = _factory.CreateDbContext();

            var existe = await ctx.SsAccidenteTrabajo.AnyAsync(a => a.Id == accidenteId);
            if (!existe)
                throw new AbrilException("Accidente de trabajo no encontrado.", 404);

            var entity = new SsAccidenteSeguimiento
            {
                AccidenteId = accidenteId,
                Fecha = dto.Fecha,
                Tipo = dto.Tipo,
                Descripcion = dto.Descripcion,
                ProximaCita = dto.ProximaCita,
                RegistradoPorId = registradoPorId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            ctx.SsAccidenteSeguimiento.Add(entity);
            await ctx.SaveChangesAsync();
            return entity.Id;
        }

        public async Task DeleteSeguimiento(int seguimientoId)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = await ctx.SsAccidenteSeguimiento.FirstOrDefaultAsync(s => s.Id == seguimientoId)
                ?? throw new AbrilException("Seguimiento no encontrado.", 404);

            ctx.SsAccidenteSeguimiento.Remove(entity);
            await ctx.SaveChangesAsync();
        }

        public async Task MarcarReinduccionAsync(int accidenteId, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var entity = await ctx.SsAccidenteTrabajo.FindAsync(accidenteId)
                ?? throw new AbrilException("Accidente no encontrado.", 404);

            entity.ReinduccionCompletada = true;
            entity.FechaReinduccion = DateOnly.FromDateTime(DateTime.UtcNow);
            entity.ReinduccionPorId = userId;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }
    }
}
