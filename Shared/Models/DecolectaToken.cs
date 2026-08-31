namespace Abril_Backend.Shared.Models
{
    /// <summary>
    /// Token (API key) de una cuenta del servicio Decolecta (consultas RENIEC/SUNAT).
    /// Cada cuenta gratuita tiene una cuota mensual de consultas que se renueva el 1 de cada mes
    /// (via cron-job.org → endpoint cron/renovar) y el token vence un año después de creada la cuenta.
    /// Los servicios de consulta rotan al siguiente token disponible cuando uno agota su cuota.
    /// </summary>
    public class DecolectaToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        /// <summary>Fecha en la que el token deja de ser válido (un año después de creada la cuenta).</summary>
        public DateOnly FechaExpiracion { get; set; }
        /// <summary>true cuando el token agotó su cuota mensual (la API respondió 401/403/429).
        /// Se resetea a false el 1 de cada mes vía el endpoint cron de renovación.</summary>
        public bool Agotado { get; set; }
        public bool State { get; set; } = true;
        public DateTime CreatedDateTime { get; set; }
        public DateTime UpdatedDateTime { get; set; }
    }
}
