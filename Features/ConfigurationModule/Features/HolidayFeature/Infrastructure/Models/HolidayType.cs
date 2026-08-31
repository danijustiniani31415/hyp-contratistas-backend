using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.ConfigurationModule.Features.HolidayFeature.Infrastructure.Models
{
    /// <summary>
    /// Catálogo de tipos de día no laborable (ej. "Feriado", "Día no laborable").
    /// Normaliza el tipo fuera de la tabla <c>holiday</c>.
    /// </summary>
    [Table("holiday_type")]
    public class HolidayType
    {
        [Column("holiday_type_id")]
        public int HolidayTypeId { get; set; }

        [Column("holiday_type_name")]
        public string HolidayTypeName { get; set; } = string.Empty;

        [Column("active")]
        public bool Active { get; set; }

        /// <summary>Soft-delete flag. false = registro eliminado (se mantiene para auditoría).</summary>
        [Column("state")]
        public bool State { get; set; } = true;
    }
}
