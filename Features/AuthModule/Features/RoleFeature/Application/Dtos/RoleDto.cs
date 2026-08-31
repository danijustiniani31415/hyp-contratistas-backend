namespace Abril_Backend.Features.AuthModule.Role.Application.Dtos
{
    public class RoleDto
    {
        public int RoleId { get; set; }
        public string RoleDescription { get; set; } = null!;
        public DateTime CreatedDateTime { get; set; }
        public bool Active { get; set; }
    }
}
