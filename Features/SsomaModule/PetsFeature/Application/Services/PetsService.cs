using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.PetsFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.PetsFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.PetsFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Interfaces;

namespace Abril_Backend.Features.SsomaModule.PetsFeature.Application.Services;

public class PetsService : IPetsService
{
    private const string ContainerName = "ssoma-pets-pasos";

    private readonly IPetsRepository _repo;
    private readonly IFileStorageService _storage;

    public PetsService(IPetsRepository repo, IFileStorageService storage)
    {
        _repo = repo;
        _storage = storage;
    }

    public Task<List<PetListItemDto>> GetListAsync() => _repo.GetListAsync();

    public async Task<PetDetalleDto> GetDetalleAsync(int id)
        => await _repo.GetDetalleAsync(id) ?? throw new AbrilException("PETS no encontrado.", 404);

    public Task<List<PetPasoDto>> GetPasosAsync(int petId) => _repo.GetPasosAsync(petId);

    public Task<int> CrearAsync(CrearPetRequest request) => _repo.CrearAsync(request);

    public Task ActualizarAsync(int id, ActualizarPetRequest request) => _repo.ActualizarAsync(id, request);

    public Task<int> AgregarPasoAsync(int petId, CrearPetPasoRequest request) => _repo.AgregarPasoAsync(petId, request);

    public Task ActualizarPasoAsync(int petId, int pasoId, ActualizarPetPasoRequest request)
        => _repo.ActualizarPasoAsync(petId, pasoId, request);

    public Task EliminarPasoAsync(int petId, int pasoId) => _repo.EliminarPasoAsync(petId, pasoId);

    public Task ReordenarPasosAsync(int petId, ReordenarPasosRequest request) => _repo.ReordenarPasosAsync(petId, request);

    public Task DesactivarSeccionAsync(int petId, string seccion) => _repo.DesactivarSeccionAsync(petId, seccion);

    public Task UpsertSeccionTextoAsync(int petId, string seccion, string contenido)
        => _repo.UpsertSeccionTextoAsync(petId, seccion, contenido);

    public async Task<string> SubirImagenPasoAsync(int petId, int pasoId, Stream fileStream, string fileName)
    {
        var urls = await _storage.UploadFilesAsync([(fileStream, fileName)], ContainerName);
        var url = urls.FirstOrDefault()
            ?? throw new AbrilException("No se pudo subir la imagen.", 500);

        await _repo.SetImagenPasoAsync(petId, pasoId, url);
        return url;
    }

    public Task<List<CatalogoItemDto>> GetCatalogoAsync(string grupo, string? tipo) => _repo.GetCatalogoAsync(grupo, tipo);

    public Task<int> CrearCatalogoItemAsync(CrearCatalogoItemRequest request) => _repo.CrearCatalogoItemAsync(request);

    public Task DesactivarCatalogoItemAsync(int catalogoItemId) => _repo.DesactivarCatalogoItemAsync(catalogoItemId);

    public Task<int> SeleccionarCatalogoItemAsync(int petId, SeleccionarItemCatalogoRequest request)
        => _repo.SeleccionarCatalogoItemAsync(petId, request);

    public Task<int> AgregarItemPersonalizadoAsync(int petId, AgregarItemPersonalizadoRequest request)
        => _repo.AgregarItemPersonalizadoAsync(petId, request);

    public Task EliminarSeleccionAsync(int petId, int seleccionId) => _repo.EliminarSeleccionAsync(petId, seleccionId);

    private const string ContainerNameAnexos = "ssoma-pets-anexos";

    public async Task<string> SubirAnexoAsync(int petId, string nombre, Stream fileStream, string fileName)
    {
        var urls = await _storage.UploadFilesAsync([(fileStream, fileName)], ContainerNameAnexos);
        var url = urls.FirstOrDefault()
            ?? throw new AbrilException("No se pudo subir el anexo.", 500);

        await _repo.AgregarAnexoAsync(petId, nombre, url);
        return url;
    }

    public Task EliminarAnexoAsync(int petId, int anexoId) => _repo.EliminarAnexoAsync(petId, anexoId);

    public Task UpsertFirmaAsync(int petId, string rol, string? nombre, string? cargo, DateOnly? fecha)
        => _repo.UpsertFirmaAsync(petId, rol, nombre, cargo, fecha);

    private const string ContainerNameFirmas = "ssoma-pets-firmas";

    public async Task<string> SubirFirmaAsync(int petId, string rol, Stream fileStream, string fileName)
    {
        var urls = await _storage.UploadFilesAsync([(fileStream, fileName)], ContainerNameFirmas);
        var url = urls.FirstOrDefault()
            ?? throw new AbrilException("No se pudo subir la firma.", 500);

        await _repo.SetFirmaUrlAsync(petId, rol, url);
        return url;
    }

    public async Task<byte[]> ExportarPdfAsync(int petId)
    {
        var pet = await GetDetalleAsync(petId);
        return await PetsPdfService.GenerarPdfAsync(pet);
    }
}
