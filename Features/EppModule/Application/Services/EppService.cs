using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AlmacenModule.Application.Dtos;
using Abril_Backend.Features.AlmacenModule.Application.Interfaces;
using Abril_Backend.Features.EppModule.Application.Dtos;
using Abril_Backend.Features.EppModule.Application.Interfaces;
using Abril_Backend.Features.EppModule.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.EppModule.Application.Services
{
    public class EppService : IEppService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IAlmacenKardexService _almacenService;

        public EppService(IDbContextFactory<AppDbContext> factory, IAlmacenKardexService almacenService)
        {
            _factory = factory;
            _almacenService = almacenService;
        }

        /// <summary>
        /// [CONTEXT_LOGISTICA.md sección 4.1] valida stock de todos los items antes de mover
        /// cualquiera (mismo criterio "todo o nada" de Pedidos.Entregar) y que la persona pueda
        /// recibir EPP según TipoVinculo.RequiereEpp — no según un código de vínculo hardcodeado.
        /// </summary>
        public async Task<EntregaEppDetailDto> Crear(EntregaEppCreateDto dto, long entregadoPorId)
        {
            if (dto.Items.Count == 0)
                throw new AbrilException("La entrega debe tener al menos un producto.", 400);
            if (dto.Items.Any(i => i.Cantidad <= 0))
                throw new AbrilException("Todas las cantidades deben ser mayores a cero.", 400);

            using var ctx = _factory.CreateDbContext();

            if (!await ctx.Almacen.AnyAsync(a => a.Id == dto.AlmacenId))
                throw new AbrilException("Almacén no encontrado.", 404);

            var vinculoVigente = await ctx.VinculoLaboral
                .Include(v => v.TipoVinculo)
                .Where(v => v.PersonaId == dto.PersonaId && v.FechaFin == null)
                .FirstOrDefaultAsync()
                ?? throw new AbrilException("Esta persona no tiene un vínculo laboral vigente.", 400);

            if (!vinculoVigente.TipoVinculo!.RequiereEpp)
                throw new AbrilException(
                    $"Las personas con vínculo {vinculoVigente.TipoVinculo.Nombre} no reciben EPP según su tipo de vínculo.", 400);

            var faltantes = new List<string>();
            foreach (var item in dto.Items)
            {
                var producto = await ctx.Producto.Include(p => p.Categoria).FirstOrDefaultAsync(p => p.Id == item.ProductoId)
                    ?? throw new AbrilException($"Producto {item.ProductoId} no encontrado.", 404);
                if (producto.Categoria!.Tipo != "EPP")
                    throw new AbrilException($"{producto.Nombre} no es un producto de categoría EPP.", 400);

                var stock = await ctx.Stock.FirstOrDefaultAsync(s =>
                    s.AlmacenId == dto.AlmacenId && s.ProductoId == item.ProductoId && s.Talla == item.Talla);
                var disponible = stock?.CantidadActual ?? 0;
                if (disponible < item.Cantidad)
                    faltantes.Add($"{producto.Nombre} (pide {item.Cantidad}, hay {disponible})");
            }
            if (faltantes.Count > 0)
                throw new AbrilException("Stock insuficiente para entregar: " + string.Join("; ", faltantes), 400);

            var entrega = new EntregaEpp
            {
                PersonaId = dto.PersonaId,
                AlmacenId = dto.AlmacenId,
                EntregadoPorUsuarioSistemaId = entregadoPorId,
                Observacion = dto.Observacion,
                CreadoEn = DateTimeOffset.UtcNow,
            };
            ctx.EntregaEpp.Add(entrega);
            await ctx.SaveChangesAsync();

            foreach (var item in dto.Items)
            {
                ctx.EntregaEppItem.Add(new EntregaEppItem
                {
                    EntregaId = entrega.Id,
                    ProductoId = item.ProductoId,
                    Talla = item.Talla,
                    Cantidad = item.Cantidad,
                });

                await _almacenService.RegistrarMovimiento(new RegistrarMovimientoDto
                {
                    AlmacenId = dto.AlmacenId,
                    ProductoId = item.ProductoId,
                    Talla = item.Talla,
                    TipoMovimiento = "SALIDA",
                    Cantidad = item.Cantidad,
                    ReferenciaTipo = "ENTREGA_EPP",
                    ReferenciaId = entrega.Id,
                }, entregadoPorId);
            }
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, entrega.Id);
        }

        public async Task<EntregaEppListResponseDto> List(string? search, int? personaId, HashSet<int>? proyectosPermitidos, int page, int pageSize)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ctx.EntregaEpp
                .Include(e => e.Persona)
                .Include(e => e.Almacen)
                .Include(e => e.EntregadoPor!).ThenInclude(u => u!.Persona)
                .Include(e => e.Items)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(e =>
                    (e.Persona!.Nombres + " " + e.Persona.Apellidos).ToLower().Contains(s) ||
                    (e.Persona.Apellidos + " " + e.Persona.Nombres).ToLower().Contains(s));
            }
            if (personaId.HasValue) query = query.Where(e => e.PersonaId == personaId.Value);
            // La entrega no tiene proyecto propio — se filtra por el proyecto de SU almacén.
            if (proyectosPermitidos != null)
                query = query.Where(e => e.Almacen!.ProyectoId != null && proyectosPermitidos.Contains(e.Almacen.ProyectoId.Value));

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(e => e.CreadoEn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EntregaEppListItemDto
                {
                    Id = e.Id,
                    PersonaNombre = e.Persona!.Apellidos + " " + e.Persona.Nombres,
                    AlmacenNombre = e.Almacen!.Nombre,
                    EntregadoPorNombre = e.EntregadoPor!.Persona!.Apellidos + " " + e.EntregadoPor.Persona.Nombres,
                    CantidadItems = e.Items.Count,
                    CreadoEn = e.CreadoEn,
                })
                .ToListAsync();

            return new EntregaEppListResponseDto
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Data = data,
            };
        }

        public async Task<EntregaEppDetailDto> GetById(long id)
        {
            using var ctx = _factory.CreateDbContext();
            return await BuildDetail(ctx, id);
        }

        private static async Task<EntregaEppDetailDto> BuildDetail(AppDbContext ctx, long id)
        {
            var entrega = await ctx.EntregaEpp
                .Include(e => e.Persona)
                .Include(e => e.Almacen)
                .Include(e => e.EntregadoPor!).ThenInclude(u => u!.Persona)
                .Include(e => e.Items).ThenInclude(i => i.Producto)
                .FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new AbrilException("Entrega no encontrada.", 404);

            return new EntregaEppDetailDto
            {
                Id = entrega.Id,
                PersonaId = entrega.PersonaId,
                PersonaNombre = $"{entrega.Persona!.Apellidos} {entrega.Persona.Nombres}",
                AlmacenNombre = entrega.Almacen!.Nombre,
                EntregadoPorNombre = $"{entrega.EntregadoPor!.Persona!.Apellidos} {entrega.EntregadoPor.Persona.Nombres}",
                Observacion = entrega.Observacion,
                CreadoEn = entrega.CreadoEn,
                Items = entrega.Items.Select(i => new EntregaEppItemDetailDto
                {
                    Id = i.Id,
                    ProductoNombre = i.Producto!.Nombre,
                    ProductoCodigo = i.Producto.Codigo,
                    Talla = i.Talla,
                    Cantidad = i.Cantidad,
                }).ToList(),
            };
        }
    }
}
