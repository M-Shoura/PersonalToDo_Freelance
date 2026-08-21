using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PersonalToDo_Freelance.Data;
using PersonalToDo_Freelance.Infrastructure.Services;
using Xunit;
using PersonalToDo_Freelance.Domain.Entities;
using PersonalToDo_Freelance.Domain.Enums;

namespace PersonalToDo_Freelance.Tests
{
    public class DashboardMetricsTests
    {
        private ApplicationDbContext CreateDbContext(string name)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(name).Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Dashboard_ReturnsCorrectCountsForToday()
        {
            var db = CreateDbContext("dash1");
            var user = new FakeCurrentUser { UserId = "u1" };

            var today = DateTime.UtcNow.Date;
            // Due today, not completed
            db.Tasks.Add(new TodoTask { UserId = "u1", Title = "T1", DueDate = today, Status = TodoTaskStatus.NotStarted });
            // Due today, completed today
            db.Tasks.Add(new TodoTask { UserId = "u1", Title = "T2", DueDate = today, Status = TodoTaskStatus.Completed, CompletedAt = DateTime.UtcNow });
            // Due today, cancelled
            db.Tasks.Add(new TodoTask { UserId = "u1", Title = "T3", DueDate = today, Status = TodoTaskStatus.Cancelled });
            // Overdue
            db.Tasks.Add(new TodoTask { UserId = "u1", Title = "T4", DueDate = today.AddDays(-1), Status = TodoTaskStatus.NotStarted });
            // Other user's task should be ignored
            db.Tasks.Add(new TodoTask { UserId = "other", Title = "T5", DueDate = today, Status = TodoTaskStatus.NotStarted });

            await db.SaveChangesAsync();

            var svc = new TaskService(db, user);
            var vm = await svc.GetDashboardAsync(today);

            Assert.Equal(3, vm.TotalToday); // T1,T2,T3
            Assert.Equal(1, vm.CompletedToday); // T2
            Assert.Equal(1, vm.PendingToday); // T1 (T3 is cancelled)
            Assert.Equal(1, vm.Overdue); // T4
            Assert.Equal(1, vm.TodayTasks.Count);
            // Completion rate = 1/3
            Assert.Equal(1.0 / 3.0, vm.CompletionRate, 6);
        }
    }
}
