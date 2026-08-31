namespace Abril_Backend.Infrastructure.Models {
    public class User {
        public int UserId {get; set;}
        public string? Email {get; set;}
        public string? Password {get; set;}
        public bool EmailConfirmed {get;set;}
        public DateTime CreatedDateTime {get; set;}
        public int? CreatedUserId {get; set;}
        public DateTime? UpdatedDateTime {get; set;}
        public int? UpdatedUserId {get; set;}
        public bool Active {get; set;}
        public bool State {get; set;}
        public Person Person { get; set; }
    }
}