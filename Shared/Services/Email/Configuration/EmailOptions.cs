namespace Abril_Backend.Shared.Services.Email.Configuration
{
    /// <summary>
    /// Configuración completa del servicio de correo (sección <c>Email</c> del appsettings).
    /// </summary>
    public class EmailOptions
    {
        /// <summary>Proveedor activo: <c>PowerAutomate</c> (el usado), <c>SendGrid</c> o SMTP por defecto.</summary>
        public string? EmailProvider { get; set; }

        /// <summary>
        /// Clave de <see cref="Senders"/> que se usa cuando un envío no especifica remitente.
        /// Se valida al arrancar: si no existe en <see cref="Senders"/>, la app no levanta.
        /// </summary>
        public string DefaultSender { get; set; } = null!;

        /// <summary>
        /// Parámetros SMTP del tenant. Son iguales para todos los buzones @abril.pe, por eso
        /// van una sola vez y no repetidos por remitente. Solo los usa SmtpEmailService.
        /// </summary>
        public SmtpOptions Smtp { get; set; } = new();

        /// <summary>
        /// Remitentes registrados, indexados por clave (ej. <c>"Gth"</c>). Agregar un buzón
        /// nuevo es agregar una entrada aquí — no requiere tocar código.
        /// ⚠ Todo buzón que se agregue necesita permiso <b>Send As</b> para la cuenta conectada
        /// al Flow de PowerAutomate, si no el envío falla dentro del Flow sin que el backend lo vea.
        /// </summary>
        public Dictionary<string, EmailSenderOptions> Senders { get; set; } = new();
    }
}
