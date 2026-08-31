using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Interfaces
{
    /// <summary>
    /// Pantalla de Configuración de correos: una sección por cada correo del flujo, con su
    /// interruptor maestro y sus destinatarios (dinámicos del catálogo + correos adicionales
    /// escritos a mano).
    ///
    /// Hay dos pantallas y cada una administra solo sus correos, identificadas por el slug del
    /// módulo en la URL: <c>solicitud-personal</c> (flujo del solicitante) y <c>reclutamiento</c>
    /// (los que salen desde la bandeja de GTH). Todas las operaciones lo reciben para que desde
    /// una pantalla no se pueda tocar la configuración de la otra.
    /// </summary>
    public interface ICorreoConfigService
    {
        /// <summary>Los correos de esa pantalla con todos sus destinatarios.</summary>
        Task<CorreoConfigDto> GetConfig(string pantalla);

        /// <summary>Agrega un correo adicional a un correo. Devuelve el id creado.</summary>
        Task<int> CrearAdicional(string pantalla, CorreoAdicionalCreateDto dto, int? userId);

        /// <summary>Edita un correo adicional.</summary>
        Task ActualizarAdicional(string pantalla, int destinatarioId, CorreoAdicionalUpdateDto dto, int? userId);

        /// <summary>Prende o apaga un destinatario.</summary>
        Task SetDestinatarioActive(string pantalla, int destinatarioId, bool active, int? userId);

        /// <summary>Prende o apaga un correo completo.</summary>
        Task SetCorreoActive(string pantalla, string tipoSlug, bool active, int? userId);

        /// <summary>Prende o apaga el destinatario principal que asigna el sistema.</summary>
        Task SetPrincipalAutomaticoActive(string pantalla, string tipoSlug, bool active, int? userId);

        /// <summary>Elimina (baja lógica) un correo adicional.</summary>
        Task EliminarAdicional(string pantalla, int destinatarioId, int? userId);
    }
}
