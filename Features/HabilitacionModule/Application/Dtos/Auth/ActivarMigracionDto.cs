namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Auth
{
    public class ActivarMigracionDto
    {
        public string Ruc { get; set; } = null!;
        public string SpPassword { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
