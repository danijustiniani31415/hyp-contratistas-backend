using Abril_Backend.Features.Evaluaciones.Application.Dtos;
using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;

namespace Abril_Backend.Features.Evaluaciones.Application.Interfaces
{
    public interface IEvEvaluacionResidenteRepository
    {
        Task<EvEvaluacionResidente> CreateAsync(EvEvaluacionResidente eval, List<EvEvaluacionResidenteDetalle> detalles);
        Task<EvEvaluacionResidente?> GetByIdAsync(int id);
        Task<EvEvaluacionResidente> UpdateAsync(int id, decimal nota, string? comentario, bool noAplica, string? noAplicaMotivo, List<EvEvaluacionResidenteDetalle> detalles);
        Task<List<EvEvaluacionResidenteResponseDto>> GetByPeriodoAsync(int periodoId);
        Task<List<EvEvaluacionResidenteResponseDto>> GetByEvaluadorAsync(int evaluadorUserId, int periodoId);
        Task<List<EvEvaluacionResidenteResponseDto>> GetByEvaluadoAsync(int evaluadoUserId, int periodoId);
        Task<bool> ExisteAsync(int periodoId, int evaluadorUserId, int evaluadoUserId, string areaNombre);
        Task<EvEvaluacionResidenteResponseDto?> GetDetalleAsync(int id);
        Task<List<ResidenteEvaluableDto>> GetResidentesEvaluablesAsync(int evaluadorUserId);
        Task<string?> GetMiSubareaAsync(int userId);
    }
}
