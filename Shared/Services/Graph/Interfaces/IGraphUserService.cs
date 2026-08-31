using Abril_Backend.Shared.Services.Graph.Dtos;

namespace Abril_Backend.Shared.Services.Graph.Interfaces
{
    public interface IGraphUserService
    {
        /// <summary>
        /// Obtiene los perfiles de Graph de una lista de emails en una sola consulta (o en chunks de 15).
        /// Usa permiso de aplicación (client credentials) — no requiere token del usuario.
        /// Retorna un diccionario email → perfil para lookup O(1).
        /// Emails que no existan en el tenant simplemente no aparecerán en el resultado.
        /// </summary>
        Task<Dictionary<string, GraphUserProfileDto>> GetUsersByEmailsAsync(List<string> emails);

        /// <summary>
        /// Igual que GetUsersByEmailsAsync pero expande automáticamente los grupos de correo:
        /// si un email no corresponde a un usuario individual, consulta si es un grupo y
        /// retorna los perfiles de sus miembros en lugar del grupo.
        /// Retorna una lista plana de perfiles individuales listos para mostrar en una tabla.
        /// </summary>
        Task<List<GraphUserProfileDto>> GetResolvedProfilesAsync(List<string> emails);

        /// <summary>
        /// Obtiene el perfil del usuario autenticado (GET /me) usando un token delegado.
        /// Retorna null si el token es inválido o el perfil no puede obtenerse.
        /// </summary>
        Task<GraphUserProfileDto?> GetCurrentUserProfileAsync(string graphAccessToken);
    }
}
