namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

public class ImportHhResultDto
{
    public int CargaId { get; set; }
    public string NombreArchivo { get; set; } = null!;
    public int TotalLineas { get; set; }
    public int LineasNuevas { get; set; }
    public int LineasActualizadas { get; set; }
    public int LineasEliminadas { get; set; }
    public int LineasSinCambio { get; set; }
    public decimal HorasLaboradasTotales { get; set; }
    public string Estado { get; set; } = null!;
    public List<string> Advertencias { get; set; } = [];
}

public class HhCargaResumenDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string NombreArchivo { get; set; } = null!;
    public int AnioMin { get; set; }
    public int SemanaMin { get; set; }
    public int AnioMax { get; set; }
    public int SemanaMax { get; set; }
    public int TotalLineas { get; set; }
    public int LineasNuevas { get; set; }
    public int LineasActualizadas { get; set; }
    public int LineasEliminadas { get; set; }
    public string Estado { get; set; } = null!;
    public DateTimeOffset CreadoEn { get; set; }
}
