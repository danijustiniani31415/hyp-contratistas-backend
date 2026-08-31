namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models;

public class SsDossierDocumento
{
    public int Id { get; set; }
    public int DossierId { get; set; }
    public string TipoDoc { get; set; } = "";
    public string? NombreArchivo { get; set; }
    public string? ArchivoPath { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public string? ObsRevisor { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public SsDossierSemana? Dossier { get; set; }
    public ICollection<SsDossierDocumentoArchivo> Archivos { get; set; } = [];
}
