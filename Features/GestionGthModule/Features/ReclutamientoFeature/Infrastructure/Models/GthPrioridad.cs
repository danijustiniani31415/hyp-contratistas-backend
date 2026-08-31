namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Catálogo de prioridad de un requerimiento de reclutamiento (tabla <c>gth_prioridad</c>).
    /// Valores iniciales: <c>Alta</c>, <c>Media</c>, <c>Baja</c>. La asigna GTH desde la bandeja de
    /// reclutamiento; al crear una solicitud se asigna <c>Media</c> por defecto. Normalizado a tabla
    /// en vez de texto plano en el requerimiento (regla de normalización del proyecto).
    /// <c>codigo</c> es la clave estable usada en código; <c>orden</c> define el orden semántico
    /// (Alta→Media→Baja) del desplegable.
    /// </summary>
    public class GthPrioridad
    {
        public int GthPrioridadId { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public int Orden { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}
