namespace Abril_Backend.Features.AuthModule.UserFeature.Application.Dtos
{
    /// <summary>
    /// Solicitud para crear manualmente el usuario de un trabajador de Abril que NO
    /// está registrado en <c>workers</c> (casos especiales, p. ej. gerencia). El correo
    /// @abril.pe se escribe a mano y se valida contra el directorio de Abril (Microsoft
    /// Graph) antes de crear el usuario; el usuario queda activo, sin contraseña
    /// (ingresa vía SSO), y se le asignan los roles indicados.
    /// </summary>
    public class AbrilManualUserCreateDto
    {
        public string Email { get; set; } = null!;
        public List<int> RoleIds { get; set; } = new();
    }
}
