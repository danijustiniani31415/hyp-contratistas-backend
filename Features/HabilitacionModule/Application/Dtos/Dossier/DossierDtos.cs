using Microsoft.AspNetCore.Http;

namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Dossier;

public record DossierArchivoDto(int Id, string NombreArchivo, string ArchivoPath, DateTime CreatedAt);

public record DossierDocumentoDto(
    int Id,
    int DossierId,
    string TipoDoc,
    string? NombreArchivo,
    string? ArchivoPath,
    string Estado,
    string? ObsRevisor,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<DossierArchivoDto> Archivos);

public record DossierSemanaDto(
    int Id,
    int ContributorId,
    string? EmpresaNombre,
    int ProyectoId,
    string? ProyectoNombre,
    int Anio,
    int NumeroSemana,
    DateTime FechaInicio,
    DateTime FechaFin,
    string Estado,
    string? ObsRevisor,
    DateTime CreatedAt,
    int TotalDocs,
    int DocsSubidos,
    int DocsNa,
    int DocsAprobados);

public record DossierSemanaDetalleDto(
    int Id,
    int ContributorId,
    int ProyectoId,
    int Anio,
    int NumeroSemana,
    DateTime FechaInicio,
    DateTime FechaFin,
    string Estado,
    string? ObsRevisor,
    DateTime CreatedAt,
    int TotalDocs,
    int DocsSubidos,
    int DocsNa,
    int DocsAprobados,
    List<DossierDocumentoDto> Documentos);

public record EnsureSemanaRequest(int ContributorId, int ProyectoId, int NumeroSemana, int Anio);

public record RevisarDossierRequest(string Estado, string? ObsRevisor);

public record RevisarDocumentoRequest(string Estado, string? ObsRevisor);

public class SubirDocumentoDossierRequest
{
    public IFormFile? File { get; set; }
    public string TipoDoc { get; set; } = "";
}
