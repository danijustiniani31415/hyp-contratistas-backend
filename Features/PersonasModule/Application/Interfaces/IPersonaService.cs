using Abril_Backend.Features.PersonasModule.Application.Dtos;

namespace Abril_Backend.Features.PersonasModule.Application.Interfaces
{
    public interface IPersonaService
    {
        Task<PersonaListResponseDto> List(string? search, int? cargoId, short? tipoVinculoId, string? estado, int page, int pageSize);
        Task<PersonaDetailDto> GetById(int id);
        Task<PersonaDetailDto> Create(PersonaCreateDto dto);
        Task<PersonaDetailDto> ActualizarDatos(int personaId, PersonaUpdateDto dto);
        Task<PersonaDetailDto> ActualizarPlanilla(int personaId, PersonaPlanillaDto dto);
        Task<PersonaDetailDto> NuevoVinculo(int personaId, NuevoVinculoDto dto);
        Task<PersonaDetailDto> CrearUsuario(int personaId, CrearUsuarioDto dto, long? otorgadoPor);
        Task<PersonaDetailDto> CambiarEmail(int personaId, CambiarEmailDto dto);
        Task<PersonaDetailDto> ReenviarCredenciales(int personaId);
        Task<PersonaDetailDto> NuevaAsignacion(int personaId, NuevaAsignacionDto dto, long? otorgadoPor);
        Task<PersonaDetailDto> RevocarAsignacion(int personaId, long asignacionId);
        Task<PersonaDetailDto> ToggleNotificarAsignacion(int personaId, long asignacionId, bool notificar);
        Task<CatalogosPersonasDto> GetCatalogos();
        Task<ReniecPersonaDto?> BuscarPorDni(string dni);

        Task<List<CargoDetailDto>> ListCargos();
        Task<CargoDetailDto> CrearCargo(CargoCreateDto dto);
        Task<CargoDetailDto> ActualizarCargo(int id, CargoUpdateDto dto);

        Task<DashboardPlanillaDto> GetDashboardPlanilla();
    }
}
