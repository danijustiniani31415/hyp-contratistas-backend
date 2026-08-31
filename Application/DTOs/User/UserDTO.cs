namespace Abril_Backend.Application.DTOs {
    public class UserDTO {
        public int UserId {get; set;}
        public PersonDTO Person {get; set;}
        public List<RoleSimpleDTO>? Roles {get;set;}
        public bool Active {get; set;}
    }
}