using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PersonalToDo_Freelance.Application.Interfaces;
using PersonalToDo_Freelance.Application.ViewModels;
using PersonalToDo_Freelance.Data;
using PersonalToDo_Freelance.Domain.Entities;
using PersonalToDo_Freelance.Domain.Enums;

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

        public async Task<(bool Succeeded, string? Error)> RescheduleAsync(long id, DateTime newDueDate)
        {
            var userId = _user.UserId ?? string.Empty;
            var t = await _db.Tasks.Where(x => x.Id == id && x.UserId == userId && !x.IsDeleted).FirstOrDefaultAsync();
            if (t == null) return (false, "Task not found.");
            if (t.Status == Domain.Enums.TodoTaskStatus.Completed || t.Status == Domain.Enums.TodoTaskStatus.Cancelled)
                return (false, "Cannot reschedule completed or cancelled tasks.");
            var newDate = newDueDate.Date;
            var today = DateTime.UtcNow.Date;
            if (newDate < today) return (false, "New due date must be today or later.");
            t.DueDate = newDate;
            if (t.StartDate.HasValue && t.StartDate.Value.Date > newDate) t.StartDate = newDate;
            t.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<int> BulkRescheduleOverdueAsync(DateTime newDueDate)
        {
            var userId = _user.UserId ?? string.Empty;
            var today = DateTime.UtcNow.Date;
            var newDate = newDueDate.Date;
            if (newDate < today) return 0;
            var items = await _db.Tasks.Where(t => t.UserId == userId && !t.IsDeleted && t.DueDate.HasValue && t.DueDate.Value.Date < today && t.Status != Domain.Enums.TodoTaskStatus.Completed && t.Status != Domain.Enums.TodoTaskStatus.Cancelled).ToListAsync();
            foreach (var t in items)
            {
                t.DueDate = newDate;
                if (t.StartDate.HasValue && t.StartDate.Value.Date > newDate) t.StartDate = newDate;
                t.UpdatedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();
            return items.Count;
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
            var t = await _db.Tasks.AsNoTracking()
                .Include(x => x.RecurrenceRule)
                .Where(x => x.Id == id && x.UserId == userId && !x.IsDeleted)
                .FirstOrDefaultAsync();
            if (t == null) return null;
            return new TaskEditViewModel
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                CategoryId = t.CategoryId,
                Priority = t.Priority,
                StartDate = t.StartDate,
                DueDate = t.DueDate,
                Recurrence = ToRecurrenceViewModel(t.RecurrenceRule)
            };
        }

        public async Task<(bool Succeeded, string? Error)> UpdateAsync(TaskEditViewModel model)
        {
            var userId = _user.UserId ?? string.Empty;
            var t = await _db.Tasks
                .Include(x => x.RecurrenceRule)
                .Where(x => x.Id == model.Id && x.UserId == userId && !x.IsDeleted)
                .FirstOrDefaultAsync();
            if (t == null) return (false, "Task not found.");
            if (model.CategoryId.HasValue)
            {
                var cat = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == model.CategoryId.Value && c.UserId == userId && !c.IsDeleted);
                if (cat == null) return (false, "Selected category not found.");
            }
            if (model.StartDate.HasValue && model.DueDate.HasValue && model.StartDate > model.DueDate)
                return (false, "Start date cannot be after due date.");
            var recurrenceError = ValidateRecurrence(model.Recurrence);
            if (recurrenceError != null) return (false, recurrenceError);
            t.Title = model.Title;
            t.Description = model.Description;
            t.CategoryId = model.CategoryId;
            t.Priority = model.Priority;
            t.StartDate = model.StartDate;
            t.DueDate = model.DueDate;
            t.UpdatedAt = DateTime.UtcNow;
            ApplyRecurrence(t, model.Recurrence, userId);
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<IReadOnlyList<TaskListItemViewModel>> GetUserTasksAsync(Application.ViewModels.TaskQueryParameters? query = null)
        {
            var userId = _user.UserId ?? string.Empty;
            query ??= new Application.ViewModels.TaskQueryParameters();
            var q = _db.Tasks.AsNoTracking().Where(t => t.UserId == userId && !t.IsDeleted);
            if (query.Status.HasValue)
                q = q.Where(t => t.Status == query.Status.Value);
            if (query.CategoryId.HasValue)
                q = q.Where(t => t.CategoryId == query.CategoryId.Value);
            if (query.Priority.HasValue)
                q = q.Where(t => t.Priority == query.Priority.Value);
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var s = query.SearchTerm.Trim();
                var sLower = s.ToLower();
                q = q.Where(t => (t.Title != null && EF.Functions.Like(t.Title.ToLower(), "%" + sLower + "%")) || (t.Description != null && EF.Functions.Like(t.Description.ToLower(), "%" + sLower + "%")));
            }
            var now = DateTime.UtcNow.Date;
            if (query.DateFilter != Application.ViewModels.DateRangeFilter.None)
            {
                switch (query.DateFilter)
                {
                    case Application.ViewModels.DateRangeFilter.Today:
                        q = q.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == now);
                        break;
                    case Application.ViewModels.DateRangeFilter.Tomorrow:
                        var tom = now.AddDays(1);
                        q = q.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == tom);
                        break;
                    case Application.ViewModels.DateRangeFilter.ThisWeek:
                        var start = now;
                        var end = now.AddDays(7);
                        q = q.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date >= start && t.DueDate.Value.Date <= end);
                        break;
                    case Application.ViewModels.DateRangeFilter.ThisMonth:
                        var monthStart = new DateTime(now.Year, now.Month, 1);
                        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                        q = q.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date >= monthStart && t.DueDate.Value.Date <= monthEnd);
                        break;
                    case Application.ViewModels.DateRangeFilter.Custom:
                        if (query.CustomStart.HasValue)
                            q = q.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date >= query.CustomStart.Value.Date);
                        if (query.CustomEnd.HasValue)
                            q = q.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date <= query.CustomEnd.Value.Date);
                        break;
                    case Application.ViewModels.DateRangeFilter.Overdue:
                        q = q.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date < now && t.Status != Domain.Enums.TodoTaskStatus.Completed && t.Status != Domain.Enums.TodoTaskStatus.Cancelled);
                        break;
                }
            }
            IOrderedQueryable<Domain.Entities.TodoTask>? ordered = null;
            switch (query.SortBy)
            {
                case Application.ViewModels.TaskSortField.DueDate:
                    ordered = query.SortDirection == Application.ViewModels.SortDirection.Asc ? q.OrderBy(t => t.DueDate) : q.OrderByDescending(t => t.DueDate);
                    break;
                case Application.ViewModels.TaskSortField.Priority:
                    ordered = query.SortDirection == Application.ViewModels.SortDirection.Asc ? q.OrderBy(t => t.Priority) : q.OrderByDescending(t => t.Priority);
                    break;
                case Application.ViewModels.TaskSortField.CreatedAt:
                    ordered = query.SortDirection == Application.ViewModels.SortDirection.Asc ? q.OrderBy(t => t.CreatedAt) : q.OrderByDescending(t => t.CreatedAt);
                    break;
                case Application.ViewModels.TaskSortField.UpdatedAt:
                    ordered = query.SortDirection == Application.ViewModels.SortDirection.Asc ? q.OrderBy(t => t.UpdatedAt) : q.OrderByDescending(t => t.UpdatedAt);
                    break;
                case Application.ViewModels.TaskSortField.CompletedAt:
                    ordered = query.SortDirection == Application.ViewModels.SortDirection.Asc ? q.OrderBy(t => t.CompletedAt) : q.OrderByDescending(t => t.CompletedAt);
                    break;
                case Application.ViewModels.TaskSortField.Title:
                    ordered = query.SortDirection == Application.ViewModels.SortDirection.Asc ? q.OrderBy(t => t.Title) : q.OrderByDescending(t => t.Title);
                    break;
                default:
                    ordered = q.OrderBy(t => t.DueDate);
                    break;
            }
            var list = await ordered.Select(t => new TaskListItemViewModel
            {
                Id = t.Id,
                Title = t.Title,
                Priority = t.Priority,
                DueDate = t.DueDate,
                Status = t.Status,
                IsOverdue = t.DueDate.HasValue && t.DueDate.Value.Date < now && t.Status != Domain.Enums.TodoTaskStatus.Completed && t.Status != Domain.Enums.TodoTaskStatus.Cancelled
            }).ToListAsync();
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
            var recurrenceError = ValidateRecurrence(model.Recurrence);
            if (recurrenceError != null) return (false, recurrenceError, null);
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
            ApplyRecurrence(task, model.Recurrence, userId);
            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();
            return (true, null, task.Id);
        }

        private static string? ValidateRecurrence(RecurrenceRuleViewModel recurrence)
        {
            var result = recurrence.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(recurrence)).FirstOrDefault();
            return result?.ErrorMessage;
        }

        private static RecurrenceRuleViewModel ToRecurrenceViewModel(RecurrenceRule? rule)
        {
            if (rule == null || rule.IsDeleted || rule.Type == RecurrenceType.None)
            {
                return new RecurrenceRuleViewModel();
            }

            var endCondition = RecurrenceEndCondition.Never;
            if (rule.EndDate.HasValue)
            {
                endCondition = RecurrenceEndCondition.OnDate;
            }
            else if (rule.OccurrenceCount.HasValue)
            {
                endCondition = RecurrenceEndCondition.AfterOccurrences;
            }

            return new RecurrenceRuleViewModel
            {
                IsRecurring = true,
                Type = rule.Type,
                Interval = rule.Interval,
                DaysOfWeek = rule.DaysOfWeek,
                EndCondition = endCondition,
                EndDate = rule.EndDate,
                OccurrenceCount = rule.OccurrenceCount
            };
        }

        private static void ApplyRecurrence(TodoTask task, RecurrenceRuleViewModel recurrence, string userId)
        {
            if (!recurrence.IsRecurring)
            {
                if (task.RecurrenceRule != null)
                {
                    task.RecurrenceRule.IsDeleted = true;
                    task.RecurrenceRule.Type = RecurrenceType.None;
                    task.RecurrenceRule.UpdatedAt = DateTime.UtcNow;
                }

                return;
            }

            task.RecurrenceRule ??= new RecurrenceRule
            {
                CreatedAt = DateTime.UtcNow
            };

            task.RecurrenceRule.UserId = userId;
            task.RecurrenceRule.Type = recurrence.Type;
            task.RecurrenceRule.Interval = recurrence.Interval;
            task.RecurrenceRule.DaysOfWeek = recurrence.Type == RecurrenceType.Weekly ? recurrence.DaysOfWeek : DaysOfWeekFlags.None;
            task.RecurrenceRule.StartDate = task.StartDate?.Date ?? task.DueDate?.Date ?? DateTime.UtcNow.Date;
            task.RecurrenceRule.EndDate = recurrence.EndCondition == RecurrenceEndCondition.OnDate ? recurrence.EndDate?.Date : null;
            task.RecurrenceRule.OccurrenceCount = recurrence.EndCondition == RecurrenceEndCondition.AfterOccurrences ? recurrence.OccurrenceCount : null;
            task.RecurrenceRule.IsDeleted = false;
            task.RecurrenceRule.UpdatedAt = DateTime.UtcNow;
        }
    }
}
