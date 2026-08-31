using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;

/// <summary>Historial de cargas del Excel semanal de HH (planilla/Tareo), una fila por subida.</summary>
public class SsHhCarga
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string NombreArchivo { get; set; } = null!;
    public string HashArchivo { get; set; } = null!;
    public int AnioMin { get; set; }
    public int SemanaMin { get; set; }
    public int AnioMax { get; set; }
    public int SemanaMax { get; set; }
    public int TotalLineas { get; set; }
    public int LineasNuevas { get; set; }
    public int LineasActualizadas { get; set; }
    public int LineasEliminadas { get; set; }
    public string Estado { get; set; } = "ACTIVA";
    public int SubidoPor { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

    public Project Proyecto { get; set; } = null!;
    public ICollection<SsHhCargaLinea> Lineas { get; set; } = [];
}
