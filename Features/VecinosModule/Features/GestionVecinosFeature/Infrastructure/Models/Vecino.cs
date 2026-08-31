using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Infrastructure.Models
{
    public class Vecino
    {
        public int VecinoId { get; set; }

        /// <summary>
        /// Proyecto del vecino/departamento. Se mantiene poblado (denormalizado desde el lote)
        /// para evitar joins adicionales en listados y dashboard. Siempre coincide con
        /// <see cref="VecinoLote"/>.ProjectId.
        /// </summary>
        public int ProjectId { get; set; }
        public Project? Project { get; set; }

        /// <summary>
        /// Lote/edificio al que pertenece este vecino/departamento. Un lote agrupa a N vecinos.
        /// La dirección y las observaciones viven en el lote.
        /// </summary>
        public int VecinoLoteId { get; set; }
        public VecinoLote? Lote { get; set; }

        /// <summary>Obsoleto: reemplazado por <see cref="VecinoUsoId"/>. Se conserva por auditoría.</summary>
        public string? Predio { get; set; }

        /// <summary>Obsoleto: la dirección vive ahora en <see cref="VecinoLote"/>. Se conserva por auditoría.</summary>
        public string? Direccion { get; set; }
        public string? InteriorDepartamento { get; set; }

        /// <summary>Obsoleto: los datos de la persona viven ahora en <see cref="Personas"/>. Se conserva por auditoría.</summary>
        public string? NombrePropietario { get; set; }
        /// <summary>Obsoleto: ver <see cref="Personas"/>. Se conserva por auditoría.</summary>
        public string? Dni { get; set; }
        /// <summary>Obsoleto: ver <see cref="Personas"/>. Se conserva por auditoría.</summary>
        public string? Celular { get; set; }

        public int? VecinoUsoId { get; set; }
        public VecinoUso? Uso { get; set; }

        public int VecinoColindanciaId { get; set; }
        public VecinoColindancia? Colindancia { get; set; }

        public int VecinoTipoConstruccionId { get; set; }
        public VecinoTipoConstruccion? TipoConstruccion { get; set; }

        /// <summary>Obsoleto: las observaciones viven ahora en <see cref="VecinoLote"/>. Se conserva por auditoría.</summary>
        public string? Observaciones { get; set; }

        public ICollection<VecinoPersona> Personas { get; set; } = new List<VecinoPersona>();

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
