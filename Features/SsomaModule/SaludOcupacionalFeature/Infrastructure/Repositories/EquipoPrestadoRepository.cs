using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.EquipoPrestado;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Repositories
{
    public class EquipoPrestadoRepository : IEquipoPrestadoRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public EquipoPrestadoRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<EquipoPrestadoListItemDto>> GetByAccidenteId(int accidenteId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsEquipoPrestado
                .Where(e => e.AccidenteId == accidenteId && e.State)
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
        }

        public async Task<List<(int Id, string Nombre)>> GetTipos()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsEquipoTipo
                .Where(t => t.Active)
                .OrderBy(t => t.Nombre)
                .Select(t => new { t.Id, t.Nombre })
                .ToListAsync()
                .ContinueWith(r => r.Result.Select(x => (x.Id, x.Nombre)).ToList());
        }

        public async Task<int> Create(int accidenteId, EquipoPrestadoCreateDto dto, int registradoPorId)
        {
            using var ctx = _factory.CreateDbContext();

            var existe = await ctx.SsAccidenteTrabajo.AnyAsync(a => a.Id == accidenteId);
            if (!existe) throw new AbrilException("Accidente de trabajo no encontrado.", 404);

            var entity = new SsEquipoPrestado
            {
                AccidenteId = accidenteId,
                TipoEquipoId = dto.TipoEquipoId,
                Cantidad = dto.Cantidad,
                FechaPrestamo = dto.FechaPrestamo,
                Observaciones = dto.Observaciones,
                UrlEvidencia = dto.UrlEvidencia,
                RegistradoPorId = registradoPorId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            ctx.SsEquipoPrestado.Add(entity);
            await ctx.SaveChangesAsync();
            return entity.Id;
        }

        public async Task Devolver(int id, EquipoPrestadoDevolverDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = await ctx.SsEquipoPrestado.FirstOrDefaultAsync(e => e.Id == id && e.State)
                ?? throw new AbrilException("Equipo prestado no encontrado.", 404);

            if (entity.Devuelto)
                throw new AbrilException("El equipo ya fue registrado como devuelto.", 400);

            entity.Devuelto = true;
            entity.FechaDevolucion = dto.FechaDevolucion;
            entity.Observaciones = dto.Observaciones ?? entity.Observaciones;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            await ctx.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            using var ctx = _factory.CreateDbContext();

            var entity = await ctx.SsEquipoPrestado.FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new AbrilException("Equipo prestado no encontrado.", 404);

            entity.State = false;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }
    }
}
