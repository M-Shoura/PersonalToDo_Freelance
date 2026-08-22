using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PersonalToDo_Freelance.Data;
using PersonalToDo_Freelance.Infrastructure.Services;
using Xunit;
using PersonalToDo_Freelance.Domain.Entities;
using PersonalToDo_Freelance.Domain.Enums;

namespace PersonalToDo_Freelance.Tests
{
    public class StatisticsTests
    {
        private ApplicationDbContext CreateDbContext(string name)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(name).Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Statistics_Aggregations_AreCorrect()
        {
            var db = CreateDbContext("stats1");
            var user = new FakeCurrentUser { UserId = "u1" };
            var svc = new TaskService(db, user);

            var start = new DateTime(2026, 8, 1);
            var end = new DateTime(2026, 8, 7);

            // Created inside range
            db.Tasks.Add(new TodoTask { UserId = "u1", Title = "C1", CreatedAt = start.AddDays(1), DueDate = start.AddDays(2), Priority = TaskPriority.Medium, Status = TodoTaskStatus.NotStarted });
            db.Tasks.Add(new TodoTask { UserId = "u1", Title = "C2", CreatedAt = start.AddDays(2), DueDate = start.AddDays(3), Priority = TaskPriority.High, Status = TodoTaskStatus.Completed, CompletedAt = start.AddDays(3) });
            // Created before range
            db.Tasks.Add(new TodoTask { UserId = "u1", Title = "Old", CreatedAt = start.AddDays(-5), DueDate = start.AddDays(2), Priority = TaskPriority.Low, Status = TodoTaskStatus.NotStarted });
            // Overdue as of end
            db.Tasks.Add(new TodoTask { UserId = "u1", Title = "OD", CreatedAt = start.AddDays(1), DueDate = start.AddDays(-1), Status = TodoTaskStatus.NotStarted });
            // Other user
            db.Tasks.Add(new TodoTask { UserId = "other", Title = "Other", CreatedAt = start.AddDays(2), DueDate = start.AddDays(2), Status = TodoTaskStatus.NotStarted });

            await db.SaveChangesAsync();

            var stats = await svc.GetStatisticsAsync(start, end);

            Assert.Equal(3, stats.TasksCreated); // C1,C2,OD (other excluded)
            Assert.Equal(1, stats.TasksCompleted); // C2
            Assert.Equal(2, stats.PendingTasks); // C1 and Old? Pending counts tasks due in range and not completed/cancelled: C1 and Old => 2
            Assert.Equal(1, stats.OverdueTasks);
            Assert.True(stats.TasksByCategory != null);
            Assert.True(stats.TasksByPriority.Any());
            Assert.True(stats.CompletedPerDay.Any());
        }
    }
}
