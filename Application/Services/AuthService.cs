using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Application.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Abril_Backend.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IJWTService _jwtService;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly FrontendSettings _frontendSettings;
        private readonly IUserPasswordTokenRepository _tokenRepository;

        public AuthService(
            IAuthRepository authRepository,
            IJWTService jwtService,
            IUserRepository userRepository,
            IEmailService emailService,
            IUserPasswordTokenRepository tokenRepository,
            IOptions<FrontendSettings> frontendSettings
            )
        {
            _authRepository = authRepository;
            _jwtService = jwtService;
            _userRepository = userRepository;
            _emailService = emailService;
            _tokenRepository = tokenRepository;
            _frontendSettings = frontendSettings.Value;
        }

        public async Task<LoginResponseDTO> Login(LoginDTO dto)
        {
            var user = await _authRepository.ValidateUserAsync(
                dto.Email,
                dto.Password
            );

            if (user == null)
                throw new AbrilException("Credenciales inválidas.", 401);

            var accessToken     = _jwtService.GenerateToken(user);
            var session         = await _authRepository.CreateSessionAsync(user.UserId);
            var allowedFeatures = await _authRepository.GetAllowedFeaturesAsync(user.UserId);

            return new LoginResponseDTO
            {
                AccessToken     = accessToken,
                SessionToken    = session.Token,
                ExpiresAt       = session.ExpiresAt,
                AllowedFeatures = allowedFeatures
            };
        }

        public async Task<RefreshResponseDTO> Refresh(string sessionToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
                throw new AbrilException("Sesión inválida.", 401);

            var userId = await _authRepository.GetUserIdByValidSessionAsync(sessionToken);
            if (userId == null)
                throw new AbrilException("Sesión inválida o expirada.", 401);

            var user = await _authRepository.GetUserForTokenAsync(userId.Value);
            if (user == null)
                throw new AbrilException("Sesión inválida o expirada.", 401);

            var accessToken     = _jwtService.GenerateToken(user);
            var allowedFeatures = await _authRepository.GetAllowedFeaturesAsync(userId.Value);

            return new RefreshResponseDTO
            {
                AccessToken     = accessToken,
                AllowedFeatures = allowedFeatures
            };
        }

        public async Task SetPassword(SetPasswordDTO dto)
        {
            if (dto.Password != dto.ConfirmPassword)
                throw new AbrilException("Las contraseñas no coinciden.");

            var tokenEntity = await _tokenRepository.GetValidTokenAsync(dto.Token);
            if (tokenEntity == null)
                throw new AbrilException("Token inválido o expirado.");

            await _userRepository.SetPassword(tokenEntity.UserId, dto.Password);

            await _tokenRepository.InvalidateTokensByUserAsync(tokenEntity.UserId);
            await _tokenRepository.SaveAsync();
        }

        public async Task ForgotPassword(ForgotPasswordDTO dto)
        {
            var user = await _authRepository.GetUserByIdAsync(dto.UserId);

            if (user == null)
                return;

            await _tokenRepository.InvalidateTokensByUserAsync(user.Value.UserId);

            var token = GenerateToken();

            await _tokenRepository.CreateAsync(new UserPasswordTokenDTO
            {
                UserId = user.Value.UserId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                Used = false,
                CreatedDateTime = DateTime.UtcNow
            });

            var link = $"{_frontendSettings.SetPasswordUrl}?token={token}";

            var body = $@"
                <p>Estimado usuario,</p>
                <p>Hemos recibido una solicitud para restablecer una contraseña para su cuenta.</p>
                <p>Para continuar, haga clic en el siguiente enlace:</p>
                <p>
                    <a href='{link}' target='_blank'
                    style='display:inline-block; padding:10px 20px; background-color:#1a73e8; color:#ffffff; text-decoration:none; border-radius:4px;'>
                        Restablecer contraseña
                    </a>
                </p>
                <p style='font-size: 12px; color: #666;'>
                    Este enlace expirará en 1 hora. Si usted no realizó esta solicitud, ignore este correo.
                </p>
            ";

            await _emailService.SendAsync(
                to: new List<string> { user.Value.Email },
                subject: "Restablece tu contraseña",
                body: body,
                isHtml: true,
                bcc: new List<string> { "calvarez@abril.pe" }
            );
        }

        private string GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}