using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Infrastructure.Models
{
    /// <summary>
    /// Un lote/edificio de un proyecto. Un lote agrupa a N vecinos/departamentos
    /// (<see cref="Vecino"/>). La dirección y las observaciones se registran a nivel de lote.
    /// Cada lote se representa como un polígono en el croquis del proyecto
    /// (<see cref="Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Infrastructure.Models.ProjectCroquisLote"/>).
    /// </summary>
    public class VecinoLote
    {
        public int VecinoLoteId { get; set; }

        public int ProjectId { get; set; }
        public Project? Project { get; set; }

        /// <summary>Dirección del lote. Puede ser nula hasta que se registren vecinos sobre el lote.</summary>
        public string? Direccion { get; set; }

        /// <summary>Observaciones libres del lote (opcional).</summary>
        public string? Observaciones { get; set; }

        public ICollection<Vecino> Vecinos { get; set; } = new List<Vecino>();

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
