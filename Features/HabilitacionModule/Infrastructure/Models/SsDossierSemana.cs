namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models;

public class SsDossierSemana
{
    public int Id { get; set; }
    public int ContributorId { get; set; }
    public int ProyectoId { get; set; }
    public int Anio { get; set; }
    public int NumeroSemana { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Estado { get; set; } = "Borrador";
    public string? ObsRevisor { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<SsDossierDocumento> Documentos { get; set; } = [];
}
