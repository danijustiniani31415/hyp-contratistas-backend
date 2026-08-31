using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;

namespace Abril_Backend.Shared.Services.ReclutamientoEmoIngreso.Interfaces
{
    /// <summary>
    /// El enlace entre el resultado de un EMO de Ingreso y el requerimiento de Reclutamiento que
    /// dejó a esa persona como finalista aprobado. Es lo que decide si el proceso de selección
    /// termina o vuelve atrás.
    ///
    /// Vive en <c>Shared</c> porque lo cruzan dos módulos: la regla es de Reclutamiento
    /// (GestionGthModule) pero quien la dispara es Salud Ocupacional (SsomaModule), que es donde se
    /// registra la aptitud del examen. Ninguno de los dos puede ser el dueño del archivo.
    /// </summary>
    public interface IReclutamientoEmoIngresoService
    {
        /// <summary>
        /// Aplica al requerimiento la aptitud con la que quedó el EMO de Ingreso del trabajador:
        ///
        /// <list type="bullet">
        ///   <item><description><b>Apto</b> → <c>EMO_APTO</c>; <b>Apto con Restricciones</b> →
        ///   <c>EMO_APTO_RESTRICCIONES</c>. El proceso NO cierra acá: queda esperando a que GTH lo
        ///   cierre y lo pase a onboarding desde el detalle del requerimiento. Un requerimiento que
        ///   GTH ya cerró no vuelve atrás por reguardar un apto.</description></item>
        ///   <item><description><b>No Apto</b> → el seleccionado sale del proceso y el requerimiento
        ///   vuelve a manos de GTH: a <c>EMO_NO_APTO</c> si hay rechazados que retomar, y directo a
        ///   <c>LONG_LIST</c> si no queda ninguno, que es el único trabajo que le quedaría.</description></item>
        ///   <item><description><b>Observado</b> → <c>EMO_OBSERVADO</c>: el proceso queda a la
        ///   espera del resultado de la interconsulta. Ni cierra ni continúa con otro candidato,
        ///   porque este todavía puede resultar apto.</description></item>
        ///   <item><description>Cualquier otro valor (o el EMO sin calificar) → no se toca
        ///   nada.</description></item>
        /// </list>
        ///
        /// <b>No guarda</b>: deja los cambios en el <paramref name="ctx"/> que se le pasa para que
        /// entren en el mismo <c>SaveChanges</c> que el EMO que los provocó. Y no lanza nunca: si
        /// algo no calza (la persona no viene de un proceso de reclutamiento, el requerimiento ya
        /// avanzó, falta un código del catálogo) no toca nada y devuelve false. Un examen médico ya
        /// registrado no puede fallar por el estado de un requerimiento.
        /// </summary>
        /// <param name="worker">
        /// Ficha del trabajador del EMO, ya cargada por quien llama. Solo se actúa si sigue siendo
        /// de pre-ingreso (<c>FINALISTA_APROBADO</c>): una vez que la persona firma y entra, su
        /// requerimiento es historia y ningún EMO posterior debe moverlo.
        /// </param>
        /// <param name="tipoEmoNombre">
        /// Nombre del tipo de EMO ("Ingreso", "Periódico Anual"…). Solo el de Ingreso mueve el
        /// proceso de selección.
        /// </param>
        /// <param name="aptitud">Aptitud registrada en el EMO.</param>
        /// <param name="userId">Usuario que registró el resultado, para la trazabilidad.</param>
        /// <returns>true si el requerimiento cambió de fase.</returns>
        Task<bool> AplicarAptitudAsync(
            AppDbContext ctx, Worker worker, string? tipoEmoNombre, string? aptitud, int? userId);

        /// <summary>
        /// Le copia al requerimiento la razón social que se le acaba de elegir a la ficha de
        /// pre-ingreso desde el modal "Programar EMO con clínica".
        ///
        /// Es el caso del <b>ingreso directo FFT</b>: su vacante no se aprueba ni se publica, así
        /// que nadie pasó por la asignación interna de Reclutamiento y el requerimiento llegó al EMO
        /// sin razón social. Quien programa la cita tiene que elegir una, y esa elección es de las
        /// dos: la ficha (<c>workers.contributor_id</c>, que lo escribe quien llama) y el
        /// requerimiento. Sin copiarla acá, la pantalla de Reclutamiento la seguiría mostrando
        /// vacía y asignar otra ahí le pisaría a la ficha la que ya se eligió.
        ///
        /// Solo escribe si el requerimiento no tiene ninguna: una razón social ya asignada por GTH
        /// manda sobre esta pantalla.
        ///
        /// <b>No guarda</b> ni lanza, por el mismo motivo que
        /// <see cref="AplicarAptitudAsync"/>: los cambios entran en el <c>SaveChanges</c> de quien
        /// llama y una cita médica no puede caerse por el estado de un requerimiento.
        /// </summary>
        /// <returns>true si el requerimiento se actualizó.</returns>
        Task<bool> SincronizarRazonSocialAsync(
            AppDbContext ctx, Worker worker, int contributorId, int? userId);
    }
}
