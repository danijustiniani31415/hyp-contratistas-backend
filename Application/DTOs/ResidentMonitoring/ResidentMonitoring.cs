namespace Abril_Backend.Application.DTOs
{
    public class TrackingQueryDto
    {
        public int? Month { get; set; }
        public int? Year { get; set; }
        public int? ProjectId { get; set; }
        public int? ResidentUserId { get; set; }
    }
    public class TrackingResultDto
    {
        public IEnumerable<TrackingItemDto> Items { get; set; }
        public TrackingSummaryDto Summary { get; set; }
    }

    public class TrackingItemDto
    {
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; }
        public int ResidentUserId { get; set; }
        public string ResidentFullName { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
        public bool IsAllPeriods { get; set; }
        public TrackingScheduleDto Schedule { get; set; }
        public TrackingFileDto Ivts { get; set; }
        public TrackingFileDto ConstructionLog { get; set; }
        public TrackingIncidencesDto Incidences { get; set; }
        public decimal CompliancePercentage { get; set; }
    }
    public class TrackingSummaryDto
    {
        public decimal AverageCompliance { get; set; }
        public int PendingDeliverables { get; set; }
        public int FullyCompleted { get; set; }
        public int CriticalPending { get; set; }
    }

    public class TrackingScheduleDto
    {
        public int Expected { get; set; }
        public int Reported { get; set; }
        public bool Completed { get; set; }
    }

    public class TrackingFileDto
    {
        public int Expected { get; set; }
        public int Uploaded { get; set; }
        public bool Completed => Expected == 0 || Uploaded >= Expected;
    }

    public class TrackingIncidencesDto
    {
        public int Total { get; set; }
        public int Answered { get; set; }
        public bool NoPending => Total == 0;
        public bool Completed => NoPending || Answered >= Total;
    }

    public class TrackingRawDto
    {
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; }
        public int ResidentUserId { get; set; }
        public string ResidentFullName { get; set; }
        public int ScheduleReportedCount { get; set; }
        public int IvtsUploaded { get; set; }
        public int ConstructionLogsUploaded { get; set; }
        public int TotalIncidences { get; set; }
        public int AnsweredIncidences { get; set; }
    }

    public class TrackingFiltersDto
    {
        public List<ProjectSimpleDTO> Projects { get; set; }
        public List<UserFilterDTO> Residents { get; set; }
    }

    public class PeriodFilterDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string Label { get; set; }
    }
}