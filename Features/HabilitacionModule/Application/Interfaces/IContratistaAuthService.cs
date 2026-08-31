using Abril_Backend.Features.Habilitacion.Application.Dtos.Auth;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Empresa;

namespace Abril_Backend.Features.Habilitacion.Application.Interfaces
{
    public interface IContratistaAuthService
    {
        Task<ContratistaTokenDto> LoginAsync(ContratistaLoginDto dto);
        Task<List<EmpresaSimpleDto>> GetEmpresasParaLoginAsync();
        Task SolicitarActivacionAsync(int empresaId);
        Task<ContratistaTokenDto> ActivarCuentaAsync(ActivarCuentaDto dto);
        Task SolicitarResetPasswordAsync(SolicitarResetDto dto);
        Task ResetPasswordAsync(ResetPasswordDto dto);
        Task CambiarPasswordAsync(int userId, CambiarPasswordDto dto);
        Task<ValidarMigracionResultDto> ValidarMigracionAsync(ValidarMigracionDto dto);
        Task ActivarMigracionAsync(ActivarMigracionDto dto);
    }
}
