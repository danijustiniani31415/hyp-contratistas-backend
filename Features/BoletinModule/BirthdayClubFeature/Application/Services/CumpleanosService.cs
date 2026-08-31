using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.BoletinModule.BirthdayClubFeature.Application.Dtos;
using Abril_Backend.Features.BoletinModule.BirthdayClubFeature.Application.Interfaces;
using Abril_Backend.Features.BoletinModule.BirthdayClubFeature.Infrastructure.Interfaces;
using Abril_Backend.Shared.Services.Graph.Interfaces;

namespace Abril_Backend.Features.BoletinModule.BirthdayClubFeature.Application.Services
{
    public class CumpleanosService : ICumpleanosService
    {
        private readonly ICumpleanosRepository _repo;
        private readonly IUserPhotoService _photoService;

        public CumpleanosService(ICumpleanosRepository repo, IUserPhotoService photoService)
        {
            _repo = repo;
            _photoService = photoService;
        }

        public async Task<TrimestreCumpleanosDto> GetTrimestre(int trimestre)
        {
            if (trimestre < 1 || trimestre > 4)
                throw new AbrilException("El trimestre debe estar entre 1 y 4.", 400);

            // Solo datos: las fotos se traen aparte, bajo demanda (hover), vía GetFoto.
            // Traerlas todas aquí era lo que hacía que la carga del trimestre tardara demasiado.
            var cumpleaneros = await _repo.GetCumpleaneros(trimestre);

            return new TrimestreCumpleanosDto
            {
                Trimestre = trimestre,
                Cumpleaneros = cumpleaneros,
            };
        }

        public async Task<string?> GetFoto(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new AbrilException("El correo es obligatorio.", 400);

            // Reutiliza el batch de Graph con un único correo → devuelve solo esa foto.
            var fotos = await _photoService.GetPhotosByEmailsAsync(new List<string> { email });
            return fotos.TryGetValue(email, out var foto) ? foto : null;
        }
    }
}
