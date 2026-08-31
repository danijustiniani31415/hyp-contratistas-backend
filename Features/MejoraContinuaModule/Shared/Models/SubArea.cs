namespace Abril_Backend.Infrastructure.Models
{
    public class SubArea
    {
        public int SubAreaId { get; set; }
        public int AreaId { get; set; }
        public string SubAreaDescription { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }

        public Area Area { get; set; }
    }
}
