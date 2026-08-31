namespace Abril_Backend.Application.DTOs {
    public class UserCreateDTO {
        public string DocumentIdentityCode {get; set;}
        public string FirstNames {get; set;}
        public string FirstLastName {get; set;}
        public string SecondLastName {get; set;}
        public string Email {get; set;}
        public int PhoneNumber {get; set;}
        public int CreatedUserId {get; set;}
        public bool Active {get; set;}
        public int RoleId {get; set;}
    }
}