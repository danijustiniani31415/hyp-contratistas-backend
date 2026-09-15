namespace Abril_Backend.Features.PersonasModule.Application.Dtos
{
    public class LbLoginRequestDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class LbAsignacionDto
    {
        public string RolCodigo { get; set; } = null!;
        public bool EsGlobal { get; set; }
        public int? ProyectoId { get; set; }
        public int? AlmacenId { get; set; }
    }

    public class LbLoginResponseDto
    {
        public string Token { get; set; } = null!;
        public long UsuarioSistemaId { get; set; }
        public string NombreCompleto { get; set; } = null!;
        public string Email { get; set; } = null!;
        public List<LbAsignacionDto> Asignaciones { get; set; } = new();
        public List<string> Permisos { get; set; } = new();
    }

    public class LbSolicitarResetDto
    {
        public string Email { get; set; } = null!;
    }

    public class LbResetPasswordDto
    {
        public string Token { get; set; } = null!;
        public string NuevaPassword { get; set; } = null!;
    }
}
