using System.Threading.Tasks;
using PersonalToDo_Freelance.Application.ViewModels;

namespace PersonalToDo_Freelance.Application.Interfaces
{
    public interface ITaskService
    {
        Task<(bool Succeeded, string? Error, long? Id)> CreateAsync(TaskCreateViewModel model);
        Task<TaskDetailsViewModel?> GetDetailsAsync(long id);
        Task<TaskEditViewModel?> GetForEditAsync(long id);
        Task<(bool Succeeded, string? Error)> UpdateAsync(TaskEditViewModel model);
        Task<(bool Succeeded, string? Error)> DeleteAsync(long id);
    }
}
