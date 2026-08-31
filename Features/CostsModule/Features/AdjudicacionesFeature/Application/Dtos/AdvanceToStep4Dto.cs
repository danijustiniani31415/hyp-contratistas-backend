namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos
{
    public class AdvanceToStep4Dto
    {
        /// <summary>
        /// Token de Microsoft Graph del usuario autenticado.
        /// El correo de notificación al Staff de Obra se envía vía Graph
        /// como ese usuario (no por un proveedor externo).
        /// </summary>
        public string GraphAccessToken { get; set; } = null!;
    }
}
