namespace Abril_Backend.Shared.Services.Graph.Interfaces
{
    public interface IUserPhotoService
    {
        /// <summary>
        /// Descarga las fotos de perfil de una lista de correos usando permiso de aplicación
        /// (client credentials) — no requiere token del usuario. Retorna un diccionario
        /// email → foto en formato data URI base64 (ej. "data:image/jpeg;base64,...").
        /// Los correos sin foto en Graph (o que no existan) devuelven <c>null</c> como valor.
        /// </summary>
        Task<Dictionary<string, string?>> GetPhotosByEmailsAsync(List<string> emails);
    }
}
