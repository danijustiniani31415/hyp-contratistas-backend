using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Application.Interfaces;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Abril_Backend.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserPasswordTokenRepository _tokenRepository;
        private readonly IEmailService _emailService;
        private readonly FrontendSettings _frontendSettings;
        public UserService(
            IUserRepository userRepository,
            IUserPasswordTokenRepository tokenRepository,
            IEmailService emailService,
            IOptions<FrontendSettings> frontendSettings
            )
        {
            _userRepository = userRepository;
            _tokenRepository = tokenRepository;
            _emailService = emailService;
            _tokenRepository = tokenRepository;
            _frontendSettings = frontendSettings.Value;
        }
        public async Task<PagedResult<UserDTO>> GetPagedFactory(int page, int pageSize)
        {
            var registros = await _userRepository.GetPagedFactory(page, pageSize);
            return registros;
        }

        public async Task Create(UserCreateDTO dto)
        {
            var user = await _userRepository.Create(dto);
            if (user == null)
                throw new AbrilException("La persona ya tiene un usuario registrado.");
            var token = GenerateToken();

            await _tokenRepository.CreateAsync(new UserPasswordTokenDTO
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
                bcc: new List<string> { "calvarez@abril.pe" }
            );
        }

        public async Task Update(int userId, UserUpdateDTO dto)
        {
            await _userRepository.Update(userId, dto);
        }

        public async Task ToggleActive(int userId, int updatedUserId)
        {
            await _userRepository.ToggleActive(userId, updatedUserId);
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