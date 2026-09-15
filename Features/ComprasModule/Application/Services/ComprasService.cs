using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AlmacenModule.Application.Dtos;
using Abril_Backend.Features.AlmacenModule.Application.Interfaces;
using Abril_Backend.Features.ComprasModule.Application.Dtos;
using Abril_Backend.Features.ComprasModule.Application.Interfaces;
using Abril_Backend.Features.ComprasModule.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.ComprasModule.Application.Services
{
    public class ComprasService : IComprasService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IAlmacenKardexService _almacenService;

        public ComprasService(IDbContextFactory<AppDbContext> factory, IAlmacenKardexService almacenService)
        {
            _factory = factory;
            _almacenService = almacenService;
        }

        public async Task<List<ProveedorDto>> ListProveedores()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Proveedor.Where(p => p.Activo)
                .OrderBy(p => p.RazonSocial)
                .Select(p => new ProveedorDto
                {
                    Id = p.Id,
                    RazonSocial = p.RazonSocial,
                    Ruc = p.Ruc,
                    Contacto = p.Contacto,
                    Telefono = p.Telefono,
                    Email = p.Email,
                })
                .ToListAsync();
        }

        public async Task<ProveedorDto> CrearProveedor(ProveedorCreateDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            if (!string.IsNullOrWhiteSpace(dto.Ruc) && await ctx.Proveedor.AnyAsync(p => p.Ruc == dto.Ruc))
                throw new AbrilException($"Ya existe un proveedor con RUC {dto.Ruc}.", 409);

            var proveedor = new Proveedor
            {
                RazonSocial = dto.RazonSocial,
                Ruc = dto.Ruc,
                Contacto = dto.Contacto,
                Telefono = dto.Telefono,
                Email = dto.Email,
                Activo = true,
            };
            ctx.Proveedor.Add(proveedor);
            await ctx.SaveChangesAsync();

            return new ProveedorDto
            {
                Id = proveedor.Id,
                RazonSocial = proveedor.RazonSocial,
                Ruc = proveedor.Ruc,
                Contacto = proveedor.Contacto,
                Telefono = proveedor.Telefono,
                Email = proveedor.Email,
            };
        }

        public async Task<OrdenCompraDetailDto> Crear(OrdenCompraCreateDto dto, long solicitadoPorId)
        {
            if (dto.Items.Count == 0)
                throw new AbrilException("La orden debe tener al menos un producto.", 400);
            if (dto.Items.Any(i => i.CantidadSolicitada <= 0 || i.CostoUnitario < 0))
                throw new AbrilException("Cantidades y costos deben ser válidos.", 400);

            using var ctx = _factory.CreateDbContext();

            if (!await ctx.Proveedor.AnyAsync(p => p.Id == dto.ProveedorId))
                throw new AbrilException("Proveedor no encontrado.", 404);
            if (!await ctx.Almacen.AnyAsync(a => a.Id == dto.AlmacenId))
                throw new AbrilException("Almacén no encontrado.", 404);

            var correlativo = await ctx.Database
                .SqlQuery<long>($"""SELECT nextval('lb_orden_compra_correlativo') AS "Value" """)
                .SingleAsync();
            var codigo = $"OC-{DateTime.UtcNow.Year}-{correlativo:D6}";

            var orden = new OrdenCompra
            {
                Codigo = codigo,
                ProveedorId = dto.ProveedorId,
                AlmacenId = dto.AlmacenId,
                SolicitadoPorUsuarioSistemaId = solicitadoPorId,
                Estado = "PENDIENTE",
                Observacion = dto.Observacion,
                CreadoEn = DateTimeOffset.UtcNow,
            };
            ctx.OrdenCompra.Add(orden);
            await ctx.SaveChangesAsync();

            foreach (var item in dto.Items)
            {
                if (!await ctx.Producto.AnyAsync(p => p.Id == item.ProductoId))
                    throw new AbrilException($"Producto {item.ProductoId} no encontrado.", 404);

                ctx.OrdenCompraItem.Add(new OrdenCompraItem
                {
                    OrdenCompraId = orden.Id,
                    ProductoId = item.ProductoId,
                    Talla = item.Talla,
                    CantidadSolicitada = item.CantidadSolicitada,
                    CostoUnitario = item.CostoUnitario,
                    CantidadRecibida = 0,
                });
            }
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, orden.Id);
        }

        public async Task<OrdenCompraListResponseDto> List(string? estado, int page, int pageSize)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ctx.OrdenCompra
                .Include(o => o.Proveedor)
                .Include(o => o.Almacen)
                .Include(o => o.Items)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(o => o.Estado == estado);

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(o => o.CreadoEn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrdenCompraListItemDto
                {
                    Id = o.Id,
                    Codigo = o.Codigo,
                    ProveedorNombre = o.Proveedor!.RazonSocial,
                    AlmacenNombre = o.Almacen!.Nombre,
                    Estado = o.Estado,
                    CantidadItems = o.Items.Count,
                    CreadoEn = o.CreadoEn,
                })
                .ToListAsync();

            return new OrdenCompraListResponseDto
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Data = data,
            };
        }

        public async Task<OrdenCompraDetailDto> GetById(long id)
        {
            using var ctx = _factory.CreateDbContext();
            return await BuildDetail(ctx, id);
        }

        public async Task<OrdenCompraDetailDto> RecibirItem(long ordenId, long itemId, RecibirItemDto dto, long recibidoPorId)
        {
            if (dto.Cantidad <= 0)
                throw new AbrilException("La cantidad recibida debe ser mayor a cero.", 400);

            using var ctx = _factory.CreateDbContext();
            var orden = await ctx.OrdenCompra.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == ordenId)
                ?? throw new AbrilException("Orden de compra no encontrada.", 404);

            if (orden.Estado == "CANCELADA")
                throw new AbrilException("Esta orden está cancelada.", 400);

            var item = orden.Items.FirstOrDefault(i => i.Id == itemId)
                ?? throw new AbrilException("Ítem de la orden no encontrado.", 404);

            var pendiente = item.CantidadSolicitada - item.CantidadRecibida;
            if (dto.Cantidad > pendiente)
                throw new AbrilException($"Solo quedan {pendiente} pendientes de este ítem — no se puede recibir {dto.Cantidad}.", 400);

            await _almacenService.RegistrarMovimiento(new RegistrarMovimientoDto
            {
                AlmacenId = orden.AlmacenId,
                ProductoId = item.ProductoId,
                Talla = item.Talla,
                TipoMovimiento = "INGRESO",
                Cantidad = dto.Cantidad,
                CostoUnitario = item.CostoUnitario,
                ReferenciaTipo = "COMPRA",
                ReferenciaId = orden.Id,
            }, recibidoPorId);

            ctx.OrdenCompraRecepcion.Add(new OrdenCompraRecepcion
            {
                OrdenCompraItemId = item.Id,
                Cantidad = dto.Cantidad,
                RecibidoPorUsuarioSistemaId = recibidoPorId,
                CreadoEn = DateTimeOffset.UtcNow,
            });

            item.CantidadRecibida += dto.Cantidad;

            var todosCompletos = orden.Items.All(i => i.CantidadRecibida >= i.CantidadSolicitada);
            var algunoRecibido = orden.Items.Any(i => i.CantidadRecibida > 0);
            orden.Estado = todosCompletos ? "RECIBIDA" : algunoRecibido ? "RECIBIDA_PARCIAL" : "PENDIENTE";

            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, ordenId);
        }

        public async Task<OrdenCompraDetailDto> Cancelar(long id)
        {
            using var ctx = _factory.CreateDbContext();
            var orden = await ctx.OrdenCompra.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id)
                ?? throw new AbrilException("Orden de compra no encontrada.", 404);

            if (orden.Items.Any(i => i.CantidadRecibida > 0))
                throw new AbrilException("No se puede cancelar una orden que ya tiene mercadería recibida.", 400);
            if (orden.Estado == "CANCELADA")
                throw new AbrilException("Esta orden ya está cancelada.", 400);

            orden.Estado = "CANCELADA";
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, id);
        }

        private static async Task<OrdenCompraDetailDto> BuildDetail(AppDbContext ctx, long id)
        {
            var orden = await ctx.OrdenCompra
                .Include(o => o.Proveedor)
                .Include(o => o.Almacen)
                .Include(o => o.SolicitadoPor!).ThenInclude(u => u!.Persona)
                .Include(o => o.Items).ThenInclude(i => i.Producto)
                .FirstOrDefaultAsync(o => o.Id == id)
                ?? throw new AbrilException("Orden de compra no encontrada.", 404);

            return new OrdenCompraDetailDto
            {
                Id = orden.Id,
                Codigo = orden.Codigo,
                ProveedorNombre = orden.Proveedor!.RazonSocial,
                AlmacenNombre = orden.Almacen!.Nombre,
                SolicitadoPorNombre = $"{orden.SolicitadoPor!.Persona!.Nombres} {orden.SolicitadoPor.Persona.Apellidos}",
                Estado = orden.Estado,
                Observacion = orden.Observacion,
                CreadoEn = orden.CreadoEn,
                Items = orden.Items.Select(i => new OrdenCompraItemDetailDto
                {
                    Id = i.Id,
                    ProductoNombre = i.Producto!.Nombre,
                    ProductoCodigo = i.Producto.Codigo,
                    Talla = i.Talla,
                    CantidadSolicitada = i.CantidadSolicitada,
                    CostoUnitario = i.CostoUnitario,
                    CantidadRecibida = i.CantidadRecibida,
                    CantidadPendiente = i.CantidadSolicitada - i.CantidadRecibida,
                }).ToList(),
            };
        }
    }
}
