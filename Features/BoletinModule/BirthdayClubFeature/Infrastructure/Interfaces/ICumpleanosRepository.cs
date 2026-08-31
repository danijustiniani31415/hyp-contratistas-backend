using Abril_Backend.Features.BoletinModule.BirthdayClubFeature.Application.Dtos;

namespace Abril_Backend.Features.BoletinModule.BirthdayClubFeature.Infrastructure.Interfaces
{
    public interface ICumpleanosRepository
    {
        /// <summary>
        /// Devuelve los cumpleañeros cuyo cumpleaños cae dentro de los meses del trimestre
        /// indicado (1-4), sin foto. Solo trabajadores con email_corporativo @abril.pe y una
        /// persona relacionada. El cumpleaños usa person.cumpleanos con fallback a
        /// workers.fecha_nacimiento.
        /// </summary>
        Task<List<CumpleaneroDto>> GetCumpleaneros(int trimestre);
    }
}
