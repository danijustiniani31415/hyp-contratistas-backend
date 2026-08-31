namespace Abril_Backend.Shared.Services.Email.Configuration
{
    /// <summary>Parámetros del servidor SMTP del tenant (iguales para todos los buzones).</summary>
    public class SmtpOptions
    {
        public string Host { get; set; } = "smtp.office365.com";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
    }
}
