namespace Abril_Backend.Infrastructure.Models {
    public class ImageType {
        public int ImageTypeId {get; set;}
        public string ImageTypeDescription {get; set;}
        public DateTime CreatedDateTime {get; set;}
        public int CreatedUserId {get; set;}
        public DateTime? UpdatedDateTime {get; set;}
        public int? UpdatedUserId {get; set;}
        public bool Active {get; set;}
        public bool State {get; set;}
    }
}