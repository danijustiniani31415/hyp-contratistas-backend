using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.AuditoriaAtsFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.AuditoriaAtsFeature.Application.Interfaces;

namespace Abril_Backend.Features.SsomaModule.AuditoriaAtsFeature.Application.Services;

public class AuditoriaAtsService : IAuditoriaAtsService
{
    private readonly IAuditoriaAtsRepository _repo;

    public AuditoriaAtsService(IAuditoriaAtsRepository repo) => _repo = repo;

    public Task<List<AuditoriaAtsPreguntaDto>> GetPreguntasAsync()
        => _repo.GetPreguntasAsync();

    public async Task<object> GetListAsync(
        int? auditadoWorkerId, int? auditorWorkerId, int? proyectoId,
        DateOnly? fechaDesde, DateOnly? fechaHasta, string? estado,
        int page, int pageSize, int? empresaIdContratista = null)
    {
        var (items, total) = await _repo.GetListAsync(
            auditadoWorkerId, auditorWorkerId, proyectoId,
            fechaDesde, fechaHasta, estado, page, pageSize, empresaIdContratista);
        return new { items, total, page, pageSize };
    }

    public async Task<AuditoriaAtsDetalleDto> GetDetalleAsync(int id)
    {
        var result = await _repo.GetDetalleAsync(id);
        if (result is null)
            throw new AbrilException("Auditoría de ATS no encontrada.", 404);
        return result;
    }

    public async Task<int> CrearAsync(CrearAuditoriaAtsRequest request)
    {
        if (request.AuditorWorkerId <= 0)
            throw new AbrilException("El auditor es requerido.", 400);
        if (request.AuditadoWorkerId <= 0)
            throw new AbrilException("El trabajador auditado es requerido.", 400);
        if (request.Respuestas.Count == 0)
            throw new AbrilException("Debe completar la evaluación de criterios.", 400);
        if (request.Respuestas.Any(r => r.Puntaje < 0 || r.Puntaje > 5))
            throw new AbrilException("El puntaje de cada criterio debe estar entre 0 y 5.", 400);

        var promedio = Math.Round((decimal)request.Respuestas.Average(r => r.Puntaje), 2);
        var nivel = CalcularNivel(promedio);

        return await _repo.CrearAsync(request, promedio, nivel);
    }

    private static string CalcularNivel(decimal promedio) => Math.Round(promedio) switch
    {
        0 => "No consigna",
        1 => "Muy bajo",
        2 => "Bajo",
        3 => "Regular",
        4 => "Bueno",
        _ => "Muy bueno"
    };
}
