using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.CasoSocial;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    public class CasoSocialService : ICasoSocialService
    {
        private static readonly HashSet<string> TiposValidos = new()
        {
            "Familiar", "Económico", "Salud Mental", "Legal", "Laboral", "Otro"
        };

        private static readonly HashSet<string> PrioridadesValidas = new()
        {
            "Alta", "Media", "Baja"
        };

        private readonly ICasoSocialRepository _repo;
        private readonly ISeguimientoRepository _seguimientoRepo;

        public CasoSocialService(ICasoSocialRepository repo, ISeguimientoRepository seguimientoRepo)
        {
            _repo = repo;
            _seguimientoRepo = seguimientoRepo;
        }

        public Task<PagedResult<CasoSocialListItemDto>> ListPaged(CasoSocialFilterDto filter) =>
            _repo.ListPaged(filter);

        public Task<CasoSocialDetalleDto> GetById(Guid id) => _repo.GetById(id);

        public Task<Guid> Create(CasoSocialCreateDto dto, int? userId)
        {
            Validar(dto.TipoCaso, dto.Prioridad);
            if (dto.WorkerId <= 0)
                throw new AbrilException("El trabajador es obligatorio.", 400);
            return _repo.Create(dto, userId);
        }

        public Task Update(Guid id, CasoSocialUpdateDto dto, int? userId)
        {
            Validar(dto.TipoCaso, dto.Prioridad);
            return _repo.Update(id, dto, userId);
        }

        public Task Cerrar(Guid id, CasoSocialCerrarDto dto, int? userId) =>
            _repo.Cerrar(id, dto, userId);

        public Task Delete(Guid id) => _repo.Delete(id);

        public Task<Guid> CreateSeguimiento(Guid casoId, SeguimientoCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Tipo))
                throw new AbrilException("El tipo de seguimiento es obligatorio.", 400);
            return _seguimientoRepo.Create(casoId, dto);
        }

        public Task DeleteSeguimiento(Guid seguimientoId) =>
            _seguimientoRepo.Delete(seguimientoId);

        private static void Validar(string tipoCaso, string prioridad)
        {
            if (string.IsNullOrWhiteSpace(tipoCaso) || !TiposValidos.Contains(tipoCaso))
                throw new AbrilException($"Tipo de caso no válido. Valores permitidos: {string.Join(", ", TiposValidos)}.", 400);
            if (string.IsNullOrWhiteSpace(prioridad) || !PrioridadesValidas.Contains(prioridad))
                throw new AbrilException("La prioridad debe ser Alta, Media o Baja.", 400);
        }
    }
}
