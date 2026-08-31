using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Infrastructure.Models {
    public class LessonImages {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LessonImageId {get; set;}
        public string ImageUrl {get; set;}
        public int LessonId {get;set;}
        public int ImageTypeId {get;set;}
        public DateTimeOffset CreatedDateTime {get; set;}
        public int CreatedUserId {get; set;}
        public DateTimeOffset? UpdatedDateTime {get; set;}
        public int? UpdatedUserId {get; set;}
        public bool Active {get; set;}
        public bool State {get; set;}
    }
}