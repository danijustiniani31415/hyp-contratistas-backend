using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Topico;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Repositories
{
    public class TopicoRepository : ITopicoRepository
    {
        private const int PageSize = 20;
        private readonly IDbContextFactory<AppDbContext> _factory;

        public TopicoRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<PagedResult<TopicoListItemDto>> ListPaged(TopicoFilterDto filter)
        {
            using var ctx = _factory.CreateDbContext();

            var q =
                from t in ctx.SsTopicoAtencion
                join w in ctx.Worker on t.WorkerId equals w.Id
                join ta in ctx.SsTopicoTipoAtencion on t.TipoAtencionId equals ta.Id into taj
                from ta in taj.DefaultIfEmpty()
                join em in ctx.Contributor on t.EmpresaId equals em.ContributorId into emj
                from em in emj.DefaultIfEmpty()
                join p in ctx.Project on t.ProyectoId equals p.ProjectId into pj
                from p in pj.DefaultIfEmpty()
                where t.State
                select new { t, w, ta, em, p };

            if (filter.WorkerId.HasValue)
                q = q.Where(x => x.t.WorkerId == filter.WorkerId.Value);
            if (filter.TipoAtencionId.HasValue)
                q = q.Where(x => x.t.TipoAtencionId == filter.TipoAtencionId.Value);
            if (filter.EmpresaId.HasValue)
                q = q.Where(x => x.t.EmpresaId == filter.EmpresaId.Value);
            if (filter.ProyectoId.HasValue)
                q = q.Where(x => x.t.ProyectoId == filter.ProyectoId.Value);
            if (filter.FechaDesde.HasValue)
                q = q.Where(x => x.t.Fecha >= filter.FechaDesde.Value);
            if (filter.FechaHasta.HasValue)
                q = q.Where(x => x.t.Fecha <= filter.FechaHasta.Value);
            if (!string.IsNullOrEmpty(filter.Estado))
                q = q.Where(x => x.t.Estado == filter.Estado);

            var total = await q.CountAsync();
            var page = filter.Page < 1 ? 1 : filter.Page;

            var items = await q
                .OrderByDescending(x => x.t.Fecha)
                .ThenByDescending(x => x.t.Hora)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(x => new TopicoListItemDto
                {
                    Id = x.t.Id,
                    WorkerId = x.t.WorkerId,
                    WorkerNombre = x.w.Person != null ? x.w.Person.FullName : null,
                    WorkerDni = x.w.Person != null ? x.w.Person.DocumentIdentityCode : null,
                    EmpresaNombre = x.em != null ? x.em.ContributorName : null,
                    ProyectoNombre = x.p != null ? x.p.ProjectDescription : null,
                    Fecha = x.t.Fecha,
                    Hora = x.t.Hora,
                    TipoAtencionId = x.t.TipoAtencionId,
                    TipoAtencionNombre = x.ta != null ? x.ta.Nombre : null,
                    Motivo = x.t.Motivo,
                    Diagnostico = x.t.Diagnostico,
                    DerivadoClinica = x.t.DerivadoClinica,
                    GeneraDescanso = x.t.GeneraDescanso,
                    GeneraAccidente = x.t.GeneraAccidente,
                    SctrActivado = x.t.SctrActivado,
                    UrlInforme = x.t.UrlInforme,
                    DescansoGeneradoId = x.t.DescansoGeneradoId,
                    Estado = x.t.Estado,
                    FechaCierre = x.t.FechaCierre,
                    CreatedAt = x.t.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<TopicoListItemDto>
            {
                Page = page,
                PageSize = PageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)PageSize),
                Data = items
            };
        }

        public async Task<TopicoDetalleDto> GetById(int id)
        {
            using var ctx = _factory.CreateDbContext();

            var row = await (
                from t in ctx.SsTopicoAtencion
                where t.Id == id && t.State
                join w in ctx.Worker on t.WorkerId equals w.Id
                join ta in ctx.SsTopicoTipoAtencion on t.TipoAtencionId equals ta.Id into taj
                from ta in taj.DefaultIfEmpty()
                join em in ctx.Contributor on t.EmpresaId equals em.ContributorId into emj
                from em in emj.DefaultIfEmpty()
                join p in ctx.Project on t.ProyectoId equals p.ProjectId into pj
                from p in pj.DefaultIfEmpty()
                select new { t, w, ta, em, p }
            ).FirstOrDefaultAsync()
              ?? throw new AbrilException("Atención de tópico no encontrada.", 404);

            return new TopicoDetalleDto
            {
                Id = row.t.Id,
                WorkerId = row.t.WorkerId,
                WorkerNombre = row.w.Person != null ? row.w.Person.FullName : null,
                WorkerDni = row.w.Person != null ? row.w.Person.DocumentIdentityCode : null,
                ProyectoId = row.t.ProyectoId,
                ProyectoNombre = row.p != null ? row.p.ProjectDescription : null,
                EmpresaId = row.t.EmpresaId,
                EmpresaNombre = row.em != null ? row.em.ContributorName : null,
                Fecha = row.t.Fecha,
                Hora = row.t.Hora,
                TipoAtencionId = row.t.TipoAtencionId,
                TipoAtencionNombre = row.ta != null ? row.ta.Nombre : null,
                Motivo = row.t.Motivo,
                Diagnostico = row.t.Diagnostico,
                DiagnosticoCie10 = row.t.DiagnosticoCie10,
                Tratamiento = row.t.Tratamiento,
                Medicamentos = row.t.Medicamentos,
                PresionArterial = row.t.PresionArterial,
                Temperatura = row.t.Temperatura,
                FrecuenciaCardiaca = row.t.FrecuenciaCardiaca,
                SaturacionOxigeno = row.t.SaturacionOxigeno,
                Peso = row.t.Peso,
                DerivadoClinica = row.t.DerivadoClinica,
                ClinicaDerivacion = row.t.ClinicaDerivacion,
                GeneraDescanso = row.t.GeneraDescanso,
                DescansoDias = row.t.DescansoDias,
                GeneraAccidente = row.t.GeneraAccidente,
                AccidenteId = row.t.AccidenteId,
                Observaciones = row.t.Observaciones,
                SctrActivado = row.t.SctrActivado,
                TipoCasoSctr = row.t.TipoCasoSctr,
                UrlInforme = row.t.UrlInforme,
                DescansoGeneradoId = row.t.DescansoGeneradoId,
                Estado = row.t.Estado,
                CerradoPorId = row.t.CerradoPorId,
                FechaCierre = row.t.FechaCierre,
                RegistradoPorId = row.t.RegistradoPorId,
                CreatedAt = row.t.CreatedAt,
                UpdatedAt = row.t.UpdatedAt
            };
        }

        public async Task<int> Create(TopicoCreateDto dto, int registradoPorId)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = new TopicoAtencion
            {
                WorkerId = dto.WorkerId,
                Fecha = dto.Fecha,
                Hora = dto.Hora,
                TipoAtencionId = dto.TipoAtencionId,
                Motivo = dto.Motivo,
                Diagnostico = dto.Diagnostico,
                DiagnosticoCie10 = dto.DiagnosticoCie10,
                Tratamiento = dto.Tratamiento,
                Medicamentos = dto.Medicamentos,
                PresionArterial = dto.PresionArterial,
                Temperatura = dto.Temperatura,
                FrecuenciaCardiaca = dto.FrecuenciaCardiaca,
                SaturacionOxigeno = dto.SaturacionOxigeno,
                Peso = dto.Peso,
                DerivadoClinica = dto.DerivadoClinica,
                ClinicaDerivacion = dto.ClinicaDerivacion,
                GeneraDescanso = dto.GeneraDescanso,
                DescansoDias = dto.DescansoDias,
                GeneraAccidente = dto.GeneraAccidente,
                ProyectoId = dto.ProyectoId,
                EmpresaId = dto.EmpresaId,
                Observaciones = dto.Observaciones,
                SctrActivado = dto.SctrActivado,
                TipoCasoSctr = dto.TipoCasoSctr,
                UrlInforme = dto.UrlInforme,
                State = true,
                RegistradoPorId = registradoPorId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            ctx.SsTopicoAtencion.Add(entity);
            await ctx.SaveChangesAsync();
            return entity.Id;
        }

        public async Task Update(int id, TopicoUpdateDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = await ctx.SsTopicoAtencion.FirstOrDefaultAsync(t => t.Id == id && t.State)
                ?? throw new AbrilException("Atención de tópico no encontrada.", 404);

            entity.TipoAtencionId = dto.TipoAtencionId;
            entity.Motivo = dto.Motivo;
            entity.Diagnostico = dto.Diagnostico;
            entity.DiagnosticoCie10 = dto.DiagnosticoCie10;
            entity.Tratamiento = dto.Tratamiento;
            entity.Medicamentos = dto.Medicamentos;
            entity.PresionArterial = dto.PresionArterial;
            entity.Temperatura = dto.Temperatura;
            entity.FrecuenciaCardiaca = dto.FrecuenciaCardiaca;
            entity.SaturacionOxigeno = dto.SaturacionOxigeno;
            entity.Peso = dto.Peso;
            entity.DerivadoClinica = dto.DerivadoClinica;
            entity.ClinicaDerivacion = dto.ClinicaDerivacion;
            entity.GeneraDescanso = dto.GeneraDescanso;
            entity.DescansoDias = dto.DescansoDias;
            entity.GeneraAccidente = dto.GeneraAccidente;
            entity.ProyectoId = dto.ProyectoId;
            entity.EmpresaId = dto.EmpresaId;
            entity.Observaciones = dto.Observaciones;
            entity.SctrActivado = dto.SctrActivado;
            entity.TipoCasoSctr = dto.TipoCasoSctr;
            if (dto.UrlInforme != null)
                entity.UrlInforme = dto.UrlInforme;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            await ctx.SaveChangesAsync();
        }

        public async Task Cerrar(int id, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = await ctx.SsTopicoAtencion.FirstOrDefaultAsync(t => t.Id == id && t.State)
                ?? throw new AbrilException("Atención de tópico no encontrada.", 404);

            if (entity.Estado == "Cerrada")
                throw new AbrilException("La atención ya está cerrada.", 400);

            entity.Estado = "Cerrada";
            entity.CerradoPorId = userId;
            entity.FechaCierre = DateTimeOffset.UtcNow;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = await ctx.SsTopicoAtencion.FirstOrDefaultAsync(t => t.Id == id && t.State)
                ?? throw new AbrilException("Atención de tópico no encontrada.", 404);

            entity.State = false;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }

        public async Task<List<TopicoListItemDto>> GetHoy()
        {
            using var ctx = _factory.CreateDbContext();
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

            return await (
                from t in ctx.SsTopicoAtencion
                where t.Fecha == hoy && t.State
                join w in ctx.Worker on t.WorkerId equals w.Id
                join tipo in ctx.SsTopicoTipoAtencion on t.TipoAtencionId equals tipo.Id into tipoj
                from tipo in tipoj.DefaultIfEmpty()
                join em in ctx.Contributor on t.EmpresaId equals em.ContributorId into emj
                from em in emj.DefaultIfEmpty()
                join p in ctx.Project on t.ProyectoId equals p.ProjectId into pj
                from p in pj.DefaultIfEmpty()
                orderby t.Hora descending
                select new TopicoListItemDto
                {
                    Id = t.Id,
                    WorkerId = t.WorkerId,
                    WorkerNombre = w.Person != null ? w.Person.FullName : null,
                    WorkerDni = w.Person != null ? w.Person.DocumentIdentityCode : null,
                    EmpresaNombre = em != null ? em.ContributorName : null,
                    ProyectoNombre = p != null ? p.ProjectDescription : null,
                    Fecha = t.Fecha,
                    Hora = t.Hora,
                    TipoAtencionId = t.TipoAtencionId,
                    TipoAtencionNombre = tipo != null ? tipo.Nombre : null,
                    Motivo = t.Motivo,
                    Diagnostico = t.Diagnostico,
                    DerivadoClinica = t.DerivadoClinica,
                    GeneraDescanso = t.GeneraDescanso,
                    GeneraAccidente = t.GeneraAccidente,
                    SctrActivado = t.SctrActivado,
                    UrlInforme = t.UrlInforme,
                    DescansoGeneradoId = t.DescansoGeneradoId,
                    CreatedAt = t.CreatedAt
                }
            ).ToListAsync();
        }

        public async Task<List<TopicoTipoAtencionDto>> GetTiposAtencion()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsTopicoTipoAtencion
                .Where(t => t.State)
                .OrderBy(t => t.Nombre)
                .Select(t => new TopicoTipoAtencionDto
                {
                    Id = t.Id,
                    Nombre = t.Nombre
                })
                .ToListAsync();
        }

        public async Task SetDescansoGenerado(int topicoId, int descansoId)
        {
            using var ctx = _factory.CreateDbContext();
            var entity = await ctx.SsTopicoAtencion.FindAsync(topicoId);
            if (entity is null) return;
            entity.DescansoGeneradoId = descansoId;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }

        public async Task<List<TopicoEvolucionDto>> GetEvoluciones(int topicoId)
        {
            using var ctx = _factory.CreateDbContext();

            // Verificar que el tópico existe
            var exists = await ctx.SsTopicoAtencion.AnyAsync(t => t.Id == topicoId && t.State);
            if (!exists) throw new AbrilException("Atención de tópico no encontrada.", 404);

            return await (
                from ev in ctx.SsTopicoEvolucion
                join u in ctx.User on ev.RegistradoPorId equals u.UserId into uj
                from u in uj.DefaultIfEmpty()
                join p in ctx.Person on u.UserId equals p.UserId into pj
                from p in pj.DefaultIfEmpty()
                where ev.AtencionId == topicoId && ev.State
                orderby ev.FechaEvolucion descending
                select new TopicoEvolucionDto
                {
                    Id                  = ev.Id,
                    AtencionId          = ev.AtencionId,
                    FechaEvolucion      = ev.FechaEvolucion,
                    NotaEvolucion       = ev.NotaEvolucion,
                    RegistradoPorId     = ev.RegistradoPorId,
                    RegistradoPorNombre = p != null ? p.FullName : null,
                    UrlEvidencia        = ev.UrlEvidencia,
                    CreatedAt           = ev.CreatedAt,
                }
            ).ToListAsync();
        }

        public async Task<int> CreateEvolucion(int topicoId, TopicoEvolucionCreateDto dto, int registradoPorId)
        {
            using var ctx = _factory.CreateDbContext();

            var exists = await ctx.SsTopicoAtencion.AnyAsync(t => t.Id == topicoId && t.State);
            if (!exists) throw new AbrilException("Atención de tópico no encontrada.", 404);

            if (string.IsNullOrWhiteSpace(dto.NotaEvolucion))
                throw new AbrilException("La nota de evolución es requerida.", 400);

            var entity = new SsTopicoEvolucion
            {
                AtencionId      = topicoId,
                FechaEvolucion  = DateTimeOffset.UtcNow,
                NotaEvolucion   = dto.NotaEvolucion.Trim(),
                RegistradoPorId = registradoPorId,
                UrlEvidencia    = dto.UrlEvidencia,
                State           = true,
                CreatedAt       = DateTimeOffset.UtcNow,
            };

            ctx.SsTopicoEvolucion.Add(entity);
            await ctx.SaveChangesAsync();
            return entity.Id;
        }

        public async Task DeleteEvolucion(int evolucionId)
        {
            using var ctx = _factory.CreateDbContext();
            var entity = await ctx.SsTopicoEvolucion.FindAsync(evolucionId)
                ?? throw new AbrilException("Evolución no encontrada.", 404);
            entity.State = false;
            await ctx.SaveChangesAsync();
        }
    }
}
