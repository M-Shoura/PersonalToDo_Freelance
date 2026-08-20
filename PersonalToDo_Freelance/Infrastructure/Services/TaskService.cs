using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PersonalToDo_Freelance.Application.Interfaces;
using PersonalToDo_Freelance.Application.ViewModels;
using PersonalToDo_Freelance.Data;
using PersonalToDo_Freelance.Domain.Entities;

namespace PersonalToDo_Freelance.Infrastructure.Services
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _user;

        public TaskService(ApplicationDbContext db, ICurrentUserService user)
        {
            _db = db;
            _user = user;
        }

        public async Task<TaskDetailsViewModel?> GetDetailsAsync(long id)
        {
            var userId = _user.UserId ?? string.Empty;
            var t = await _db.Tasks.AsNoTracking()
                .Include(x => x.Category)
                .Where(x => x.Id == id && x.UserId == userId && !x.IsDeleted)
                .FirstOrDefaultAsync();
            if (t == null) return null;
            return new TaskDetailsViewModel
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                CategoryId = t.CategoryId,
                CategoryName = t.Category?.Name,
                Priority = t.Priority,
                Status = t.Status,
                StartDate = t.StartDate,
                DueDate = t.DueDate,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                CompletedAt = t.CompletedAt,
                IsOverdue = t.DueDate.HasValue && t.DueDate.Value.Date < DateTime.UtcNow.Date && t.Status != Domain.Enums.TodoTaskStatus.Completed && t.Status != Domain.Enums.TodoTaskStatus.Cancelled
            };
        }

        public async Task<TaskEditViewModel?> GetForEditAsync(long id)
        {
            var userId = _user.UserId ?? string.Empty;
            var t = await _db.Tasks.AsNoTracking().Where(x => x.Id == id && x.UserId == userId && !x.IsDeleted).FirstOrDefaultAsync();
            if (t == null) return null;
            return new TaskEditViewModel
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                CategoryId = t.CategoryId,
                Priority = t.Priority,
                StartDate = t.StartDate,
                DueDate = t.DueDate
            };
        }

        public async Task<(bool Succeeded, string? Error)> UpdateAsync(TaskEditViewModel model)
        {
            var userId = _user.UserId ?? string.Empty;
            var t = await _db.Tasks.Where(x => x.Id == model.Id && x.UserId == userId && !x.IsDeleted).FirstOrDefaultAsync();
            if (t == null) return (false, "Task not found.");
            if (model.CategoryId.HasValue)
            {
                var cat = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == model.CategoryId.Value && c.UserId == userId && !c.IsDeleted);
                if (cat == null) return (false, "Selected category not found.");
            }
            if (model.StartDate.HasValue && model.DueDate.HasValue && model.StartDate > model.DueDate)
                return (false, "Start date cannot be after due date.");
            t.Title = model.Title;
            t.Description = model.Description;
            t.CategoryId = model.CategoryId;
            t.Priority = model.Priority;
            t.StartDate = model.StartDate;
            t.DueDate = model.DueDate;
            t.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<IReadOnlyList<TaskListItemViewModel>> GetUserTasksAsync()
        {
            var userId = _user.UserId ?? string.Empty;
            var list = await _db.Tasks.AsNoTracking()
                .Where(t => t.UserId == userId && !t.IsDeleted)
                .OrderBy(t => t.DueDate)
                .Select(t => new TaskListItemViewModel
                {
                    Id = t.Id,
                    Title = t.Title,
                    Priority = t.Priority,
                    DueDate = t.DueDate,
                    Status = t.Status,
                    IsOverdue = t.DueDate.HasValue && t.DueDate.Value.Date < DateTime.UtcNow.Date && t.Status != Domain.Enums.TodoTaskStatus.Completed && t.Status != Domain.Enums.TodoTaskStatus.Cancelled
                })
                .ToListAsync();
            return list;
        }

        public async Task<(bool Succeeded, string? Error)> DeleteAsync(long id)
        {
            var userId = _user.UserId ?? string.Empty;
            var t = await _db.Tasks.Where(x => x.Id == id && x.UserId == userId && !x.IsDeleted).FirstOrDefaultAsync();
            if (t == null) return (false, "Task not found.");
            t.IsDeleted = true;
            t.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Succeeded, string? Error)> ChangeStatusAsync(long id, Domain.Enums.TodoTaskStatus newStatus)
        {
            var userId = _user.UserId ?? string.Empty;
            var t = await _db.Tasks.Where(x => x.Id == id && x.UserId == userId && !x.IsDeleted).FirstOrDefaultAsync();
            if (t == null) return (false, "Task not found.");
            var old = t.Status;
            if (old == newStatus) return (true, null);
            if (newStatus == Domain.Enums.TodoTaskStatus.Completed)
            {
                t.Status = newStatus;
                t.CompletedAt = DateTime.UtcNow;
            }
            else
            {
                if (old == Domain.Enums.TodoTaskStatus.Completed)
                {
                    t.CompletedAt = null;
                }
                t.Status = newStatus;
            }
            t.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Succeeded, string? Error, long? Id)> CreateAsync(TaskCreateViewModel model)
        {
            var userId = _user.UserId ?? string.Empty;
            if (model.CategoryId.HasValue)
            {
                var cat = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == model.CategoryId.Value && c.UserId == userId && !c.IsDeleted);
                if (cat == null) return (false, "Selected category not found.", null);
            }
            if (model.StartDate.HasValue && model.DueDate.HasValue && model.StartDate > model.DueDate)
                return (false, "Start date cannot be after due date.", null);
            var task = new TodoTask
            {
                UserId = userId,
                Title = model.Title,
                Description = model.Description,
                CategoryId = model.CategoryId,
                Priority = model.Priority,
                Status = model.Status,
                StartDate = model.StartDate,
                DueDate = model.DueDate,
                CreatedAt = DateTime.UtcNow
            };
            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();
            return (true, null, task.Id);
        }
    }
}
