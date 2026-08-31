using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.ConfigurationModule.Features.HolidayFeature.Infrastructure.Models
{
    /// <summary>
    /// Feriado o día no laborable registrado en el sistema.
    /// </summary>
    [Table("holiday")]
    public class Holiday
    {
        [Column("holiday_id")]
        public int HolidayId { get; set; }

        [Column("holiday_type_id")]
        public int HolidayTypeId { get; set; }

        [Column("holiday_date")]
        public DateOnly HolidayDate { get; set; }

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>Si es true, el día aplica todos los años (ej. 28 de julio).</summary>
        [Column("recurring_yearly")]
        public bool RecurringYearly { get; set; }

        [Column("created_date_time")]
        public DateTime CreatedDateTime { get; set; }

        [Column("updated_date_time")]
        public DateTime? UpdatedDateTime { get; set; }

        [Column("active")]
        public bool Active { get; set; }

        /// <summary>Soft-delete flag. false = registro eliminado (se mantiene para auditoría).</summary>
        [Column("state")]
        public bool State { get; set; } = true;

        public HolidayType? HolidayType { get; set; }
    }
}
