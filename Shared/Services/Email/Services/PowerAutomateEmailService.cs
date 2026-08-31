using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Shared.Services.Email.Interfaces;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Abril_Backend.Infrastructure.Services
{
    public class PowerAutomateEmailService : IEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly string? _sharedWebhookUrl;
        private readonly IEmailSenderResolver _senderResolver;

        public PowerAutomateEmailService(
            IConfiguration config,
            HttpClient httpClient,
            IEmailSenderResolver senderResolver)
        {
            _httpClient = httpClient;
            _senderResolver = senderResolver;
            _sharedWebhookUrl = config["Email:PowerAutomate:WebhookUrl"];
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
            var htmlBody = isHtml
                ? body
                : "<p>" + body.Replace("\r\n", "\n").Replace("\n\n", "</p><p>").Replace("\n", "<br>") + "</p>";

            // El Flow tiene una sola conexión (aprobaciones@abril.pe) para todos los buzones: es
            // esa cuenta la que envía "en nombre de" la dirección que va en From, mapeada al
            // parámetro avanzado "From (Send as)" de la acción "Enviar correo electrónico (V2)".
            // Si ese parámetro no está poblado en el Flow, PowerAutomate ignora este campo y
            // todo sale como aprobaciones@abril.pe sin devolver error.
            var from = _senderResolver.Resolve(sender);

            // Un solo Flow sirve para todos los buzones. Un remitente puede tener Flow propio
            // (WebhookUrl en su entrada de Email:Senders) sin que los demás cambien.
            var webhookUrl = string.IsNullOrWhiteSpace(from.WebhookUrl)
                ? _sharedWebhookUrl
                : from.WebhookUrl;

            if (string.IsNullOrWhiteSpace(webhookUrl))
                throw new InvalidOperationException(
                    $"No hay WebhookUrl de PowerAutomate para el remitente '{from.Address}': " +
                    "configura Email:PowerAutomate:WebhookUrl o el WebhookUrl propio del remitente.");

            var payload = new
            {
                To = to,
                Subject = subject,
                Body = htmlBody,
                //IsHtml = isHtml,
                From = from.Address,
                Cc = cc,
                Bcc = bcc,
                Attachments = attachments?.Select(a => new
                {
                    FileName = a.FileName,
                    ContentType =  a.ContentType,
                    Content = Convert.ToBase64String(a.Content).Replace("\n", "").Replace("\r", "")
                })
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(webhookUrl, content);

            response.EnsureSuccessStatusCode(); // Lanza excepción si falla
        }
    }
}
