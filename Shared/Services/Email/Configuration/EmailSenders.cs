namespace Abril_Backend.Shared.Services.Email.Configuration
{
    /// <summary>
    /// Claves de los remitentes registrados en <c>Email:Senders</c> del appsettings.
    /// Se usan como parámetro <c>sender</c> de <c>IEmailService.SendAsync</c> para no
    /// repartir direcciones de correo como strings mágicos por el código: si un buzón
    /// cambia de dirección, se edita el appsettings y ningún call site.
    /// </summary>
    public static class EmailSenders
    {
        /// <summary>Buzón institucional por defecto (aprobaciones@abril.pe).</summary>
        public const string Aprobaciones = "Aprobaciones";

        /// <summary>Gestión del Talento Humano (gth@abril.pe).</summary>
        public const string Gth = "Gth";

        /// <summary>Salud Ocupacional — EMO, interconsultas (medicinaocupacionalnm@abril.pe).</summary>
        public const string MedicinaOcupacional = "MedicinaOcupacional";
    }
}
