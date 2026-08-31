using System.Security.Cryptography;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.AuthModule.UserFeature.Application.Dtos;
using Abril_Backend.Features.AuthModule.UserFeature.Application.Interfaces;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Realtime;
using Abril_Backend.Shared.Services.Graph.Interfaces;
using Microsoft.Extensions.Options;

namespace Abril_Backend.Features.AuthModule.UserFeature.Application.Services
{
    public class UserFeatureService : IUserFeatureService
    {
        private readonly IUserFeatureRepository _repo;
        private readonly IUserPasswordTokenRepository _tokenRepo;
        private readonly IEmailService _emailService;
        private readonly FrontendSettings _frontendSettings;
        private readonly IRealtimeNotifier _notifier;
        private readonly IGraphUserService _graphUserService;

        public UserFeatureService(
            IUserFeatureRepository repo,
            IUserPasswordTokenRepository tokenRepo,
            IEmailService emailService,
            IOptions<FrontendSettings> frontendSettings,
            IRealtimeNotifier notifier,
            IGraphUserService graphUserService)
        {
            _repo = repo;
            _tokenRepo = tokenRepo;
            _emailService = emailService;
            _frontendSettings = frontendSettings.Value;
            _notifier = notifier;
            _graphUserService = graphUserService;
        }

        public Task<PagedResult<UserListItemDto>> GetPaged(int page, int pageSize, string? search = null, int? categoriaId = null) =>
            _repo.GetPaged(page, pageSize, search, categoriaId);

        /// <summary>
        /// Carga inicial de la pantalla: opciones del filtro + primera página de la tabla en
        /// una sola llamada. Secuencial y no con Task.WhenAll a propósito, para no tomar dos
        /// conexiones de la base de datos a la vez.
        /// </summary>
        public async Task<UserListInitialDto> GetInitial(int page, int pageSize) => new()
        {
            Categorias = await _repo.GetCategoriaOptions(),
            Users = await _repo.GetPaged(page, pageSize),
        };

        public Task<List<AbrilWorkerOptionDto>> GetAbrilWorkersWithoutUser() =>
            _repo.GetAbrilWorkersWithoutUser();

        public Task CreateAbrilWorkerUser(AbrilWorkerUserCreateDto dto, int createdUserId) =>
            _repo.CreateAbrilWorkerUser(dto, createdUserId);

        public async Task CreateAbrilManualUser(AbrilManualUserCreateDto dto, int createdUserId)
        {
            var email = dto.Email?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email)
                || !email.EndsWith("@abril.pe", StringComparison.OrdinalIgnoreCase))
                throw new AbrilException("El correo debe ser una cuenta corporativa @abril.pe.", 400);

            if (dto.RoleIds is null || dto.RoleIds.Count == 0)
                throw new AbrilException("Debe asignar al menos un rol.", 400);

            // El correo debe existir en el directorio de Abril (tenant Microsoft). Se consulta
            // con permiso de aplicación; los correos inexistentes no vuelven en el diccionario.
            var profiles = await _graphUserService.GetUsersByEmailsAsync(new List<string> { email });
            if (!profiles.TryGetValue(email, out var profile) || string.IsNullOrWhiteSpace(profile.Mail))
                throw new AbrilException(
                    "El correo no existe en el directorio de Abril (Microsoft). Verifica que esté bien escrito.", 404);

            // Se persiste el correo canónico del directorio (así coincidirá con el que
            // recibe el login SSO) y el nombre para mostrar del perfil de Graph.
            await _repo.CreateAbrilManualUser(profile.Mail, profile.DisplayName, dto.RoleIds, createdUserId);
        }

        public async Task Create(UserFeatureCreateDto dto)
        {
            var user = await _repo.Create(dto);

            var token = GenerateToken();
            await _tokenRepo.CreateAsync(new UserPasswordTokenDTO
            {
                UserId = user.UserId,
                Token = token,
                CreatedDateTime = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                Used = false
            });

            var link = $"{_frontendSettings.SetPasswordUrl}?token={token}";
            var body = $@"
                <p>Estimado usuario,</p>
                <p>Le informamos que se ha creado una cuenta a su nombre en nuestro sistema.</p>
                <p>Para activar su cuenta y establecer su contraseña, haga clic en el siguiente enlace:</p>
                <p>
                    <a href='{link}' target='_blank'
                        style='display:inline-block; padding:10px 20px; background-color:#1a73e8; color:#ffffff; text-decoration:none; border-radius:4px;'>
                        Activar cuenta
                    </a>
                </p>
                <p style='font-size: 12px; color: #666;'>
                    Este enlace expirará en 24 horas. Si usted no esperaba este correo, puede ignorarlo o contactar a su administrador.
                </p>
            ";

            await _emailService.SendAsync(
                to: new List<string> { user.Email },
                subject: "Completa tu registro",
                body: body,
                isHtml: true,
                bcc: new List<string> { "calvarez@abril.pe" });
        }

        public async Task Update(int userId, UserFeatureUpdateDto dto, int updatedUserId)
        {
            await _repo.Update(userId, dto, updatedUserId);
            // Sus roles pudieron cambiar: avisarle para que refresque sus permisos al instante.
            await _notifier.NotifyUserRolesChanged(userId);
        }

        public Task ToggleActive(int userId, int updatedUserId) =>
            _repo.ToggleActive(userId, updatedUserId);

        public Task Delete(int userId, int updatedUserId) =>
            _repo.Delete(userId, updatedUserId);

        private static string GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}
