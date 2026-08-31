using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos;
using Abril_Backend.Features.PlaneamientoBimFeature.Application.Interfaces;
using Abril_Backend.Features.PlaneamientoBimFeature.Infrastructure.Interfaces;

namespace Abril_Backend.Features.PlaneamientoBimFeature.Application.Services
{
    public class PlaneamientoBimPortafolioService : IPlaneamientoBimPortafolioService
    {
        private readonly IPlaneamientoBimPortafolioRepository _repository;
        private readonly IPlaneamientoBimCargaDiariaService _cargaDiariaService;
        private readonly IPlaneamientoBimDashboardService _dashboardService;
        private readonly PlaneamientoBimReportePdfService _pdfService;

        public PlaneamientoBimPortafolioService(
            IPlaneamientoBimPortafolioRepository repository,
            IPlaneamientoBimCargaDiariaService cargaDiariaService,
            IPlaneamientoBimDashboardService dashboardService,
            PlaneamientoBimReportePdfService pdfService)
        {
            _repository = repository;
            _cargaDiariaService = cargaDiariaService;
            _dashboardService = dashboardService;
            _pdfService = pdfService;
        }

        public Task<PortafolioKpisDto> GetKpis() => _repository.GetKpis();

        public Task<List<ProyectoPortafolioDto>> GetProyectos() => _repository.GetProyectos();

        public async Task<byte[]> ExportarPdf(int projectId, DateOnly fecha)
        {
            var contexto = await _repository.GetContextoProyecto(projectId);
            if (contexto == null)
                throw new AbrilException("El proyecto no existe.", 404);

            var carga = await _cargaDiariaService.GetCargaDiaria(projectId, fecha);
            var ppc = await _dashboardService.GetPpcHistorico(projectId, fecha, fecha);

            return _pdfService.GenerarPdf(contexto.Value.ProjectNombre, contexto.Value.FaseActualNombre, carga, ppc);
        }
    }
}
