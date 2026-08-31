using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Configuracion;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces
{
    public interface IEmoCorreoConfigRepository
    {
        /// <summary>
        /// Toda la matriz (perfiles + los 4 correos con sus destinatarios y celdas) en un
        /// solo roundtrip, para la pantalla de Configuración de EMOs.
        /// </summary>
        Task<EmoCorreosConfigDto> GetConfigAsync();

        /// <summary>
        /// Da de alta un correo adicional y le crea sus celdas en los 4 correos × 4 perfiles.
        /// Nace activo únicamente en los 4 perfiles del correo desde el que se agregó.
        /// </summary>
        Task<int> CreateAdicionalAsync(string eventoCodigo, string tipoCodigo, string email, string? nombre);

        /// <summary>Cambia el correo/nombre de un buzón de área o de un correo adicional.</summary>
        Task UpdateDestinatarioAsync(int id, string email, string? nombre, string? tipoCodigo);

        /// <summary>Prende o apaga una celda de la matriz.</summary>
        Task SetReglaActiveAsync(int reglaId, bool active);

        /// <summary>Soft delete de un correo adicional y de sus celdas. No aplica al catálogo.</summary>
        Task DeleteAdicionalAsync(int id);

        /// <summary>
        /// Celdas activas de un correo, aplanadas para el envío. Las consumen la
        /// programación (manual y automática) y las notificaciones de la clínica a
        /// través de <c>EmoDestinatariosResolver</c>.
        /// </summary>
        Task<List<EmoCorreoReglaEnvioDto>> GetReglasEnvioAsync(string eventoCodigo);
    }
}
