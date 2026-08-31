namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;

public class SsMaterialFamilia
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string NombreNormalizado { get; set; } = null!;
    public int TipoId { get; set; }
    // HH | AREATECHADA | TRABAJADORES | CALCULADO | FIJO | METRADO
    public string VariableBase { get; set; } = null!;
    public bool PerteneceSsoma { get; set; } = true;
    public string? UnidadMedida { get; set; }
    public bool Activo { get; set; } = true;
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ActualizadoEn { get; set; }

    public SsMaterialTipo Tipo { get; set; } = null!;
    public ICollection<SsMaterialItem> Items { get; set; } = [];
}
