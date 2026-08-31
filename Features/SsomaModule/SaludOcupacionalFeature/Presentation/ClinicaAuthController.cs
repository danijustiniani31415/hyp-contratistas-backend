using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Auth;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Presentation
{
    [ApiController]
    [Route("api/v1/ssoma/salud-ocupacional/auth")]
    [AllowAnonymous]
    public class ClinicaAuthController : ControllerBase
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IClinicaUsuarioService _usuarioService;
        private readonly ILogger<ClinicaAuthController> _logger;

        public ClinicaAuthController(
            IDbContextFactory<AppDbContext> factory,
            IConfiguration configuration,
            IEmailService emailService,
            IClinicaUsuarioService usuarioService,
            ILogger<ClinicaAuthController> logger)
        {
            _factory = factory;
            _configuration = configuration;
            _emailService = emailService;
            _usuarioService = usuarioService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] ClinicaLoginDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            try
            {
                using var ctx = _factory.CreateDbContext();

                var email = dto.Email.Trim().ToLower();
                var usuario = await ctx.SsClinicaUsuario
                    .FirstOrDefaultAsync(u => u.Email == email && u.Activo);

                if (usuario is null)
                {
                    await _usuarioService.RegistrarAuditoriaAsync("LOGIN_FALLIDO", null, null, ip, userAgent,
                        $"{{\"email\":\"{dto.Email.Trim()}\"}}");
                    throw new AbrilException("Credenciales inválidas.", 401);
                }

                if (usuario.PasswordHash == "PENDIENTE_RESET")
                {
                    await _usuarioService.RegistrarAuditoriaAsync("LOGIN_FALLIDO", usuario.ClinicaUsuarioId, usuario.ClinicaId, ip, userAgent,
                        "{\"motivo\":\"cuenta_sin_activar\"}");
                    throw new AbrilException("Credenciales inválidas.", 401);
                }

                if (!BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
                {
                    await _usuarioService.RegistrarAuditoriaAsync("LOGIN_FALLIDO", usuario.ClinicaUsuarioId, usuario.ClinicaId, ip, userAgent,
                        "{\"motivo\":\"password_incorrecto\"}");
                    throw new AbrilException("Credenciales inválidas.", 401);
                }

                usuario.UltimoAcceso = DateTime.UtcNow;
                await ctx.SaveChangesAsync();

                await _usuarioService.RegistrarAuditoriaAsync("LOGIN", usuario.ClinicaUsuarioId, usuario.ClinicaId, ip, userAgent, null);

                return Ok(GenerarTokenDto(usuario));
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ClinicaAuthController.Login"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("solicitar-activacion")]
        public async Task<IActionResult> SolicitarActivacion([FromBody] ClinicaEmailDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            try
            {
                using var ctx = _factory.CreateDbContext();

                var email = dto.Email.Trim().ToLower();
                var usuario = await ctx.SsClinicaUsuario
                    .FirstOrDefaultAsync(u => u.Email == email && u.Activo);

                if (usuario is null)
                    throw new AbrilException("Correo no encontrado.", 404);

                var anteriores = await ctx.SsClinicaToken
                    .Where(t => t.ClinicaUsuarioId == usuario.ClinicaUsuarioId && t.Tipo == "ACTIVACION" && t.UsadoEn == null)
                    .ToListAsync();
                foreach (var t in anteriores)
                    t.UsadoEn = DateTime.UtcNow;

                var tokenRaw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                ctx.SsClinicaToken.Add(new SsClinicaToken
                {
                    ClinicaUsuarioId = usuario.ClinicaUsuarioId,
                    Token = tokenRaw,
                    Tipo = "ACTIVACION",
                    Expiracion = DateTime.UtcNow.AddHours(48),
                    CreadoEn = DateTime.UtcNow,
                    IpSolicitud = ip
                });
                await ctx.SaveChangesAsync();

                var frontendUrl = _configuration["App:FrontendUrl"]?.TrimEnd('/');
                var link = $"{frontendUrl}/clinica/activar?token={Uri.EscapeDataString(tokenRaw)}";

                var html = $@"<h2>Bienvenido a Abril Grupo Inmobiliario</h2>
<p>Hola <strong>{usuario.Nombre}</strong>, solicitaste activar tu cuenta.</p>
<p>Haz clic en el siguiente enlace para establecer tu contraseña:</p>
<a href='{link}' style='background:#64bc04;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;display:inline-block;margin:16px 0'>Activar cuenta</a>
<p>Este enlace expira en 48 horas.</p>
<p>Si no solicitaste este acceso, ignora este correo.</p>";

                await _emailService.SendAsync(
                    to: new List<string> { usuario.Email },
                    subject: "Activa tu cuenta de clínica en Abril Grupo Inmobiliario",
                    body: html,
                    isHtml: true);

                await _usuarioService.RegistrarAuditoriaAsync("ACTIVACION_ENVIADA", usuario.ClinicaUsuarioId, usuario.ClinicaId, ip, null, null);

                return Ok(new { message = "Se envió el enlace de activación al correo registrado." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ClinicaAuthController.SolicitarActivacion"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        [HttpPost("activar")]
        public async Task<IActionResult> Activar([FromBody] ClinicaActivarDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            try
            {
                using var ctx = _factory.CreateDbContext();

                dto.Token = dto.Token?.Replace(" ", "+");
                var tokenReg = await ctx.SsClinicaToken
                    .FirstOrDefaultAsync(t => t.Token == dto.Token && t.Tipo == "ACTIVACION" && t.UsadoEn == null && t.Expiracion > DateTime.UtcNow);

                if (tokenReg is null)
                    throw new AbrilException("Token inválido o expirado.", 400);

                var usuario = await ctx.SsClinicaUsuario
                    .FirstOrDefaultAsync(u => u.ClinicaUsuarioId == tokenReg.ClinicaUsuarioId)
                    ?? throw new AbrilException("Usuario no encontrado.", 404);

                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                tokenReg.UsadoEn = DateTime.UtcNow;

                ctx.SsClinicaAuditoria.Add(new SsClinicaAuditoria
                {
                    Accion = "ACTIVACION",
                    ClinicaUsuarioId = usuario.ClinicaUsuarioId,
                    ClinicaId = usuario.ClinicaId,
                    IpOrigen = ip,
                    RealizadoEn = DateTime.UtcNow
                });

                await ctx.SaveChangesAsync();

                return Ok(new { message = "Contraseña establecida correctamente." });
            }
            catch (AbrilException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (Exception ex) { _logger.LogError(ex, "Error en ClinicaAuthController.Activar"); return StatusCode(500, new { message = "Error del servidor. Por favor contactar al administrador del sistema." }); }
        }

        private ClinicaTokenDto GenerarTokenDto(SsClinicaUsuario usuario)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.ClinicaUsuarioId.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Role, Roles.Clinica),
                new Claim("role_name", "CLINICA"),
                new Claim("clinicaUsuarioId", usuario.ClinicaUsuarioId.ToString()),
                new Claim("clinicaId", usuario.ClinicaId.ToString()),
                new Claim("email", usuario.Email),
                new Claim("tipo", "CLINICA"),
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            return new ClinicaTokenDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ClinicaUsuarioId = usuario.ClinicaUsuarioId,
                ClinicaId = usuario.ClinicaId,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Tipo = "CLINICA"
            };
        }
    }
}
