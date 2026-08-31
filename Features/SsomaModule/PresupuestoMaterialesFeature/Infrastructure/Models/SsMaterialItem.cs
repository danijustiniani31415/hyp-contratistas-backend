namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;

public class SsMaterialItem
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string NombreNormalizado { get; set; } = null!;
    public int FamiliaId { get; set; }
    public string? Talla { get; set; }
    public string? DimensionNorm { get; set; }
    public bool NoUsar { get; set; } = false;
    public bool Activo { get; set; } = true;
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

    public SsMaterialFamilia Familia { get; set; } = null!;
    public ICollection<SsMaterialAlias> Aliases { get; set; } = [];
}
