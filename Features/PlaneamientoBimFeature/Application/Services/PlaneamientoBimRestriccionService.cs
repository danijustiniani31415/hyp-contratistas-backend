using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos;
using Abril_Backend.Features.PlaneamientoBimFeature.Application.Interfaces;
using Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Application.Services
{
    public class PlaneamientoBimRestriccionService : IPlaneamientoBimRestriccionService
    {
        private static readonly string[] EstadosValidos = { "ABIERTO", "EN_GESTION" };

        private readonly IPlaneamientoBimRestriccionRepository _repository;

        public PlaneamientoBimRestriccionService(IPlaneamientoBimRestriccionRepository repository)
        {
            _repository = repository;
        }

        public Task<List<RestriccionDto>> GetPaged(int projectId, bool? soloActivos)
            => _repository.GetPaged(projectId, soloActivos);

        public Task<RestriccionDto> Create(int projectId, RestriccionCreateDto dto, int userId)
        {
            ValidarDescripcionYEstado(dto.Descripcion, dto.Estado);
            return _repository.Create(projectId, dto, userId);
        }

        public Task<RestriccionDto> Update(int restriccionId, RestriccionUpdateDto dto)
        {
            ValidarDescripcionYEstado(dto.Descripcion, dto.Estado);
            return _repository.Update(restriccionId, dto);
        }

        public Task<RestriccionDto> Cerrar(int restriccionId)
            => _repository.Cerrar(restriccionId);

        private static void ValidarDescripcionYEstado(string descripcion, string estado)
        {
            if (string.IsNullOrWhiteSpace(descripcion))
                throw new AbrilException("La descripción es obligatoria.", 400);

            if (!EstadosValidos.Contains(estado))
                throw new AbrilException($"Estado inválido. Use {string.Join(" o ", EstadosValidos)}.", 400);
        }
    }
}
