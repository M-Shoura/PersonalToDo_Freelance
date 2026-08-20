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
