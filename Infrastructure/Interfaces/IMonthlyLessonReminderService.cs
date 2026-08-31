namespace Abril_Backend.Infrastructure.Interfaces
{
    public interface IMonthlyLessonReminderService
    {
        Task SendLessonsLearnedMonthlyRemindersAsync(DateTime executionDate, bool isLastUploadDay = false);
        Task NotifySupervisorsAboutPendingLessonsAsync(DateTime executionDate);
        Task SendMilestoneScheduleMonthlyReminderAsync(DateTime executionDate);
        Task SendMilestoneScheduleHistoryMonthlyRemindersAsync(DateTime executionDate);
    }
}
