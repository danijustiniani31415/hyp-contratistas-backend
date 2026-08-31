using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Programacion;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces
{
    /// <summary>
    /// Única fuente de verdad de a quién le llega cada correo de EMO.
    ///
    /// Antes cada correo armaba su lista de destinatarios por su cuenta y los correos
    /// de área salían de claves del appsettings; ahora todo sale de la matriz
    /// configurable en /ssoma/salud-ocupacional/emos/configuracion
    /// (correo × perfil del trabajador × destinatario).
    ///
    /// Lo consumen los 4 envíos y la vista previa del modal "Programar EMO con clínica",
    /// así que lo que el usuario ve antes de guardar es literalmente lo que se va a enviar.
    /// </summary>
    public interface IEmoDestinatariosResolver
    {
        /// <summary>Destinatarios de un correo para un trabajador.</summary>
        /// <param name="eventoCodigo">Ver <c>EmoCorreoEventoCodigo</c>.</param>
        /// <param name="clinicaId">Clínica de la cita; null si el correo no tiene una.</param>
        Task<ProgramacionDestinatariosDto> ResolverAsync(string eventoCodigo, int workerId, int? clinicaId);

        /// <summary>
        /// Destinatarios de un correo que agrupa a varios trabajadores (el resumen de la
        /// programación automática): la unión de lo que corresponde a cada uno, resuelta
        /// en un número fijo de consultas sea para 1 o para 500.
        /// </summary>
        Task<ProgramacionDestinatariosDto> ResolverLoteAsync(
            string eventoCodigo, IReadOnlyCollection<int> workerIds, int? clinicaId);
    }
}
