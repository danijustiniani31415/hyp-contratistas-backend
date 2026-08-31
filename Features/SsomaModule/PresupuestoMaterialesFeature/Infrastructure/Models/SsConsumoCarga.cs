using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;

public class SsConsumoCarga
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string NombreArchivo { get; set; } = null!;
    public string HashArchivo { get; set; } = null!;
    public DateOnly FechaMin { get; set; }
    public DateOnly FechaMax { get; set; }
    public int TotalLineas { get; set; }
    public int LineasEstandarizadas { get; set; }
    public int LineasPendientes { get; set; }
    public int LineasNuevas { get; set; }
    public int LineasActualizadas { get; set; }
    public int LineasEliminadas { get; set; }
    // ACTIVA | ANULADA | REEMPLAZADA
    public string Estado { get; set; } = "ACTIVA";
    public int SubidoPor { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

    public Project Proyecto { get; set; } = null!;
    public ICollection<SsConsumoLinea> Lineas { get; set; } = [];
}
