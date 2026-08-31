using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Models
{
    // Cubre las 4 relaciones del flujo de Gestión SSOMA:
    //   D1. Jefe SSOMA (9)         -> Prevencionistas (72), todos, sin filtro de proyecto
    //   D2. Jefe SSOMA (9)         -> Coordinadores SSOMA (70), todos, sin filtro de proyecto
    //   D3. Coordinador SSOMA (70) -> Prevencionistas (72) de su mismo proyecto
    //   D4. Prevencionista (72)    -> su Coordinador SSOMA (70) del mismo proyecto — ANÓNIMA
    // En D1-D3, EvaluadorUserId va poblado. En D4 queda NULL a propósito: la
    // identidad de quien evaluó vive, separada y sin FK hacia acá, en
    // EvEvaluacionGestionSsomaCumplimiento (mismo patrón que Jefe SSOMA).
    [Table("ev_evaluacion_gestion_ssoma")]
    public class EvEvaluacionGestionSsoma
    {
        public int Id { get; set; }
        public int PeriodoId { get; set; }
        public int? EvaluadorUserId { get; set; }
        public string EvaluadorRol { get; set; } = string.Empty;
        public int EvaluadoUserId { get; set; }
        public string EvaluadoRol { get; set; } = string.Empty;
        public int? ProyectoId { get; set; }
        public decimal? Nota { get; set; }
        public string? Fortalezas { get; set; }
        public string? OportunidadesMejora { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<EvEvaluacionGestionSsomaDetalle> Detalles { get; set; } = [];
    }
}
