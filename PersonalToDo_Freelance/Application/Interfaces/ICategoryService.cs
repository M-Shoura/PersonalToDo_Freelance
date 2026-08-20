using System.Collections.Generic;
using System.Threading.Tasks;
using PersonalToDo_Freelance.Application.ViewModels;

namespace PersonalToDo_Freelance.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<IReadOnlyList<CategoryListItemViewModel>> GetAllAsync();
        Task<CategoryEditViewModel?> GetForEditAsync(long id);
        Task<(bool Succeeded, string? Error)> CreateAsync(CategoryCreateViewModel model);
        Task<(bool Succeeded, string? Error)> UpdateAsync(CategoryEditViewModel model);
        Task<(bool Succeeded, string? Error)> DeleteAsync(long id);
    }
}
