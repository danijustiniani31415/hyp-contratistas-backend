using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionAdministrativa.MotivosSalida.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.MotivosSalida.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.GestionAdministrativa.MotivosSalida.Infrastructure.Repositories
{
    public class GaMotivoSalidaRepository : IGaMotivoSalidaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public GaMotivoSalidaRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<GaMotivoSalidaConfigItemDto>> GetAll()
        {
            using var ctx = _factory.CreateDbContext();

            return await ctx.GaMotivoSalida
                .OrderBy(m => m.Descripcion)
                .Select(m => new GaMotivoSalidaConfigItemDto
                {
                    Id              = m.Id,
                    Descripcion     = m.Descripcion,
                    Activo          = m.Activo,
                    RequiereAdjunto = m.RequiereAdjunto,
                    EsHoraEstimada  = m.EsHoraEstimada,
                    RequiereMotivoAdicional = m.RequiereMotivoAdicional,
                    CreatedAt       = m.CreatedAt,
                })
                .ToListAsync();
        }

        public async Task Create(GaMotivoSalidaCreateDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var descripcion = dto.Descripcion?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(descripcion))
                throw new AbrilException("La descripción no puede estar vacía.", 400);

            var existe = await ctx.GaMotivoSalida
                .AnyAsync(m => m.Descripcion.ToLower() == descripcion.ToLower());

            if (existe)
                throw new AbrilException("Ya existe un motivo con esa descripción.", 409);

            ctx.GaMotivoSalida.Add(new GaMotivoSalida
            {
                Descripcion     = descripcion,
                Activo          = true,
                RequiereAdjunto = dto.RequiereAdjunto,
                EsHoraEstimada  = dto.EsHoraEstimada,
                RequiereMotivoAdicional = dto.RequiereMotivoAdicional,
                CreatedAt       = DateTimeOffset.UtcNow,
            });

            await ctx.SaveChangesAsync();
        }

        public async Task<bool> Toggle(int id)
        {
            using var ctx = _factory.CreateDbContext();

            var motivo = await ctx.GaMotivoSalida.FindAsync(id)
                ?? throw new AbrilException("Motivo no encontrado.", 404);

            motivo.Activo = !motivo.Activo;
            await ctx.SaveChangesAsync();
            return motivo.Activo;
        }

        public async Task Edit(int id, GaMotivoSalidaEditDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var descripcion = dto.Descripcion?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(descripcion))
                throw new AbrilException("La descripción no puede estar vacía.", 400);

            var motivo = await ctx.GaMotivoSalida.FindAsync(id)
                ?? throw new AbrilException("Motivo no encontrado.", 404);

            var existe = await ctx.GaMotivoSalida
                .AnyAsync(m => m.Id != id && m.Descripcion.ToLower() == descripcion.ToLower());

            if (existe)
                throw new AbrilException("Ya existe un motivo con esa descripción.", 409);

            motivo.Descripcion     = descripcion;
            motivo.RequiereAdjunto = dto.RequiereAdjunto;
            motivo.EsHoraEstimada  = dto.EsHoraEstimada;
            motivo.RequiereMotivoAdicional = dto.RequiereMotivoAdicional;
            await ctx.SaveChangesAsync();
        }
    }
}
