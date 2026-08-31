namespace Abril_Backend.Infrastructure.Models {
    public class DocumentIdentityType {
        public int DocumentIdentityTypeId {get; set;}
        public string DocumentIdentityTypeDescription {get; set;}
        public string DocumentIdentityTypeAbbreviation {get; set;}
        public DateTime CreatedDateTime {get; set;}
        public int CreatedUserId {get; set;}
        public DateTime? UpdatedDateTime {get; set;}
        public int? UpdatedUserId {get; set;}
        public bool Active {get; set;}
        public bool State {get; set;}
    }
}