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
    }
}
