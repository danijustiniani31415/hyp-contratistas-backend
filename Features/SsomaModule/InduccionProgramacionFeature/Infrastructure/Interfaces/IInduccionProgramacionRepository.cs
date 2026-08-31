using Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Infrastructure.Models;

namespace Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Infrastructure.Interfaces
{
    /// <summary>Destinatarios de un proyecto para el aviso de inducción.</summary>
    public class InduccionDestinatariosDto
    {
        public string? EmailCoordAdmin { get; set; }
        public string? EmailCoordSsoma { get; set; }
        public string? EmailResidente { get; set; }
        public List<string> EmailsPrevencionistas { get; set; } = new();
    }

    public interface IInduccionProgramacionRepository
    {
        // ── Rotación ──────────────────────────────────────────────────────
        Task<List<(int ProyectoId, string Nombre)>> GetProyectosActivosAsync();
        Task<List<SsInduccionRotacionProyecto>> GetRotacionAsync();
        Task<SsInduccionRotacionProyecto> AgregarARotacionAsync(int proyectoId, int? responsableWorkerId);
        Task<bool> ReordenarAsync(List<(int Id, int Orden)> items);
        Task<bool> SetActivoAsync(int id, bool activo);
        Task<bool> SetResponsableAsync(int id, int? responsableWorkerId);

        /// <summary>Coordinador SSOMA y Prevencionista(s) con vínculo activo en el proyecto —
        /// candidatos a responsable de un turno de inducción.</summary>
        Task<List<ResponsableProyectoDto>> GetResponsablesDisponiblesAsync(int proyectoId);

        // ── Cursor de rotación ────────────────────────────────────────────
        Task<SsInduccionRotacionCursor> GetOrCreateCursorAsync();
        Task GuardarCursorAsync(int? ultimoProyectoRotacionId, DateOnly ultimaFechaGenerada);

        // ── Feriados (reutiliza el catálogo global de Configuración) ───────
        Task<HashSet<DateOnly>> GetFeriadosAsync(DateOnly desde, DateOnly hasta);

        // ── Programación (calendario) ──────────────────────────────────────
        Task<List<SsInduccionProgramacion>> GetProgramacionAsync(DateOnly desde, DateOnly hasta);
        Task<SsInduccionProgramacion?> GetProgramacionByIdAsync(int id);
        Task<SsInduccionProgramacion> CrearProgramacionAsync(DateOnly fecha, int proyectoId, int? responsableWorkerId);
        Task GuardarProgramacionAsync(SsInduccionProgramacion programacion);
        Task<List<SsInduccionProgramacion>> GetPendientesDeAvisoAsync(DateOnly hasta);

        // ── Destinatarios del correo ────────────────────────────────────────
        Task<InduccionDestinatariosDto> GetDestinatariosAsync(int proyectoId);
        Task<string> GetProyectoNombreAsync(int proyectoId);
        Task<Dictionary<int, string>> GetProyectoNombresAsync(IEnumerable<int> proyectoIds);
        Task<Dictionary<int, string>> GetWorkerNombresAsync(IEnumerable<int> workerIds);
    }
}
