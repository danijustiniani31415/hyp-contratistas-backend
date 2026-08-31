using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;

namespace Abril_Backend.Features.Evaluaciones.Application.Interfaces
{
    public interface IEvRecordatorioRepository
    {
        Task<EvPeriodo?> GetPeriodoActivoAsync();
        Task<EvPeriodo?> GetPeriodoCerradoAyerAsync();
        Task<List<EvaluadorDto>> GetEvaluadoresPendientesAsync(int periodoId, bool soloSinEvaluar);
        Task RegistrarLogAsync(int periodoId, int? userId, string tipo, string emailDestino, bool ccJefatura, bool ccGerencia);
        Task<bool> YaEnvioRecordatorioHoyAsync(int periodoId, int? userId, string tipo);
    }

    public class EvaluadorDto
    {
        public int? UserId { get; set; }
        /// <summary>Necesario para resolver su jefe con la configuración global de revisores.</summary>
        public int WorkerId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string EmailCorporativo { get; set; } = string.Empty;
        public string Subarea { get; set; } = string.Empty;
        /// <summary>Jefe del evaluador (se le hace CC del recordatorio), resuelto por
        /// IJefeRevisorResolver: revisor directo → revisor del área → fallback GTH.</summary>
        public string? JefeEmail { get; set; }
        public string? JefeNombre { get; set; }
    }
}
