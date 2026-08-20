using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PersonalToDo_Freelance.Application.Interfaces;
using PersonalToDo_Freelance.Application.ViewModels;
using PersonalToDo_Freelance.Data;
using PersonalToDo_Freelance.Domain.Entities;

namespace PersonalToDo_Freelance.Infrastructure.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _user;

        public CategoryService(ApplicationDbContext db, ICurrentUserService user)
        {
            _db = db;
            _user = user;
        }

        public async Task<IReadOnlyList<CategoryListItemViewModel>> GetAllAsync()
        {
            var userId = _user.UserId ?? string.Empty;
            var items = await _db.Categories
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.Name)
                .Select(c => new CategoryListItemViewModel { Id = c.Id, Name = c.Name, Description = c.Description, IsDeleted = c.IsDeleted })
                .ToListAsync();
            return items;
        }

        public async Task<CategoryEditViewModel?> GetForEditAsync(long id)
        {
            var userId = _user.UserId ?? string.Empty;
            var cat = await _db.Categories.Where(c => c.UserId == userId && c.Id == id).FirstOrDefaultAsync();
            if (cat == null) return null;
            return new CategoryEditViewModel { Id = cat.Id, Name = cat.Name, Description = cat.Description, IsDeleted = cat.IsDeleted };
        }

        public async Task<(bool Succeeded, string? Error)> CreateAsync(CategoryCreateViewModel model)
        {
            var userId = _user.UserId ?? string.Empty;
            var exists = await _db.Categories.AnyAsync(c => c.UserId == userId && c.Name == model.Name && !c.IsDeleted);
            if (exists) return (false, "A category with the same name already exists.");
            var cat = new Category { UserId = userId, Name = model.Name, Description = model.Description };
            _db.Categories.Add(cat);
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Succeeded, string? Error)> UpdateAsync(CategoryEditViewModel model)
        {
            var userId = _user.UserId ?? string.Empty;
            var cat = await _db.Categories.Where(c => c.UserId == userId && c.Id == model.Id).FirstOrDefaultAsync();
            if (cat == null) return (false, "Category not found.");
            var duplicate = await _db.Categories.AnyAsync(c => c.UserId == userId && c.Id != model.Id && c.Name == model.Name && !c.IsDeleted);
            if (duplicate) return (false, "A category with the same name already exists.");
            cat.Name = model.Name;
            cat.Description = model.Description;
            cat.IsDeleted = model.IsDeleted;
            cat.UpdatedAt = System.DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Succeeded, string? Error)> DeleteAsync(long id)
        {
            var userId = _user.UserId ?? string.Empty;
            var cat = await _db.Categories.Where(c => c.UserId == userId && c.Id == id).FirstOrDefaultAsync();
            if (cat == null) return (false, "Category not found.");
            cat.IsDeleted = true;
            cat.UpdatedAt = System.DateTime.UtcNow;
            var tasks = await _db.Tasks.Where(t => t.UserId == userId && t.CategoryId == id).ToListAsync();
            foreach (var t in tasks)
            {
                t.CategoryId = null;
                t.UpdatedAt = System.DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();
            return (true, null);
        }
    }
}
