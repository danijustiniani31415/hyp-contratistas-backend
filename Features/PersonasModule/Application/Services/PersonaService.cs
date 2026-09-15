using System.Security.Cryptography;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PersonasModule.Application.Dtos;
using Abril_Backend.Features.PersonasModule.Application.Interfaces;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Abril_Backend.Features.PersonasModule.Application.Services
{
    public class PersonaService : IPersonaService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IPasswordHasher<UsuarioSistema> _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly FrontendSettings _frontendSettings;

        public PersonaService(
            IDbContextFactory<AppDbContext> factory,
            IPasswordHasher<UsuarioSistema> passwordHasher,
            IEmailService emailService,
            IOptions<FrontendSettings> frontendSettings)
        {
            _factory = factory;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _frontendSettings = frontendSettings.Value;
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

            var email = dto.EmailLogin.Trim().ToLower();

            // [REVISADO en CONTEXT_LOGISTICA.md] 1 persona = máximo 1 usuario_sistema — si ya
            // tiene uno (aunque esté INACTIVO), se reactiva en vez de crear uno nuevo.
            var usuarioExistente = await ctx.UsuarioSistema.FirstOrDefaultAsync(u => u.PersonaId == personaId);
            UsuarioSistema usuario;
            if (usuarioExistente != null)
            {
                if (usuarioExistente.Estado == "ACTIVO")
                    throw new AbrilException("Esta persona ya tiene un usuario de sistema activo.", 409);

                usuarioExistente.Estado = "ACTIVO";
                usuarioExistente.EmailLogin = email;
                usuario = usuarioExistente;
            }
            else
            {
                var emailEnUso = await ctx.UsuarioSistema.AnyAsync(u => u.EmailLogin == email);
                if (emailEnUso)
                    throw new AbrilException("Ese correo ya está en uso por otro usuario.", 409);

                usuario = new UsuarioSistema
                {
                    PersonaId = personaId,
                    EmailLogin = email,
                    Estado = "ACTIVO",
                    CreadoEn = DateTimeOffset.UtcNow,
                };
                ctx.UsuarioSistema.Add(usuario);
            }

            // Contraseña interna aleatoria e inutilizable — nadie la conoce nunca. La persona
            // la define ella misma con el enlace de activación de abajo (mismo mecanismo que
            // "olvidé mi contraseña"). Así el admin jamás maneja ni ve una contraseña ajena.
            usuario.PasswordHash = _passwordHasher.HashPassword(usuario, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
            await ctx.SaveChangesAsync();

            var tokensPrevios = await ctx.LbUsuarioPasswordToken
                .Where(t => t.UsuarioSistemaId == usuario.Id && !t.Usado)
                .ToListAsync();
            foreach (var t in tokensPrevios) t.Usado = true;
            if (tokensPrevios.Count > 0) await ctx.SaveChangesAsync();

            var token = GenerarToken();
            ctx.LbUsuarioPasswordToken.Add(new LbUsuarioPasswordToken
            {
                UsuarioSistemaId = usuario.Id,
                Token = token,
                ExpiraEn = DateTime.UtcNow.AddHours(48),
                Usado = false,
                CreadoEn = DateTime.UtcNow,
            });
            await ctx.SaveChangesAsync();

            var link = $"{_frontendSettings.LbSetPasswordUrl}?token={token}";
            var html = $@"<h2>Bienvenido a HP Constructores Generales</h2>
<p>Hola {persona.Nombres}, se creó tu acceso al sistema.</p>
<p>Haz clic en el siguiente enlace para crear tu contraseña y empezar a usarlo:</p>
<a href='{link}' style='background:#0F172A;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;display:inline-block;margin:16px 0'>Activar mi cuenta</a>
<p>Este enlace expira en 48 horas.</p>";

            await _emailService.SendAsync(
                to: new List<string> { email },
                subject: "Activa tu cuenta - HP Constructores Generales",
                body: html,
                isHtml: true);

            return await BuildDetail(ctx, persona);
        }

        /// <summary>Token opaco de un solo uso — mismo formato que LbAuthService.GenerarToken.</summary>
        private static string GenerarToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
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

        public async Task<PersonaDetailDto> RevocarAsignacion(int personaId, long asignacionId)
        {
            using var ctx = _factory.CreateDbContext();
            var persona = await ctx.Persona.FindAsync(personaId)
                ?? throw new AbrilException("Persona no encontrada.", 404);

            var usuario = await ctx.UsuarioSistema.FirstOrDefaultAsync(u => u.PersonaId == personaId)
                ?? throw new AbrilException("Esta persona no tiene usuario de sistema.", 404);

            var asignacion = await ctx.UsuarioAsignacion
                .FirstOrDefaultAsync(a => a.Id == asignacionId && a.UsuarioSistemaId == usuario.Id && a.FechaFin == null)
                ?? throw new AbrilException("Asignación no encontrada o ya revocada.", 404);

            asignacion.FechaFin = DateOnly.FromDateTime(DateTime.UtcNow);
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
                    .Select(r => new RolCatalogoItemDto { Id = r.Id, Nombre = r.Nombre, EsGlobal = r.EsGlobal }).ToListAsync(),
                Proyectos = await ctx.Proyecto
                    .Select(p => new CatalogoItemDto { Id = p.Id, Nombre = p.Nombre }).ToListAsync(),
                Almacenes = await ctx.Almacen.Where(a => a.Activo)
                    .Select(a => new AlmacenCatalogoItemDto { Id = a.Id, Nombre = a.Nombre, ProyectoId = a.ProyectoId }).ToListAsync(),
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

            var asignaciones = new List<AsignacionDetalleDto>();
            if (usuario != null)
            {
                asignaciones = await ctx.UsuarioAsignacion
                    .Where(a => a.UsuarioSistemaId == usuario.Id && a.FechaFin == null)
                    .Include(a => a.Rol)
                    .Include(a => a.Proyecto)
                    .Include(a => a.Almacen)
                    .OrderByDescending(a => a.FechaInicio)
                    .Select(a => new AsignacionDetalleDto
                    {
                        Id = a.Id,
                        RolNombre = a.Rol!.Nombre,
                        EsGlobal = a.Rol.EsGlobal,
                        ProyectoNombre = a.Proyecto != null ? a.Proyecto.Nombre : null,
                        AlmacenNombre = a.Almacen != null ? a.Almacen.Nombre : null,
                        FechaInicio = a.FechaInicio,
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
