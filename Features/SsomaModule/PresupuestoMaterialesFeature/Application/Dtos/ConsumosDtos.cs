namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

// ─── Import S10 ───────────────────────────────────────────────────────────────

public class ImportConsumoResultDto
{
    public int CargaId { get; set; }
    public string NombreArchivo { get; set; } = null!;
    public int TotalLineas { get; set; }
    public int LineasNuevas { get; set; }
    public int LineasActualizadas { get; set; }
    public int LineasEliminadas { get; set; }
    public int LineasSinCambio { get; set; }
    public int LineasEstandarizadas { get; set; }
    public int LineasAutoRechazadas { get; set; }
    public int LineasPendientes { get; set; }
    public int LineasSinMatch { get; set; }
    public string Estado { get; set; } = null!;
    public List<string> Advertencias { get; set; } = [];
}

public class ConsumoCargaResumenDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string NombreArchivo { get; set; } = null!;
    public DateOnly FechaMin { get; set; }
    public DateOnly FechaMax { get; set; }
    public int TotalLineas { get; set; }
    public int LineasNuevas { get; set; }
    public int LineasActualizadas { get; set; }
    public int LineasEliminadas { get; set; }
    public int LineasEstandarizadas { get; set; }
    public int LineasPendientes { get; set; }
    public string Estado { get; set; } = null!;
    public DateTimeOffset CreadoEn { get; set; }
    public double PorcentajeEstandarizado => TotalLineas == 0 ? 0 : Math.Round((double)LineasEstandarizadas / TotalLineas * 100, 1);
}

// ─── Revisión de materiales ───────────────────────────────────────────────────

public class MaterialPendienteDto
{
    public long LineaId { get; set; }
    public string RecursoCrudo { get; set; } = null!;
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal PrecioTotal { get; set; }
    public DateOnly FechaGuia { get; set; }
    public string? NombreItemSugerido { get; set; }
    public string? NombreFamiliaSugerida { get; set; }
    public decimal? ScoreMatch { get; set; }
    public string? MetodoMatch { get; set; }
    public string? EstadoRevision { get; set; }
    public int? ItemIdSugerido { get; set; }
}

public class RevisionDecisionDto
{
    public long LineaId { get; set; }
    /// <summary>AUTORIZADO o RECHAZADO</summary>
    public string Decision { get; set; } = null!;
    /// <summary>Si AUTORIZADO: id del item confirmado (puede ser diferente al sugerido)</summary>
    public int? ItemIdConfirmado { get; set; }
    /// <summary>Si RECHAZADO: motivo opcional para notificar a Oficina Técnica</summary>
    public string? MotivoRechazo { get; set; }
}

public class RevisionLoteDto
{
    public List<RevisionDecisionDto> Decisiones { get; set; } = [];
    public int ProjectId { get; set; }
}

public class RevisionResultDto
{
    public int Autorizados { get; set; }
    public int Rechazados { get; set; }
    public int NotificacionesEnviadas { get; set; }
    /// <summary>Otras líneas pendientes (cualquier proyecto) con el mismo texto crudo exacto que se
    /// resolvieron solas al aplicar la misma decisión que el usuario ya confirmó para ese texto.</summary>
    public int AplicadosRetroactivamente { get; set; }
    public List<string> Errores { get; set; } = [];
}

// ─── Búsqueda en catálogo para UI de revisión ─────────────────────────────────

public class BuscarItemDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string NombreFamilia { get; set; } = null!;
    public string TipoMaterial { get; set; } = null!;
    public bool PerteneceSsoma { get; set; }
}
