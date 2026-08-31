namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;

/// <summary>
/// Receta fija (BOM) de un kit/equipo SSOMA que se compra completo (ej. un Botiquín, una Estación
/// de Emergencia): el usuario ingresa "cuántos kits" necesita el proyecto y el sistema multiplica
/// cada línea del BOM para obtener la cantidad inicial de cada material.
/// </summary>
public class SsKit
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public int TipoId { get; set; }
    public bool Activo { get; set; } = true;
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

    public SsMaterialTipo Tipo { get; set; } = null!;
    public ICollection<SsKitItem> Items { get; set; } = [];
}

public class SsKitItem
{
    public int Id { get; set; }
    public int KitId { get; set; }
    public int FamiliaId { get; set; }
    public decimal CantidadPorKit { get; set; }
    /// <summary>Si es true, además de la cantidad inicial (kit × cantidad), este ítem se repone
    /// periódicamente (consumibles: gasas, alcohol, guantes...). Si es false, es equipo durable
    /// (camilla, tijera, maletín...) que solo se compra una vez con el kit inicial.</summary>
    public bool EsConsumible { get; set; } = true;

    public SsKit Kit { get; set; } = null!;
    public SsMaterialFamilia Familia { get; set; } = null!;
}
