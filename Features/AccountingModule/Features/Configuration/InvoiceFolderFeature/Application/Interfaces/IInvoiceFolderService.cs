using Abril_Backend.Features.AccountingModule.Features.Configuration.InvoiceFolderFeature.Application.Dtos;

namespace Abril_Backend.Features.AccountingModule.Features.Configuration.InvoiceFolderFeature.Application.Interfaces
{
    public interface IInvoiceFolderService
    {
        /// <summary>Devuelve la carpeta única configurada (o null si aún no se configuró).</summary>
        Task<InvoiceFolderDto?> GetSingleton();

        /// <summary>
        /// Valida el link (debe pertenecer al tenant y apuntar a una carpeta), lo resuelve a su
        /// ubicación estable y guarda/actualiza la carpeta única. Devuelve la carpeta resultante.
        /// </summary>
        Task<InvoiceFolderDto> Save(InvoiceFolderSaveDto dto, int userId);
    }
}
