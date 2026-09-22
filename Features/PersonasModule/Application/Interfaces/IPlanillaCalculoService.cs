using Abril_Backend.Features.PersonasModule.Application.Dtos;

namespace Abril_Backend.Features.PersonasModule.Application.Interfaces
{
    public interface IPlanillaCalculoService
    {
        Task<List<ConceptoPlanillaDto>> ListConceptos();
        Task<ConceptoPlanillaDto> CrearConcepto(ConceptoPlanillaCreateDto dto);
        Task<ConceptoPlanillaDto> ActualizarConcepto(int id, ConceptoPlanillaUpdateDto dto);

        Task<List<PlanillaPeriodoListItemDto>> ListPeriodos();
        Task<PlanillaPeriodoDetailDto> CrearPeriodo(PlanillaPeriodoCreateDto dto);
        Task<PlanillaPeriodoDetailDto> GetPeriodo(int id);

        /// <summary>Calcula (o recalcula, si sigue en BORRADOR) todas las boletas del período.</summary>
        Task<PlanillaPeriodoDetailDto> Calcular(int periodoId, long calculadoPorId);

        /// <summary>Cierra el período — ya no se puede recalcular (evita pisar una planilla ya pagada).</summary>
        Task<PlanillaPeriodoDetailDto> Cerrar(int periodoId);
    }
}
