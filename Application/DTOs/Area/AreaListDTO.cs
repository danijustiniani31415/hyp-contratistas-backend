namespace Abril_Backend.Application.DTOs {
    public class AreaDTO {
        public int AreaId {get; set;}
        public string AreaDescription {get; set;}
        public DateTimeOffset CreatedDateTime {get; set;}
        public int CreatedUserId {get; set;}
        public DateTimeOffset? UpdatedDateTime {get; set;}
        public int? UpdatedUserId {get; set;}
        public bool Active {get; set;}
    }
}