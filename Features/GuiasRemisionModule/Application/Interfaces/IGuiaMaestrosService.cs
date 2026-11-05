using Abril_Backend.Features.GuiasRemisionModule.Application.Dtos;

namespace Abril_Backend.Features.GuiasRemisionModule.Application.Interfaces
{
    public interface IGuiaMaestrosService
    {
        Task<List<TransportistaDto>> ListarTransportistas();
        Task<TransportistaDto> GuardarTransportista(long? id, TransportistaDto dto);

        Task<List<VehiculoDto>> ListarVehiculos();
        Task<VehiculoDto> GuardarVehiculo(long? id, VehiculoDto dto);

        Task<List<ConductorDto>> ListarConductores();
        Task<ConductorDto> GuardarConductor(long? id, ConductorDto dto);
    }
}
