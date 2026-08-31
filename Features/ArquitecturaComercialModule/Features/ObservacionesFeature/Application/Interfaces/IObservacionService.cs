using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Dtos;

namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Interfaces;

public interface IObservacionService
{
    Task<ObservacionListResponseDTO> GetObservaciones(int? proyectoId, string? estado, string? partida, DateTime? desde, DateTime? hasta, string? search, int pagina, int porPagina);
    Task<ObservacionListItemDTO?> GetObservacionById(int id);
    Task<ObservacionFiltrosDTO> GetFiltros();
    Task<ObservacionDashboardDTO> GetDashboard(DateTime? desde, DateTime? hasta, int? proyectoId);
    Task<ObservacionStatsDTO> GetStats(DateTime? desde, DateTime? hasta, int? proyectoId);
    Task<ObservacionListItemDTO> CreateObservacion(CreateObservacionDTO body, Stream? fotoStream, string? fotoFileName);
    Task<ObservacionListItemDTO?> LevantarObservacion(int id, Stream? fotoStream, string? fotoFileName, LevantarObservacionDTO body);
    Task<ObservacionListItemDTO?> UpdateObservacion(int id, UpdateObservacionDTO body);
    Task<string> AgregarFotoObservacion(int observacionId, Stream fotoStream, string fotoFileName);
    Task<string> ReemplazarFoto(int fotoId, Stream fotoStream, string fotoFileName);
    Task<(byte[] Bytes, string ContentType)?> GetFotoContenido(int fotoId);
}
