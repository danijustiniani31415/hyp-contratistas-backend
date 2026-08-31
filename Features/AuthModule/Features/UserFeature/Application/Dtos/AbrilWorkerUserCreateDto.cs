namespace Abril_Backend.Features.AuthModule.UserFeature.Application.Dtos
{
    /// <summary>
    /// Solicitud para crear manualmente el usuario de un trabajador de Abril.
    /// El correo se toma del <c>email_corporativo</c> (@abril.pe) registrado en
    /// <c>workers</c>; el usuario queda activo y se le asignan los roles indicados.
    /// </summary>
    public class AbrilWorkerUserCreateDto
    {
        public int PersonId { get; set; }
        public List<int> RoleIds { get; set; } = new();
    }
}
