using Abril_Backend.Shared.Models;

namespace Abril_Backend.Shared.Services.Decolecta.Interfaces
{
    /// <summary>
    /// Acceso a la tabla decolecta_token: tokens rotativos del servicio Decolecta (RENIEC/SUNAT).
    /// </summary>
    public interface IDecolectaTokenStore
    {
        /// <summary>Tokens usables ahora mismo: state = true, no agotados y no vencidos,
        /// ordenados por fecha de expiración ascendente (se consume primero el que vence antes).</summary>
        Task<List<DecolectaToken>> GetDisponiblesAsync();

        /// <summary>Marca un token como agotado (cuota mensual consumida) para que la rotación lo salte.</summary>
        Task MarcarAgotadoAsync(int id);

        /// <summary>Resetea el flag agotado de todos los tokens vigentes. Lo llama el cron mensual
        /// (1 de cada mes a las 00:00) porque Decolecta renueva la cuota mensual de cada cuenta.
        /// Devuelve la cantidad de tokens renovados.</summary>
        Task<int> RenovarCuotaMensualAsync();
    }
}
