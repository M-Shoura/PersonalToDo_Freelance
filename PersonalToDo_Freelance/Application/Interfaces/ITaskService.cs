using System.Threading.Tasks;
using PersonalToDo_Freelance.Application.ViewModels;

namespace PersonalToDo_Freelance.Application.Interfaces
{
    public interface ITaskService
    {
        Task<(bool Succeeded, string? Error, long? Id)> CreateAsync(TaskCreateViewModel model);
    }
}
