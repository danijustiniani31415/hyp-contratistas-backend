using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PersonasModule.Infrastructure.Models
{
    /// <summary>
    /// Historial de vínculos laborales de una persona. Vigencia por fila (fecha_fin NULL =
    /// vigente) — nunca pisar/reescribir, siempre cerrar el anterior y crear uno nuevo. La DB
    /// fuerza "máximo un vínculo vigente por persona" vía índice único parcial
    /// (uq_vinculo_laboral_vigente_por_persona): un segundo INSERT vigente para la misma persona
    /// sin cerrar el anterior falla con violación de constraint — traducir eso a un mensaje claro
    /// en el service, no dejar que llegue como 500 genérico.
    /// </summary>
    [Table("lb_vinculo_laboral")]
    public class VinculoLaboral
    {
        public int Id { get; set; }
        public int PersonaId { get; set; }
        public short TipoVinculoId { get; set; }
        public int? EmpresaContratistaId { get; set; }
        public int? CargoId { get; set; }
        public DateOnly FechaInicio { get; set; }
        public DateOnly? FechaFin { get; set; }
        public string Estado { get; set; } = "ACTIVO";
        public string? MotivoCese { get; set; }
        public long? RegistradoPorUsuarioSistemaId { get; set; }
        public DateTimeOffset CreadoEn { get; set; }

        [ForeignKey(nameof(PersonaId))]
        public Persona? Persona { get; set; }
        [ForeignKey(nameof(TipoVinculoId))]
        public TipoVinculo? TipoVinculo { get; set; }
        [ForeignKey(nameof(EmpresaContratistaId))]
        public EmpresaContratista? EmpresaContratista { get; set; }
        [ForeignKey(nameof(CargoId))]
        public Cargo? Cargo { get; set; }
    }
}
