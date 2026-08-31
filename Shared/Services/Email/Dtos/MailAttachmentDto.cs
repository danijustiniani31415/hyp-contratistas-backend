namespace Abril_Backend.Shared.Services.Email.Dtos
{
    public class MailAttachmentDto
    {
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = "application/octet-stream";
        public byte[] Content { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Cuando es <c>true</c> el adjunto se trata como imagen en línea y se referencia
        /// en el HTML mediante <c>cid:&lt;ContentId&gt;</c> en lugar de aparecer como
        /// archivo descargable.
        /// </summary>
        public bool IsInline { get; set; } = false;

        /// <summary>
        /// Identificador de contenido usado en el HTML con <c>src="cid:ContentId"</c>.
        /// Solo relevante cuando <see cref="IsInline"/> es <c>true</c>.
        /// </summary>
        public string? ContentId { get; set; }
    }
}
