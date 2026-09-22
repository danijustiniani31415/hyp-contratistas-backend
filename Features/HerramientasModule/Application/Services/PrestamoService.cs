using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AlmacenModule.Application.Dtos;
using Abril_Backend.Features.AlmacenModule.Application.Interfaces;
using Abril_Backend.Features.HerramientasModule.Application.Dtos;
using Abril_Backend.Features.HerramientasModule.Application.Interfaces;
using Abril_Backend.Features.HerramientasModule.Infrastructure.Models;
using Abril_Backend.Features.PersonasModule;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.HerramientasModule.Application.Services
{
    public class PrestamoService : IPrestamoService
    {
        private static readonly string[] EstadosDevolucionValidos = { "DEVUELTO", "PERDIDO", "DANADO" };

        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IAlmacenKardexService _almacenService;

        public PrestamoService(IDbContextFactory<AppDbContext> factory, IAlmacenKardexService almacenService)
        {
            _factory = factory;
            _almacenService = almacenService;
        }

        public async Task<PrestamoDetailDto> Crear(PrestamoCreateDto dto, long prestadoPorId)
        {
            if (dto.Items.Count == 0)
                throw new AbrilException("El préstamo debe tener al menos un producto.", 400);
            if (dto.Items.Any(i => i.Cantidad <= 0))
                throw new AbrilException("Todas las cantidades deben ser mayores a cero.", 400);

            using var ctx = _factory.CreateDbContext();

            if (!await ctx.Almacen.AnyAsync(a => a.Id == dto.AlmacenId))
                throw new AbrilException("Almacén no encontrado.", 404);
            if (!await ctx.Persona.AnyAsync(p => p.Id == dto.PersonaId))
                throw new AbrilException("Persona no encontrada.", 404);

            var faltantes = new List<string>();
            foreach (var item in dto.Items)
            {
                var producto = await ctx.Producto.FirstOrDefaultAsync(p => p.Id == item.ProductoId)
                    ?? throw new AbrilException($"Producto {item.ProductoId} no encontrado.", 404);
                if (!producto.EsRetornable)
                    throw new AbrilException($"{producto.Nombre} no está marcado como retornable — no se puede prestar, solo entregar.", 400);

                var stock = await ctx.Stock.FirstOrDefaultAsync(s =>
                    s.AlmacenId == dto.AlmacenId && s.ProductoId == item.ProductoId && s.Talla == item.Talla);
                var disponible = stock?.CantidadActual ?? 0;
                if (disponible < item.Cantidad)
                    faltantes.Add($"{producto.Nombre} (pide {item.Cantidad}, hay {disponible})");
            }
            if (faltantes.Count > 0)
                throw new AbrilException("Stock insuficiente para prestar: " + string.Join("; ", faltantes), 400);

            var correlativo = await ctx.Database
                .SqlQuery<long>($"""SELECT nextval('lb_prestamo_correlativo') AS "Value" """)
                .SingleAsync();
            var codigo = $"PRES-{DateTime.UtcNow.Year}-{correlativo:D6}";

            var prestamo = new Prestamo
            {
                Codigo = codigo,
                AlmacenId = dto.AlmacenId,
                PersonaId = dto.PersonaId,
                ProyectoId = dto.ProyectoId,
                PrestadoPorUsuarioSistemaId = prestadoPorId,
                FechaDevolucionEstimada = dto.FechaDevolucionEstimada,
                Observacion = dto.Observacion,
                CreadoEn = DateTimeOffset.UtcNow,
            };
            ctx.Prestamo.Add(prestamo);
            await ctx.SaveChangesAsync();

            foreach (var item in dto.Items)
            {
                ctx.PrestamoItem.Add(new PrestamoItem
                {
                    PrestamoId = prestamo.Id,
                    ProductoId = item.ProductoId,
                    Talla = item.Talla,
                    Cantidad = item.Cantidad,
                    Estado = "PRESTADO",
                });

                await _almacenService.RegistrarMovimiento(new RegistrarMovimientoDto
                {
                    AlmacenId = dto.AlmacenId,
                    ProductoId = item.ProductoId,
                    Talla = item.Talla,
                    TipoMovimiento = "SALIDA",
                    Cantidad = item.Cantidad,
                    ReferenciaTipo = "PRESTAMO_HERRAMIENTA",
                    ReferenciaId = prestamo.Id,
                }, prestadoPorId);
            }
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, prestamo.Id);
        }

        public async Task<PrestamoListResponseDto> List(string? search, bool soloAbiertos, HashSet<int>? proyectosPermitidos, int page, int pageSize)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ctx.Prestamo
                .Include(p => p.Persona)
                .Include(p => p.Almacen)
                .Include(p => p.Items)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Codigo.ToLower().Contains(s) ||
                    (p.Persona!.Nombres + " " + p.Persona.Apellidos).ToLower().Contains(s) ||
                    (p.Persona.Apellidos + " " + p.Persona.Nombres).ToLower().Contains(s));
            }
            if (soloAbiertos)
                query = query.Where(p => p.Items.Any(i => i.Estado == "PRESTADO"));
            // null = acceso global. Con valor: solo préstamos de esos proyectos (o sin proyecto
            // asignado, que cuenta como "central"/sin sede específica).
            if (proyectosPermitidos != null)
                query = query.Where(p => p.ProyectoId != null && proyectosPermitidos.Contains(p.ProyectoId.Value));

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(p => p.CreadoEn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PrestamoListItemDto
                {
                    Id = p.Id,
                    Codigo = p.Codigo,
                    PersonaNombre = p.Persona!.Apellidos + " " + p.Persona.Nombres,
                    AlmacenNombre = p.Almacen!.Nombre,
                    CantidadItems = p.Items.Count,
                    CantidadPendientes = p.Items.Count(i => i.Estado == "PRESTADO"),
                    Estado = p.Items.Any(i => i.Estado == "PRESTADO") ? "ABIERTO" : "CERRADO",
                    CreadoEn = p.CreadoEn,
                })
                .ToListAsync();

            return new PrestamoListResponseDto
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Data = data,
            };
        }

        public async Task<PrestamoDetailDto> GetById(long id)
        {
            using var ctx = _factory.CreateDbContext();
            return await BuildDetail(ctx, id);
        }

        public async Task<PrestamoDetailDto> DevolverItem(long prestamoId, long itemId, DevolverItemDto dto, long devueltoPorId, LbScopeProyectos scope)
        {
            if (!EstadosDevolucionValidos.Contains(dto.Estado))
                throw new AbrilException("Estado inválido — debe ser DEVUELTO, PERDIDO o DANADO.", 400);

            using var ctx = _factory.CreateDbContext();
            var prestamo = await ctx.Prestamo.FirstOrDefaultAsync(p => p.Id == prestamoId)
                ?? throw new AbrilException("Préstamo no encontrado.", 404);

            // Sin proyecto asignado = "central", visible para todos con el permiso; con proyecto,
            // exige que el scope del usuario lo cubra.
            if (prestamo.ProyectoId.HasValue && !scope.Permite(prestamo.ProyectoId.Value))
                throw new AbrilException("No tienes permiso para registrar devoluciones de este proyecto.", 403);

            var item = await ctx.PrestamoItem.FirstOrDefaultAsync(i => i.Id == itemId && i.PrestamoId == prestamoId)
                ?? throw new AbrilException("Ítem del préstamo no encontrado.", 404);

            if (item.Estado != "PRESTADO")
                throw new AbrilException($"Este ítem ya fue registrado como {item.Estado}.", 400);

            // Solo DEVUELTO repone stock — PERDIDO/DANADO se da de baja del inventario.
            if (dto.Estado == "DEVUELTO")
            {
                await _almacenService.RegistrarMovimiento(new RegistrarMovimientoDto
                {
                    AlmacenId = prestamo.AlmacenId,
                    ProductoId = item.ProductoId,
                    Talla = item.Talla,
                    TipoMovimiento = "INGRESO",
                    Cantidad = item.Cantidad,
                    ReferenciaTipo = "PRESTAMO_HERRAMIENTA",
                    ReferenciaId = prestamo.Id,
                }, devueltoPorId);
            }

            item.Estado = dto.Estado;
            item.DevueltoPorUsuarioSistemaId = devueltoPorId;
            item.FechaDevolucion = DateTimeOffset.UtcNow;
            item.ObservacionDevolucion = dto.Observacion;
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, prestamoId);
        }

        private static async Task<PrestamoDetailDto> BuildDetail(AppDbContext ctx, long id)
        {
            var prestamo = await ctx.Prestamo
                .Include(p => p.Almacen)
                .Include(p => p.Persona)
                .Include(p => p.Proyecto)
                .Include(p => p.PrestadoPor!).ThenInclude(u => u!.Persona)
                .Include(p => p.Items).ThenInclude(i => i.Producto)
                .Include(p => p.Items).ThenInclude(i => i.DevueltoPor!).ThenInclude(u => u!.Persona)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new AbrilException("Préstamo no encontrado.", 404);

            return new PrestamoDetailDto
            {
                Id = prestamo.Id,
                Codigo = prestamo.Codigo,
                AlmacenNombre = prestamo.Almacen!.Nombre,
                PersonaId = prestamo.PersonaId,
                PersonaNombre = $"{prestamo.Persona!.Apellidos} {prestamo.Persona.Nombres}",
                ProyectoNombre = prestamo.Proyecto?.Nombre,
                PrestadoPorNombre = $"{prestamo.PrestadoPor!.Persona!.Apellidos} {prestamo.PrestadoPor.Persona.Nombres}",
                FechaDevolucionEstimada = prestamo.FechaDevolucionEstimada,
                Observacion = prestamo.Observacion,
                CreadoEn = prestamo.CreadoEn,
                Estado = prestamo.Items.Any(i => i.Estado == "PRESTADO") ? "ABIERTO" : "CERRADO",
                Items = prestamo.Items.Select(i => new PrestamoItemDetailDto
                {
                    Id = i.Id,
                    ProductoNombre = i.Producto!.Nombre,
                    ProductoCodigo = i.Producto.Codigo,
                    Talla = i.Talla,
                    Cantidad = i.Cantidad,
                    Estado = i.Estado,
                    DevueltoPorNombre = i.DevueltoPor?.Persona != null
                        ? $"{i.DevueltoPor.Persona.Apellidos} {i.DevueltoPor.Persona.Nombres}" : null,
                    FechaDevolucion = i.FechaDevolucion,
                    ObservacionDevolucion = i.ObservacionDevolucion,
                }).ToList(),
            };
        }
    }
}
