namespace Abril_Backend.Features.Habilitacion.Infrastructure.Models;

public class SsDossierDocumentoArchivo
{
    public int Id { get; set; }
    public int DocumentoId { get; set; }
    public string NombreArchivo { get; set; } = "";
    public string ArchivoPath { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public SsDossierDocumento Documento { get; set; } = null!;
}
