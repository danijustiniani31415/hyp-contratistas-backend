using Abril_Backend.Features.PersonasModule.Application.Dtos;

namespace Abril_Backend.Features.PersonasModule.Application.Interfaces
{
    public interface IPersonaService
    {
        Task<PersonaListResponseDto> List(string? search, int page, int pageSize);
        Task<PersonaDetailDto> GetById(int id);
        Task<PersonaDetailDto> Create(PersonaCreateDto dto);
        Task<PersonaDetailDto> NuevoVinculo(int personaId, NuevoVinculoDto dto);
        Task<PersonaDetailDto> CrearUsuario(int personaId, CrearUsuarioDto dto, long? otorgadoPor);
        Task<PersonaDetailDto> NuevaAsignacion(int personaId, NuevaAsignacionDto dto, long? otorgadoPor);
        Task<CatalogosPersonasDto> GetCatalogos();
    }
}
