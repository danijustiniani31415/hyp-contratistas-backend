using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Workers;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces
{
    /// <summary>
    /// Valida los dos correos de un trabajador:
    /// <list type="bullet">
    /// <item><b>Corporativo</b> (<c>workers.email_corporativo</c>): buzón del tenant de Abril. Solo
    /// aplica al personal de Casa de Staff/Oficina Central, o a cualquier correo del dominio de
    /// Abril. Debe existir en el directorio y ser único entre trabajadores no retirados.</item>
    /// <item><b>Personal / de contacto</b> (<c>person.email</c>): solo formato. Se puede repetir a
    /// propósito — varios trabajadores de una contratista comparten el correo de su RR.HH.</item>
    /// </list>
    /// Todo trabajador debe quedar con al menos uno de los dos.
    /// </summary>
    public interface IWorkerEmailValidator
    {
        /// <summary>
        /// Verifica solo el corporativo y sin lanzar excepciones (para la verificación en vivo del
        /// formulario). <paramref name="esCorporativo"/> fuerza el tratamiento como corporativo; si
        /// es null se deduce de la clasificación guardada del <paramref name="workerId"/> y del dominio.
        /// </summary>
        Task<EmailCorporativoValidacionDto> ValidarCorporativoAsync(
            string? email,
            int? workerId,
            bool? esCorporativo,
            bool obligatorio = false);

        /// <summary>
        /// Valida ambos correos y devuelve los valores canónicos a persistir. Lanza
        /// <c>AbrilException</c> si alguno es inválido o si el trabajador quedaría sin ningún correo.
        /// Resuelve todo con un único roundtrip a la BD.
        /// </summary>
        /// <param name="workerId">Null al crear. Al editar se excluye del chequeo de duplicados.</param>
        /// <param name="esCorporativo">
        /// Clasificación que envía el formulario (Staff/Oficina Central = true). Si es null se usa la
        /// guardada del trabajador.
        /// </param>
        Task<WorkerCorreosDto> ValidarYNormalizarAsync(
            string? emailCorporativo,
            string? emailPersonal,
            int? workerId,
            bool? esCorporativo);
    }
}
