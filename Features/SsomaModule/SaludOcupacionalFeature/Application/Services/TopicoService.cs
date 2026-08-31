using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.DescansoMedico;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Topico;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    public class TopicoService : ITopicoService
    {
        /// <summary>Tipo (nombre de ss_descanso_tipo) con el que nace el descanso que genera un tópico.</summary>
        private const string TipoDescansoTopico = "Accidente ocupacional";

        private readonly ITopicoRepository _repo;
        private readonly IDescansoMedicoRepository _descansoRepo;

        public TopicoService(ITopicoRepository repo, IDescansoMedicoRepository descansoRepo)
        {
            _repo = repo;
            _descansoRepo = descansoRepo;
        }

        public Task<PagedResult<TopicoListItemDto>> ListPaged(TopicoFilterDto filter) =>
            _repo.ListPaged(filter);

        public Task<TopicoDetalleDto> GetById(int id) => _repo.GetById(id);

        public Task<List<TopicoListItemDto>> GetHoy() => _repo.GetHoy();

        public Task<List<TopicoTipoAtencionDto>> GetTiposAtencion() => _repo.GetTiposAtencion();

        public async Task<int> Create(TopicoCreateDto dto, int? userId)
        {
            if (dto.WorkerId <= 0)
                throw new AbrilException("El trabajador es obligatorio.", 400);
            if (dto.TipoAtencionId <= 0)
                throw new AbrilException("El tipo de atención es obligatorio.", 400);

            var topicoId = await _repo.Create(dto, userId ?? 0);

            // Si genera descanso, crear automáticamente un descanso ocupacional (la atención de
            // tópico es un evento en obra). SSOMA puede recategorizarlo después desde Descansos.
            if (dto.GeneraDescanso && dto.DescansoDias.HasValue && dto.DescansoDias.Value > 0)
            {
                var fechaInicio = dto.Fecha;
                var fechaFin = dto.Fecha.AddDays(dto.DescansoDias.Value - 1);

                var descansoId = await _descansoRepo.Create(new DescansoMedicoCreateDto
                {
                    WorkerId        = dto.WorkerId,
                    TipoId          = await _descansoRepo.GetTipoIdPorNombre(TipoDescansoTopico),
                    FechaInicio     = fechaInicio,
                    FechaFin        = fechaFin,
                    Diagnostico     = dto.Diagnostico,
                    DiagnosticoCie10= dto.DiagnosticoCie10,
                    TopicoOrigenId  = topicoId,
                    ProyectoId      = dto.ProyectoId,
                    EmpresaId       = dto.EmpresaId,
                }, userId ?? 0, []);

                // Vincular el descanso al tópico
                await _repo.SetDescansoGenerado(topicoId, descansoId);
            }

            return topicoId;
        }

        public Task Update(int id, TopicoUpdateDto dto)
        {
            if (dto.TipoAtencionId <= 0)
                throw new AbrilException("El tipo de atención es obligatorio.", 400);
            return _repo.Update(id, dto);
        }

        public Task Cerrar(int id, int? userId) => _repo.Cerrar(id, userId);

        public Task Delete(int id) => _repo.Delete(id);

        public Task<List<TopicoEvolucionDto>> GetEvoluciones(int topicoId) =>
            _repo.GetEvoluciones(topicoId);

        public Task<int> CreateEvolucion(int topicoId, TopicoEvolucionCreateDto dto, int? userId) =>
            _repo.CreateEvolucion(topicoId, dto, userId ?? 0);

        public Task DeleteEvolucion(int evolucionId) => _repo.DeleteEvolucion(evolucionId);
    }
}
