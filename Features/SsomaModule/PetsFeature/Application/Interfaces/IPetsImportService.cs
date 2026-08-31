using Abril_Backend.Features.SsomaModule.PetsFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.PetsFeature.Application.Interfaces;

public interface IPetsImportService
{
    PetsImportPreviewDto PreviewDesdeDocx(Stream docxStream);
    Task ConfirmarImportacionAsync(int petId, ConfirmarImportacionRequest request);
}
