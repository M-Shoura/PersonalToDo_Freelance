using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PersonalToDo_Freelance.Application.Interfaces;
using PersonalToDo_Freelance.Data;
using PersonalToDo_Freelance.Domain.Enums;

namespace PersonalToDo_Freelance.Infrastructure.Workers
{
    public class ReminderWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReminderWorker> _logger;

        public ReminderWorker(IServiceProvider serviceProvider, ILogger<ReminderWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReminderWorker starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessRemindersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing email reminders.");
                }

                // Check every 15 minutes
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }

            _logger.LogInformation("ReminderWorker stopping.");
        }

        private async Task ProcessRemindersAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var now = DateTime.UtcNow;
            var windowEnd = now.AddHours(1);

            // Fetch tasks that are due soon, not completed, not deleted, and haven't been reminded
            var tasksToRemind = await db.Tasks
                .Include(t => t.User)
                .Where(t => !t.IsDeleted &&
                            t.Status != TodoTaskStatus.Completed &&
                            t.Status != TodoTaskStatus.Cancelled &&
                            t.DueDate.HasValue &&
                            t.DueDate.Value > now &&
                            t.DueDate.Value <= windowEnd &&
                            t.ReminderSentAt == null &&
                            t.User != null &&
                            !string.IsNullOrEmpty(t.User.Email))
                .ToListAsync(stoppingToken);

            int count = 0;
            foreach (var task in tasksToRemind)
            {
                if (stoppingToken.IsCancellationRequested) break;

                var subject = $"Reminder: Task '{task.Title}' is due soon!";
                var body = $"<p>Hi {task.User!.UserName},</p><p>This is a reminder that your task <strong>{task.Title}</strong> is due at {task.DueDate!.Value:f} UTC.</p>";
                
                await emailService.SendEmailAsync(task.User.Email!, subject, body);

                task.ReminderSentAt = now;
                count++;
            }

            // Also check task occurrences (for recurring tasks)
            var occurrencesToRemind = await db.TaskOccurrences
                .Include(o => o.TodoTask)
                .ThenInclude(t => t!.User)
                .Where(o => !o.IsDeleted &&
                            !o.TodoTask!.IsDeleted &&
                            o.Status == OccurrenceStatus.Pending &&
                            o.OccurrenceDate > now &&
                            o.OccurrenceDate <= windowEnd &&
                            o.ReminderSentAt == null &&
                            o.TodoTask.User != null &&
                            !string.IsNullOrEmpty(o.TodoTask.User.Email))
                .ToListAsync(stoppingToken);

            foreach (var occurrence in occurrencesToRemind)
            {
                if (stoppingToken.IsCancellationRequested) break;

                var subject = $"Reminder: Recurring Task '{occurrence.TodoTask!.Title}' is due soon!";
                var body = $"<p>Hi {occurrence.TodoTask.User!.UserName},</p><p>This is a reminder that your recurring task <strong>{occurrence.TodoTask.Title}</strong> is scheduled for {occurrence.OccurrenceDate:f} UTC.</p>";
                
                await emailService.SendEmailAsync(occurrence.TodoTask.User.Email!, subject, body);

                occurrence.ReminderSentAt = now;
                count++;
            }

            if (count > 0)
            {
                await db.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Sent {Count} email reminders.", count);
            }
        }
    }
}
