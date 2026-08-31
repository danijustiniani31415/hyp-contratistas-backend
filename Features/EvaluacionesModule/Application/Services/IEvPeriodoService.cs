using Abril_Backend.Features.Evaluaciones.Application.Dtos;
using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;

namespace Abril_Backend.Features.Evaluaciones.Application.Services
{
    public interface IEvPeriodoService
    {
        Task<EvPeriodoDto?> GetActivoAsync();
        Task<List<EvPeriodoDto>> GetAllAsync();
        Task<EvPeriodo> CreateAsync(EvPeriodoCreateDto dto);
        Task ActivarAsync(int id);
    }
}
