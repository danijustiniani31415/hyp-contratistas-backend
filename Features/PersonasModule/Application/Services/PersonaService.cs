using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PersonasModule.Application.Dtos;
using Abril_Backend.Features.PersonasModule.Application.Interfaces;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.PersonasModule.Application.Services
{
    public class PersonaService : IPersonaService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IPasswordHasher<UsuarioSistema> _passwordHasher;

        public PersonaService(IDbContextFactory<AppDbContext> factory, IPasswordHasher<UsuarioSistema> passwordHasher)
        {
            _factory = factory;
            _passwordHasher = passwordHasher;
        }

        public async Task<PersonaListResponseDto> List(string? search, int page, int pageSize)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ctx.Persona.Where(p => p.Activo).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Nombres.ToLower().Contains(s) ||
                    p.Apellidos.ToLower().Contains(s) ||
                    p.NumeroDocumento.Contains(s));
            }

            var total = await query.CountAsync();

            var personas = await query
                .OrderBy(p => p.Apellidos).ThenBy(p => p.Nombres)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    NombreCompleto = p.Nombres + " " + p.Apellidos,
                    p.NumeroDocumento,
                    Vinculo = p.Vinculos
                        .Where(v => v.FechaFin == null)
                        .Select(v => new { v.Estado, v.TipoVinculo!.Nombre, Cargo = v.Cargo!.Nombre })
                        .FirstOrDefault(),
                    TieneUsuario = ctx.UsuarioSistema.Any(u => u.PersonaId == p.Id),
                })
                .ToListAsync();

            return new PersonaListResponseDto
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Data = personas.Select(p => new PersonaListItemDto
                {
                    Id = p.Id,
                    NombreCompleto = p.NombreCompleto,
                    NumeroDocumento = p.NumeroDocumento,
                    TipoVinculoNombre = p.Vinculo?.Nombre,
                    CargoNombre = p.Vinculo?.Cargo,
                    EstadoVinculo = p.Vinculo?.Estado ?? "SIN_VINCULO",
                    TieneUsuario = p.TieneUsuario,
                }).ToList(),
            };
        }

        public async Task<PersonaDetailDto> GetById(int id)
        {
            using var ctx = _factory.CreateDbContext();
            var persona = await ctx.Persona.FindAsync(id)
                ?? throw new AbrilException("Persona no encontrada.", 404);

            return await BuildDetail(ctx, persona);
        }

        public async Task<PersonaDetailDto> Create(PersonaCreateDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var yaExiste = await ctx.Persona.AnyAsync(p =>
                p.TipoDocumento == dto.TipoDocumento && p.NumeroDocumento == dto.NumeroDocumento);
            if (yaExiste)
                throw new AbrilException($"Ya existe una persona con {dto.TipoDocumento} {dto.NumeroDocumento}.", 409);

            var persona = new Persona
            {
                Nombres = dto.Nombres,
                Apellidos = dto.Apellidos,
                TipoDocumento = dto.TipoDocumento,
                NumeroDocumento = dto.NumeroDocumento,
                Telefono = dto.Telefono,
                EmailPersonal = dto.EmailPersonal,
                CreadoEn = DateTimeOffset.UtcNow,
                ActualizadoEn = DateTimeOffset.UtcNow,
            };
            ctx.Persona.Add(persona);
            await ctx.SaveChangesAsync();

            ctx.VinculoLaboral.Add(new VinculoLaboral
            {
                PersonaId = persona.Id,
                TipoVinculoId = dto.TipoVinculoId,
                EmpresaContratistaId = dto.EmpresaContratistaId,
                CargoId = dto.CargoId,
                FechaInicio = dto.FechaInicio,
                Estado = "ACTIVO",
                CreadoEn = DateTimeOffset.UtcNow,
            });
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, persona);
        }

        public async Task<PersonaDetailDto> NuevoVinculo(int personaId, NuevoVinculoDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var persona = await ctx.Persona.FindAsync(personaId)
                ?? throw new AbrilException("Persona no encontrada.", 404);

            using var tx = await ctx.Database.BeginTransactionAsync();

            // [DECIDIDO en CONTEXT_LOGISTICA.md] máximo un vínculo vigente por persona — cerrar
            // el anterior en la misma transacción antes de abrir el nuevo.
            var vigente = await ctx.VinculoLaboral
                .FirstOrDefaultAsync(v => v.PersonaId == personaId && v.FechaFin == null);
            if (vigente != null)
            {
                vigente.FechaFin = dto.FechaInicio.AddDays(-1) >= vigente.FechaInicio
                    ? dto.FechaInicio.AddDays(-1)
                    : dto.FechaInicio;
                vigente.Estado = "CESADO";
                vigente.MotivoCese = dto.MotivoCeseAnterior ?? "Reemplazado por nuevo vínculo";
                await ctx.SaveChangesAsync();
            }

            ctx.VinculoLaboral.Add(new VinculoLaboral
            {
                PersonaId = personaId,
                TipoVinculoId = dto.TipoVinculoId,
                EmpresaContratistaId = dto.EmpresaContratistaId,
                CargoId = dto.CargoId,
                FechaInicio = dto.FechaInicio,
                Estado = "ACTIVO",
                CreadoEn = DateTimeOffset.UtcNow,
            });
            await ctx.SaveChangesAsync();
            await tx.CommitAsync();

            return await BuildDetail(ctx, persona);
        }

        public async Task<PersonaDetailDto> CrearUsuario(int personaId, CrearUsuarioDto dto, long? otorgadoPor)
        {
            using var ctx = _factory.CreateDbContext();
            var persona = await ctx.Persona.FindAsync(personaId)
                ?? throw new AbrilException("Persona no encontrada.", 404);

            // [REVISADO en CONTEXT_LOGISTICA.md] 1 persona = máximo 1 usuario_sistema — si ya
            // tiene uno (aunque esté INACTIVO), se reactiva en vez de crear uno nuevo.
            var usuarioExistente = await ctx.UsuarioSistema.FirstOrDefaultAsync(u => u.PersonaId == personaId);
            if (usuarioExistente != null)
            {
                if (usuarioExistente.Estado == "ACTIVO")
                    throw new AbrilException("Esta persona ya tiene un usuario de sistema activo.", 409);

                usuarioExistente.Estado = "ACTIVO";
                usuarioExistente.EmailLogin = dto.EmailLogin;
                usuarioExistente.PasswordHash = _passwordHasher.HashPassword(usuarioExistente, dto.Password);
                await ctx.SaveChangesAsync();
                return await BuildDetail(ctx, persona);
            }

            var emailEnUso = await ctx.UsuarioSistema.AnyAsync(u => u.EmailLogin == dto.EmailLogin);
            if (emailEnUso)
                throw new AbrilException("Ese correo ya está en uso por otro usuario.", 409);

            var usuario = new UsuarioSistema
            {
                PersonaId = personaId,
                EmailLogin = dto.EmailLogin,
                Estado = "ACTIVO",
                CreadoEn = DateTimeOffset.UtcNow,
            };
            usuario.PasswordHash = _passwordHasher.HashPassword(usuario, dto.Password);
            ctx.UsuarioSistema.Add(usuario);
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, persona);
        }

        public async Task<PersonaDetailDto> NuevaAsignacion(int personaId, NuevaAsignacionDto dto, long? otorgadoPor)
        {
            using var ctx = _factory.CreateDbContext();
            var persona = await ctx.Persona.FindAsync(personaId)
                ?? throw new AbrilException("Persona no encontrada.", 404);

            var usuario = await ctx.UsuarioSistema.FirstOrDefaultAsync(u => u.PersonaId == personaId)
                ?? throw new AbrilException("Esta persona todavía no tiene usuario de sistema — créalo primero.", 400);

            var rol = await ctx.Rol.FindAsync(dto.RolId)
                ?? throw new AbrilException("Rol no encontrado.", 404);

            if (!rol.EsGlobal && dto.ProyectoId == null)
                throw new AbrilException($"El rol {rol.Nombre} requiere un proyecto (no es un rol global).", 400);

            ctx.UsuarioAsignacion.Add(new UsuarioAsignacion
            {
                UsuarioSistemaId = usuario.Id,
                RolId = dto.RolId,
                ProyectoId = rol.EsGlobal ? null : dto.ProyectoId,
                AlmacenId = dto.AlmacenId,
                FechaInicio = DateOnly.FromDateTime(DateTime.UtcNow),
                OtorgadoPorUsuarioSistemaId = otorgadoPor,
                CreadoEn = DateTimeOffset.UtcNow,
            });
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, persona);
        }

        public async Task<CatalogosPersonasDto> GetCatalogos()
        {
            using var ctx = _factory.CreateDbContext();
            return new CatalogosPersonasDto
            {
                TiposVinculo = await ctx.TipoVinculo.Where(t => t.Activo)
                    .Select(t => new CatalogoItemDto { Id = t.Id, Nombre = t.Nombre }).ToListAsync(),
                Cargos = await ctx.Cargo.Where(c => c.Activo)
                    .Select(c => new CatalogoItemDto { Id = c.Id, Nombre = c.Nombre }).ToListAsync(),
                EmpresasContratistas = await ctx.EmpresaContratista.Where(e => e.Activo)
                    .Select(e => new CatalogoItemDto { Id = e.Id, Nombre = e.RazonSocial }).ToListAsync(),
                Roles = await ctx.Rol.Where(r => r.Activo)
                    .Select(r => new CatalogoItemDto { Id = r.Id, Nombre = r.Nombre }).ToListAsync(),
                Proyectos = await ctx.Proyecto
                    .Select(p => new CatalogoItemDto { Id = p.Id, Nombre = p.Nombre }).ToListAsync(),
            };
        }

        // ── Interno ──────────────────────────────────────────────────────
        private static async Task<PersonaDetailDto> BuildDetail(AppDbContext ctx, Persona persona)
        {
            var vinculos = await ctx.VinculoLaboral
                .Where(v => v.PersonaId == persona.Id)
                .Include(v => v.TipoVinculo)
                .Include(v => v.EmpresaContratista)
                .Include(v => v.Cargo)
                .OrderByDescending(v => v.FechaInicio)
                .Select(v => new VinculoLaboralDto
                {
                    Id = v.Id,
                    TipoVinculoNombre = v.TipoVinculo!.Nombre,
                    EmpresaContratistaNombre = v.EmpresaContratista != null ? v.EmpresaContratista.RazonSocial : null,
                    CargoNombre = v.Cargo != null ? v.Cargo.Nombre : null,
                    FechaInicio = v.FechaInicio,
                    FechaFin = v.FechaFin,
                    Estado = v.Estado,
                    MotivoCese = v.MotivoCese,
                })
                .ToListAsync();

            var usuario = await ctx.UsuarioSistema.FirstOrDefaultAsync(u => u.PersonaId == persona.Id);

            var asignaciones = new List<LbAsignacionDto>();
            if (usuario != null)
            {
                asignaciones = await ctx.UsuarioAsignacion
                    .Where(a => a.UsuarioSistemaId == usuario.Id && a.FechaFin == null)
                    .Include(a => a.Rol)
                    .Select(a => new LbAsignacionDto
                    {
                        RolCodigo = a.Rol!.Codigo,
                        EsGlobal = a.Rol.EsGlobal,
                        ProyectoId = a.ProyectoId,
                        AlmacenId = a.AlmacenId,
                    })
                    .ToListAsync();
            }

            return new PersonaDetailDto
            {
                Id = persona.Id,
                Nombres = persona.Nombres,
                Apellidos = persona.Apellidos,
                TipoDocumento = persona.TipoDocumento,
                NumeroDocumento = persona.NumeroDocumento,
                Telefono = persona.Telefono,
                EmailPersonal = persona.EmailPersonal,
                Activo = persona.Activo,
                Vinculos = vinculos,
                UsuarioSistemaId = usuario?.Id,
                EmailLogin = usuario?.EmailLogin,
                Asignaciones = asignaciones,
            };
        }
    }
}
