using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.SctrGestion;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Repositories
{
    public class SctrGestionRepository : ISctrGestionRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public SctrGestionRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<SctrGestionDto>> GetByCasoSocialId(Guid casoSocialId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsSctrGestion
                .Where(s => s.CasoSocialId == casoSocialId && s.State)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new SctrGestionDto
                {
                    Id = s.Id,
                    CasoSocialId = s.CasoSocialId,
                    NumeroSiniestro = s.NumeroSiniestro,
                    FechaReporteSctr = s.FechaReporteSctr,
                    FechaAtencionSctr = s.FechaAtencionSctr,
                    Aseguradora = s.Aseguradora,
                    MontoCubierto = s.MontoCubierto,
                    UrlHojaAtencion = s.UrlHojaAtencion,
                    UrlDocumentosAdicionales = s.UrlDocumentosAdicionales,
                    EstadoId = s.EstadoId,
                    EstadoNombre = s.Estado != null ? s.Estado.Nombre : string.Empty,
                    Observaciones = s.Observaciones,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<int> Create(Guid casoSocialId, SctrGestionCreateDto dto, int registradoPorId)
        {
            using var ctx = _factory.CreateDbContext();

            var existeCaso = await ctx.SsCasoSocial.AnyAsync(c => c.Id == casoSocialId && c.State);
            if (!existeCaso) throw new AbrilException("Caso social no encontrado.", 404);

            var entity = new SsSctrGestion
            {
                CasoSocialId = casoSocialId,
                NumeroSiniestro = dto.NumeroSiniestro,
                FechaReporteSctr = dto.FechaReporteSctr,
                FechaAtencionSctr = dto.FechaAtencionSctr,
                Aseguradora = dto.Aseguradora,
                MontoCubierto = dto.MontoCubierto,
                UrlHojaAtencion = dto.UrlHojaAtencion,
                UrlDocumentosAdicionales = dto.UrlDocumentosAdicionales,
                EstadoId = dto.EstadoId,
                Observaciones = dto.Observaciones,
                RegistradoPorId = registradoPorId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            ctx.SsSctrGestion.Add(entity);
            await ctx.SaveChangesAsync();
            return entity.Id;
        }

        public async Task Update(int id, SctrGestionUpdateDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = await ctx.SsSctrGestion.FirstOrDefaultAsync(s => s.Id == id && s.State)
                ?? throw new AbrilException("Gestión SCTR no encontrada.", 404);

            entity.NumeroSiniestro = dto.NumeroSiniestro;
            entity.FechaReporteSctr = dto.FechaReporteSctr;
            entity.FechaAtencionSctr = dto.FechaAtencionSctr;
            entity.Aseguradora = dto.Aseguradora;
            entity.MontoCubierto = dto.MontoCubierto;
            entity.UrlHojaAtencion = dto.UrlHojaAtencion;
            entity.UrlDocumentosAdicionales = dto.UrlDocumentosAdicionales;
            entity.EstadoId = dto.EstadoId;
            entity.Observaciones = dto.Observaciones;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            await ctx.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = await ctx.SsSctrGestion.FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new AbrilException("Gestión SCTR no encontrada.", 404);

            entity.State = false;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }
    }
}
