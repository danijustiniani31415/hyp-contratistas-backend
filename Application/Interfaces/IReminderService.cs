using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Application.Interfaces
{
    public interface IReminderService
    {
        Task<bool> ExecuteReminders();
    }
}