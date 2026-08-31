using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Emo;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    public class EmoService : IEmoService
    {
        private static readonly HashSet<string> AptitudesValidas = new()
        {
            "Apto", "Apto con Restricciones", "No Apto", "Observado", "Pendiente"
        };

        private static readonly HashSet<string> EstadosValidos = new()
        {
            "Vigente", "Por Vencer", "Vencido", "Convalidado", "Anulado"
        };

        private readonly IEmoRepository _repo;

        public EmoService(IEmoRepository repo)
        {
            _repo = repo;
        }

        public Task<PagedResult<EmoListItemDto>> ListPaged(EmoFilterDto filter) => _repo.ListPaged(filter);

        public Task<PagedResult<EmoPorTrabajadorDto>> ListPorTrabajador(EmoPorTrabajadorFilterDto filter) => _repo.ListPorTrabajador(filter);

        public Task<EmoDetalleDto> GetById(int id) => _repo.GetById(id);

        public Task<WorkerEmoHistorialDto> GetHistorialByWorker(int workerId) => _repo.GetHistorialByWorker(workerId);

        public Task<EmoCreateResultDto> Create(EmoCreateDto dto, int? userId)
        {
            ValidarComun(dto.WorkerId, dto.TipoEmoId, dto.Aptitud, dto.RequiereInterconsulta);
            return _repo.Create(dto, userId);
        }

        public Task Update(int id, EmoUpdateDto dto, int? userId)
        {
            ValidarComun(workerId: null, dto.TipoEmoId, dto.Aptitud, dto.RequiereInterconsulta);
            return _repo.Update(id, dto, userId);
        }

        public Task UpdateEstado(int id, string estado, int? userId)
        {
            if (string.IsNullOrWhiteSpace(estado) || !EstadosValidos.Contains(estado))
                throw new AbrilException("El estado del EMO no es válido.", 400);
            return _repo.UpdateEstado(id, estado, userId);
        }

        public Task CompletarLecturaAbril(int id, DateOnly fechaLectura, string urlResultado, int? userId)
        {
            if (string.IsNullOrWhiteSpace(urlResultado))
                throw new AbrilException("El archivo de lectura es obligatorio.", 400);
            return _repo.CompletarLecturaAbril(id, fechaLectura, urlResultado, userId);
        }

        private static void ValidarComun(int? workerId, int? tipoEmoId, string? aptitud, bool requiereInterconsulta)
        {
            if (workerId.HasValue && workerId.Value <= 0)
                throw new AbrilException("El trabajador es obligatorio.", 400);
            if (!tipoEmoId.HasValue || tipoEmoId.Value <= 0)
                throw new AbrilException("El tipo de EMO es obligatorio.", 400);
            if (!string.IsNullOrWhiteSpace(aptitud) && !AptitudesValidas.Contains(aptitud))
                throw new AbrilException("La aptitud indicada no es válida.", 400);
            if (requiereInterconsulta && aptitud != "Observado")
                throw new AbrilException("La interconsulta solo aplica cuando la aptitud es 'Observado'.", 400);
        }
    }
}
