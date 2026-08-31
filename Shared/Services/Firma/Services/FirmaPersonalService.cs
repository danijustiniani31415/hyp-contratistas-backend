using Abril_Backend.Application.Exceptions;
using Abril_Backend.Shared.Helpers;
using Abril_Backend.Shared.Services.Firma.Dtos;
using Abril_Backend.Shared.Services.Firma.Interfaces;

namespace Abril_Backend.Shared.Services.Firma.Services
{
    public class FirmaPersonalService : IFirmaPersonalService
    {
        private readonly IFirmaPersonalRepository _repository;

        public FirmaPersonalService(IFirmaPersonalRepository repository)
        {
            _repository = repository;
        }

        public Task<FirmaPersonalDto?> Get(int userId) => _repository.GetByUserId(userId);

        public async Task<FirmaPersonalDto> Save(FirmaPersonalSaveDto dto, int userId)
        {
            // Mismas reglas para todos: las tres pantallas que registran firma (Contabilidad,
            // Onboarding y Gestión Administrativa) escriben las mismas columnas person.signature_*
            // y el mismo helper estampa el PDF, así que lo que se acepta acá tiene que ser
            // estampable en cualquiera de los tres.
            var bytes = FirmaImagenHelper.DecodePng(dto?.ImageBase64);

            await _repository.Upsert(userId, bytes, FirmaImagenHelper.Mime);

            return await _repository.GetByUserId(userId)
                ?? throw new AbrilException("No se pudo guardar la firma.", 500);
        }
    }
}
