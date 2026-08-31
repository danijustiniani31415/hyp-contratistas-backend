using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.CasoSocial;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Repositories
{
    public class CasoSocialRepository : ICasoSocialRepository
    {
        private const int PageSize = 20;
        private readonly IDbContextFactory<AppDbContext> _factory;

        public CasoSocialRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<PagedResult<CasoSocialListItemDto>> ListPaged(CasoSocialFilterDto filter)
        {
            using var ctx = _factory.CreateDbContext();

            var q =
                from c in ctx.SsCasoSocial
                where c.State
                join w in ctx.Worker on c.WorkerId equals w.Id
                join em in ctx.Contributor on c.EmpresaId equals em.ContributorId into emj
                from em in emj.DefaultIfEmpty()
                join p in ctx.Project on c.ProyectoId equals p.ProjectId into pj
                from p in pj.DefaultIfEmpty()
                select new { c, w, em, p };

            if (filter.WorkerId.HasValue)
                q = q.Where(x => x.c.WorkerId == filter.WorkerId.Value);
            if (!string.IsNullOrWhiteSpace(filter.Estado))
                q = q.Where(x => x.c.Estado == filter.Estado);
            if (!string.IsNullOrWhiteSpace(filter.TipoCaso))
                q = q.Where(x => x.c.TipoCaso == filter.TipoCaso);
            if (!string.IsNullOrWhiteSpace(filter.Prioridad))
                q = q.Where(x => x.c.Prioridad == filter.Prioridad);
            if (filter.EmpresaId.HasValue)
                q = q.Where(x => x.c.EmpresaId == filter.EmpresaId.Value);

            var total = await q.CountAsync();
            var page = filter.Page < 1 ? 1 : filter.Page;

            var items = await q
                .OrderByDescending(x => x.c.FechaApertura)
                .ThenByDescending(x => x.c.CreatedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(x => new CasoSocialListItemDto
                {
                    Id = x.c.Id,
                    WorkerId = x.c.WorkerId,
                    WorkerNombre = x.w.Person != null ? x.w.Person.FullName : null,
                    WorkerDni = x.w.Person != null ? x.w.Person.DocumentIdentityCode : null,
                    EmpresaNombre = x.em != null ? x.em.ContributorName : null,
                    ProyectoNombre = x.p != null ? x.p.ProjectDescription : null,
                    FechaApertura = x.c.FechaApertura,
                    TipoCaso = x.c.TipoCaso,
                    Prioridad = x.c.Prioridad,
                    Estado = x.c.Estado,
                    FechaCierre = x.c.FechaCierre,
                    TotalSeguimientos = ctx.SsCasoSocialSeguimiento.Count(s => s.CasoId == x.c.Id && s.State)
                })
                .ToListAsync();

            return new PagedResult<CasoSocialListItemDto>
            {
                Page = page,
                PageSize = PageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)PageSize),
                Data = items
            };
        }

        public async Task<CasoSocialDetalleDto> GetById(Guid id)
        {
            using var ctx = _factory.CreateDbContext();

            var row = await (
                from c in ctx.SsCasoSocial
                where c.Id == id && c.State
                join w in ctx.Worker on c.WorkerId equals w.Id
                join em in ctx.Contributor on c.EmpresaId equals em.ContributorId into emj
                from em in emj.DefaultIfEmpty()
                join p in ctx.Project on c.ProyectoId equals p.ProjectId into pj
                from p in pj.DefaultIfEmpty()
                select new { c, w, em, p }
            ).FirstOrDefaultAsync()
              ?? throw new AbrilException("Caso social no encontrado.", 404);

            var seguimientos = await (
                from s in ctx.SsCasoSocialSeguimiento
                where s.CasoId == id && s.State
                join resp in ctx.Worker on s.ResponsableId equals resp.Id into respj
                from resp in respj.DefaultIfEmpty()
                orderby s.Fecha descending
                select new SeguimientoDto
                {
                    Id = s.Id,
                    CasoId = s.CasoId,
                    Fecha = s.Fecha,
                    Tipo = s.Tipo,
                    Descripcion = s.Descripcion,
                    ResponsableId = s.ResponsableId,
                    ResponsableNombre = resp != null && resp.Person != null ? resp.Person.FullName : null,
                    ProximaAccion = s.ProximaAccion,
                    AccionTomada = s.AccionTomada,
                    CreatedAt = s.CreatedAt
                }
            ).ToListAsync();

            return new CasoSocialDetalleDto
            {
                Id = row.c.Id,
                WorkerId = row.c.WorkerId,
                WorkerNombre = row.w.Person != null ? row.w.Person.FullName : null,
                WorkerDni = row.w.Person != null ? row.w.Person.DocumentIdentityCode : null,
                ProyectoId = row.c.ProyectoId,
                ProyectoNombre = row.p != null ? row.p.ProjectDescription : null,
                EmpresaId = row.c.EmpresaId,
                EmpresaNombre = row.em != null ? row.em.ContributorName : null,
                FechaApertura = row.c.FechaApertura,
                TipoCaso = row.c.TipoCaso,
                Prioridad = row.c.Prioridad,
                Motivo = row.c.Motivo,
                Descripcion = row.c.Descripcion,
                Estado = row.c.Estado,
                FechaCierre = row.c.FechaCierre,
                Resultado = row.c.Resultado,
                RegistradoPorId = row.c.RegistradoPorId,
                CerradoPorId = row.c.CerradoPorId,
                Seguimientos = seguimientos
            };
        }

        public async Task<Guid> Create(CasoSocialCreateDto dto, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var caso = new SsCasoSocial
            {
                Id = Guid.NewGuid(),
                WorkerId = dto.WorkerId,
                ProyectoId = dto.ProyectoId,
                EmpresaId = dto.EmpresaId,
                FechaApertura = dto.FechaApertura,
                TipoCaso = dto.TipoCaso,
                Prioridad = dto.Prioridad,
                Motivo = dto.Motivo,
                Descripcion = dto.Descripcion,
                Estado = "Abierto",
                RegistradoPorId = userId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                State = true
            };

            ctx.SsCasoSocial.Add(caso);
            await ctx.SaveChangesAsync();
            return caso.Id;
        }

        public async Task Update(Guid id, CasoSocialUpdateDto dto, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var caso = await ctx.SsCasoSocial.FirstOrDefaultAsync(c => c.Id == id && c.State)
                ?? throw new AbrilException("Caso social no encontrado.", 404);

            caso.ProyectoId = dto.ProyectoId;
            caso.EmpresaId = dto.EmpresaId;
            caso.FechaApertura = dto.FechaApertura;
            caso.TipoCaso = dto.TipoCaso;
            caso.Prioridad = dto.Prioridad;
            caso.Motivo = dto.Motivo;
            caso.Descripcion = dto.Descripcion;
            caso.UpdatedAt = DateTimeOffset.UtcNow;

            await ctx.SaveChangesAsync();
        }

        public async Task Cerrar(Guid id, CasoSocialCerrarDto dto, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var caso = await ctx.SsCasoSocial.FirstOrDefaultAsync(c => c.Id == id && c.State)
                ?? throw new AbrilException("Caso social no encontrado.", 404);

            if (caso.Estado == "Cerrado")
                throw new AbrilException("El caso ya está cerrado.", 400);

            caso.Estado = "Cerrado";
            caso.FechaCierre = dto.FechaCierre;
            caso.Resultado = dto.Resultado;
            caso.CerradoPorId = userId;
            caso.UpdatedAt = DateTimeOffset.UtcNow;

            await ctx.SaveChangesAsync();
        }

        public async Task Delete(Guid id)
        {
            using var ctx = _factory.CreateDbContext();

            var caso = await ctx.SsCasoSocial.FirstOrDefaultAsync(c => c.Id == id && c.State)
                ?? throw new AbrilException("Caso social no encontrado.", 404);

            caso.State = false;
            caso.UpdatedAt = DateTimeOffset.UtcNow;

            await ctx.SaveChangesAsync();
        }
    }
}
