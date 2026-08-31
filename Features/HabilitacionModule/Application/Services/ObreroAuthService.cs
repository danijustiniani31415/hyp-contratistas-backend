using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Auth;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Abril_Backend.Features.Habilitacion.Application.Services
{
    public class ObreroAuthService : IObreroAuthService
    {
        private const string RolObrero = "OBRERO";

        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ObreroAuthService> _logger;

        public ObreroAuthService(
            IDbContextFactory<AppDbContext> factory,
            IConfiguration configuration,
            ILogger<ObreroAuthService> logger)
        {
            _factory = factory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ObreroTokenDto> LoginAsync(ObreroLoginDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var dni = dto.Dni.Trim();

            var person = await ctx.Person
                .FirstOrDefaultAsync(p => p.DocumentIdentityCode == dni && p.Active);

            if (person?.UserId is null)
                throw new AbrilException("Credenciales incorrectas.", 401);

            var user = await ctx.User
                .FirstOrDefaultAsync(u => u.UserId == person.UserId && u.Active && u.State);

            if (user is null || string.IsNullOrEmpty(user.Password) || !VerificarPassword(user, dto.Password, user.Password))
                throw new AbrilException("Credenciales incorrectas.", 401);

            var worker = await ctx.Worker.FirstOrDefaultAsync(w => w.PersonId == person.PersonId)
                ?? throw new AbrilException("Este usuario no tiene un trabajador asociado.", 403);

            var rolObreroAsignado = await ctx.UserRole
                .Join(ctx.Role, ur => ur.RoleId, r => r.RoleId, (ur, r) => new { ur, r })
                .Where(x => x.ur.UserId == user.UserId && x.ur.Active && x.r.RoleDescription == RolObrero)
                .Select(x => x.r.RoleId)
                .FirstOrDefaultAsync();
            if (rolObreroAsignado == 0)
                throw new AbrilException("Este usuario no tiene acceso como obrero.", 403);

            var allowedFeatures = await GetFeatureKeysAsync(ctx, user.UserId);
            var nombre = worker.ApellidoNombre ?? person.FullName ?? dni;

            return GenerarTokenDto(user, worker.Id, nombre, rolObreroAsignado, allowedFeatures);
        }

        public async Task SetPasswordAsync(ObreroSetPasswordDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            if (string.IsNullOrEmpty(dto.Password) || dto.Password.Length < 4)
                throw new AbrilException("La contraseña debe tener al menos 4 caracteres.", 400);

            var worker = await ctx.Worker.Include(w => w.Person)
                .FirstOrDefaultAsync(w => w.Id == dto.WorkerId)
                ?? throw new AbrilException("Trabajador no encontrado.", 404);

            var person = worker.Person
                ?? throw new AbrilException("El trabajador no tiene una persona asociada.", 400);

            var rolObrero = await ctx.Role.FirstOrDefaultAsync(r => r.RoleDescription == RolObrero && r.Active)
                ?? throw new AbrilException("El rol OBRERO no existe. Contactar al administrador del sistema.", 500);

            User user;
            if (person.UserId is int existingUserId)
            {
                user = await ctx.User.FirstOrDefaultAsync(u => u.UserId == existingUserId)
                    ?? throw new AbrilException("Usuario no encontrado.", 404);
                user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                user.Active = true;
                user.State = true;
                user.UpdatedDateTime = DateTime.UtcNow;
            }
            else
            {
                user = new User
                {
                    Email = $"obrero-{person.DocumentIdentityCode}@abril.pe.interno",
                    Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    EmailConfirmed = false,
                    Active = true,
                    State = true,
                    CreatedDateTime = DateTime.UtcNow,
                };
                ctx.User.Add(user);
                await ctx.SaveChangesAsync();

                person.UserId = user.UserId;
            }

            var yaTieneRol = await ctx.UserRole.AnyAsync(ur => ur.UserId == user.UserId && ur.RoleId == rolObrero.RoleId && ur.Active);
            if (!yaTieneRol)
            {
                ctx.UserRole.Add(new UserRole
                {
                    UserId = user.UserId,
                    RoleId = rolObrero.RoleId,
                    CreatedDateTime = DateTime.UtcNow,
                    CreatedUserId = user.UserId,
                    Active = true,
                    State = true,
                });
            }

            await ctx.SaveChangesAsync();
        }

        public async Task CambiarPasswordAsync(int userId, ObreroCambiarPasswordDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var user = await ctx.User.FirstOrDefaultAsync(u => u.UserId == userId)
                ?? throw new AbrilException("Usuario no encontrado.", 404);

            if (string.IsNullOrEmpty(user.Password) || !VerificarPassword(user, dto.PasswordActual, user.Password))
                throw new AbrilException("Contraseña actual incorrecta.", 400);

            if (string.IsNullOrEmpty(dto.PasswordNuevo) || dto.PasswordNuevo.Length < 4)
                throw new AbrilException("La nueva contraseña debe tener al menos 4 caracteres.", 400);

            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.PasswordNuevo);
            user.UpdatedDateTime = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }

        private static Task<List<string>> GetFeatureKeysAsync(AppDbContext ctx, int userId)
            => ctx.Database.SqlQuery<string>($"""
                SELECT DISTINCT f.feature_key
                FROM feature f
                JOIN role_feature rf ON rf.feature_id = f.feature_id
                JOIN user_role ur ON ur.role_id = rf.role_id
                WHERE ur.user_id = {userId}
                  AND ur.active = true
                  AND ur.state = true
                """)
                .ToListAsync();

        /// <summary>Mismo esquema de verificación (BCrypt con fallback a Identity/PBKDF2) que ContratistaAuthService.</summary>
        private bool VerificarPassword(User user, string plainPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(plainPassword))
                return false;

            if (storedHash.StartsWith("$2"))
            {
                try
                {
                    if (BCrypt.Net.BCrypt.Verify(plainPassword, storedHash))
                        return true;
                }
                catch (BCrypt.Net.SaltParseException)
                {
                    _logger.LogWarning("Hash BCrypt inválido para el usuario {UserId}.", user.UserId);
                }
            }

            try
            {
                var hasher = new PasswordHasher<User>();
                var resultado = hasher.VerifyHashedPassword(user, storedHash, plainPassword);
                if (resultado != PasswordVerificationResult.Failed)
                    return true;
            }
            catch (FormatException)
            {
                _logger.LogWarning("Hash de password no reconocido para el usuario {UserId}.", user.UserId);
            }

            return false;
        }

        private ObreroTokenDto GenerarTokenDto(User user, int workerId, string nombre, int roleId, List<string> allowedFeatures)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, nombre),
                new Claim(ClaimTypes.Role, roleId.ToString()),
                new Claim("role_name", RolObrero),
                new Claim("tipo", RolObrero),
                new Claim("workerId", workerId.ToString()),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: creds);

            return new ObreroTokenDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                WorkerId = workerId,
                Nombre = nombre,
                Tipo = RolObrero,
                AllowedFeatures = allowedFeatures,
            };
        }
    }
}
