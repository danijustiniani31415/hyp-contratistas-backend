using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos;
using Abril_Backend.Features.PlaneamientoBimFeature.Application.Interfaces;
using Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Application.Services
{
    public class PlaneamientoBimDashboardService : IPlaneamientoBimDashboardService
    {
        private readonly IPlaneamientoBimDashboardRepository _repository;

        public PlaneamientoBimDashboardService(IPlaneamientoBimDashboardRepository repository)
        {
            _repository = repository;
        }

        public async Task<AvanceProyectoDto> GetAvance(int projectId, DateOnly? desde, DateOnly? hasta)
        {
            ValidarRangoFechas(desde, hasta);

            var dto = await _repository.GetAvance(projectId, desde, hasta);
            if (dto == null)
                throw new AbrilException("El proyecto no existe.", 404);

            return dto;
        }

        public async Task<PpcHistoricoDto> GetPpcHistorico(int projectId, DateOnly? desde, DateOnly? hasta)
        {
            ValidarRangoFechas(desde, hasta);

            var dto = await _repository.GetPpcHistorico(projectId, desde, hasta);
            if (dto == null)
                throw new AbrilException("El proyecto no existe.", 404);

            return dto;
        }

        public async Task<List<MetaSemanalDto>> GetMetasSemanales(int projectId)
        {
            var lista = await _repository.GetMetasSemanales(projectId);
            if (lista == null)
                throw new AbrilException("El proyecto no existe.", 404);

            return lista;
        }

        public Task GuardarMetasSemanales(int projectId, MetaSemanalUpdateDto dto, int userId)
        {
            foreach (var item in dto.Items)
            {
                if (item.FechaInicioSemana > item.FechaFinSemana)
                    throw new AbrilException("La fecha de inicio de semana no puede ser posterior a la fecha de fin.", 400);
                if (item.MetaAvance < 0 || item.MetaAvance > 100)
                    throw new AbrilException("La meta de avance debe estar entre 0 y 100.", 400);
            }

            return _repository.GuardarMetasSemanales(projectId, dto, userId);
        }

        public async Task<List<PlanMaestroSemanaDto>> GetPlanMaestro(int projectId)
        {
            var lista = await _repository.GetPlanMaestro(projectId);
            if (lista == null)
                throw new AbrilException("El proyecto no existe.", 404);

            return lista;
        }

        public async Task<CausasParetoDto> GetCausasPareto(int projectId, DateOnly? desde, DateOnly? hasta)
        {
            ValidarRangoFechas(desde, hasta);

            var dto = await _repository.GetCausasPareto(projectId, desde, hasta);
            if (dto == null)
                throw new AbrilException("El proyecto no existe.", 404);

            return dto;
        }

        private static void ValidarRangoFechas(DateOnly? desde, DateOnly? hasta)
        {
            if (desde.HasValue && hasta.HasValue && desde.Value > hasta.Value)
                throw new AbrilException("La fecha 'desde' no puede ser posterior a la fecha 'hasta'.", 400);
        }
    }
}
