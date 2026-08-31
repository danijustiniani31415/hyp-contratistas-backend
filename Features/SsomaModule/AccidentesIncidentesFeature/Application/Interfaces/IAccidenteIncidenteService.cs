using Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Application.Interfaces;

public interface IAccidenteIncidenteService
{
    Task<FlashReportInicializarDto> GetInicializarAsync();
    Task<object> GetListAsync(int? proyectoId, int? tipoId, string? estado,
        DateTime? fechaDesde, DateTime? fechaHasta, bool? soloEnviados, string? areaOrigen, int page, int pageSize);
    Task<FlashReportDetalleDto> GetDetalleAsync(int id);
    Task<int> CrearAsync(CrearFlashReportRequest request, int? usuarioId);
    Task ActualizarAsync(int id, ActualizarFlashReportRequest request);
    Task ActualizarSeveridadAsync(int id, int? consecuenciaRealPersonal, int? consecuenciaPotencialPersonal);
    Task EnviarFlashReportAsync(int id, bool enviarEmail = true);
    Task EliminarAsync(int id);
    Task<List<EntregableDto>> GetEntregablesAsync(int accidenteId);
    Task ActualizarEntregableAsync(int entregableId, ActualizarEntregableRequest req);
    Task<string> SubirArchivoEntregableAsync(int entregableId, IFormFile archivo);
    Task EliminarArchivoEntregableAsync(int archivoId);

    // RM-050
    Task<Rm050Dto> GetRm050Async(int accidenteId);
    Task GuardarRm050Async(int accidenteId, GuardarRm050Request req);

    // PDF y fotos on-demand
    Task<byte[]> GenerarPdfAsync(int id);
    Task<(byte[] Bytes, string ContentType, string FileName)> ObtenerFotoAsync(int id, int slot);

    // Acciones vencidas
    Task<List<AccionCorrectivaVencidaDto>> GetAccionesVencidasAsync();

    // Medidas de control (bidireccional con RM-050)
    Task<List<AccionCorrectivaDto>> GetMedidasAsync(int accidenteId);
    Task<int> AddMedidaAsync(int accidenteId, GuardarAccionCorrectivaRequest req);
    Task UpdateMedidaAsync(int accionId, GuardarAccionCorrectivaRequest req);
    Task DeleteMedidaAsync(int accionId);

    // MINTRA
    Task<byte[]> GenerarMintraAsync(int id);

    // Reclasificar
    Task<int> ReclasificarComoAccidenteAsync(int id, int? usuarioId);
}
