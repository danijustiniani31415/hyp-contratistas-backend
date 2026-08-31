namespace Abril_Backend.Shared.Services.Email.Configuration
{
    /// <summary>Un buzón desde el que la aplicación puede enviar correos.</summary>
    public class EmailSenderOptions
    {
        /// <summary>Dirección del buzón (ej. <c>gth@abril.pe</c>). Con SMTP también hace de usuario.</summary>
        public string Address { get; set; } = null!;

        /// <summary>
        /// Nombre visible. Solo lo aplican SMTP y SendGrid: con PowerAutomate el nombre lo
        /// resuelve Exchange desde el propio buzón y este valor no tiene efecto.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Contraseña del buzón. Solo la necesita el proveedor SMTP; con PowerAutomate
        /// autentica la conexión del Flow, así que puede quedar vacía.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Flow propio de PowerAutomate para este buzón. Opcional: si va vacío se usa el Flow
        /// compartido de <c>Email:PowerAutomate:WebhookUrl</c>, que envía desde cualquier buzón
        /// vía "From (Send as)". Solo vale la pena separarlo para un buzón que no pueda tener
        /// permiso Send As, que necesite un Flow distinto, o que mueva tanto volumen que
        /// convenga no gastar la cuota de envío de la cuenta conectada al Flow compartido.
        /// </summary>
        public string? WebhookUrl { get; set; }
    }
}
