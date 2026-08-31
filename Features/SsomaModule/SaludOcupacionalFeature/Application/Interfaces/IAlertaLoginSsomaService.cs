using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Alerta;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces
{
    public interface IAlertaLoginSsomaService
    {
        /// <summary>
        /// Interconsultas pendientes y EMOs vencidos de los proyectos donde el usuario dado es
        /// Administrador (EmailCoordAdmin) o Coordinador SSOMA (EmailCoordSsoma). Vacío si el
        /// usuario no coincide con ninguno.
        /// </summary>
        Task<AlertaLoginSsomaResultDto> GetResumen(int userId);
    }
}
