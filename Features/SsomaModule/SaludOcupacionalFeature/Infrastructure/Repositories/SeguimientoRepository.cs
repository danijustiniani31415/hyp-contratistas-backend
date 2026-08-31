using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.CasoSocial;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Repositories
{
    public class SeguimientoRepository : ISeguimientoRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public SeguimientoRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<Guid> Create(Guid casoId, SeguimientoCreateDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var casoExiste = await ctx.SsCasoSocial.AnyAsync(c => c.Id == casoId && c.State);
            if (!casoExiste)
                throw new AbrilException("Caso social no encontrado.", 404);

            var seguimiento = new SsCasoSocialSeguimiento
            {
                Id = Guid.NewGuid(),
                CasoId = casoId,
                Fecha = dto.Fecha,
                Tipo = dto.Tipo,
                Descripcion = dto.Descripcion,
                ResponsableId = dto.ResponsableId,
                ProximaAccion = dto.ProximaAccion,
                AccionTomada = dto.AccionTomada,
                CreatedAt = DateTimeOffset.UtcNow,
                State = true
            };

            ctx.SsCasoSocialSeguimiento.Add(seguimiento);
            await ctx.SaveChangesAsync();
            return seguimiento.Id;
        }

        public async Task Delete(Guid id)
        {
            using var ctx = _factory.CreateDbContext();

            var seguimiento = await ctx.SsCasoSocialSeguimiento.FirstOrDefaultAsync(s => s.Id == id && s.State)
                ?? throw new AbrilException("Seguimiento no encontrado.", 404);

            seguimiento.State = false;
            await ctx.SaveChangesAsync();
        }
    }
}
