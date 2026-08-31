using Abril_Backend.Features.Evaluaciones.Application.Dtos;

namespace Abril_Backend.Features.Evaluaciones.Application.Services
{
    public interface IEvEvaluacionResidenteService
    {
        Task<EvEvaluacionResidenteResponseDto> CreateAsync(EvEvaluacionCreateDto dto, int evaluadorUserId);
        Task<EvEvaluacionResidenteResponseDto> UpdateAsync(int id, EvEvaluacionCreateDto dto, int evaluadorUserId);
        Task<List<EvEvaluacionResidenteResponseDto>> GetByEvaluadorAsync(int evaluadorUserId, int periodoId);
        Task<List<EvEvaluacionResidenteResponseDto>> GetByEvaluadoAsync(int evaluadoUserId, int periodoId);
    }
}
