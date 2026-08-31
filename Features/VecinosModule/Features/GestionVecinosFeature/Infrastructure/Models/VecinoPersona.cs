namespace Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Infrastructure.Models
{
    /// <summary>
    /// Una persona asociada a una casa/lote (Vecino). Una casa puede tener varias personas
    /// (propietario, inquilinos, otros). El DNI es opcional.
    /// </summary>
    public class VecinoPersona
    {
        public int VecinoPersonaId { get; set; }

        public int VecinoId { get; set; }
        public Vecino? Vecino { get; set; }

        public string Nombre { get; set; } = null!;
        public string? Dni { get; set; }
        public string? Celular { get; set; }

        public int VecinoRelacionTipoId { get; set; }
        public VecinoRelacionTipo? RelacionTipo { get; set; }

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
