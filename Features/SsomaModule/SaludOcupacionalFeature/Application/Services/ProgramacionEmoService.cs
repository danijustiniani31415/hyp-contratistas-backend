using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Programacion;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Shared.Models;
using Abril_Backend.Shared.Services;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    public class ProgramacionEmoService : IProgramacionEmoService
    {
        private static readonly HashSet<string> EstadosValidos = new()
        {
            "Programado", "Confirmado", "Realizado", "No se presentó", "Cancelado", "Reprogramado",
            "Aceptado por Clínica", "Rechazado por Clínica", "En Atención", "Completado"
        };

        private readonly IProgramacionEmoRepository _repo;

        public ProgramacionEmoService(IProgramacionEmoRepository repo)
        {
            _repo = repo;
        }

        public Task<PagedResponseDto<ProgramacionListDto>> List(ProgramacionFilterDto filter) => _repo.List(filter);

        public Task<int> Create(ProgramacionCreateDto dto, int? userId)
        {
            if (dto.WorkerId <= 0) throw new AbrilException("El trabajador es obligatorio.", 400);
            if (dto.TipoEmoId <= 0) throw new AbrilException("El tipo de EMO es obligatorio.", 400);
            return _repo.Create(dto, userId);
        }

        public Task Update(int id, ProgramacionUpdateDto dto, int? userId)
        {
            if (dto.TipoEmoId <= 0) throw new AbrilException("El tipo de EMO es obligatorio.", 400);
            return _repo.Update(id, dto, userId);
        }

        public Task UpdateEstado(int id, string estado, int? emoResultadoId, int? userId)
        {
            if (string.IsNullOrWhiteSpace(estado) || !EstadosValidos.Contains(estado))
                throw new AbrilException("El estado de la programación no es válido.", 400);
            return _repo.UpdateEstado(id, estado, emoResultadoId, userId);
        }

        public Task ClinicaAccion(int id, ProgramacionClinicaAccionDto dto, int? userId)
        {
            var accion = dto.Accion?.Trim();
            if (string.IsNullOrWhiteSpace(accion))
                throw new AbrilException("La acción es obligatoria.", 400);

            if (accion == "Rechazar" && string.IsNullOrWhiteSpace(dto.MotivoRechazo))
                throw new AbrilException("El motivo de rechazo es obligatorio.", 400);

            if (accion is not ("Aceptar" or "Rechazar" or "CheckIn" or "Completar" or "No Asistió"))
                throw new AbrilException("Acción no reconocida. Use: Aceptar, Rechazar, CheckIn, Completar, No Asistió.", 400);

            return _repo.ClinicaAccion(id, dto, userId);
        }

        public Task<List<ProgramacionHabilitacionDto>> GetHabilitacionAsync(ProgramacionHabilitacionFiltrosDto filtros)
            => _repo.GetHabilitacionAsync(filtros);

        public Task PatchNotificadoAsync(int id, bool notificado)
            => _repo.PatchNotificadoAsync(id, notificado);

        public Task UndoCheckInAsync(int id)
            => _repo.UndoCheckInAsync(id);

        public Task<ProgramacionResumenDto> GetResumen(ProgramacionFilterDto filter)
            => _repo.GetResumen(filter);

        public Task<ProgramacionDestinatariosPreviewDto> GetDestinatarios(int workerId, int? clinicaId)
            => _repo.GetDestinatarios(workerId, clinicaId);

        public Task<List<RazonSocialCupoDto>> GetRazonesSociales() => _repo.GetRazonesSociales();

        public Task<ProgramacionInasistenciaEnviarCorreoResultDto> EnviarInasistencias(DateOnly fecha)
            => _repo.EnviarInasistencias(fecha);

        public Task<int> CerrarInasistenciasVencidasAsync()
            => _repo.CerrarInasistenciasVencidasAsync();
    }
}
