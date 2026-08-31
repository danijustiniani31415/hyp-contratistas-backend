using Abril_Backend.Infrastructure.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Shared.Services.Email.Interfaces;

namespace Abril_Backend.Infrastructure.Services
{
    public class SendGridEmailService : IEmailService
    {
        private readonly string _apiKey;
        private readonly IEmailSenderResolver _senderResolver;

        public SendGridEmailService(IConfiguration config, IEmailSenderResolver senderResolver)
        {
            _apiKey = config["Email:SendGrid:ApiKeySendGrid"];
            _senderResolver = senderResolver;
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
            var client = new SendGridClient(_apiKey);
            var resolved = _senderResolver.Resolve(sender);
            var from = new EmailAddress(resolved.Address, resolved.DisplayName);

            var tos = to.Select(email => new EmailAddress(email)).ToList();

            var msg = MailHelper.CreateSingleEmailToMultipleRecipients(
                from,
                tos,
                subject,
                isHtml ? null : body,
                isHtml ? body : null,
                false
            );

            if (cc != null)
                msg.AddCcs(cc.Select(e => new EmailAddress(e)).ToList());

            if (bcc != null)
                msg.AddBccs(bcc.Select(e => new EmailAddress(e)).ToList());

            if (attachments != null)
            {
                foreach (var file in attachments)
                {
                    msg.AddAttachment(file.FileName, Convert.ToBase64String(file.Content), file.ContentType);
                }
            }

            // SendGrid no lanza excepción por un envío rechazado (from no verificado, API key
            // inválida, etc.) — devuelve un Response con el código de error y ya. Sin este check
            // el caller (ej. InterconsultaService.EnviarRecordatorios) veía "enviado" sin
            // excepción y reportaba éxito aunque SendGrid nunca haya entregado nada.
            var response = await client.SendEmailAsync(msg);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Body.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"SendGrid rechazó el envío (HTTP {(int)response.StatusCode}): {errorBody}");
            }
        }
    }
}