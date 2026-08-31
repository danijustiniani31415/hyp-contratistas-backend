namespace Abril_Backend.Features.AuthModule.UserFeature.Application.Dtos
{
    public class UserListItemDto
    {
        public int UserId { get; set; }
        public bool Active { get; set; }
        public string UserType { get; set; } = null!;
        public string? DisplayName { get; set; }
        public string? DocumentIdentityCode { get; set; }
        public string Email { get; set; } = null!;
        public string? FirstNames { get; set; }
        public string? FirstLastName { get; set; }
        public string? SecondLastName { get; set; }
        public int? PhoneNumber { get; set; }
        public List<RoleItemDto> Roles { get; set; } = new();
    }

    public class RoleItemDto
    {
        public int RoleId { get; set; }
        public string RoleDescription { get; set; } = null!;
    }
}
