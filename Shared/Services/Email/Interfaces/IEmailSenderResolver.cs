using Abril_Backend.Shared.Services.Email.Configuration;

namespace Abril_Backend.Shared.Services.Email.Interfaces
{
    /// <summary>
    /// Traduce la clave de remitente que recibe <c>IEmailService.SendAsync</c> al buzón
    /// configurado en <c>Email:Senders</c>.
    /// </summary>
    public interface IEmailSenderResolver
    {
        /// <summary>Remitente por defecto (<c>Email:DefaultSender</c>).</summary>
        EmailSenderOptions Default { get; }

        /// <summary>
        /// Resuelve una clave de <c>Email:Senders</c> (o, como red de seguridad, la dirección
        /// de un remitente ya registrado). Solo devuelve buzones registrados: cualquier valor
        /// desconocido cae al remitente por defecto dejando un warning en el log, porque un
        /// buzón sin permiso Send As en el Flow de PowerAutomate falla sin que el backend lo vea.
        /// </summary>
        EmailSenderOptions Resolve(string? sender);
    }
}
