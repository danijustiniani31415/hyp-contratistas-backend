namespace Abril_Backend.Shared.Models
{
    public class UserCronogramaPreference
    {
        public int UserId { get; set; }
        public int ProjectId { get; set; }
        public string TipoCronograma { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
}
