namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;

public class SsMaterialTipo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public bool Activo { get; set; } = true;

    public ICollection<SsMaterialFamilia> Familias { get; set; } = [];
}
