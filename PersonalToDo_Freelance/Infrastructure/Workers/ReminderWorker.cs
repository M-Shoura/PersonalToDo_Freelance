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
                var body = GenerateEmailTemplate(task.User!.UserName ?? "User", task.Title, task.DueDate!.Value, false);
                
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
                var body = GenerateEmailTemplate(occurrence.TodoTask.User!.UserName ?? "User", occurrence.TodoTask.Title, occurrence.OccurrenceDate, true);
                
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

        private string GenerateEmailTemplate(string userName, string taskTitle, DateTime dueDate, bool isRecurring)
        {
            var taskType = isRecurring ? "recurring task" : "task";
            return $@"
<div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; background-color: #f8fafc; padding: 30px; border-radius: 12px;"">
    <div style=""background-color: #4f46e5; padding: 25px; border-radius: 12px 12px 0 0; text-align: center;"">
        <h2 style=""color: #ffffff; margin: 0; font-size: 24px; font-weight: 600; letter-spacing: 0.5px;"">TaskPulse Reminder</h2>
    </div>
    <div style=""background-color: #ffffff; padding: 40px 30px; border-radius: 0 0 12px 12px; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05), 0 2px 4px -1px rgba(0, 0, 0, 0.03);"">
        <h3 style=""color: #0f172a; margin-top: 0; font-size: 20px;"">Hello {userName},</h3>
        <p style=""color: #475569; font-size: 16px; line-height: 1.6; margin-bottom: 25px;"">
            This is a quick reminder that your {taskType} is due soon! Stay on top of your goals.
        </p>
        
        <div style=""background-color: #f1f5f9; padding: 20px; border-left: 4px solid #4f46e5; border-radius: 0 8px 8px 0; margin: 30px 0;"">
            <div style=""font-size: 18px; font-weight: bold; color: #0f172a; margin-bottom: 8px;"">{taskTitle}</div>
            <div style=""color: #64748b; font-size: 14px;"">
                <span style=""font-weight: 600; color: #475569;"">Due at:</span> {dueDate:f} UTC
            </div>
        </div>
        
        <p style=""color: #475569; font-size: 16px; line-height: 1.6;"">
            Head over to your dashboard to mark it as completed or reschedule it if needed.
        </p>
        
        <div style=""text-align: center; margin-top: 40px;"">
            <a href=""#"" style=""background-color: #4f46e5; color: #ffffff; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: 600; font-size: 16px; display: inline-block; box-shadow: 0 4px 6px -1px rgba(79, 70, 229, 0.3);"">Open Dashboard</a>
        </div>
    </div>
    <div style=""text-align: center; margin-top: 25px; color: #94a3b8; font-size: 13px;"">
        &copy; {DateTime.UtcNow.Year} TaskPulse. All rights reserved.<br>
        <span style=""font-size: 11px; margin-top: 10px; display: inline-block;"">You received this email because you have active reminders set.</span>
    </div>
</div>";
        }
    }
}
