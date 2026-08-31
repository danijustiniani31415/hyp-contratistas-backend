namespace Abril_Backend.Application.DTOs {
    public class ResidentReportResponseCreateDTO {
        public int ResidentReportIncidenceId {get;set;}
        public string ResidentResponseDescription {get; set;}
        public List<IFormFile> Images {get;set;}
    }
}