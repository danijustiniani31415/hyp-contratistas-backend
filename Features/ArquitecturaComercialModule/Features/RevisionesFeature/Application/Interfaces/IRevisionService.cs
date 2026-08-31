using Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Application.Dtos;

namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Application.Interfaces;

public interface IRevisionService
{
    Task<List<RevisionDTO>> GetRevisiones(int? proyectoId, bool soloActivas);
    Task<RevisionDTO> CreateRevision(CreateRevisionDTO body);
    Task<bool> DeleteRevision(int id);

    Task<RevisionObservacionListResponseDTO> GetObservaciones(int? revisionId, int? proyectoId, string? estado, string? partida, DateTime? desde, DateTime? hasta, string? search, int pagina, int porPagina);
    Task<RevisionObservacionListItemDTO?> GetObservacionById(int id);
    Task<RevisionFiltrosDTO> GetFiltros();
    Task<RevisionDashboardDTO> GetDashboard(DateTime? desde, DateTime? hasta, int? proyectoId);
    Task<RevisionObservacionStatsDTO> GetStats(DateTime? desde, DateTime? hasta, int? proyectoId);
    Task<RevisionObservacionListItemDTO> CreateObservacion(CreateRevisionObservacionDTO body, Stream? fotoStream, string? fotoFileName);
    Task<RevisionObservacionListItemDTO?> LevantarObservacion(int id, Stream? fotoStream, string? fotoFileName, LevantarRevisionObservacionDTO body);
    Task<RevisionObservacionListItemDTO?> UpdateObservacion(int id, UpdateRevisionObservacionDTO body);
    Task<string> AgregarFotoObservacion(int revisionObservacionId, Stream fotoStream, string fotoFileName);
    Task<string> ReemplazarFoto(int fotoId, Stream fotoStream, string fotoFileName);
    Task<(byte[] Bytes, string ContentType)?> GetFotoContenido(int fotoId);
}
