namespace Abril_Backend.Application.DTOs {
    public class ResidentReportIncidenceCreateDTO {
        public string ResidentReportIncidenceDescription {get; set;}
        public int ProjectId {get;set;}
        public List<IFormFile> Images {get;set;}
    }
}