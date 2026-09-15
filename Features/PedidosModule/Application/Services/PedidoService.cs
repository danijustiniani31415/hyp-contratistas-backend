using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AlmacenModule.Application.Dtos;
using Abril_Backend.Features.AlmacenModule.Application.Interfaces;
using Abril_Backend.Features.PedidosModule.Application.Dtos;
using Abril_Backend.Features.PedidosModule.Application.Interfaces;
using Abril_Backend.Features.PedidosModule.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.PedidosModule.Application.Services
{
    public class PedidoService : IPedidoService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IAlmacenKardexService _almacenService;

        public PedidoService(IDbContextFactory<AppDbContext> factory, IAlmacenKardexService almacenService)
        {
            _factory = factory;
            _almacenService = almacenService;
        }

        public async Task<PedidoDetailDto> Crear(PedidoCreateDto dto, long solicitanteId)
        {
            if (dto.Items.Count == 0)
                throw new AbrilException("El pedido debe tener al menos un producto.", 400);
            if (dto.Items.Any(i => i.CantidadSolicitada <= 0))
                throw new AbrilException("Todas las cantidades deben ser mayores a cero.", 400);

            using var ctx = _factory.CreateDbContext();

            if (!await ctx.Proyecto.AnyAsync(p => p.Id == dto.ProyectoId))
                throw new AbrilException("Proyecto no encontrado.", 404);
            if (!await ctx.Almacen.AnyAsync(a => a.Id == dto.AlmacenId))
                throw new AbrilException("Almacén no encontrado.", 404);

            // Correlativo legible (PED-2026-000001) vía secuencia — no MAX(id)+1, que colisiona
            // entre transacciones concurrentes.
            var correlativo = await ctx.Database
                .SqlQuery<long>($"""SELECT nextval('lb_pedido_correlativo') AS "Value" """)
                .SingleAsync();
            var codigo = $"PED-{DateTime.UtcNow.Year}-{correlativo:D6}";

            var pedido = new Pedido
            {
                Codigo = codigo,
                ProyectoId = dto.ProyectoId,
                AlmacenId = dto.AlmacenId,
                SolicitanteUsuarioSistemaId = solicitanteId,
                Estado = "PENDIENTE",
                Observacion = dto.Observacion,
                CreadoEn = DateTimeOffset.UtcNow,
            };
            ctx.Pedido.Add(pedido);
            await ctx.SaveChangesAsync();

            foreach (var item in dto.Items)
            {
                if (!await ctx.Producto.AnyAsync(p => p.Id == item.ProductoId))
                    throw new AbrilException($"Producto {item.ProductoId} no encontrado.", 404);

                ctx.PedidoItem.Add(new PedidoItem
                {
                    PedidoId = pedido.Id,
                    ProductoId = item.ProductoId,
                    Talla = item.Talla,
                    CantidadSolicitada = item.CantidadSolicitada,
                });
            }
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, pedido.Id);
        }

        public async Task<PedidoListResponseDto> List(string? estado, int? proyectoId, bool soloPropios, long usuarioSistemaId, int page, int pageSize)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ctx.Pedido
                .Include(p => p.Proyecto)
                .Include(p => p.Almacen)
                .Include(p => p.Solicitante!).ThenInclude(u => u!.Persona)
                .Include(p => p.Items)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(p => p.Estado == estado);
            if (proyectoId.HasValue) query = query.Where(p => p.ProyectoId == proyectoId.Value);
            if (soloPropios) query = query.Where(p => p.SolicitanteUsuarioSistemaId == usuarioSistemaId);

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(p => p.CreadoEn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PedidoListItemDto
                {
                    Id = p.Id,
                    Codigo = p.Codigo,
                    ProyectoNombre = p.Proyecto!.Nombre,
                    AlmacenNombre = p.Almacen!.Nombre,
                    SolicitanteNombre = p.Solicitante!.Persona!.Nombres + " " + p.Solicitante.Persona.Apellidos,
                    Estado = p.Estado,
                    CantidadItems = p.Items.Count,
                    CreadoEn = p.CreadoEn,
                })
                .ToListAsync();

            return new PedidoListResponseDto
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Data = data,
            };
        }

        public async Task<PedidoDetailDto> GetById(long id)
        {
            using var ctx = _factory.CreateDbContext();
            return await BuildDetail(ctx, id);
        }

        public async Task<PedidoDetailDto> Aprobar(long id, long aprobadorId)
        {
            using var ctx = _factory.CreateDbContext();
            var pedido = await ctx.Pedido.FindAsync(id) ?? throw new AbrilException("Pedido no encontrado.", 404);

            if (pedido.Estado != "PENDIENTE")
                throw new AbrilException($"Solo se puede aprobar un pedido PENDIENTE (está en {pedido.Estado}).", 400);

            pedido.Estado = "APROBADO";
            pedido.AprobadoPorUsuarioSistemaId = aprobadorId;
            pedido.AprobadoEn = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, id);
        }

        public async Task<PedidoDetailDto> Rechazar(long id, long aprobadorId, RechazarPedidoDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.MotivoRechazo))
                throw new AbrilException("El motivo de rechazo es obligatorio.", 400);

            using var ctx = _factory.CreateDbContext();
            var pedido = await ctx.Pedido.FindAsync(id) ?? throw new AbrilException("Pedido no encontrado.", 404);

            if (pedido.Estado != "PENDIENTE")
                throw new AbrilException($"Solo se puede rechazar un pedido PENDIENTE (está en {pedido.Estado}).", 400);

            pedido.Estado = "RECHAZADO";
            pedido.MotivoRechazo = dto.MotivoRechazo;
            pedido.AprobadoPorUsuarioSistemaId = aprobadorId;
            pedido.AprobadoEn = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, id);
        }

        /// <summary>
        /// Descuenta stock del almacén del pedido, un lb_movimiento SALIDA por item (referenciado
        /// al pedido). Valida stock de TODOS los items antes de mover cualquiera — si uno no
        /// alcanza, no se entrega nada (todo o nada; entrega parcial queda para más adelante).
        /// </summary>
        public async Task<PedidoDetailDto> Entregar(long id, long entregadorId)
        {
            using var ctx = _factory.CreateDbContext();
            var pedido = await ctx.Pedido.Include(p => p.Items).ThenInclude(i => i.Producto)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new AbrilException("Pedido no encontrado.", 404);

            if (pedido.Estado != "APROBADO")
                throw new AbrilException($"Solo se puede entregar un pedido APROBADO (está en {pedido.Estado}).", 400);

            var faltantes = new List<string>();
            foreach (var item in pedido.Items)
            {
                var stock = await ctx.Stock.FirstOrDefaultAsync(s =>
                    s.AlmacenId == pedido.AlmacenId && s.ProductoId == item.ProductoId && s.Talla == item.Talla);
                var disponible = stock?.CantidadActual ?? 0;
                if (disponible < item.CantidadSolicitada)
                    faltantes.Add($"{item.Producto!.Nombre} (pide {item.CantidadSolicitada}, hay {disponible})");
            }
            if (faltantes.Count > 0)
                throw new AbrilException("Stock insuficiente para entregar: " + string.Join("; ", faltantes), 400);

            foreach (var item in pedido.Items)
            {
                await _almacenService.RegistrarMovimiento(new RegistrarMovimientoDto
                {
                    AlmacenId = pedido.AlmacenId,
                    ProductoId = item.ProductoId,
                    Talla = item.Talla,
                    TipoMovimiento = "SALIDA",
                    Cantidad = item.CantidadSolicitada,
                    ReferenciaTipo = "PEDIDO",
                    ReferenciaId = pedido.Id,
                }, entregadorId);

                item.CantidadEntregada = item.CantidadSolicitada;
            }

            pedido.Estado = "ENTREGADO";
            pedido.EntregadoPorUsuarioSistemaId = entregadorId;
            pedido.EntregadoEn = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, id);
        }

        public async Task<PedidoDetailDto> Cancelar(long id, long solicitanteId)
        {
            using var ctx = _factory.CreateDbContext();
            var pedido = await ctx.Pedido.FindAsync(id) ?? throw new AbrilException("Pedido no encontrado.", 404);

            if (pedido.SolicitanteUsuarioSistemaId != solicitanteId)
                throw new AbrilException("Solo quien creó el pedido puede cancelarlo.", 403);
            if (pedido.Estado != "PENDIENTE")
                throw new AbrilException($"Solo se puede cancelar un pedido PENDIENTE (está en {pedido.Estado}).", 400);

            pedido.Estado = "CANCELADO";
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, id);
        }

        private static async Task<PedidoDetailDto> BuildDetail(AppDbContext ctx, long id)
        {
            var pedido = await ctx.Pedido
                .Include(p => p.Proyecto)
                .Include(p => p.Almacen)
                .Include(p => p.Solicitante!).ThenInclude(u => u!.Persona)
                .Include(p => p.AprobadoPor!).ThenInclude(u => u!.Persona)
                .Include(p => p.EntregadoPor!).ThenInclude(u => u!.Persona)
                .Include(p => p.Items).ThenInclude(i => i.Producto)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new AbrilException("Pedido no encontrado.", 404);

            return new PedidoDetailDto
            {
                Id = pedido.Id,
                Codigo = pedido.Codigo,
                ProyectoNombre = pedido.Proyecto!.Nombre,
                AlmacenNombre = pedido.Almacen!.Nombre,
                SolicitanteUsuarioSistemaId = pedido.SolicitanteUsuarioSistemaId,
                SolicitanteNombre = $"{pedido.Solicitante!.Persona!.Nombres} {pedido.Solicitante.Persona.Apellidos}",
                Estado = pedido.Estado,
                Observacion = pedido.Observacion,
                MotivoRechazo = pedido.MotivoRechazo,
                AprobadoPorNombre = pedido.AprobadoPor?.Persona != null
                    ? $"{pedido.AprobadoPor.Persona.Nombres} {pedido.AprobadoPor.Persona.Apellidos}" : null,
                AprobadoEn = pedido.AprobadoEn,
                EntregadoPorNombre = pedido.EntregadoPor?.Persona != null
                    ? $"{pedido.EntregadoPor.Persona.Nombres} {pedido.EntregadoPor.Persona.Apellidos}" : null,
                EntregadoEn = pedido.EntregadoEn,
                CreadoEn = pedido.CreadoEn,
                Items = pedido.Items.Select(i => new PedidoItemDetailDto
                {
                    Id = i.Id,
                    ProductoNombre = i.Producto!.Nombre,
                    ProductoCodigo = i.Producto.Codigo,
                    UnidadMedida = i.Producto.UnidadMedida,
                    Talla = i.Talla,
                    CantidadSolicitada = i.CantidadSolicitada,
                    CantidadEntregada = i.CantidadEntregada,
                }).ToList(),
            };
        }
    }
}
