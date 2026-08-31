using Abril_Backend.Shared.Services.Email.Dtos;

namespace Abril_Backend.Shared.Services.Email.Interfaces
{
    public interface IDelegatedMailService
    {
        Task SendAsync(
            string graphAccessToken,
            List<string> to,
            string subject,
            string body,
            bool isHtml,
            List<string>? cc = null,
            List<string>? bcc = null,
            List<MailAttachmentDto>? attachments = null);
    }
}
