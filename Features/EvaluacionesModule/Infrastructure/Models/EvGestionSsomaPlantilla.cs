using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Evaluaciones.Infrastructure.Models
{
    [Table("ev_gestion_ssoma_plantilla")]
    public class EvGestionSsomaPlantilla
    {
        public int Id { get; set; }
        public string Criterio { get; set; } = string.Empty;
        /// <summary>'COORDINADOR' o 'PREVENCIONISTA' — a qué rol evaluado le corresponde
        /// este criterio (Coordinador SSOMA lidera equipo, Prevencionista es operativo).</summary>
        public string RolEvaluado { get; set; } = string.Empty;
        public int Orden { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
