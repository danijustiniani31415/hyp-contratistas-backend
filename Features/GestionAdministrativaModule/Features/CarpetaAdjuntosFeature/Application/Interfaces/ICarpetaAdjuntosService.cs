using Abril_Backend.Features.GestionAdministrativa.CarpetaAdjuntos.Application.Dtos;

namespace Abril_Backend.Features.GestionAdministrativa.CarpetaAdjuntos.Application.Interfaces
{
    public interface ICarpetaAdjuntosService
    {
        Task<GaAdjuntoFolderDto?> GetSingleton();
        Task<GaAdjuntoFolderDto> Save(GaAdjuntoFolderSaveDto dto, int userId);
    }
}
