namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Dtos
{
    // ── MilestoneSchedule ────────────────────────────────────────────────────
    public class MilestoneScheduleCulminarRequest
    {
        public DateOnly? FechaRealFin { get; set; }
    }

    public class MilestoneScheduleMarcarCriticoRequest
    {
        public bool EsHitoCritico { get; set; }
    }

    public class MilestoneScheduleCreateDTO
    {
        /// <summary>Null si es un hito personalizado del proyecto (ver CustomDescription).</summary>
        public int? MilestoneId { get; set; }
        public string? CustomDescription { get; set; }
        public int Order { get; set; }
        public DateOnly PlannedStartDate { get; set; }
        public DateOnly? PlannedEndDate { get; set; }
        public bool EsHitoCritico { get; set; }
    }

    public class MilestoneScheduleDTO
    {
        public int MilestoneScheduleId { get; set; }
        public int? MilestoneId { get; set; }
        public string MilestoneDescription { get; set; }
        public int MilestoneScheduleHistoryId { get; set; }
        public int Order { get; set; }
        public DateOnly? PlannedStartDate { get; set; }
        public DateOnly? PlannedEndDate { get; set; }
        public DateOnly? FechaRealFin { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool EsHitoCritico { get; set; }
    }

    public class MilestoneScheduleFakeDataDTO
    {
        public int MilestoneId { get; set; }
        public string MilestoneDescription { get; set; }
        public int Order { get; set; }
        public DateTime PlannedStartDate { get; set; }
        public DateTime? PlannedEndDate { get; set; }
    }

    public class ScheduleChangeInfoDTO
    {
        public string ProjectDescription { get; set; }
        public string ChangedBy { get; set; }
        public List<DateTime> ChangeDate { get; set; }
    }

    // ── MilestoneScheduleHistory ─────────────────────────────────────────────
    public class MilestoneScheduleHistoryCreateDTO
    {
        public int ProjectId { get; set; }
        public List<MilestoneScheduleCreateDTO> MilestoneSchedules { get; set; }
        public bool ForceSave { get; set; }
    }

    public class MilestoneScheduleHistoryDTO
    {
        public int MilestoneScheduleHistoryId { get; set; }
        public int ProjectId { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
    }

    public class MilestoneChange
    {
        public int? MilestoneId { get; set; }
        public string MilestoneDescription { get; set; }
        public string ChangeType { get; set; }
        public bool OrderChanged { get; set; }
        public bool StartDateChanged { get; set; }
        public bool EndDateChanged { get; set; }
    }

    public class ScheduleChangeResult
    {
        public string ProjectName { get; set; } = string.Empty;
        public List<MilestoneChange> Changes { get; set; } = new();
    }

    public class UserWithoutMilestoneDTO
    {
        public int UserId { get; set; }
        public string? UserFullName { get; set; }
        public string? Email { get; set; }
        public List<Abril_Backend.Application.DTOs.ProjectSimpleDTO>? Projects { get; set; }
    }
}
