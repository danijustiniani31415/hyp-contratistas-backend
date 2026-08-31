using Abril_Backend.Features.BoletinModule.BirthdayClubFeature.Application.Dtos;

namespace Abril_Backend.Features.BoletinModule.BirthdayClubFeature.Application.Interfaces
{
    public interface ICumpleanosService
    {
        /// <summary>
        /// Devuelve los cumpleañeros del trimestre (1-4) SIN foto. Las fotos se resuelven
        /// aparte, bajo demanda (hover), vía <see cref="GetFoto"/> para no cargar todas de golpe.
        /// </summary>
        Task<TrimestreCumpleanosDto> GetTrimestre(int trimestre);

        /// <summary>
        /// Devuelve la foto de perfil (data URI base64) del correo indicado, o null si Graph
        /// no tiene foto para ese usuario. Resuelve una sola foto a demanda al hacer hover.
        /// </summary>
        Task<string?> GetFoto(string email);
    }
}
