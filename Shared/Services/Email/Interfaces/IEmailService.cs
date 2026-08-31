using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Infrastructure.Interfaces
{
    public interface IEmailService
    {
        /// <summary>
        /// Envía un correo con el proveedor configurado en <c>Email:EmailProvider</c>.
        /// </summary>
        /// <param name="sender">
        /// Clave del buzón remitente registrado en <c>Email:Senders</c> — usar las constantes de
        /// <c>EmailSenders</c> (ej. <c>EmailSenders.Gth</c>). Si se omite se usa
        /// <c>Email:DefaultSender</c> (aprobaciones@abril.pe). Un valor no registrado también cae
        /// al remitente por defecto y deja un warning en el log: enviar desde un buzón sin
        /// permiso Send As en el Flow de PowerAutomate fallaría en silencio.
        /// </param>
        Task SendAsync(
            List<string> to,
            string subject,
            string body,
            bool isHtml,
            List<string>? cc = null,
            List<string>? bcc = null,
            List<EmailAttachment>? attachments = null,
            string? sender = null);
    }
}
