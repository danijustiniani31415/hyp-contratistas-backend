using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.SsomaModule.MiSaludFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.Shared.DescansoCertificados;

namespace Abril_Backend.Features.SsomaModule.MiSaludFeature.Application.Interfaces
{
    public interface IMiSaludService
    {
        Task<MiSaludResumenDto> GetResumen(int userId);
        Task<PagedResult<MiDescansoDto>> GetDescansos(int userId, int page);
        Task<int> CreateDescanso(int userId, CrearMiDescansoDto dto);
        /// <summary>
        /// Contenido de un certificado propio, servido por el backend (que lo baja de SharePoint
        /// con su token de app) para no depender de la sesión de Microsoft 365 del navegador.
        /// </summary>
        Task<DescansoCertificadoArchivoDto> GetCertificado(int userId, int adjuntoId);

        // ── Configuración de correos de descanso médico ──
        Task<List<MiDescansoCorreoConfigDto>> GetCorreoConfigs();
        Task SetCorreoConfigActive(int id, bool active);
    }
}
