using System.Security.Cryptography;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PersonasModule.Application.Dtos;
using Abril_Backend.Features.PersonasModule.Application.Interfaces;
using Abril_Backend.Features.PersonasModule.Infrastructure.Interfaces;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Abril_Backend.Features.PersonasModule.Application.Services
{
    public class LbAuthService : ILbAuthService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IPasswordHasher<UsuarioSistema> _passwordHasher;
        private readonly ILbJwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly FrontendSettings _frontendSettings;

        public LbAuthService(
            IDbContextFactory<AppDbContext> factory,
            IPasswordHasher<UsuarioSistema> passwordHasher,
            ILbJwtService jwtService,
            IEmailService emailService,
            IOptions<FrontendSettings> frontendSettings)
        {
            _factory = factory;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _emailService = emailService;
            _frontendSettings = frontendSettings.Value;
        }

        public async Task<LbLoginResponseDto> Login(LbLoginRequestDto request)
        {
            using var ctx = _factory.CreateDbContext();

            var usuario = await ctx.UsuarioSistema
                .Include(u => u.Persona)
                .FirstOrDefaultAsync(u => u.EmailLogin == request.Email);

            if (usuario is null)
                throw new AbrilException("Credenciales inválidas.", 401);

            if (usuario.Estado != "ACTIVO")
                throw new AbrilException("Este usuario está inactivo. Contacta al administrador.", 403);

            var resultado = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password);
            if (resultado == PasswordVerificationResult.Failed)
                throw new AbrilException("Credenciales inválidas.", 401);

            // Resolución de asignaciones vigentes + permisos — misma query de
            // CONTEXT_LOGISTICA.md sección 4.4.
            var asignaciones = await ctx.UsuarioAsignacion
                .Where(ua => ua.UsuarioSistemaId == usuario.Id && ua.FechaFin == null)
                .Include(ua => ua.Rol)
                .ToListAsync();

            var rolIds = asignaciones.Select(a => a.RolId).Distinct().ToList();
            var permisosPorRol = await ctx.RolPermiso
                .Where(rp => rolIds.Contains(rp.RolId))
                .Include(rp => rp.Permiso)
                .Select(rp => new { rp.RolId, Codigo = rp.Permiso!.Codigo })
                .ToListAsync();
            var permisosPorRolLookup = permisosPorRol
                .GroupBy(x => x.RolId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Codigo).ToList());

            usuario.UltimoAcceso = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();

            var response = new LbLoginResponseDto
            {
                UsuarioSistemaId = usuario.Id,
                Email = usuario.EmailLogin,
                NombreCompleto = $"{usuario.Persona!.Apellidos} {usuario.Persona.Nombres}",
                // EsGlobal es por asignación (ProyectoId null en ESTA fila), no por rol — el mismo
                // rol puede ser global para una persona y de un solo proyecto para otra (ej.
                // "Logística" en Lima vs "Logística" en Las Bravas).
                Asignaciones = asignaciones.Select(a => new LbAsignacionDto
                {
                    RolCodigo = a.Rol!.Codigo,
                    EsGlobal = a.ProyectoId == null,
                    ProyectoId = a.ProyectoId,
                    AlmacenId = a.AlmacenId,
                    Permisos = permisosPorRolLookup.GetValueOrDefault(a.RolId, new List<string>()),
                }).ToList(),
                Permisos = permisosPorRol.Select(x => x.Codigo).Distinct().ToList(),
            };

            response.Token = _jwtService.GenerateToken(response);
            return response;
        }

        public async Task<LbLoginResponseDto> SeedAdmin(LbLoginRequestDto request)
        {
            using var ctx = _factory.CreateDbContext();

            if (await ctx.UsuarioSistema.AnyAsync())
                throw new AbrilException("Ya existe al menos un usuario — el bootstrap solo corre en una base vacía.", 409);

            var rolAdmin = await ctx.Rol.FirstOrDefaultAsync(r => r.Codigo == "ADMIN")
                ?? throw new AbrilException("No existe el rol ADMIN — corre el DDL de la sección 4 primero.", 500);

            var tipoPlanilla = await ctx.TipoVinculo.FirstOrDefaultAsync(t => t.Codigo == "PLANILLA")
                ?? throw new AbrilException("No existe el tipo de vínculo PLANILLA.", 500);

            var persona = new Persona
            {
                Nombres = "Administrador",
                Apellidos = "Inicial",
                TipoDocumento = "DNI",
                NumeroDocumento = "00000000",
                CreadoEn = DateTimeOffset.UtcNow,
                ActualizadoEn = DateTimeOffset.UtcNow,
            };
            ctx.Persona.Add(persona);
            await ctx.SaveChangesAsync();

            ctx.VinculoLaboral.Add(new VinculoLaboral
            {
                PersonaId = persona.Id,
                TipoVinculoId = tipoPlanilla.Id,
                FechaInicio = DateOnly.FromDateTime(DateTime.UtcNow),
                Estado = "ACTIVO",
                CreadoEn = DateTimeOffset.UtcNow,
            });

            var usuario = new UsuarioSistema
            {
                PersonaId = persona.Id,
                EmailLogin = request.Email,
                Estado = "ACTIVO",
                CreadoEn = DateTimeOffset.UtcNow,
            };
            usuario.PasswordHash = _passwordHasher.HashPassword(usuario, request.Password);
            ctx.UsuarioSistema.Add(usuario);
            await ctx.SaveChangesAsync();

            ctx.UsuarioAsignacion.Add(new UsuarioAsignacion
            {
                UsuarioSistemaId = usuario.Id,
                RolId = rolAdmin.Id,
                FechaInicio = DateOnly.FromDateTime(DateTime.UtcNow),
                CreadoEn = DateTimeOffset.UtcNow,
            });
            await ctx.SaveChangesAsync();

            return await Login(request);
        }

        public async Task SolicitarReset(LbSolicitarResetDto request)
        {
            using var ctx = _factory.CreateDbContext();

            var email = request.Email.Trim().ToLower();
            var usuario = await ctx.UsuarioSistema.FirstOrDefaultAsync(u => u.EmailLogin == email && u.Estado == "ACTIVO");
            // Silencioso a propósito si no existe/no está activo: este endpoint es público
            // (AllowAnonymous) y no debe servir para averiguar qué correos están registrados.
            if (usuario is null) return;

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
                ExpiraEn = DateTime.UtcNow.AddHours(2),
                Usado = false,
                CreadoEn = DateTime.UtcNow,
            });
            await ctx.SaveChangesAsync();

            var link = $"{_frontendSettings.LbSetPasswordUrl}?token={token}";
            var html = $@"<h2>Restablece tu contraseña</h2>
<p>Hola, recibimos una solicitud para restablecer tu contraseña en HP Constructores / Las Bravas.</p>
<p>Haz clic en el siguiente enlace para crear una nueva contraseña:</p>
<a href='{link}' style='background:#1E3A5F;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;display:inline-block;margin:16px 0'>Restablecer contraseña</a>
<p>Este enlace expira en 2 horas.</p>
<p>Si no solicitaste este cambio, ignora este correo.</p>";

            await _emailService.SendAsync(
                to: new List<string> { usuario.EmailLogin },
                subject: "Restablece tu contraseña - HP Constructores Generales",
                body: html,
                isHtml: true);
        }

        public async Task ResetPassword(LbResetPasswordDto request)
        {
            using var ctx = _factory.CreateDbContext();

            if (string.IsNullOrEmpty(request.NuevaPassword) || request.NuevaPassword.Length < 6)
                throw new AbrilException("La contraseña debe tener al menos 6 caracteres.", 400);

            var token = await ctx.LbUsuarioPasswordToken
                .FirstOrDefaultAsync(t => t.Token == request.Token && !t.Usado && t.ExpiraEn > DateTime.UtcNow)
                ?? throw new AbrilException("Enlace inválido o expirado.", 400);

            var usuario = await ctx.UsuarioSistema.FirstOrDefaultAsync(u => u.Id == token.UsuarioSistemaId)
                ?? throw new AbrilException("Usuario no encontrado.", 404);

            usuario.PasswordHash = _passwordHasher.HashPassword(usuario, request.NuevaPassword);
            token.Usado = true;

            await ctx.SaveChangesAsync();
        }

        /// <summary>Token opaco de un solo uso — 64 bytes aleatorios, base64url (sin +, /, =).</summary>
        private static string GenerarToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }
    }
}
