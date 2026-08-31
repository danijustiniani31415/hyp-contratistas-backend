namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Auth
{
    public class ResetPasswordDto
    {
        public string Token { get; set; } = string.Empty;
        public string NuevaPassword { get; set; } = string.Empty;
    }
}
