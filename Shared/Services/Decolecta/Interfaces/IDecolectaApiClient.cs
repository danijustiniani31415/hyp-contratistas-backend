namespace Abril_Backend.Shared.Services.Decolecta.Interfaces
{
    /// <summary>
    /// Cliente HTTP hacia la API de Decolecta con rotación automática de tokens.
    /// Prueba los tokens disponibles de la tabla decolecta_token en orden; si uno responde
    /// 401/403/429 (cuota agotada o credencial inválida) lo marca como agotado y pasa al siguiente.
    /// </summary>
    public interface IDecolectaApiClient
    {
        /// <summary>
        /// Hace un GET a la API de Decolecta rotando tokens. Devuelve la respuesta del primer token
        /// que no falle por cuota (puede ser un 404 u otro error de negocio: eso lo maneja el caller).
        /// Lanza <c>AbrilException</c> con <paramref name="mensajeAgotado"/> (503) si todos los
        /// tokens agotaron su cuota.
        /// </summary>
        Task<HttpResponseMessage> GetAsync(string requestUri, string mensajeAgotado);
    }
}
