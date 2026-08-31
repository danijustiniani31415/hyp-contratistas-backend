namespace Abril_Backend.Application.DTOs {
    public class PersonDTO {
        public int PersonId { get; set; }
        public string DocumentIdentityCode {get;set;}
        public DocumentIdentityTypeDTO DocumentIdentityType { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
    }
}