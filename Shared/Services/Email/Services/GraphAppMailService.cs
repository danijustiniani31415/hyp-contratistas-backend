using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Shared.Services.Email.Interfaces;
using Abril_Backend.Shared.Services.Graph.Interfaces;

namespace Abril_Backend.Shared.Services.Email.Services
{
    /// <summary>
    /// Envía correo con Microsoft Graph usando permiso de <b>aplicación</b> (<c>Mail.Send</c>):
    /// <c>POST /v1.0/users/{buzón}/sendMail</c>.
    ///
    /// Frente a <see cref="PowerAutomateEmailService"/>: no necesita licencia Power Automate
    /// Premium, no necesita permiso Send As entre buzones (la app envía desde el buzón indicado
    /// en From directamente), y un fallo devuelve error HTTP real en vez del 202 que devolvía el
    /// Flow antes de intentar el envío.
    ///
    /// ⚠ <c>Mail.Send</c> de aplicación alcanza a CUALQUIER buzón del tenant. Debe restringirse
    /// con una Application Access Policy de Exchange Online limitada a los buzones de
    /// <c>Email:Senders</c>, si no la app puede suplantar a toda la organización.
    /// </summary>
    public class GraphAppMailService : IEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly IGraphAppTokenProvider _tokenProvider;
        private readonly IEmailSenderResolver _senderResolver;
        private readonly ILogger<GraphAppMailService> _logger;

        /// <summary>
        /// Graph rechaza <c>sendMail</c> cuando el mensaje completo (adjuntos ya en base64)
        /// pasa de 4 MB. Se valida antes de enviar para dar un error entendible en vez del
        /// 413 crudo; para adjuntos más grandes haría falta crear un borrador y subirlos por
        /// upload session, que hoy no se usa en ningún envío.
        /// </summary>
        private const int MaxMessageBytes = 4 * 1024 * 1024;

        public GraphAppMailService(
            HttpClient httpClient,
            IGraphAppTokenProvider tokenProvider,
            IEmailSenderResolver senderResolver,
            ILogger<GraphAppMailService> logger)
        {
            _httpClient = httpClient;
            _tokenProvider = tokenProvider;
            _senderResolver = senderResolver;
            _logger = logger;
        }

        public async Task SendAsync(
            List<string> to,
            string subject,
            string body,
            bool isHtml,
            List<string>? cc = null,
            List<string>? bcc = null,
            List<EmailAttachment>? attachments = null,
            string? sender = null)
        {
            // Misma conversión que hacía PowerAutomateEmailService: Graph soporta contentType
            // "Text" nativo, pero mandar siempre HTML mantiene los correos idénticos a los que
            // la empresa ya venía recibiendo, que es lo que se quiere en un cambio de proveedor.
            var htmlBody = isHtml
                ? body
                : "<p>" + body.Replace("\r\n", "\n").Replace("\n\n", "</p><p>").Replace("\n", "<br>") + "</p>";

            var from = _senderResolver.Resolve(sender);

            var message = new Dictionary<string, object?>
            {
                ["subject"] = subject,
                ["body"] = new { contentType = "HTML", content = htmlBody },
                ["toRecipients"] = to.Select(Recipient),
                ["ccRecipients"] = (cc ?? new List<string>()).Select(Recipient),
                ["bccRecipients"] = (bcc ?? new List<string>()).Select(Recipient),
            };

            if (attachments is { Count: > 0 })
                message["attachments"] = attachments.Select(a => new Dictionary<string, object?>
                {
                    // La clave lleva punto, así que no puede salir de un objeto anónimo.
                    ["@odata.type"] = "#microsoft.graph.fileAttachment",
                    ["name"] = a.FileName,
                    ["contentType"] = a.ContentType,
                    ["contentBytes"] = Convert.ToBase64String(a.Content),
                });

            var json = JsonSerializer.Serialize(new { message, saveToSentItems = true });

            if (Encoding.UTF8.GetByteCount(json) > MaxMessageBytes)
                throw new InvalidOperationException(
                    $"El correo '{subject}' pesa más de {MaxMessageBytes / (1024 * 1024)} MB con sus adjuntos " +
                    "y Graph no lo aceptará. Reducir los adjuntos o enviarlos por enlace de SharePoint.");

            var request = new HttpRequestMessage(
                HttpMethod.Post, $"v1.0/users/{Uri.EscapeDataString(from.Address)}/sendMail")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer", await _tokenProvider.GetTokenAsync());

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();

                _logger.LogError(
                    "Graph sendMail falló con {StatusCode} enviando desde {From} a {Destinatarios}: {Error}",
                    (int)response.StatusCode, from.Address, string.Join(", ", to), errorBody);

                throw new InvalidOperationException(
                    $"Graph sendMail falló con {(int)response.StatusCode} desde '{from.Address}': {errorBody}");
            }
        }

        private static object Recipient(string address) => new { emailAddress = new { address } };
    }
}
