using Abril_Backend.Features.Habilitacion.Application.Dtos.ControlAcceso;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces
{
    public interface IControlAccesoRepository
    {
        Task<List<ControlAccesoWorkerDto>> GetConsultaAsync(string? search, int? proyectoId);
        Task<List<ControlAccesoWorkerDto>> GetNoAutorizadosAsync(int proyectoId, string? estadoHabilitacion);
        Task<List<ControlAccesoWorkerDto>> GetOficinaCentralAsync(int? proyectoId);
        Task<List<InduccionHoyDto>> GetInduccionesHoyAsync();
        Task ConfirmarIngresoAsync(int induccionId);
        Task<List<TareoPartidaDto>> GetPartidasAsync();
        Task<List<TareoEmpresaDto>> GetEmpresasContratistasByProyectoAsync(int proyectoId);
        Task<TareoDto?> GetTareoAsync(int proyectoId, DateOnly fecha);
        Task<TareoDto> CreateTareoAsync(TareoCreateDto dto, int? userId);
        Task<TareoDto> UpdateTareoAsync(int id, TareoCreateDto dto);
    }
}
