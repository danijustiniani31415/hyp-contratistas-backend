using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.AltaMedica;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Repositories
{
    public class AltaMedicaRepository : IAltaMedicaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public AltaMedicaRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<AltaMedicaDto?> GetByAccidenteId(int accidenteId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsAltaMedica
                .Where(a => a.AccidenteId == accidenteId && a.State)
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
        }

        public async Task<List<(int Id, string Nombre)>> GetTipos()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsAltaTipo
                .Where(t => t.Active)
                .OrderBy(t => t.Nombre)
                .Select(t => new { t.Id, t.Nombre })
                .ToListAsync()
                .ContinueWith(r => r.Result.Select(x => (x.Id, x.Nombre)).ToList());
        }

        public async Task<int> Create(int accidenteId, AltaMedicaCreateDto dto, int registradoPorId)
        {
            using var ctx = _factory.CreateDbContext();

            var existeAccidente = await ctx.SsAccidenteTrabajo.AnyAsync(a => a.Id == accidenteId);
            if (!existeAccidente) throw new AbrilException("Accidente de trabajo no encontrado.", 404);

            var existeAlta = await ctx.SsAltaMedica.AnyAsync(a => a.AccidenteId == accidenteId && a.State);
            if (existeAlta) throw new AbrilException("Ya existe un alta médica registrada para este accidente.", 400);

            var entity = new SsAltaMedica
            {
                AccidenteId = accidenteId,
                TipoId = dto.TipoId,
                FechaAlta = dto.FechaAlta,
                Medico = dto.Medico,
                DiagnosticoFinal = dto.DiagnosticoFinal,
                TieneRestriccion = dto.TieneRestriccion,
                DescripcionRestriccion = dto.DescripcionRestriccion,
                FechaFinRestriccion = dto.FechaFinRestriccion,
                UrlCertificado = dto.UrlCertificado,
                Observaciones = dto.Observaciones,
                RegistradoPorId = registradoPorId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            ctx.SsAltaMedica.Add(entity);

            // Actualizar fecha_alta en ss_accidente_trabajo para compatibilidad
            var accidente = await ctx.SsAccidenteTrabajo.FindAsync(accidenteId);
            if (accidente != null)
            {
                accidente.FechaAlta = dto.FechaAlta;
                if (dto.TieneRestriccion && !string.IsNullOrWhiteSpace(dto.DescripcionRestriccion))
                    accidente.RestriccionesReintegro = dto.DescripcionRestriccion;
                accidente.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await ctx.SaveChangesAsync();
            return entity.Id;
        }

        public async Task Update(int accidenteId, AltaMedicaUpdateDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = await ctx.SsAltaMedica.FirstOrDefaultAsync(a => a.AccidenteId == accidenteId && a.State)
                ?? throw new AbrilException("Alta médica no encontrada para este accidente.", 404);

            entity.TipoId = dto.TipoId;
            entity.FechaAlta = dto.FechaAlta;
            entity.Medico = dto.Medico;
            entity.DiagnosticoFinal = dto.DiagnosticoFinal;
            entity.TieneRestriccion = dto.TieneRestriccion;
            entity.DescripcionRestriccion = dto.DescripcionRestriccion;
            entity.FechaFinRestriccion = dto.FechaFinRestriccion;
            entity.UrlCertificado = dto.UrlCertificado;
            entity.Observaciones = dto.Observaciones;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            // Sincronizar en ss_accidente_trabajo
            var accidente = await ctx.SsAccidenteTrabajo.FindAsync(accidenteId);
            if (accidente != null)
            {
                accidente.FechaAlta = dto.FechaAlta;
                accidente.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await ctx.SaveChangesAsync();
        }

        public async Task Delete(int accidenteId)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = await ctx.SsAltaMedica.FirstOrDefaultAsync(a => a.AccidenteId == accidenteId && a.State)
                ?? throw new AbrilException("Alta médica no encontrada para este accidente.", 404);

            entity.State = false;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            // Limpiar fecha_alta en ss_accidente_trabajo
            var accidente = await ctx.SsAccidenteTrabajo.FindAsync(accidenteId);
            if (accidente != null)
            {
                accidente.FechaAlta = null;
                accidente.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await ctx.SaveChangesAsync();
        }
    }
}
