using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.SsomaModule.MiSaludFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.Shared.DescansoCertificados;

namespace Abril_Backend.Features.SsomaModule.MiSaludFeature.Infrastructure.Interfaces
{
    public interface IMiSaludRepository
    {
        Task<int> ResolverWorkerIdAsync(int userId);
        Task<MiSaludResumenDto> GetResumen(int workerId);
        Task<PagedResult<MiDescansoDto>> GetDescansos(int workerId, int page);
        Task<int> CreateDescanso(int workerId, CrearMiDescansoDto dto, int? userId, List<DescansoCertificadoSubidoDto> adjuntos);
        Task<DescansoNotificacionDatosDto> GetDatosNotificacionDescansoAsync(int workerId, int userId, int tipoId);
        /// <summary>
        /// Adjunto de un descanso del propio trabajador. Devuelve null si el adjunto no existe
        /// o pertenece al descanso de otra persona — así nadie puede leer certificados ajenos
        /// probando ids desde Mi Salud.
        /// </summary>
        Task<MiDescansoAdjuntoArchivoDto?> GetAdjuntoDelWorkerAsync(int adjuntoId, int workerId);

        // ── Configuración de correos de descanso médico ──
        Task<List<MiDescansoCorreoConfigDto>> GetCorreoConfigsAsync();
        /// <summary>codigo → active. Se usa al enviar el correo para respetar los toggles.</summary>
        Task<Dictionary<string, bool>> GetCorreoConfigMapAsync();
        /// <summary>Actualiza el flag active de un destinatario. Devuelve false si no existe.</summary>
        Task<bool> SetCorreoConfigActiveAsync(int id, bool active);
    }
}
