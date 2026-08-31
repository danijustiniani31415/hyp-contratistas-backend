using Abril_Backend.Features.GestionAdministrativa.CorreosSalida.Application.Dtos;

namespace Abril_Backend.Features.GestionAdministrativa.CorreosSalida.Application.Interfaces
{
    /// <summary>
    /// Configuración de los destinatarios de los correos del flujo de solicitud de salidas.
    /// </summary>
    public interface ICorreoConfigService
    {
        /// <summary>Carga inicial de la pantalla (correos + reglas + opciones) en una sola llamada.</summary>
        Task<CorreoConfigInicialDto> GetInicialAsync();

        /// <summary>Reemplaza las reglas (inclusiones + exclusiones) del correo indicado por su código.</summary>
        Task UpdateReglasAsync(string eventoCodigo, CorreoReglasUpdateDto dto);
    }
}
