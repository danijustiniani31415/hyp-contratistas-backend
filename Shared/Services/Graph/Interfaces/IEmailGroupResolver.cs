namespace Abril_Backend.Shared.Services.Graph.Interfaces
{
    /// <summary>
    /// Resuelve una lista de correos antes de enviar un email, desglosando los que
    /// corresponden a un grupo (mail-enabled) en los correos de sus miembros, y dejando
    /// pasar tal cual los que NO son grupos (usuarios del tenant, correos externos, alias).
    ///
    /// A diferencia de <see cref="IGraphUserService.GetResolvedProfilesAsync"/> (que devuelve
    /// perfiles y descarta lo que no existe en el tenant), este resolver NO pierde destinatarios:
    /// si no puede determinar que un correo es grupo, lo conserva. Pensado para llamarse SIEMPRE
    /// antes de enviar (p. ej. desde el cronjob de recordatorios de lecciones aprendidas que envía
    /// por PowerAutomate y no sabe entregar a grupos).
    ///
    /// Usa permiso de aplicación (client credentials) — no requiere usuario autenticado.
    /// </summary>
    public interface IEmailGroupResolver
    {
        /// <summary>
        /// Devuelve la lista plana de correos individuales (sin duplicados): cada grupo se
        /// reemplaza por los correos de sus miembros usuario; el resto se conserva igual.
        /// </summary>
        Task<List<string>> ExpandAsync(IEnumerable<string> emails);
    }
}
