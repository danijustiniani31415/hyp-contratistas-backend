using Abril_Backend.Features.Ssoma.Paso.Dtos;
using Microsoft.AspNetCore.Http;

namespace Abril_Backend.Features.Ssoma.Paso.Services;

public interface IPasoService
{
    Task<PagedResult<PasoListItemDto>> GetListAsync(PasoListQuery q);
    Task<PasoDetalleDto?> GetDetalleAsync(int id);
    Task<PasoListItemDto> CrearAsync(CrearPasoRequest req, int userId);
    Task<PasoListItemDto> EditarAsync(int id, EditarPasoRequest req);
    Task<PasoListItemDto> AprobarAsync(int id, int userId);
    Task<PasoListItemDto> InstanciarAsync(int plantillaId, InstanciarPasoRequest req, int userId);
    Task<List<PasoHistoricoAnioDto>> GetHistoricoProyectoAsync(int proyectoId);
    Task<PasoSpiDto> GetSpiAsync(int id);
    Task<PasoDashboardDto> GetDashboardAsync(int? anio);
    Task<List<PasoAlertaDto>> GetAlertasAsync();
    Task<List<PasoCategoriaDto>> GetCategoriasAsync();
    Task<PasoActividadDto> CrearActividadAsync(CrearActividadRequest req);
    Task<PasoActividadDto> EditarActividadAsync(int id, EditarActividadRequest req);
    Task DeleteActividadAsync(int id, string motivo, int userId);
    Task<PasoEjecucionDto> RegistrarEjecucionAsync(RegistrarEjecucionRequest req, int userId);
    Task<PasoEjecucionDto> ProgramarEjecucionAsync(ProgramarEjecucionRequest req);
    Task<PasoEjecucionDto> ReprogramarEjecucionAsync(int id, ReprogramarEjecucionRequest req, int userId);
    Task<PasoEjecucionDto> SubirEvidenciaAsync(int ejecucionId, IFormFile file, int userId);
    Task<PasoEjecucionArchivoDto> AgregarArchivoEjecucionAsync(int ejecucionId, IFormFile file, int userId);
    Task EliminarArchivoEjecucionAsync(int archivoId);
    Task ProcesarCronAsync();
    Task<PasoResumenMesDto> GetResumenMesAsync(int id, int anio, int mes);
    Task<List<PasoAuditoriaDto>> GetAuditoriaAsync(int actividadId);
    Task<PagedResult<PasoSaludActividadListItemDto>> GetActividadesSaludAsync(PasoSaludListQuery q);
}
