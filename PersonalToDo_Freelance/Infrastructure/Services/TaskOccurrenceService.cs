using Microsoft.EntityFrameworkCore;
using PersonalToDo_Freelance.Application.Interfaces;
using PersonalToDo_Freelance.Application.ViewModels;
using PersonalToDo_Freelance.Data;
using PersonalToDo_Freelance.Domain.Entities;
using PersonalToDo_Freelance.Domain.Enums;

namespace PersonalToDo_Freelance.Infrastructure.Services
{
    public class TaskOccurrenceService : ITaskOccurrenceService
    {
        private const int DefaultSafetyLimit = 10000;

        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _user;

        public TaskOccurrenceService(ApplicationDbContext db, ICurrentUserService user)
        {
            _db = db;
            _user = user;
        }

        public async Task<IReadOnlyList<TaskOccurrenceViewModel>> GenerateForTaskAsync(long taskId, DateTime windowEndDate)
        {
            var userId = _user.UserId ?? string.Empty;
            return await GenerateForTaskSystemAsync(taskId, userId, windowEndDate);
        }

        public async Task<IReadOnlyList<TaskOccurrenceViewModel>> GenerateForTaskSystemAsync(long taskId, string userId, DateTime windowEndDate)
        {
            var task = await _db.Tasks
                .Include(t => t.RecurrenceRule)
                .Where(t => t.Id == taskId && t.UserId == userId && !t.IsDeleted)
                .FirstOrDefaultAsync();

            if (task?.RecurrenceRule == null || task.RecurrenceRule.IsDeleted || task.RecurrenceRule.Type == RecurrenceType.None)
            {
                return Array.Empty<TaskOccurrenceViewModel>();
            }

            var rule = task.RecurrenceRule;
            var windowEnd = windowEndDate.Date;
            var existingDates = await _db.TaskOccurrences
                .Where(o => o.TodoTaskId == task.Id && o.RecurrenceRuleId == rule.Id && !o.IsDeleted)
                .Select(o => (o.OriginalOccurrenceDate ?? o.OccurrenceDate).Date)
                .ToListAsync();
            var existingDateSet = existingDates.ToHashSet();

            var scheduledDates = BuildSchedule(rule, windowEnd);
            foreach (var scheduledDate in scheduledDates)
            {
                if (existingDateSet.Contains(scheduledDate))
                {
                    continue;
                }

                _db.TaskOccurrences.Add(new TaskOccurrence
                {
                    TodoTaskId = task.Id,
                    RecurrenceRuleId = rule.Id,
                    OccurrenceDate = scheduledDate,
                    OriginalOccurrenceDate = scheduledDate,
                    Status = OccurrenceStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                });
                existingDateSet.Add(scheduledDate);
            }

            await _db.SaveChangesAsync();
            return await GetForTaskSystemAsync(taskId, userId);
        }

        private async Task<IReadOnlyList<TaskOccurrenceViewModel>> GetForTaskSystemAsync(long taskId, string userId)
        {
            return await _db.TaskOccurrences.AsNoTracking()
                .Include(o => o.TodoTask)
                .Where(o => o.TodoTaskId == taskId && o.TodoTask != null && o.TodoTask.UserId == userId && !o.TodoTask.IsDeleted && !o.IsDeleted)
                .OrderBy(o => o.OccurrenceDate)
                .Select(o => new TaskOccurrenceViewModel
                {
                    Id = o.Id,
                    TodoTaskId = o.TodoTaskId,
                    RecurrenceRuleId = o.RecurrenceRuleId,
                    TaskTitle = o.TodoTask!.Title,
                    ScheduledDate = o.OccurrenceDate,
                    OriginalScheduledDate = (o.OriginalOccurrenceDate ?? o.OccurrenceDate).Date,
                    Status = o.Status,
                    CompletedAt = o.CompletedAt,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<IReadOnlyList<TaskOccurrenceViewModel>> GetForTaskAsync(long taskId)
        {
            var userId = _user.UserId ?? string.Empty;
            return await _db.TaskOccurrences.AsNoTracking()
                .Include(o => o.TodoTask)
                .Where(o => o.TodoTaskId == taskId && o.TodoTask != null && o.TodoTask.UserId == userId && !o.TodoTask.IsDeleted && !o.IsDeleted)
                .OrderBy(o => o.OccurrenceDate)
                .Select(o => new TaskOccurrenceViewModel
                {
                    Id = o.Id,
                    TodoTaskId = o.TodoTaskId,
                    RecurrenceRuleId = o.RecurrenceRuleId,
                    TaskTitle = o.TodoTask!.Title,
                    ScheduledDate = o.OccurrenceDate,
                    OriginalScheduledDate = (o.OriginalOccurrenceDate ?? o.OccurrenceDate).Date,
                    Status = o.Status,
                    CompletedAt = o.CompletedAt,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<TaskOccurrenceViewModel?> GetDetailsAsync(long occurrenceId)
        {
            var userId = _user.UserId ?? string.Empty;
            return await _db.TaskOccurrences.AsNoTracking()
                .Include(o => o.TodoTask)
                .Where(o => o.Id == occurrenceId && o.TodoTask != null && o.TodoTask.UserId == userId && !o.TodoTask.IsDeleted && !o.IsDeleted)
                .Select(o => new TaskOccurrenceViewModel
                {
                    Id = o.Id,
                    TodoTaskId = o.TodoTaskId,
                    RecurrenceRuleId = o.RecurrenceRuleId,
                    TaskTitle = o.TodoTask!.Title,
                    ScheduledDate = o.OccurrenceDate,
                    OriginalScheduledDate = (o.OriginalOccurrenceDate ?? o.OccurrenceDate).Date,
                    Status = o.Status,
                    CompletedAt = o.CompletedAt,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<(bool Succeeded, string? Error)> ChangeStatusAsync(long occurrenceId, OccurrenceStatus status)
        {
            var occurrence = await FindOwnedOccurrenceAsync(occurrenceId);
            if (occurrence == null) return (false, "Occurrence not found.");

            occurrence.Status = status;
            occurrence.CompletedAt = status == OccurrenceStatus.Completed ? DateTime.UtcNow : null;
            occurrence.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public Task<(bool Succeeded, string? Error)> ReopenAsync(long occurrenceId)
        {
            return ChangeStatusAsync(occurrenceId, OccurrenceStatus.Pending);
        }

        public async Task<(bool Succeeded, string? Error)> RescheduleAsync(long occurrenceId, DateTime newScheduledDate)
        {
            var occurrence = await FindOwnedOccurrenceAsync(occurrenceId);
            if (occurrence == null) return (false, "Occurrence not found.");

            var newDate = newScheduledDate.Date;
            var duplicateExists = await _db.TaskOccurrences.AnyAsync(o =>
                o.Id != occurrence.Id &&
                o.TodoTaskId == occurrence.TodoTaskId &&
                o.RecurrenceRuleId == occurrence.RecurrenceRuleId &&
                o.OccurrenceDate.Date == newDate &&
                !o.IsDeleted);

            if (duplicateExists) return (false, "Another occurrence already exists on that date.");

            occurrence.OccurrenceDate = newDate;
            occurrence.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public Task<(bool Succeeded, string? Error)> SkipAsync(long occurrenceId)
        {
            return ChangeStatusAsync(occurrenceId, OccurrenceStatus.Skipped);
        }

        private async Task<TaskOccurrence?> FindOwnedOccurrenceAsync(long occurrenceId)
        {
            var userId = _user.UserId ?? string.Empty;
            return await _db.TaskOccurrences
                .Include(o => o.TodoTask)
                .Where(o => o.Id == occurrenceId && o.TodoTask != null && o.TodoTask.UserId == userId && !o.TodoTask.IsDeleted && !o.IsDeleted)
                .FirstOrDefaultAsync();
        }

        private static IReadOnlyList<DateTime> BuildSchedule(RecurrenceRule rule, DateTime windowEndDate)
        {
            var start = (rule.StartDate ?? DateTime.UtcNow.Date).Date;
            var hardEnd = rule.EndDate.HasValue && rule.EndDate.Value.Date < windowEndDate.Date
                ? rule.EndDate.Value.Date
                : windowEndDate.Date;

            if (hardEnd < start)
            {
                return Array.Empty<DateTime>();
            }

            return rule.Type switch
            {
                RecurrenceType.Daily => BuildIntervalSchedule(start, hardEnd, rule.Interval, (d, i) => d.AddDays(i), rule.OccurrenceCount),
                RecurrenceType.Custom => BuildIntervalSchedule(start, hardEnd, rule.Interval, (d, i) => d.AddDays(i), rule.OccurrenceCount),
                RecurrenceType.Monthly => BuildIntervalSchedule(start, hardEnd, rule.Interval, (d, i) => d.AddMonths(i), rule.OccurrenceCount),
                RecurrenceType.Yearly => BuildIntervalSchedule(start, hardEnd, rule.Interval, (d, i) => d.AddYears(i), rule.OccurrenceCount),
                RecurrenceType.Weekly => BuildWeeklySchedule(start, hardEnd, rule.Interval, rule.DaysOfWeek, rule.OccurrenceCount),
                _ => Array.Empty<DateTime>()
            };
        }

        private static IReadOnlyList<DateTime> BuildIntervalSchedule(DateTime start, DateTime hardEnd, int interval, Func<DateTime, int, DateTime> next, int? maxCount)
        {
            interval = Math.Max(1, interval);
            var dates = new List<DateTime>();
            var current = start.Date;

            for (var guard = 0; current <= hardEnd && guard < DefaultSafetyLimit; guard++)
            {
                dates.Add(current);
                if (maxCount.HasValue && dates.Count >= maxCount.Value) break;
                current = next(current, interval).Date;
            }

            return dates;
        }

        private static IReadOnlyList<DateTime> BuildWeeklySchedule(DateTime start, DateTime hardEnd, int interval, DaysOfWeekFlags daysOfWeek, int? maxCount)
        {
            interval = Math.Max(1, interval);
            if (daysOfWeek == DaysOfWeekFlags.None)
            {
                return Array.Empty<DateTime>();
            }

            var dates = new List<DateTime>();
            var current = start.Date;

            for (var guard = 0; current <= hardEnd && guard < DefaultSafetyLimit; guard++, current = current.AddDays(1))
            {
                var weekOffset = (int)((current - start).TotalDays / 7);
                if (weekOffset % interval != 0) continue;
                if (!IsSelectedWeekday(current, daysOfWeek)) continue;

                dates.Add(current);
                if (maxCount.HasValue && dates.Count >= maxCount.Value) break;
            }

            return dates;
        }

        private static bool IsSelectedWeekday(DateTime date, DaysOfWeekFlags daysOfWeek)
        {
            var flag = date.DayOfWeek switch
            {
                DayOfWeek.Sunday => DaysOfWeekFlags.Sunday,
                DayOfWeek.Monday => DaysOfWeekFlags.Monday,
                DayOfWeek.Tuesday => DaysOfWeekFlags.Tuesday,
                DayOfWeek.Wednesday => DaysOfWeekFlags.Wednesday,
                DayOfWeek.Thursday => DaysOfWeekFlags.Thursday,
                DayOfWeek.Friday => DaysOfWeekFlags.Friday,
                DayOfWeek.Saturday => DaysOfWeekFlags.Saturday,
                _ => DaysOfWeekFlags.None
            };

            return daysOfWeek.HasFlag(flag);
        }
    }
}
