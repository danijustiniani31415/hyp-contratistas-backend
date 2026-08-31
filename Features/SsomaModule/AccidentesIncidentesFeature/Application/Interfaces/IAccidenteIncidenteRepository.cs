using Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Infrastructure.Models;

namespace Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Application.Interfaces;

public interface IAccidenteIncidenteRepository
{
    Task<FlashReportInicializarDto> GetInicializarAsync();
    Task<(List<FlashReportListItemDto> Items, int Total)> GetListAsync(
        int? proyectoId, int? tipoId, string? estado,
        DateTime? fechaDesde, DateTime? fechaHasta,
        bool? soloEnviados, string? areaOrigen, int page, int pageSize);
    Task<FlashReportDetalleDto?> GetDetalleAsync(int id);
    Task<string> GenerarCodigoAsync(int proyectoId, string tipoCodigoCorto);
    Task<int> CrearAsync(CrearFlashReportRequest request, string codigo, string? urlFoto1, string? urlFoto2, int? usuarioId, bool generarEntregables);
    Task ActualizarAsync(int id, ActualizarFlashReportRequest request, string? urlFoto1, string? urlFoto2);
    Task ActualizarSeveridadAsync(int id, int? consecuenciaRealPersonal, int? consecuenciaPotencialPersonal);
    Task MarcarEnviadoAsync(int id, string urlPdf);
    Task<int?> CrearAccidenteTrabajoVinculadoAsync(FlashReportDetalleDto fr, int registradoPorId);
    Task EliminarAsync(int id);

    // Entregables
    Task<List<EntregableDto>> GetEntregablesAsync(int accidenteId);
    Task ActualizarEntregableAsync(int entregableId, ActualizarEntregableRequest req);
    Task SubirArchivoEntregableAsync(int entregableId, string urlArchivo, string nombreArchivo);
    Task EliminarArchivoEntregableAsync(int archivoId);

    // RM-050
    Task<Rm050Dto?> GetRm050Async(int accidenteId);
    Task GuardarRm050Async(int accidenteId, GuardarRm050Request req);

    // Acciones vencidas
    Task<List<AccionCorrectivaVencidaDto>> GetAccionesVencidasAsync();
    Task<List<string>> GetDestinatariosFlashReportAsync();
    Task<List<AccionCorrectivaDto>> GetMedidasAsync(int accidenteId);
    Task<int> AddMedidaAsync(int accidenteId, GuardarAccionCorrectivaRequest req);
    Task UpdateMedidaAsync(int accionId, GuardarAccionCorrectivaRequest req);
    Task DeleteMedidaAsync(int accionId);
    Task<SsomaAccionCorrectiva?> GetAccionCorrectivaAsync(int accionId);

    // Reclasificar
    Task ReclasificarTipoAsync(int id, int tipoId, string tipoCodigo, string tipoNombre);
}
