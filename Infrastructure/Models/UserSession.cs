namespace Abril_Backend.Infrastructure.Models
{
    public class UserSession
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public bool Revoked { get; set; }
        public DateTime CreatedDateTime { get; set; }
    }
}
