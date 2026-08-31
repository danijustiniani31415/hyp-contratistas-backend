using Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Application.Dtos;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Infrastructure.Models;

namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Infrastructure.Interfaces;

public interface IRevisionRepository
{
    Task<List<RevisionDTO>> GetRevisiones(int? proyectoId, bool soloActivas);
    Task<string?> GetProyectoNombre(int proyectoId);
    Task<AcRevision?> GetRevisionEntityById(int id);
    Task<AcRevision> CreateRevision(int proyectoId, string tipo, string lugar, string nombre);
    Task<bool> DeleteRevision(int id);

    Task<RevisionObservacionListResponseDTO> GetObservaciones(int? revisionId, int? proyectoId, string? estado, string? partida, DateTime? desde, DateTime? hasta, string? search, int pagina, int porPagina);
    Task<RevisionObservacionListItemDTO?> GetObservacionById(int id);
    Task<RevisionFiltrosDTO> GetFiltros();
    Task<RevisionDashboardDTO> GetDashboard(DateTime? desde, DateTime? hasta, int? proyectoId);
    Task<RevisionObservacionStatsDTO> GetStats(DateTime? desde, DateTime? hasta, int? proyectoId);
    Task<AcRevisionObservacion> CreateObservacion(CreateRevisionObservacionDTO body);
    Task<AcRevisionObservacionFoto> AgregarFoto(int revisionObservacionId, string tipo, string url, int orden);
    Task<AcRevisionObservacionFoto?> GetFotoById(int fotoId);
    Task ActualizarFoto(int fotoId, string url);
    Task<RevisionObservacionListItemDTO?> LevantarObservacion(int id, int? levantaPorWorkerId);
    Task<RevisionObservacionListItemDTO?> UpdateObservacion(int id, UpdateRevisionObservacionDTO body);
}
