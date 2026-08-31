namespace Abril_Backend.Features.AuthModule.UserFeature.Application.Dtos
{
    public class UserFeatureCreateDto
    {
        public string DocumentIdentityCode { get; set; } = null!;
        public string FirstNames { get; set; } = null!;
        public string FirstLastName { get; set; } = null!;
        public string SecondLastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public int? PhoneNumber { get; set; }
        public int CreatedUserId { get; set; }
        public bool Active { get; set; } = false;
        public List<int> RoleIds { get; set; } = new();
    }
}
