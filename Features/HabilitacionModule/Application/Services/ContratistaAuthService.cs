using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Auth;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Empresa;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Constants;
using Abril_Backend.Shared.Services.Contractors;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Abril_Backend.Features.Habilitacion.Application.Services
{
    public class ContratistaAuthService : IContratistaAuthService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ILogger<ContratistaAuthService> _logger;

        public ContratistaAuthService(
            IDbContextFactory<AppDbContext> factory,
            IConfiguration configuration,
            IEmailService emailService,
            ILogger<ContratistaAuthService> logger)
        {
            _factory = factory;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<ContratistaTokenDto> LoginAsync(ContratistaLoginDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var email = dto.Email.Trim().ToLower();

            var user = await ctx.User
                .FirstOrDefaultAsync(u => u.Email == email && u.Active && u.State);

            if (user is null || string.IsNullOrEmpty(user.Password))
                throw new AbrilException("Credenciales incorrectas.", 401);

            if (!VerificarPassword(user, dto.Password, user.Password))
                throw new AbrilException("Credenciales incorrectas.", 401);

            // La empresa del usuario titular se resuelve por contractor_user (un usuario
            // pertenece a UNA sola contratista). contractor_email es solo correos de contacto.
            var contractorUser = await ctx.ContractorUser
                .Include(cu => cu.Contractor)
                    .ThenInclude(c => c.Contributor)
                .Where(cu => cu.UserId == user.UserId && cu.Active && cu.State)
                .OrderBy(cu => cu.ContractorUserId)
                .FirstOrDefaultAsync();

            Contractor? contractor;
            Contributor? contributor;

            if (contractorUser is not null)
            {
                contractor  = contractorUser.Contractor;
                contributor = contractor.Contributor;
            }
            else
            {
                var contractorId = (await ctx.SsContratistaUsuarios
                    .FirstOrDefaultAsync(cu => cu.UserId == user.UserId && cu.Activo))
                    ?.ContractorId;

                if (contractorId == null)
                    throw new AbrilException("El usuario no tiene empresa contratista asociada.", 403);

                contractor = await ctx.Contractor
                    .Include(c => c.Contributor)
                    .FirstOrDefaultAsync(c => c.ContributorId == contractorId)
                    ?? throw new AbrilException("Empresa no encontrada.", 401);

                contributor = contractor.Contributor
                    ?? throw new AbrilException("Empresa no encontrada.", 401);
            }

            var allowedFeatures = await GetContratistasFeatureKeysAsync(ctx, user.UserId);
            var systemRoleIds   = await GetSystemRoleIdsAsync(ctx, user.UserId);

            var usuarioContratista = await ctx.SsContratistaUsuarios
                .Include(u => u.Proyectos)
                .FirstOrDefaultAsync(u => u.UserId == user.UserId
                                       && u.ContractorId == contractor.ContributorId
                                       && u.Activo);

            var scope = usuarioContratista?.Scope ?? "TODOS";
            var proyectoIds = usuarioContratista?.Proyectos?
                .Select(p => p.ProyectoId).ToList() ?? new List<int>();
            var modulos = usuarioContratista?.Modulos ?? "AMBOS";

            return GenerarTokenDto(user, contractor, contributor, allowedFeatures, systemRoleIds, scope, proyectoIds, modulos);
        }

        public async Task<List<EmpresaSimpleDto>> GetEmpresasParaLoginAsync()
        {
            using var ctx = _factory.CreateDbContext();

            return await (
                from c in ctx.Contributor
                join ct in ctx.Contractor on c.ContributorId equals ct.ContributorId
                where !c.EsAbril && c.Active && ct.Active && ct.State
                orderby c.ContributorName
                select new EmpresaSimpleDto
                {
                    Id = c.ContributorId,
                    RazonSocial = c.ContributorName,
                    NombreComercial = c.ContributorNombreComercial,
                    LogoUrl = ct.LogoFileUrl
                }
            ).ToListAsync();
        }

        public async Task SolicitarActivacionAsync(int empresaId)
        {
            using var ctx = _factory.CreateDbContext();

            var contributor = await ctx.Contributor.FirstOrDefaultAsync(c => c.ContributorId == empresaId)
                ?? throw new AbrilException("Empresa no encontrada.", 404);

            var contractor = await ctx.Contractor.FirstOrDefaultAsync(c => c.ContributorId == empresaId && c.Active)
                ?? throw new AbrilException("Empresa contratista no encontrada.", 404);

            var destinatario = await ctx.ContractorEmail
                .Where(ce => ce.ContractorId == contractor.ContractorId && ce.Active && ce.State)
                .OrderBy(ce => ce.ContractorEmailId)
                .Select(ce => ce.Email)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(destinatario))
                throw new AbrilException("La empresa no tiene email registrado.", 400);

            destinatario = destinatario.Trim().ToLower();

            var user = await ctx.User.FirstOrDefaultAsync(u => u.Email == destinatario)
                ?? throw new AbrilException("No existe un usuario registrado para este email.", 400);

            var tokensPrevios = await ctx.SsResetToken
                .Where(t => t.UserId == user.UserId && !t.Usado)
                .ToListAsync();
            foreach (var t in tokensPrevios) t.Usado = true;
            if (tokensPrevios.Count > 0) await ctx.SaveChangesAsync();

            var token = await CrearTokenAsync(ctx, user.UserId, TimeSpan.FromHours(48));

            var baseUrl = _configuration["FrontendSettings:SetPasswordUrl"];
            var link = $"{baseUrl}?token={token}&tipo=activacion-contratista";

            var html = $@"<h2>Bienvenido a Abril Grupo Inmobiliario</h2>
<p>Tu empresa <strong>{contributor.ContributorName}</strong> ha sido registrada.</p>
<p>Haz clic en el siguiente enlace para activar tu cuenta y crear tu contraseña:</p>
<a href='{link}' style='background:#64bc04;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;display:inline-block;margin:16px 0'>Activar mi cuenta</a>
<p>Este enlace expira en 48 horas.</p>
<p>Si no solicitaste este registro, ignora este correo.</p>";

            await _emailService.SendAsync(
                to: new List<string> { destinatario },
                subject: "Activa tu cuenta en Abril Grupo Inmobiliario",
                body: html,
                isHtml: true);
        }

        public async Task<ContratistaTokenDto> ActivarCuentaAsync(ActivarCuentaDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var token = await BuscarTokenVigenteAsync(ctx, dto.Token)
                ?? throw new AbrilException("Enlace inválido o expirado.", 400);

            if (string.IsNullOrEmpty(dto.Password) || dto.Password.Length < 6)
                throw new AbrilException("La contraseña debe tener al menos 6 caracteres.", 400);

            var user = await ctx.User.FirstOrDefaultAsync(u => u.UserId == token.UserId && u.Active && u.State)
                ?? throw new AbrilException("Usuario no encontrado.", 404);

            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            user.UpdatedDateTime = DateTime.UtcNow;
            token.Usado = true;

            await ctx.SaveChangesAsync();

            // Resolver la empresa por contractor_user; si el usuario aún no tiene vínculo
            // (caso migrado/huérfano), se ubica su empresa por su correo dentro de los
            // correos de contacto y se crea el vínculo definitivo en contractor_user.
            var vinculoActivar = await ctx.ContractorUser
                .Include(cu => cu.Contractor)
                    .ThenInclude(c => c.Contributor)
                .Where(cu => cu.UserId == user.UserId && cu.Active && cu.State)
                .OrderBy(cu => cu.ContractorUserId)
                .FirstOrDefaultAsync();

            Contractor contractorActivar;
            if (vinculoActivar is not null)
            {
                contractorActivar = vinculoActivar.Contractor;
            }
            else
            {
                var emailBuscar = user.Email!.Trim().ToLower();
                var contactoActivar = await ctx.ContractorEmail
                    .Include(ce => ce.Contractor)
                        .ThenInclude(c => c.Contributor)
                    .Where(ce => ce.Email.ToLower() == emailBuscar && ce.Active && ce.State)
                    .OrderBy(ce => ce.ContractorEmailId)
                    .FirstOrDefaultAsync()
                    ?? throw new AbrilException("El usuario no tiene empresa contratista asociada.", 403);

                contractorActivar = contactoActivar.Contractor;
                ctx.ContractorUser.Add(new ContractorUser
                {
                    ContractorId = contractorActivar.ContractorId,
                    UserId = user.UserId,
                    CreatedDateTime = DateTimeOffset.UtcNow,
                    Active = true,
                    State = true
                });
                await ctx.SaveChangesAsync();
            }

            // Paso 8 — Crear OWNER en ss_contratista_usuario si no existe
            var contractorIdActivar = contractorActivar.ContributorId;
            var rolOwnerActivar = await ctx.SsContratistaRoles
                .FirstOrDefaultAsync(r => r.Nombre == "OWNER");
            if (rolOwnerActivar != null)
            {
                var ownerExisteActivar = await ctx.SsContratistaUsuarios
                    .AnyAsync(cu => cu.ContractorId == contractorIdActivar
                                 && cu.RolId == rolOwnerActivar.Id);
                if (!ownerExisteActivar)
                {
                    ctx.SsContratistaUsuarios.Add(new SsContratistaUsuario
                    {
                        ContractorId = contractorIdActivar,
                        UserId = user.UserId,
                        RolId = rolOwnerActivar.Id,
                        Scope = "TODOS",
                        Activo = true,
                        CreadoEn = DateTime.UtcNow,
                        CreadoPor = null
                    });
                    await ctx.SaveChangesAsync();
                }
            }

            var usuarioContratistaActivar = await ctx.SsContratistaUsuarios
                .Include(u => u.Proyectos)
                .FirstOrDefaultAsync(u => u.UserId == user.UserId
                                       && u.ContractorId == contractorIdActivar
                                       && u.Activo);

            var scopeActivar = usuarioContratistaActivar?.Scope ?? "TODOS";
            var proyectoIdsActivar = usuarioContratistaActivar?.Proyectos?
                .Select(p => p.ProyectoId).ToList() ?? new List<int>();
            var modulosActivar = usuarioContratistaActivar?.Modulos ?? "AMBOS";

            var allowedFeatures = await GetContratistasFeatureKeysAsync(ctx, user.UserId);
            var systemRoleIds = await GetSystemRoleIdsAsync(ctx, user.UserId);

            return GenerarTokenDto(user, contractorActivar, contractorActivar.Contributor, allowedFeatures, systemRoleIds, scopeActivar, proyectoIdsActivar, modulosActivar);
        }

        public async Task SolicitarResetPasswordAsync(SolicitarResetDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var email = dto.Email.Trim().ToLower();
            var user = await ctx.User.FirstOrDefaultAsync(u => u.Email == email && u.Active && u.State);
            if (user is null) return;

            var esContratista =
                await ctx.ContractorUser.AnyAsync(cu => cu.UserId == user.UserId && cu.Active && cu.State)
                || await ctx.SsContratistaUsuarios.AnyAsync(s => s.UserId == user.UserId && s.Activo);
            if (!esContratista) return;

            var tokensPrevios = await ctx.SsResetToken
                .Where(t => t.UserId == user.UserId && !t.Usado)
                .ToListAsync();
            foreach (var t in tokensPrevios) t.Usado = true;
            if (tokensPrevios.Count > 0) await ctx.SaveChangesAsync();

            var token = await CrearTokenAsync(ctx, user.UserId, TimeSpan.FromHours(2));

            var baseUrl = _configuration["FrontendSettings:SetPasswordUrl"];
            var link = $"{baseUrl}?token={token}&tipo=reset-contratista";

            var html = $@"<h2>Restablece tu contraseña</h2>
<p>Hola, recibimos una solicitud para restablecer tu contraseña.</p>
<p>Haz clic en el siguiente enlace para crear una nueva contraseña:</p>
<a href='{link}' style='background:#64bc04;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;display:inline-block;margin:16px 0'>Restablecer contraseña</a>
<p>Este enlace expira en 2 horas.</p>
<p>Si no solicitaste este cambio, ignora este correo.</p>";

            await _emailService.SendAsync(
                to: new List<string> { email },
                subject: "Restablece tu contraseña - Abril Grupo Inmobiliario",
                body: html,
                isHtml: true);
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var token = await BuscarTokenVigenteAsync(ctx, dto.Token)
                ?? throw new AbrilException("Enlace inválido o expirado.", 400);

            if (string.IsNullOrEmpty(dto.NuevaPassword) || dto.NuevaPassword.Length < 6)
                throw new AbrilException("La contraseña debe tener al menos 6 caracteres.", 400);

            var user = await ctx.User.FirstOrDefaultAsync(u => u.UserId == token.UserId)
                ?? throw new AbrilException("Usuario no encontrado.", 404);

            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);
            user.UpdatedDateTime = DateTime.UtcNow;
            token.Usado = true;

            await ctx.SaveChangesAsync();
        }

        public async Task CambiarPasswordAsync(int userId, CambiarPasswordDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var user = await ctx.User.FirstOrDefaultAsync(u => u.UserId == userId)
                ?? throw new AbrilException("Usuario no encontrado.", 404);

            if (string.IsNullOrEmpty(user.Password) || !VerificarPassword(user, dto.PasswordActual, user.Password))
                throw new AbrilException("Contraseña actual incorrecta.", 400);

            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.PasswordNuevo);
            user.UpdatedDateTime = DateTime.UtcNow;

            await ctx.SaveChangesAsync();
        }

        public async Task<ValidarMigracionResultDto> ValidarMigracionAsync(ValidarMigracionDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var contributor = await ctx.Contributor
                .FirstOrDefaultAsync(c => c.ContributorRuc == dto.Ruc
                    && c.SpPasswordTemp == dto.SpPassword
                    && c.Active);

            if (contributor is null)
                throw new AbrilException("RUC o contraseña temporal incorrectos.", 401);

            return new ValidarMigracionResultDto
            {
                NombreComercial = contributor.ContributorNombreComercial ?? contributor.ContributorName,
                RazonSocial = contributor.ContributorName
            };
        }

        public async Task ActivarMigracionAsync(ActivarMigracionDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var contributor = await ctx.Contributor
                .FirstOrDefaultAsync(c => c.ContributorRuc == dto.Ruc
                    && c.SpPasswordTemp == dto.SpPassword
                    && c.Active)
                ?? throw new AbrilException("RUC o contraseña temporal incorrectos.", 401);

            var contractor = await ctx.Contractor
                .FirstOrDefaultAsync(c => c.ContributorId == contributor.ContributorId && c.Active)
                ?? throw new AbrilException("No se encontró empresa contratista para este RUC.", 404);

            // El correo elegido será el usuario titular de ESTA contratista: si ya pertenece
            // a otra empresa o a un usuario interno, se rechaza (regla un usuario = una contratista).
            var emailNormalizado = dto.Email.Trim().ToLower();
            var existingUser = await ContractorAccountEmailPolicy.ValidateAndGetUserAsync(
                ctx, emailNormalizado, contractor.ContractorId, contributor.ContributorId);
            User user;

            if (existingUser != null)
            {
                user = existingUser;
                user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                user.UpdatedDateTime = DateTime.UtcNow;
            }
            else
            {
                user = new User
                {
                    Email = emailNormalizado,
                    EmailConfirmed = true,
                    Active = true,
                    State = true,
                    CreatedDateTime = DateTime.UtcNow
                };
                user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                ctx.User.Add(user);
            }

            await ctx.SaveChangesAsync();

            var contractorUserExists = await ctx.ContractorUser
                .AnyAsync(cu => cu.ContractorId == contractor.ContractorId && cu.UserId == user.UserId && cu.Active);
            if (!contractorUserExists)
                ctx.ContractorUser.Add(new ContractorUser
                {
                    ContractorId = contractor.ContractorId,
                    UserId = user.UserId,
                    CreatedDateTime = DateTimeOffset.UtcNow,
                    Active = true,
                    State = true
                });

            var roleExists = await ctx.UserRole
                .AnyAsync(ur => ur.UserId == user.UserId && ur.RoleId == 11 && ur.Active);
            if (!roleExists)
                ctx.UserRole.Add(new UserRole
                {
                    UserId = user.UserId,
                    RoleId = 11,
                    CreatedDateTime = DateTime.UtcNow,
                    CreatedUserId = user.UserId,
                    Active = true,
                    State = true
                });

            contributor.SpPasswordTemp = null;

            await ctx.SaveChangesAsync();

            // Paso 8 — Crear OWNER en ss_contratista_usuario si no existe
            var rolOwner = await ctx.SsContratistaRoles
                .FirstOrDefaultAsync(r => r.Nombre == "OWNER");
            if (rolOwner != null)
            {
                var ownerExiste = await ctx.SsContratistaUsuarios
                    .AnyAsync(cu => cu.ContractorId == contractor.ContributorId
                                 && cu.RolId == rolOwner.Id);
                if (!ownerExiste)
                {
                    ctx.SsContratistaUsuarios.Add(new SsContratistaUsuario
                    {
                        ContractorId = contractor.ContributorId,
                        UserId = user.UserId,
                        RolId = rolOwner.Id,
                        Scope = "TODOS",
                        Activo = true,
                        CreadoEn = DateTime.UtcNow,
                        CreadoPor = null
                    });
                    await ctx.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// Verifica la contraseña soportando hashes BCrypt ($2a$/$2b$/$2y$) y
        /// ASP.NET Identity (PBKDF2). Si un esquema falla, intenta con el otro,
        /// para convivir con contraseñas almacenadas en ambos formatos.
        /// </summary>
        private bool VerificarPassword(User user, string plainPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(plainPassword))
                return false;

            // 1) BCrypt: sus hashes empiezan con $2a$, $2b$ o $2y$.
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

            // 2) Fallback a ASP.NET Identity (PBKDF2).
            try
            {
                var hasher = new PasswordHasher<User>();
                var resultado = hasher.VerifyHashedPassword(user, storedHash, plainPassword);
                if (resultado != PasswordVerificationResult.Failed)
                    return true;
            }
            catch (FormatException)
            {
                // El hash no tiene formato Identity tampoco.
                _logger.LogWarning("Hash de password no reconocido para el usuario {UserId}.", user.UserId);
            }

            return false;
        }

        private static async Task<string> CrearTokenAsync(AppDbContext ctx, int userId, TimeSpan duracion)
        {
            var raw = Guid.NewGuid().ToString("N");
            ctx.SsResetToken.Add(new SsResetToken
            {
                UserId = userId,
                Token = raw,
                ExpiraAt = DateTime.UtcNow.Add(duracion),
                Usado = false,
                CreatedAt = DateTime.UtcNow
            });
            await ctx.SaveChangesAsync();
            return raw;
        }

        private static Task<SsResetToken?> BuscarTokenVigenteAsync(AppDbContext ctx, string token)
            => ctx.SsResetToken.FirstOrDefaultAsync(t =>
                t.Token == token && !t.Usado && t.ExpiraAt > DateTime.UtcNow);

        private static Task<List<string>> GetContratistasFeatureKeysAsync(AppDbContext ctx, int userId)
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

        private static Task<List<int>> GetSystemRoleIdsAsync(AppDbContext ctx, int userId)
            => ctx.UserRole
                .Where(ur => ur.UserId == userId && ur.Active && ur.State)
                .Select(ur => ur.RoleId)
                .ToListAsync();

        private ContratistaTokenDto GenerarTokenDto(User user, Contractor contractor, Contributor contributor, List<string> allowedFeatures, List<int> systemRoleIds, string scope, List<int> proyectoIds, string modulos)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, contributor.ContributorName),
                new Claim(ClaimTypes.Role, Roles.Contratista),
                new Claim("role_name", "CONTRATISTA"),
                new Claim("empresaId", contractor.ContributorId.ToString()),
                new Claim("tipo", "CONTRATISTA"),
                new Claim("systemRoles", string.Join(",", systemRoleIds)),
                new Claim("scope", scope),
                new Claim("proyectoIds", string.Join(",", proyectoIds)),
                new Claim("modulos", modulos),
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

            return new ContratistaTokenDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                EmpresaId = contractor.ContributorId,
                RazonSocial = contributor.ContributorName,
                Tipo = "CONTRATISTA",
                AllowedFeatures = allowedFeatures,
                Scope = scope,
                ProyectoIds = proyectoIds,
                Modulos = modulos
            };
        }
    }
}
