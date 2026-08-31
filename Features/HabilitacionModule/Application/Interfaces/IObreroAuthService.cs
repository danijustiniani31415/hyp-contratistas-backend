using Abril_Backend.Features.Habilitacion.Application.Dtos.Auth;

namespace Abril_Backend.Features.Habilitacion.Application.Interfaces
{
    /// <summary>
    /// Auth para el tipo de sesión OBRERO — una cuenta por trabajador (no por empresa
    /// contratista como <see cref="IContratistaAuthService"/>), login por DNI+contraseña,
    /// vinculada a su <c>Worker</c>/<c>Person</c> ya existente en Habilitación. El sistema
    /// detecta al trabajador desde el JWT (claim <c>workerId</c>) en vez de un selector manual.
    /// </summary>
    public interface IObreroAuthService
    {
        Task<ObreroTokenDto> LoginAsync(ObreroLoginDto dto);
        Task SetPasswordAsync(ObreroSetPasswordDto dto);
        Task CambiarPasswordAsync(int userId, ObreroCambiarPasswordDto dto);
    }
}
