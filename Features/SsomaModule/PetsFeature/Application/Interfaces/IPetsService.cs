using Abril_Backend.Features.SsomaModule.PetsFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.PetsFeature.Application.Interfaces;

public interface IPetsService
{
    Task<List<PetListItemDto>> GetListAsync();
    Task<PetDetalleDto> GetDetalleAsync(int id);
    Task<List<PetPasoDto>> GetPasosAsync(int petId);
    Task<int> CrearAsync(CrearPetRequest request);
    Task ActualizarAsync(int id, ActualizarPetRequest request);
    Task<int> AgregarPasoAsync(int petId, CrearPetPasoRequest request);
    Task ActualizarPasoAsync(int petId, int pasoId, ActualizarPetPasoRequest request);
    Task EliminarPasoAsync(int petId, int pasoId);
    Task ReordenarPasosAsync(int petId, ReordenarPasosRequest request);
    Task<string> SubirImagenPasoAsync(int petId, int pasoId, Stream fileStream, string fileName);
    Task DesactivarSeccionAsync(int petId, string seccion);
    Task UpsertSeccionTextoAsync(int petId, string seccion, string contenido);

    // Catálogo (Marco Legal / EPP / Recursos)
    Task<List<CatalogoItemDto>> GetCatalogoAsync(string grupo, string? tipo);
    Task<int> CrearCatalogoItemAsync(CrearCatalogoItemRequest request);
    Task DesactivarCatalogoItemAsync(int catalogoItemId);
    Task<int> SeleccionarCatalogoItemAsync(int petId, SeleccionarItemCatalogoRequest request);
    Task<int> AgregarItemPersonalizadoAsync(int petId, AgregarItemPersonalizadoRequest request);
    Task EliminarSeleccionAsync(int petId, int seleccionId);

    // Anexos
    Task<string> SubirAnexoAsync(int petId, string nombre, Stream fileStream, string fileName);
    Task EliminarAnexoAsync(int petId, int anexoId);

    // Firmas
    Task UpsertFirmaAsync(int petId, string rol, string? nombre, string? cargo, DateOnly? fecha);
    Task<string> SubirFirmaAsync(int petId, string rol, Stream fileStream, string fileName);

    // Exportar
    Task<byte[]> ExportarPdfAsync(int petId);
}
