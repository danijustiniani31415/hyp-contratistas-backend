using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;

public class SsConsumoLinea
{
    public long Id { get; set; }
    public int CargaId { get; set; }
    public int ProjectId { get; set; }
    public string RecursoCrudo { get; set; } = null!;
    /// <summary>Nro. Guía de origen (ERP). Null en consumo histórico cargado antes del soporte de carga acumulativa.</summary>
    public string? NroGuia { get; set; }
    /// <summary>Ej. "E PC", "E PCDD", "I OC/S". Solo se importan movimientos de egreso (E*).</summary>
    public string? Movimiento { get; set; }
    public string? PartidaControl { get; set; }
    /// <summary>Desambigua líneas con la misma (guía, recurso, fecha, movimiento) repetidas dentro de un mismo archivo.</summary>
    public int Ocurrencia { get; set; } = 1;
    /// <summary>False cuando la guía dejó de aparecer en una carga acumulada posterior (dada de baja/regularizada en el ERP).</summary>
    public bool Activo { get; set; } = true;
    public string? MotivoInactivo { get; set; }
    public DateTimeOffset? ActualizadoEn { get; set; }
    public int? ItemId { get; set; }
    /// <summary>Apunta al hito REAL del cronograma del proyecto (MilestoneSchedule), resuelto por fecha_guia. Null si el proyecto no tiene cronograma cargado o la fecha cae fuera de rango.</summary>
    public int? HitoId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal PrecioTotal { get; set; }
    /// <summary>Cantidad * FactorConversion del alias matcheado. Null hasta que la línea se estandariza.</summary>
    public decimal? CantidadReal { get; set; }
    /// <summary>PrecioTotal / CantidadReal. Null hasta que la línea se estandariza.</summary>
    public decimal? PrecioUnitarioReal { get; set; }
    public DateOnly FechaGuia { get; set; }
    public bool Estandarizado { get; set; } = false;
    // SEED | MANUAL | FUZZY_CONFIRMADO
    public string? MetodoMatch { get; set; }
    public decimal? ScoreMatch { get; set; }
    public bool PerteneceSsoma { get; set; } = true;
    // null | PENDIENTE | AUTORIZADO | RECHAZADO
    public string? EstadoRevision { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

    public SsConsumoCarga Carga { get; set; } = null!;
    public Project Proyecto { get; set; } = null!;
    public SsMaterialItem? Item { get; set; }
    public MilestoneSchedule? Hito { get; set; }
}
