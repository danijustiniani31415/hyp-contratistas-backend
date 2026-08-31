using Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos;
using Microsoft.AspNetCore.Http;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Application.Interfaces
{
    public interface IPlaneamientoBimCargaDiariaService
    {
        Task<CargaDiariaDto> GetCargaDiaria(int projectId, DateOnly fecha, string categoria = "GENERAL");
        Task GuardarCargaDiaria(int projectId, DateOnly fecha, CargaDiariaUpdateDto dto, int userId);
        Task<List<EvidenciaFotoDto>> SubirEvidencias(int projectId, DateOnly fecha, IFormFileCollection files, int userId, string categoria = "GENERAL");
    }
}
