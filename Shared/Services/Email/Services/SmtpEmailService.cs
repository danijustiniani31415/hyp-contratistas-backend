using Abril_Backend.Infrastructure.Interfaces;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Shared.Services.Email.Configuration;
using Abril_Backend.Shared.Services.Email.Interfaces;

namespace Abril_Backend.Infrastructure.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly SmtpOptions _smtp;
        private readonly IEmailSenderResolver _senderResolver;

        public SmtpEmailService(IOptions<EmailOptions> options, IEmailSenderResolver senderResolver)
        {
            _smtp = options.Value.Smtp;
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
            var from = _senderResolver.Resolve(sender);

            if (string.IsNullOrWhiteSpace(from.Password))
                throw new InvalidOperationException(
                    $"El remitente '{from.Address}' no tiene Password en Email:Senders y el proveedor " +
                    "SMTP la necesita para autenticar. (Con PowerAutomate no hace falta.)");

            using var message = new MailMessage
            {
                From = new MailAddress(from.Address, from.DisplayName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            foreach (var email in to.Distinct())
                message.To.Add(email);

            if (cc != null)
                foreach (var email in cc.Distinct())
                    message.CC.Add(email);

            if (bcc != null)
                foreach (var email in bcc.Distinct())
                    message.Bcc.Add(email);

            if (attachments != null && attachments.Any())
            {
                foreach (var file in attachments)
                {
                    var stream = new MemoryStream(file.Content);
                    var attachment = new Attachment(stream, file.FileName, file.ContentType);
                    message.Attachments.Add(attachment);
                }
            }

            // Se autentica con las credenciales del propio remitente, no con las de un buzón
            // fijo: Office365 rechaza enviar con un From distinto del usuario autenticado.
            using var smtp = new SmtpClient(_smtp.Host, _smtp.Port)
            {
                Credentials = new NetworkCredential(from.Address, from.Password),
                EnableSsl = _smtp.EnableSsl,
                UseDefaultCredentials = false
            };

            await smtp.SendMailAsync(message);
        }
    }
}
