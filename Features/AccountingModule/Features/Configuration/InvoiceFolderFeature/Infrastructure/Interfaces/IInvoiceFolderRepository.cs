using Abril_Backend.Features.AccountingModule.Features.Configuration.InvoiceFolderFeature.Application.Dtos;

namespace Abril_Backend.Features.AccountingModule.Features.Configuration.InvoiceFolderFeature.Infrastructure.Interfaces
{
    public interface IInvoiceFolderRepository
    {
        /// <summary>Devuelve la carpeta única vigente (state = true) o null si aún no se configuró.</summary>
        Task<InvoiceFolderDto?> GetSingleton();

        /// <summary>
        /// Crea o actualiza (upsert) la carpeta única con la ubicación resuelta. Si ya existe una
        /// fila vigente la actualiza; si no, la inserta.
        /// </summary>
        Task Upsert(string linkUrl, string driveId, string folderId, string? folderName, string? webUrl, int userId);

        /// <summary>Destino de subida vigente (id + driveId + folderId) o null si no hay carpeta configurada.</summary>
        Task<(int Id, string DriveId, string FolderId)?> GetActiveDestination();
    }
}
