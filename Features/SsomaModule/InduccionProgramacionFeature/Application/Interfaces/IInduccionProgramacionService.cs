using Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Application.Dtos;

namespace Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Application.Interfaces
{
    public interface IInduccionProgramacionService
    {
        Task<List<ProyectoSimpleInduccionDto>> GetProyectosDisponiblesAsync();
        Task<List<ResponsableProyectoDto>> GetResponsablesDisponiblesAsync(int proyectoId);
        Task<List<RotacionProyectoDto>> GetRotacionAsync();
        Task<RotacionProyectoDto> AgregarARotacionAsync(int proyectoId, int? responsableWorkerId);
        Task ReordenarAsync(RotacionReordenarDto dto);
        Task SetActivoAsync(int id, bool activo);
        Task SetResponsableAsync(int id, int? responsableWorkerId);

        /// <summary>
        /// Devuelve el calendario en [desde, hasta], generando primero las fechas hábiles
        /// (L/M/V sin feriados) del rango que todavía no existan, asignándolas al siguiente
        /// proyecto de la rotación.
        /// </summary>
        Task<List<ProgramacionInduccionDto>> GetCalendarioAsync(DateOnly desde, DateOnly hasta);

        Task ReasignarAsync(int id, ProgramacionReasignarDto dto);
        Task CancelarAsync(int id, ProgramacionCancelarDto dto);
        Task ReprogramarAsync(int id, ProgramacionReprogramarDto dto);
        Task SetProgramacionResponsableAsync(int id, int? responsableWorkerId);

        /// <summary>
        /// Envía el aviso por correo de las inducciones cuya fecha de aviso (día hábil anterior
        /// a las 3pm, o sábado 10am si la inducción cae lunes) ya llegó y todavía no se envió.
        /// Pensado para ser llamado por un cron externo — idempotente vía AvisoEnviado.
        /// </summary>
        Task<AvisoInduccionResultDto> EnviarAvisosPendientesAsync();
    }
}
