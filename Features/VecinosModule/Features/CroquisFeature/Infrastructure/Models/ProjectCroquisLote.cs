namespace Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Infrastructure.Models
{
    /// <summary>
    /// Un lote dibujado a mano sobre el croquis de un proyecto. El polígono se guarda como
    /// JSON con coordenadas relativas (0–1) al tamaño de la imagen, para que escale con el zoom.
    /// </summary>
    public class ProjectCroquisLote
    {
        public int ProjectCroquisLoteId { get; set; }

        public int ProjectCroquisId { get; set; }
        public ProjectCroquis? ProjectCroquis { get; set; }

        public string NumeroLote { get; set; } = null!;

        /// <summary>JSON: lista de puntos [[x,y], …] con x,y en rango 0–1 relativos a la imagen.</summary>
        public string Poligono { get; set; } = null!;

        /// <summary>
        /// Lote/edificio que representa este polígono. Un lote agrupa a N vecinos/departamentos.
        /// Se crea junto con el polígono al dibujarlo en el croquis.
        /// </summary>
        public int? VecinoLoteId { get; set; }
        public Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Infrastructure.Models.VecinoLote? Lote { get; set; }

        /// <summary>
        /// Obsoleto: reemplazado por <see cref="VecinoLoteId"/>. Antes un polígono se enlazaba a un
        /// único vecino; ahora enlaza a un lote (que agrupa varios vecinos). Se conserva por auditoría.
        /// </summary>
        public int? VecinoId { get; set; }

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
