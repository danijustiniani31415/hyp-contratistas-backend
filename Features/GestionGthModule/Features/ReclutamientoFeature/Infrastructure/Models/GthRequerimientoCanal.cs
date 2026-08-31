namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Publicación registrada de un requerimiento en un canal (tabla
    /// <c>gth_requerimiento_canal</c>). GTH marca los canales seleccionados en el modal de
    /// detalle y al publicar se registra una fila viva por requerimiento + canal (índice
    /// único parcial); des-seleccionar un canal hace soft delete de su fila.
    /// </summary>
    public class GthRequerimientoCanal
    {
        public int GthRequerimientoCanalId { get; set; }

        public int GthRequerimientoId { get; set; }
        public int GthCanalPublicacionId { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}
