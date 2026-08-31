using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Programacion;
using Abril_Backend.Shared.Models;
using Abril_Backend.Shared.Services;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces
{
    public interface IProgramacionEmoService
    {
        Task<PagedResponseDto<ProgramacionListDto>> List(ProgramacionFilterDto filter);
        Task<int> Create(ProgramacionCreateDto dto, int? userId);
        Task Update(int id, ProgramacionUpdateDto dto, int? userId);
        Task UpdateEstado(int id, string estado, int? emoResultadoId, int? userId);
        Task ClinicaAccion(int id, ProgramacionClinicaAccionDto dto, int? userId);
        Task<List<ProgramacionHabilitacionDto>> GetHabilitacionAsync(ProgramacionHabilitacionFiltrosDto filtros);
        Task PatchNotificadoAsync(int id, bool notificado);
        Task UndoCheckInAsync(int id);
        Task<ProgramacionResumenDto> GetResumen(ProgramacionFilterDto filter);
        /// <summary>
        /// A quién le llegaría el correo si se programara ahora mismo un EMO para ese trabajador
        /// en esa clínica. Misma lógica que el envío real; la usa el formulario para avisarlo.
        /// </summary>
        Task<ProgramacionDestinatariosPreviewDto> GetDestinatarios(int workerId, int? clinicaId);

        /// <summary>
        /// Razones sociales del grupo con sus cupos: el desplegable que reemplaza al campo de solo
        /// lectura cuando el trabajador todavía no tiene ninguna. Ver
        /// <see cref="Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces.IProgramacionEmoRepository.GetRazonesSociales"/>.
        /// </summary>
        Task<List<RazonSocialCupoDto>> GetRazonesSociales();
        Task<ProgramacionInasistenciaEnviarCorreoResultDto> EnviarInasistencias(DateOnly fecha);

        /// <summary>Cierre automático (cron 13:00 hora Lima) de citas vencidas sin asistencia. Ver <see cref="Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces.IProgramacionEmoRepository.CerrarInasistenciasVencidasAsync"/>.</summary>
        Task<int> CerrarInasistenciasVencidasAsync();
    }
}
