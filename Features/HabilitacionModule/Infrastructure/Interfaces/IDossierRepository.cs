using Abril_Backend.Features.Habilitacion.Application.Dtos.Dossier;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;

public interface IDossierRepository
{
    Task<List<DossierSemanaDto>> GetSemanasAsync(int? contributorId, int? proyectoId, int? anio);
    Task<DossierSemanaDetalleDto?> GetDetalleAsync(int id);
    Task<(int Id, DateTime FechaInicio, DateTime FechaFin)> EnsureSemanaAsync(EnsureSemanaRequest req);
    Task SubirDocumentoAsync(int dossierId, string tipoDoc, string nombreArchivo, string archivoPath);
    Task MarcarNaAsync(int docId);
    Task EnviarAsync(int dossierId);
    Task RevisarAsync(int dossierId, RevisarDossierRequest req);
    Task RevisarDocumentoAsync(int docId, RevisarDocumentoRequest req);
    Task<string?> GetArchivoPathAsync(int docId);
    Task<(int ContributorId, int ProyectoId, int NumeroSemana, DateTime FechaInicio)?> GetDossierContextoAsync(int dossierId);
    Task MarcarSemanaNoAplicaAsync(int dossierId);
    Task EliminarArchivoAsync(int archivoId);
    Task RevertirABorradorAsync(int dossierId);
    Task<string?> GetArchivoPathByIdAsync(int archivoId);
    Task<int?> GetContributorIdDeDocumentoAsync(int docId);
    Task<int?> GetContributorIdDeArchivoAsync(int archivoId);
}
