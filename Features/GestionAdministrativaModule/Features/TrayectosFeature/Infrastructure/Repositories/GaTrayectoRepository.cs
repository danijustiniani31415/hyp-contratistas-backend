using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionAdministrativa.Trayectos.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.Trayectos.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.Trayectos.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.GestionAdministrativa.Trayectos.Infrastructure.Repositories
{
    public class GaTrayectoRepository : IGaTrayectoRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public GaTrayectoRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<GaTrayectoListItemDto>> GetAll()
        {
            using var ctx = _factory.CreateDbContext();

            return await (
                from t  in ctx.GaTrayecto
                join lo in ctx.GaLugar  on t.LugarOrigenId  equals lo.Id
                join po in ctx.Project  on lo.ProjectId     equals (int?)po.ProjectId into poGroup
                from po in poGroup.DefaultIfEmpty()
                join ld in ctx.GaLugar  on t.LugarDestinoId equals ld.Id
                join pd in ctx.Project  on ld.ProjectId     equals (int?)pd.ProjectId into pdGroup
                from pd in pdGroup.DefaultIfEmpty()
                orderby t.CreatedAt descending
                select new GaTrayectoListItemDto
                {
                    Id                = t.Id,
                    LugarOrigenId     = t.LugarOrigenId,
                    LugarOrigenNombre = lo.Tipo == "proyecto"
                                        ? (po != null ? po.ProjectDescription : "[Sin proyecto]")
                                        : (lo.Nombre ?? string.Empty),
                    LugarDestinoId    = t.LugarDestinoId,
                    LugarDestinoNombre = ld.Tipo == "proyecto"
                                        ? (pd != null ? pd.ProjectDescription : "[Sin proyecto]")
                                        : (ld.Nombre ?? string.Empty),
                    Monto             = t.Monto,
                    Activo            = t.Activo,
                    CreatedAt         = t.CreatedAt,
                }
            ).ToListAsync();
        }

        public async Task<List<GaTrayectoLugarOptionDto>> GetLugaresActivos()
        {
            using var ctx = _factory.CreateDbContext();

            return await (
                from l in ctx.GaLugar
                join p in ctx.Project on l.ProjectId equals p.ProjectId into pGroup
                from p in pGroup.DefaultIfEmpty()
                where l.Activo && l.Tipo != "libre"
                orderby l.Orden
                select new GaTrayectoLugarOptionDto
                {
                    Id            = l.Id,
                    NombreDisplay = l.Tipo == "proyecto"
                                    ? (p != null ? p.ProjectDescription : "[Sin proyecto]")
                                    : (l.Nombre ?? string.Empty),
                }
            ).ToListAsync();
        }

        public async Task Create(GaTrayectoCreateDto dto)
        {
            ValidarDto(dto.LugarOrigenId, dto.LugarDestinoId, dto.Monto);

            using var ctx = _factory.CreateDbContext();

            await AsegurarLugaresActivos(ctx, dto.LugarOrigenId, dto.LugarDestinoId);

            var existe = await ctx.GaTrayecto
                .AnyAsync(t => t.LugarOrigenId == dto.LugarOrigenId && t.LugarDestinoId == dto.LugarDestinoId);
            if (existe)
                throw new AbrilException("Ya existe un trayecto entre esos dos lugares.", 409);

            ctx.GaTrayecto.Add(new GaTrayecto
            {
                LugarOrigenId  = dto.LugarOrigenId,
                LugarDestinoId = dto.LugarDestinoId,
                Monto          = dto.Monto,
                Activo         = true,
                CreatedAt      = DateTimeOffset.UtcNow,
            });

            await ctx.SaveChangesAsync();
        }

        public async Task<bool> Toggle(int id)
        {
            using var ctx = _factory.CreateDbContext();

            var trayecto = await ctx.GaTrayecto.FindAsync(id)
                ?? throw new AbrilException("Trayecto no encontrado.", 404);

            trayecto.Activo = !trayecto.Activo;
            await ctx.SaveChangesAsync();
            return trayecto.Activo;
        }

        public async Task Edit(int id, GaTrayectoEditDto dto)
        {
            ValidarDto(dto.LugarOrigenId, dto.LugarDestinoId, dto.Monto);

            using var ctx = _factory.CreateDbContext();

            var trayecto = await ctx.GaTrayecto.FindAsync(id)
                ?? throw new AbrilException("Trayecto no encontrado.", 404);

            await AsegurarLugaresActivos(ctx, dto.LugarOrigenId, dto.LugarDestinoId);

            var duplicado = await ctx.GaTrayecto.AnyAsync(t =>
                t.Id != id &&
                t.LugarOrigenId  == dto.LugarOrigenId &&
                t.LugarDestinoId == dto.LugarDestinoId);
            if (duplicado)
                throw new AbrilException("Ya existe otro trayecto entre esos dos lugares.", 409);

            trayecto.LugarOrigenId  = dto.LugarOrigenId;
            trayecto.LugarDestinoId = dto.LugarDestinoId;
            trayecto.Monto          = dto.Monto;
            await ctx.SaveChangesAsync();
        }

        private static void ValidarDto(int origenId, int destinoId, decimal monto)
        {
            if (origenId == destinoId)
                throw new AbrilException("El lugar de origen y el destino no pueden ser iguales.", 400);
            if (monto < 0)
                throw new AbrilException("El monto no puede ser negativo.", 400);
        }

        private static async Task AsegurarLugaresActivos(AppDbContext ctx, int origenId, int destinoId)
        {
            var lugares = await ctx.GaLugar
                .Where(l => l.Id == origenId || l.Id == destinoId)
                .Select(l => new { l.Id, l.Activo, l.Tipo })
                .ToListAsync();

            var origen = lugares.FirstOrDefault(l => l.Id == origenId)
                ?? throw new AbrilException("Lugar de origen no encontrado.", 404);
            var destino = lugares.FirstOrDefault(l => l.Id == destinoId)
                ?? throw new AbrilException("Lugar de destino no encontrado.", 404);

            if (!origen.Activo || origen.Tipo == "libre")
                throw new AbrilException("El lugar de origen debe estar activo y no puede ser \"Otro lugar\".", 400);
            if (!destino.Activo || destino.Tipo == "libre")
                throw new AbrilException("El lugar de destino debe estar activo y no puede ser \"Otro lugar\".", 400);
        }
    }
}
