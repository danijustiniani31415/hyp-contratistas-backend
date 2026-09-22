namespace Abril_Backend.Infrastructure.Models
{
    public class FrontendSettings
    {
        public string SetPasswordUrl { get; set; }
        public string ContractorCredentialsUrl { get; set; }
        public string LbSetPasswordUrl { get; set; }
        /// <summary>Base del frontend de Las Bravas/HP (ej. https://app.hpconstructoresgenerales.com)
        /// — usada para armar links "Ver pedido" en los correos de aprobación. Si no está
        /// configurada, el correo se manda igual pero sin el botón de link.</summary>
        public string? LbAppUrl { get; set; }
    }
}
