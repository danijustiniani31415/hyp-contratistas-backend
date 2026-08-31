using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Programacion;
using Abril_Backend.Shared.Models;
using Abril_Backend.Shared.Services;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces
{
    public interface IProgramacionEmoRepository
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
        Task<ProgramacionDestinatariosPreviewDto> GetDestinatarios(int workerId, int? clinicaId);

        /// <summary>
        /// Razones sociales del grupo con sus cupos, para el desplegable que el modal de
        /// programación muestra cuando el trabajador llegó SIN razón social (el caso del ingreso
        /// directo FFT, que pasa de la solicitud al EMO sin tocar la asignación de Reclutamiento).
        /// Es la misma lista y la misma cuenta que ofrece Reclutamiento (ver
        /// <see cref="RazonSocialCuposHelper"/>).
        /// </summary>
        Task<List<RazonSocialCupoDto>> GetRazonesSociales();
        Task<ProgramacionInasistenciaEnviarCorreoResultDto> EnviarInasistencias(DateOnly fecha);

        /// <summary>
        /// Marca como "No se presentó" toda programación en un estado previo a la atención
        /// (Programado/Confirmado/Aceptado por Clínica/Reprogramado) cuya fecha ya pasó, o es
        /// hoy y ya se cumplió la hora de corte (13:00 hora Lima). Libera al trabajador para
        /// poder reprogramarse y evita que el auto-programador la lea como "activa" y genere
        /// una segunda fila para el mismo trabajador/tipo EMO.
        /// </summary>
        Task<int> CerrarInasistenciasVencidasAsync();
    }
}
