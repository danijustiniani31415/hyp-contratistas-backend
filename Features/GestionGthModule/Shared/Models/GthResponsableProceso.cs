using Abril_Backend.Infrastructure.Models;

namespace Abril_Backend.Features.GestionGthModule.Shared.Models
{
    /// <summary>
    /// Tabla filtro de "Reclutadores" (<c>gth_responsable_proceso</c>): la lista blanca de
    /// trabajadores del área de GTH que pueden ser "Responsable del proceso" de un
    /// requerimiento. Cada fila apunta a un trabajador de la base maestra
    /// (<c>workers</c>); el nombre se resuelve vía <c>person.full_name</c>.
    ///
    /// Es una tabla aparte de <c>workers</c> a propósito: <c>workers</c> es la tabla maestra
    /// de todo el sistema, así que activar o desactivar a alguien como reclutador NO puede
    /// tocar su ficha — solo esta fila. El <c>active</c> de acá manda únicamente sobre el
    /// desplegable "Responsable del proceso" del detalle de Reclutamiento.
    ///
    /// La administra la feature <c>ReclutadoresFeature</c> (Gestión GTH → Configuración →
    /// Reclutadores) y la consume <c>ReclutamientoFeature</c>; por eso vive en el
    /// <c>Shared/</c> del módulo y no dentro de una de las dos features.
    /// </summary>
    public class GthResponsableProceso
    {
        public int GthResponsableProcesoId { get; set; }

        /// <summary>FK a <c>workers.id</c> (miembro del equipo GTH).</summary>
        public int WorkerId { get; set; }
        public Worker? Worker { get; set; }

        /// <summary>
        /// Orden heredado del seed original. La pantalla de Reclutadores y el desplegable
        /// ordenan por nombre, así que las filas nuevas entran con 0.
        /// </summary>
        public int Orden { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }

        /// <summary>
        /// true = sale en el desplegable "Responsable del proceso". Es el interruptor que
        /// prende/apaga la pantalla de Reclutadores.
        /// </summary>
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}
