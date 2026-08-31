namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;

public class SsMaterialAlias
{
    public int Id { get; set; }
    public string TextoCrudo { get; set; } = null!;
    public string TextoCrudoNorm { get; set; } = null!;
    /// <summary>Null = alias de rechazo: este texto no pertenece a SSOMA, no mapea a ningún ítem.</summary>
    public int? ItemId { get; set; }
    // SEED | MANUAL | FUZZY_CONFIRMADO | RECHAZO_CONFIRMADO
    public string Origen { get; set; } = null!;
    public decimal? Confianza { get; set; }
    /// <summary>Unidades reales contenidas en 1 "cantidad" reportada en el S10 (ej. 100 para "GUANTES x100 UN"). Default 1.</summary>
    public decimal FactorConversion { get; set; } = 1;
    public int? ConfirmadoPor { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

    public SsMaterialItem? Item { get; set; }
}
