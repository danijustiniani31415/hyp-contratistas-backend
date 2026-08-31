using Abril_Backend.Features.SsomaModule.CharlasFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.CharlasFeature.Application.Interfaces;

public interface ICharlaService
{
    Task<ProyectoDto?> GetMiProyectoAsync(int userId);
    Task<List<ProyectoDto>> GetTodosProyectosAsync();
    Task<ResumenDto> GetResumenAsync(int proyectoId, int mes, int anio);
    Task<List<StaffDto>> GetStaffProyectoAsync(int proyectoId);

    // Tab 1 — Asistencia (coordinator creates charla days + marks attendance)
    Task<List<CharlaResumenDto>> GetCharlasAsync(int proyectoId, int mes, int anio);
    Task<CharlaResumenDto> CrearCharlaAsync(CrearCharlaDto dto, int userId);
    Task EliminarCharlaAsync(int id);
    Task<List<AsistenciaDetailDto>> GetAsistenciaAsync(int charlaId);
    Task GuardarAsistenciaAsync(int charlaId, GuardarAsistenciaDto dto, int userId);

    // Tab 2 — Capacitaciones Staff (staff self-upload; coordinator approves)
    Task<List<CapacitacionDto>> GetCapacitacionesAsync(int proyectoId, int? mes, int? anio);
    Task<CapacitacionDto> SubirCapacitacionAsync(int workerId, DateTime fecha, string tema, Stream evidencia, string fileName, int userId);
    Task<CapacitacionDto> SubirMiCapacitacionAsync(int userId, DateTime fecha, string tema, Stream evidencia, string fileName);
    Task<CapacitacionDto> SubirMiCapacitacionMultiAsync(int userId, DateTime fecha, string tema, List<(Stream Stream, string FileName)> archivos);
    Task<CapacitacionDto> CambiarEstadoAsync(int id, string estado, int userId);
    Task EliminarCapacitacionAsync(int id);

    // NEW: Tab 1 — Dashboard Asistencia Supervisores
    Task<List<DashSupervisoresRowDto>> GetDashboardSupervisoresAsync(int proyectoId, int mes, int anio);

    // NEW: Tab 2 — Comparativo Programadas vs Realizadas
    Task<List<ComparativoMesDto>> GetComparativoAsync(int proyectoId, int anio);

    // NEW: Tab 3 — Crear nueva charla con supervisor + asistentes
    Task<CharlaListItemDto> CrearNuevaCharlaAsync(NuevaCharlaCreateDto dto, int userId);
    Task EditarCharlaAsync(int charlaId, EditarCharlaDto dto, int userId);
    Task<List<CharlaGaleriaItemDto>> GetCharlasProyectoAsync(int proyectoId, int mes, int anio);

    // NEW: Tab 4 — Lista paginada + detalle + aprobación
    Task<CharlaListResultDto> GetListaAsync(int? proyectoId, string? estado, int page, int pageSize);
    Task<CharlaDetalleDto> GetDetalleAsync(int id);
    Task AprobarAsync(int id, int userId);
    Task RechazarAsync(int id, string motivo, int userId);

    // NEW: Supervisor search (app_user)
    Task<List<UsuarioDto>> GetSupervisoresAsync(string? search = null);

    // NEW: Mis capacitaciones (current user's uploaded capacitaciones)
    Task<List<CapacitacionDto>> GetMisCapacitacionesAsync(int userId);

    // NEW: Dashboard por persona y por proyecto
    Task<DashPersonalResultDto> GetDashPersonalAsync(int proyectoId, int mes, int anio);
    Task<List<DashProyectoItemDto>> GetDashProyectosAsync(int mes, int anio);
}
