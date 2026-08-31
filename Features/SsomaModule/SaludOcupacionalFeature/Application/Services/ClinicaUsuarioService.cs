using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.ClinicaUsuarios;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    public class ClinicaUsuarioService : IClinicaUsuarioService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public ClinicaUsuarioService(
            IDbContextFactory<AppDbContext> factory,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _factory = factory;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<PagedResult<ClinicaUsuarioListDto>> GetUsuariosByClinicaAsync(int clinicaId, int page, int pageSize)
        {
            using var ctx = _factory.CreateDbContext();
            var q = ctx.SsClinicaUsuario.Where(u => u.ClinicaId == clinicaId);
            var total = await q.CountAsync();
            var items = await q
                .OrderBy(u => u.Nombre)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new ClinicaUsuarioListDto
                {
                    ClinicaUsuarioId = u.ClinicaUsuarioId,
                    Nombre = u.Nombre,
                    Email = u.Email,
                    Activo = u.Activo,
                    UltimoAcceso = u.UltimoAcceso,
                    CreadoEn = u.CreadoEn,
                    TienePassword = u.PasswordHash != "PENDIENTE_RESET"
                })
                .ToListAsync();
            return new PagedResult<ClinicaUsuarioListDto> { Items = items, Total = total, Page = page, PageSize = pageSize };
        }

        public async Task<ClinicaUsuarioDto> GetUsuarioByIdAsync(int clinicaId, int usuarioId)
        {
            using var ctx = _factory.CreateDbContext();
            var u = await ctx.SsClinicaUsuario
                .FirstOrDefaultAsync(u => u.ClinicaId == clinicaId && u.ClinicaUsuarioId == usuarioId)
                ?? throw new AbrilException("Usuario no encontrado.", 404);
            return MapToDto(u);
        }

        public async Task<ClinicaUsuarioDto> CreateUsuarioAsync(int clinicaId, ClinicaUsuarioCreateDto dto, int? creadoPor, string? ip)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new AbrilException("El nombre es obligatorio.", 400);
            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new AbrilException("El email es obligatorio.", 400);

            using var ctx = _factory.CreateDbContext();

            var clinicaExiste = await ctx.SsClinica.AnyAsync(c => c.Id == clinicaId);
            if (!clinicaExiste)
                throw new AbrilException("Clínica no encontrada.", 404);

            var emailNormalizado = dto.Email.Trim().ToLower();
            var emailEnUso = await ctx.SsClinicaUsuario.AnyAsync(u => u.Email == emailNormalizado);
            if (emailEnUso)
                throw new AbrilException("Ya existe un usuario con ese correo.", 409);

            var usuario = new SsClinicaUsuario
            {
                ClinicaId = clinicaId,
                Nombre = dto.Nombre.Trim(),
                Email = emailNormalizado,
                PasswordHash = "PENDIENTE_RESET",
                Activo = true,
                CreadoEn = DateTime.UtcNow,
                CreadoPor = creadoPor
            };
            ctx.SsClinicaUsuario.Add(usuario);
            await ctx.SaveChangesAsync();

            var tokenRaw = await GenerarYGuardarTokenAsync(ctx, usuario.ClinicaUsuarioId, "ACTIVACION", ip);
            await EnviarCorreoActivacionAsync(usuario, tokenRaw);

            ctx.SsClinicaAuditoria.Add(new SsClinicaAuditoria
            {
                Accion = "ACTIVACION_ENVIADA",
                ClinicaUsuarioId = usuario.ClinicaUsuarioId,
                ClinicaId = clinicaId,
                IpOrigen = ip,
                RealizadoEn = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();

            return MapToDto(usuario);
        }

        public async Task<ClinicaUsuarioDto> UpdateUsuarioAsync(int clinicaId, int usuarioId, ClinicaUsuarioUpdateDto dto, int? modificadoPor)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new AbrilException("El nombre es obligatorio.", 400);
            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new AbrilException("El email es obligatorio.", 400);

            using var ctx = _factory.CreateDbContext();
            var usuario = await ctx.SsClinicaUsuario
                .FirstOrDefaultAsync(u => u.ClinicaId == clinicaId && u.ClinicaUsuarioId == usuarioId)
                ?? throw new AbrilException("Usuario no encontrado.", 404);

            var emailNormalizado = dto.Email.Trim().ToLower();
            var emailEnUso = await ctx.SsClinicaUsuario
                .AnyAsync(u => u.Email == emailNormalizado && u.ClinicaUsuarioId != usuarioId);
            if (emailEnUso)
                throw new AbrilException("Ya existe un usuario con ese correo.", 409);

            usuario.Nombre = dto.Nombre.Trim();
            usuario.Email = emailNormalizado;
            usuario.ModificadoEn = DateTime.UtcNow;
            usuario.ModificadoPor = modificadoPor;

            await ctx.SaveChangesAsync();
            return MapToDto(usuario);
        }

        public async Task ToggleActivoAsync(int clinicaId, int usuarioId, int? modificadoPor, string? ip)
        {
            using var ctx = _factory.CreateDbContext();
            var usuario = await ctx.SsClinicaUsuario
                .FirstOrDefaultAsync(u => u.ClinicaId == clinicaId && u.ClinicaUsuarioId == usuarioId)
                ?? throw new AbrilException("Usuario no encontrado.", 404);

            usuario.Activo = !usuario.Activo;
            usuario.ModificadoEn = DateTime.UtcNow;
            usuario.ModificadoPor = modificadoPor;

            if (!usuario.Activo)
            {
                usuario.DesactivadoEn = DateTime.UtcNow;
                usuario.DesactivadoPor = modificadoPor;
            }

            ctx.SsClinicaAuditoria.Add(new SsClinicaAuditoria
            {
                Accion = usuario.Activo ? "ACTIVACION" : "DESACTIVACION",
                ClinicaUsuarioId = usuario.ClinicaUsuarioId,
                ClinicaId = clinicaId,
                IpOrigen = ip,
                RealizadoEn = DateTime.UtcNow
            });

            await ctx.SaveChangesAsync();
        }

        public async Task SoftDeleteAsync(int clinicaId, int usuarioId, string? ip)
        {
            using var ctx = _factory.CreateDbContext();
            var usuario = await ctx.SsClinicaUsuario
                .FirstOrDefaultAsync(u => u.ClinicaId == clinicaId && u.ClinicaUsuarioId == usuarioId)
                ?? throw new AbrilException("Usuario no encontrado.", 404);

            usuario.Activo = false;
            usuario.DesactivadoEn = DateTime.UtcNow;

            ctx.SsClinicaAuditoria.Add(new SsClinicaAuditoria
            {
                Accion = "DESACTIVACION",
                ClinicaUsuarioId = usuario.ClinicaUsuarioId,
                ClinicaId = clinicaId,
                IpOrigen = ip,
                RealizadoEn = DateTime.UtcNow
            });

            await ctx.SaveChangesAsync();
        }

        public async Task ReenviarActivacionAsync(int clinicaId, int usuarioId, string? ip)
        {
            using var ctx = _factory.CreateDbContext();
            var usuario = await ctx.SsClinicaUsuario
                .FirstOrDefaultAsync(u => u.ClinicaId == clinicaId && u.ClinicaUsuarioId == usuarioId)
                ?? throw new AbrilException("Usuario no encontrado.", 404);

            if (!usuario.Activo)
                throw new AbrilException("El usuario está desactivado.", 400);

            var tokenRaw = await GenerarYGuardarTokenAsync(ctx, usuario.ClinicaUsuarioId, "ACTIVACION", ip);
            await EnviarCorreoActivacionAsync(usuario, tokenRaw);

            ctx.SsClinicaAuditoria.Add(new SsClinicaAuditoria
            {
                Accion = "ACTIVACION_ENVIADA",
                ClinicaUsuarioId = usuario.ClinicaUsuarioId,
                ClinicaId = clinicaId,
                IpOrigen = ip,
                RealizadoEn = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
        }

        public async Task RegistrarAuditoriaAsync(string accion, int? clinicaUsuarioId, int? clinicaId, string? ip, string? userAgent, string? detalle)
        {
            try
            {
                using var ctx = _factory.CreateDbContext();
                ctx.SsClinicaAuditoria.Add(new SsClinicaAuditoria
                {
                    Accion = accion,
                    ClinicaUsuarioId = clinicaUsuarioId,
                    ClinicaId = clinicaId,
                    IpOrigen = ip,
                    UserAgent = userAgent,
                    DetalleAdicional = detalle,
                    RealizadoEn = DateTime.UtcNow
                });
                await ctx.SaveChangesAsync();
            }
            catch { /* las fallas de auditoría no deben cortar el flujo principal */ }
        }

        private async Task<string> GenerarYGuardarTokenAsync(AppDbContext ctx, int clinicaUsuarioId, string tipo, string? ip)
        {
            var anteriores = await ctx.SsClinicaToken
                .Where(t => t.ClinicaUsuarioId == clinicaUsuarioId && t.Tipo == tipo && t.UsadoEn == null)
                .ToListAsync();
            foreach (var t in anteriores)
                t.UsadoEn = DateTime.UtcNow;

            var tokenRaw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            ctx.SsClinicaToken.Add(new SsClinicaToken
            {
                ClinicaUsuarioId = clinicaUsuarioId,
                Token = tokenRaw,
                Tipo = tipo,
                Expiracion = DateTime.UtcNow.AddHours(48),
                CreadoEn = DateTime.UtcNow,
                IpSolicitud = ip
            });
            await ctx.SaveChangesAsync();
            return tokenRaw;
        }

        private async Task EnviarCorreoActivacionAsync(SsClinicaUsuario usuario, string token)
        {
            var frontendUrl = _configuration["App:FrontendUrl"]?.TrimEnd('/');
            var link = $"{frontendUrl}/clinica/activar?token={Uri.EscapeDataString(token)}";
            var html = $@"<h2>Bienvenido a Abril Grupo Inmobiliario</h2>
<p>Hola <strong>{usuario.Nombre}</strong>, tu cuenta de clínica ha sido creada.</p>
<p>Haz clic en el siguiente enlace para establecer tu contraseña:</p>
<a href='{link}' style='background:#64bc04;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;display:inline-block;margin:16px 0'>Activar cuenta</a>
<p>Este enlace expira en 48 horas.</p>
<p>Si no solicitaste este acceso, ignora este correo.</p>";

            await _emailService.SendAsync(
                to: new List<string> { usuario.Email },
                subject: "Activa tu cuenta de clínica en Abril Grupo Inmobiliario",
                body: html,
                isHtml: true);
        }

        private static ClinicaUsuarioDto MapToDto(SsClinicaUsuario u) => new()
        {
            ClinicaUsuarioId = u.ClinicaUsuarioId,
            ClinicaId = u.ClinicaId,
            Nombre = u.Nombre,
            Email = u.Email,
            Activo = u.Activo,
            UltimoAcceso = u.UltimoAcceso,
            CreadoEn = u.CreadoEn,
            CreadoPor = u.CreadoPor,
            ModificadoEn = u.ModificadoEn,
            ModificadoPor = u.ModificadoPor,
            TienePassword = u.PasswordHash != "PENDIENTE_RESET"
        };
    }
}
