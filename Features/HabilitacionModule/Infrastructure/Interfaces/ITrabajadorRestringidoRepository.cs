using Abril_Backend.Features.Habilitacion.Application.Dtos.Restringidos;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces
{
    public interface ITrabajadorRestringidoRepository
    {
        Task<bool> EstaRestringidoPorDniAsync(string? dni);
        /// <summary>Por defecto excluye Tipo=DESCANSO_MEDICO: esa pantalla (Amonestaciones/Inhabilitados)
        /// es de sanciones, no de bloqueos médicos temporales. Control de Acceso NO usa este método
        /// (usa EstaRestringidoPorDniAsync, que sigue bloqueando sin importar el tipo).</summary>
        Task<List<TrabajadorRestringidoListDto>> GetAllAsync(bool soloActivos = true, string? dni = null, bool incluirDescansoMedico = false);
        /// <summary>Si dto.RestringidoPor viene vacío, se rellena con el nombre del usuario userId (nunca queda "por quién" en blanco).
        /// Si dto.FechaRestriccion viene vacío, se rellena con la fecha actual. Si dto.Tipo viene vacío, "MANUAL".</summary>
        Task<TrabajadorRestringidoListDto> CreateAsync(TrabajadorRestringidoCreateDto dto, int? userId = null);
        Task DesactivarAsync(int id);
        Task DesactivarPorWorkerIdAsync(int workerId);
    }
}
