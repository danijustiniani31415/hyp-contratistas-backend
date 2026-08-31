namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Catálogo de canales donde se publica una vacante (tabla <c>gth_canal_publicacion</c>):
    /// Bumeran, LinkedIn y Computrabajo. No hay integración con las APIs de los portales — GTH
    /// publica manualmente y en Abril One solo marca en qué canales lo hizo.
    /// </summary>
    public class GthCanalPublicacion
    {
        public int GthCanalPublicacionId { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;

        /// <summary>
        /// OBSOLETO: se descartó integrar las APIs de los portales, así que ya no se sirve al
        /// frontend. La columna se conserva por auditoría; no usarla para decidir comportamiento.
        /// </summary>
        public bool ApiDisponible { get; set; }

        public int Orden { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}
