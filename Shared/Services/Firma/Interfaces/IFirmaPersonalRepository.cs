using Abril_Backend.Shared.Services.Firma.Dtos;

namespace Abril_Backend.Shared.Services.Firma.Interfaces
{
    /// <summary>
    /// Acceso a la firma de una persona (<c>person.signature_*</c>). Vive en Shared porque la MISMA
    /// firma la registran y la estampan tres módulos: Contabilidad (visado de facturas), Gestión GTH
    /// (carta oferta del postulante) y Gestión Administrativa (planilla de rendición de salidas).
    /// Una persona tiene una sola firma, no una por módulo.
    /// </summary>
    public interface IFirmaPersonalRepository
    {
        /// <summary>Firma (como data URL) del usuario indicado, o null si aún no la configuró.</summary>
        Task<FirmaPersonalDto?> GetByUserId(int userId);

        /// <summary>Crea o actualiza (upsert) la firma del usuario indicado.</summary>
        Task Upsert(int userId, byte[] imageBytes, string mime);

        /// <summary>Bytes de la firma del usuario indicado (para estampar) o null si no la configuró.</summary>
        Task<(byte[] Bytes, string Mime)?> GetActiveBytesByUserId(int userId);
    }
}
