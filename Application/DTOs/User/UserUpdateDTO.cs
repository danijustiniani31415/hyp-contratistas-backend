namespace Abril_Backend.Application.DTOs
{
    public class UserUpdateDTO
    {
        public string FirstNames { get; set; }
        public string FirstLastName { get; set; }
        public string SecondLastName { get; set; }
        public string Email { get; set; }
        public int PhoneNumber { get; set; }
        public int RoleId { get; set; }
        public int UpdatedUserId { get; set; }
    }
}
