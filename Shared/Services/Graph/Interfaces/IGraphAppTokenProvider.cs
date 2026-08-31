namespace Abril_Backend.Shared.Services.Graph.Interfaces
{
    /// <summary>
    /// Token de aplicación de Microsoft Graph (flujo <c>client_credentials</c>), cacheado en memoria.
    /// Es el token que usan los procesos de fondo: no representa a un usuario, así que sirve para
    /// operar sobre cualquier buzón del tenant sin que nadie tenga sesión iniciada.
    /// </summary>
    public interface IGraphAppTokenProvider
    {
        /// <summary>
        /// Devuelve un token válido para <c>https://graph.microsoft.com/.default</c>, reutilizando
        /// el cacheado mientras no esté por expirar.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Si falta configuración en <c>AzureAd</c> o Entra ID rechaza la solicitud.
        /// </exception>
        Task<string> GetTokenAsync(CancellationToken cancellationToken = default);
    }
}
