namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Auth
{
    public class CambiarPasswordDto
    {
        public string PasswordActual { get; set; } = string.Empty;
        public string PasswordNuevo { get; set; } = string.Empty;
    }
}
