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
        /// <summary>true = esta concesión aplica a TODOS los proyectos (ProyectoId es null en esta
        /// fila). Es por asignación, no por rol — el mismo rol (ej. LOGISTICA) puede ser global
        /// para una persona y de un solo proyecto para otra.</summary>
        public bool EsGlobal { get; set; }
        public int? ProyectoId { get; set; }
        public int? AlmacenId { get; set; }
        /// <summary>Permisos que otorga ESTA asignación puntual — necesario para calcular a qué
        /// proyectos aplica cada permiso (ver LbClaimsExtensions.GetProyectosPermitidos). El campo
        /// plano LbLoginResponseDto.Permisos sigue existiendo, aplanado, para HasLbPermiso().</summary>
        public List<string> Permisos { get; set; } = new();
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
