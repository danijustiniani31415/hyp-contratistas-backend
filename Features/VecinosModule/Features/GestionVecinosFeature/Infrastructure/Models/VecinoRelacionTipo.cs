namespace Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Infrastructure.Models
{
    /// <summary>Catálogo del vínculo de una persona con la casa/lote (Propietario, Inquilino, Otro).</summary>
    public class VecinoRelacionTipo
    {
        public int VecinoRelacionTipoId { get; set; }
        public string Descripcion { get; set; } = null!;
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
