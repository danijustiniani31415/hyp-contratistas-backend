using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Infrastructure.Models
{
    [Table("area_type")]
    public class AreaType
    {
        [Column("area_type_id")]
        public int AreaTypeId { get; set; }

        [Column("area_type_name")]
        public string AreaTypeName { get; set; } = string.Empty;

        [Column("active")]
        public bool Active { get; set; }

        /// <summary>Soft-delete flag. false = registro eliminado (se mantiene para auditoría).</summary>
        [Column("state")]
        public bool State { get; set; } = true;
    }
}
