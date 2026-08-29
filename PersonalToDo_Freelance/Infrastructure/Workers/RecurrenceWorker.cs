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
    public class RecurrenceWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RecurrenceWorker> _logger;

        public RecurrenceWorker(IServiceProvider serviceProvider, ILogger<RecurrenceWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RecurrenceWorker starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Running daily recurrence generation at: {time}", DateTimeOffset.Now);
                    await GenerateOccurrencesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while generating occurrences in the background.");
                }

                // Wait 24 hours before running again. For testing, we could lower this, but 24h is appropriate for production.
                // Or you can configure it via IConfiguration.
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }

            _logger.LogInformation("RecurrenceWorker stopping.");
        }

        private async Task GenerateOccurrencesAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var occurrenceService = scope.ServiceProvider.GetRequiredService<ITaskOccurrenceService>();

            // Get all active tasks with recurrence rules
            var activeRecurringTasks = await db.Tasks
                .Include(t => t.RecurrenceRule)
                .Where(t => !t.IsDeleted && 
                            t.RecurrenceRule != null && 
                            !t.RecurrenceRule.IsDeleted && 
                            t.RecurrenceRule.Type != RecurrenceType.None)
                .Select(t => new { t.Id, t.UserId })
                .ToListAsync(stoppingToken);

            var targetDate = DateTime.UtcNow.Date.AddDays(60);
            int count = 0;

            foreach (var taskInfo in activeRecurringTasks)
            {
                if (stoppingToken.IsCancellationRequested) break;

                try
                {
                    await occurrenceService.GenerateForTaskSystemAsync(taskInfo.Id, taskInfo.UserId, targetDate);
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate occurrences for task {TaskId}", taskInfo.Id);
                }
            }

            _logger.LogInformation("Successfully processed recurrence generation for {Count} tasks.", count);
        }
    }
}
