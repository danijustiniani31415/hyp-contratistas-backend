using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Dtos;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Infrastructure.Models;

namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Infrastructure.Interfaces;

public interface IObservacionRepository
{
    Task<ObservacionListResponseDTO> GetObservaciones(int? proyectoId, string? estado, string? partida, DateTime? desde, DateTime? hasta, string? search, int pagina, int porPagina);
    Task<ObservacionListItemDTO?> GetObservacionById(int id);
    Task<ObservacionFiltrosDTO> GetFiltros();
    Task<ObservacionDashboardDTO> GetDashboard(DateTime? desde, DateTime? hasta, int? proyectoId);
    Task<ObservacionStatsDTO> GetStats(DateTime? desde, DateTime? hasta, int? proyectoId);
    Task<AcObservacion> CreateObservacion(CreateObservacionDTO body, string codigo);
    Task<AcObservacionFoto> AgregarFoto(int observacionId, string tipo, string url, int orden);
    Task<AcObservacionFoto?> GetFotoById(int fotoId);
    Task ActualizarFoto(int fotoId, string url);
    Task<ObservacionListItemDTO?> LevantarObservacion(int id, int? levantaPorWorkerId);
    Task<int> GetProximoCorrelativo(string prefijoProyecto, int anio);
    Task<string> GetProyectoAbbreviation(int proyectoId);
    Task<ObservacionListItemDTO?> UpdateObservacion(int id, UpdateObservacionDTO body);
}
