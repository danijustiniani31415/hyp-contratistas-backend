namespace Abril_Backend.Infrastructure.Models {
    public class State {
        public int StateId {get; set;}
        public int StateCode {get; set;}
        public string StateDescription {get; set;}
        public List<ResidentReportIncidence> ResidentReportIncidences { get; set; }
    }
}