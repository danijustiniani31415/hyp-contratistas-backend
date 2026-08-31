using Abril_Backend.Features.Habilitacion.Application.Dtos.Dossier;
using Microsoft.AspNetCore.Http;

namespace Abril_Backend.Features.Habilitacion.Application.Interfaces;

public interface IDossierService
{
    Task<List<DossierSemanaDto>> GetSemanasAsync(int? contributorId, int? proyectoId, int? anio);
    Task<DossierSemanaDetalleDto> GetDetalleAsync(int id);
    Task<object> EnsureSemanaAsync(EnsureSemanaRequest req);
    Task SubirDocumentoAsync(int dossierId, string tipoDoc, IFormFile file);
    Task MarcarNaAsync(int docId);
    Task EnviarAsync(int dossierId);
    Task RevisarAsync(int dossierId, RevisarDossierRequest req);
    Task RevisarDocumentoAsync(int docId, RevisarDocumentoRequest req);
    Task<string> GetDocumentoUrlAsync(int docId);
    Task MarcarSemanaNoAplicaAsync(int dossierId);
    Task EliminarArchivoAsync(int archivoId);
    Task<string> GetArchivoUrlAsync(int archivoId);
    Task<int?> GetContributorIdDeDocumentoAsync(int docId);
    Task<int?> GetContributorIdDeArchivoAsync(int archivoId);
}
