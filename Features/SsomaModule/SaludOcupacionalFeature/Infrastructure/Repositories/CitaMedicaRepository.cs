using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.CitaMedica;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Repositories
{
    public class CitaMedicaRepository : ICitaMedicaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public CitaMedicaRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<CitaMedicaListItemDto>> GetByAccidenteId(int accidenteId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsCitaMedica
                .Where(c => c.AccidenteId == accidenteId && c.State)
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
        }

        public async Task<CitaMedicaListItemDto> GetById(int id)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsCitaMedica
                .Where(c => c.Id == id && c.State)
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
                .FirstOrDefaultAsync()
                ?? throw new AbrilException("Cita médica no encontrada.", 404);
        }

        public async Task<List<(int Id, string Nombre)>> GetTipos()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsCitaTipo
                .Where(t => t.Active)
                .OrderBy(t => t.Nombre)
                .Select(t => new { t.Id, t.Nombre })
                .ToListAsync()
                .ContinueWith(r => r.Result.Select(x => (x.Id, x.Nombre)).ToList());
        }

        public async Task<int> Create(int accidenteId, CitaMedicaCreateDto dto, int registradoPorId)
        {
            using var ctx = _factory.CreateDbContext();

            var existe = await ctx.SsAccidenteTrabajo.AnyAsync(a => a.Id == accidenteId);
            if (!existe) throw new AbrilException("Accidente de trabajo no encontrado.", 404);

            var entity = new SsCitaMedica
            {
                AccidenteId = accidenteId,
                TipoId = dto.TipoId,
                FechaCita = dto.FechaCita,
                HoraCita = dto.HoraCita,
                Clinica = dto.Clinica,
                Medico = dto.Medico,
                Diagnostico = dto.Diagnostico,
                Indicaciones = dto.Indicaciones,
                ProximaCita = dto.ProximaCita,
                UrlEvidencia = dto.UrlEvidencia,
                Observaciones = dto.Observaciones,
                RegistradoPorId = registradoPorId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            ctx.SsCitaMedica.Add(entity);
            await ctx.SaveChangesAsync();
            return entity.Id;
        }

        public async Task Update(int id, CitaMedicaUpdateDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = await ctx.SsCitaMedica.FirstOrDefaultAsync(c => c.Id == id && c.State)
                ?? throw new AbrilException("Cita médica no encontrada.", 404);

            entity.TipoId = dto.TipoId;
            entity.FechaCita = dto.FechaCita;
            entity.HoraCita = dto.HoraCita;
            entity.Clinica = dto.Clinica;
            entity.Medico = dto.Medico;
            entity.Diagnostico = dto.Diagnostico;
            entity.Indicaciones = dto.Indicaciones;
            entity.ProximaCita = dto.ProximaCita;
            entity.UrlEvidencia = dto.UrlEvidencia;
            entity.Observaciones = dto.Observaciones;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            await ctx.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = await ctx.SsCitaMedica.FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new AbrilException("Cita médica no encontrada.", 404);

            entity.State = false;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }
    }
}
