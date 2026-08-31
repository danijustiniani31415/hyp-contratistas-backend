using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Interfaces
{
    /// <summary>
    /// Única fuente de verdad de a quién le llega cada correo de Reclutamiento. Lee la
    /// configuración de la pantalla (correo prendido/apagado, destinatarios prendidos/apagados)
    /// y expande los destinatarios dinámicos al correo que corresponde en ese momento.
    ///
    /// La usan tanto el envío real como la previsualización del modal de nueva solicitud,
    /// justamente para que el aviso que ve el solicitante y el correo que sale no diverjan.
    /// </summary>
    public interface ICorreoDestinatariosResolver
    {
        /// <summary>
        /// Destinatarios efectivos de un correo. Devuelve las listas vacías si el correo está
        /// apagado con su interruptor maestro.
        /// </summary>
        /// <param name="tipoCodigo">Código estable del correo (<see cref="CorreoTipoReclutamiento"/>).</param>
        /// <param name="areaScopeId">
        /// <c>workers.area_scope_id</c> del solicitante, necesario para resolver al gerente de su
        /// área. Si es null (o su área no cuelga de ninguna gerencia con gerente registrado) esa
        /// fila simplemente no aporta a nadie.
        /// </param>
        Task<SolicitudDestinatariosDto> ResolverAsync(string tipoCodigo, int? areaScopeId = null);
    }
}
