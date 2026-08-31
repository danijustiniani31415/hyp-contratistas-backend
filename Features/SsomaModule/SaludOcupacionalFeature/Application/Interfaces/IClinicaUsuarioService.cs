using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.ClinicaUsuarios;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces
{
    public interface IClinicaUsuarioService
    {
        Task<PagedResult<ClinicaUsuarioListDto>> GetUsuariosByClinicaAsync(int clinicaId, int page, int pageSize);
        Task<ClinicaUsuarioDto> GetUsuarioByIdAsync(int clinicaId, int usuarioId);
        Task<ClinicaUsuarioDto> CreateUsuarioAsync(int clinicaId, ClinicaUsuarioCreateDto dto, int? creadoPor, string? ip);
        Task<ClinicaUsuarioDto> UpdateUsuarioAsync(int clinicaId, int usuarioId, ClinicaUsuarioUpdateDto dto, int? modificadoPor);
        Task ToggleActivoAsync(int clinicaId, int usuarioId, int? modificadoPor, string? ip);
        Task SoftDeleteAsync(int clinicaId, int usuarioId, string? ip);
        Task ReenviarActivacionAsync(int clinicaId, int usuarioId, string? ip);
        Task RegistrarAuditoriaAsync(string accion, int? clinicaUsuarioId, int? clinicaId, string? ip, string? userAgent, string? detalle);
    }
}
