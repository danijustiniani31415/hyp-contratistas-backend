namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Auth
{
    /// <summary>Usado por staff para crear o resetear el PIN/contraseña de un obrero.</summary>
    public class ObreroSetPasswordDto
    {
        public int WorkerId { get; set; }
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>Usado por el propio obrero, ya logueado, para cambiar su contraseña.</summary>
    public class ObreroCambiarPasswordDto
    {
        public string PasswordActual { get; set; } = string.Empty;
        public string PasswordNuevo { get; set; } = string.Empty;
    }
}
