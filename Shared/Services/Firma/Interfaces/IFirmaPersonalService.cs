using Abril_Backend.Shared.Services.Firma.Dtos;

namespace Abril_Backend.Shared.Services.Firma.Interfaces
{
    /// <summary>
    /// Registro de la firma del usuario actual. Una persona tiene UNA firma
    /// (<c>person.signature_*</c>) y se registra desde cualquiera de las pantallas de configuración
    /// que la ofrecen: la firma que se guarda en Contabilidad es la misma que estampa Gestión
    /// Administrativa en la planilla de rendición.
    /// </summary>
    public interface IFirmaPersonalService
    {
        /// <summary>Firma del usuario indicado (o null si aún no la configuró).</summary>
        Task<FirmaPersonalDto?> Get(int userId);

        /// <summary>Valida y guarda/actualiza la firma del usuario indicado a partir del PNG del canvas.</summary>
        Task<FirmaPersonalDto> Save(FirmaPersonalSaveDto dto, int userId);
    }
}
