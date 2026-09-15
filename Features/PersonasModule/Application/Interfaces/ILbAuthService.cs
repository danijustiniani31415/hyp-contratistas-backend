using Abril_Backend.Features.PersonasModule.Application.Dtos;

namespace Abril_Backend.Features.PersonasModule.Application.Interfaces
{
    public interface ILbAuthService
    {
        Task<LbLoginResponseDto> Login(LbLoginRequestDto request);

        /// <summary>
        /// Bootstrap: crea el primer usuario (persona + vínculo PLANILLA + usuario_sistema +
        /// asignación GERENTE_GENERAL). Falla si ya existe algún lb_usuario_sistema — es solo
        /// para arrancar en limpio, no un endpoint de registro real. Quitar/proteger antes de
        /// producción.
        /// </summary>
        Task<LbLoginResponseDto> SeedAdmin(LbLoginRequestDto request);

        /// <summary>
        /// Genera un token de reset (invalidando los previos) y envía el correo con el enlace.
        /// Nunca lanza ni informa si el email no existe — evita que alguien use este endpoint
        /// para averiguar qué correos están registrados.
        /// </summary>
        Task SolicitarReset(LbSolicitarResetDto request);

        /// <summary>Valida el token y actualiza la contraseña; marca el token como usado.</summary>
        Task ResetPassword(LbResetPasswordDto request);
    }
}
