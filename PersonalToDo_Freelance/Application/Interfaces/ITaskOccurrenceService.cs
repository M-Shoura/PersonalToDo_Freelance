using PersonalToDo_Freelance.Application.ViewModels;
using PersonalToDo_Freelance.Domain.Enums;

namespace PersonalToDo_Freelance.Application.Interfaces
{
    public interface ITaskOccurrenceService
    {
        Task<IReadOnlyList<TaskOccurrenceViewModel>> GenerateForTaskAsync(long taskId, DateTime windowEndDate);
        Task<IReadOnlyList<TaskOccurrenceViewModel>> GetForTaskAsync(long taskId);
        Task<TaskOccurrenceViewModel?> GetDetailsAsync(long occurrenceId);
        Task<(bool Succeeded, string? Error)> ChangeStatusAsync(long occurrenceId, OccurrenceStatus status);
        Task<(bool Succeeded, string? Error)> ReopenAsync(long occurrenceId);
        Task<(bool Succeeded, string? Error)> RescheduleAsync(long occurrenceId, DateTime newScheduledDate);
        Task<(bool Succeeded, string? Error)> SkipAsync(long occurrenceId);
    }
}
