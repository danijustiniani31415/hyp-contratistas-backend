namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Tipo de correo configurable del módulo de Reclutamiento (tabla <c>gth_correo_tipo</c>).
    /// Cada tipo tiene su propio juego de destinatarios en <see cref="GthCorreoDestinatario"/>.
    /// Códigos estables:
    ///   <c>SOLICITUD</c> → correo de "nueva solicitud de personal" (va a GTH).
    ///   <c>LONG_LIST</c> → correo de "long list enviada" (va al solicitante).
    /// </summary>
    public class GthCorreoTipo
    {
        public int GthCorreoTipoId { get; set; }
        /// <summary>Clave estable usada en código (SOLICITUD, LONG_LIST).</summary>
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        /// <summary>Texto que explica a quién se le manda el correo; lo muestra la pantalla de Configuración.</summary>
        public string? Descripcion { get; set; }

        /// <summary>
        /// true = el destinatario principal lo pone el backend solo (la long list va SIEMPRE al
        /// solicitante que registró la solicitud), así que la configuración solo aporta principales
        /// extra y copias. La pantalla lo usa para no advertir que el correo no le llega a nadie.
        /// </summary>
        public bool PrincipalAutomatico { get; set; }

        /// <summary>
        /// Interruptor de ese destinatario automático: false = el correo sale sin él y solo con los
        /// destinatarios configurados. Es una columna aparte de <see cref="Active"/> porque son dos
        /// decisiones distintas: apagar el correo entero o dejar de mandárselo a quien lo pone el
        /// sistema. Solo tiene sentido cuando <see cref="PrincipalAutomatico"/> es true.
        /// </summary>
        public bool PrincipalAutomaticoActive { get; set; } = true;

        /// <summary>
        /// Cómo se llama ese destinatario en la pantalla ("Solicitante del requerimiento",
        /// "Postulante"…). Es un dato del tipo y no un texto en el frontend: cada correo se lo manda
        /// a alguien distinto. Null → la pantalla usa una etiqueta genérica.
        /// </summary>
        public string? PrincipalAutomaticoNombre { get; set; }

        public int Orden { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}
