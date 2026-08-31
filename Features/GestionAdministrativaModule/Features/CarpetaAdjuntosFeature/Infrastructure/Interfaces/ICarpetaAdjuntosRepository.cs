using Abril_Backend.Features.GestionAdministrativa.CarpetaAdjuntos.Application.Dtos;

namespace Abril_Backend.Features.GestionAdministrativa.CarpetaAdjuntos.Infrastructure.Interfaces
{
    public interface ICarpetaAdjuntosRepository
    {
        Task<GaAdjuntoFolderDto?> GetSingleton();
        Task Upsert(string linkUrl, string driveId, string folderId, string? folderName, string? webUrl, int userId);
    }
}
