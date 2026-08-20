using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PersonalToDo_Freelance.Data;
using PersonalToDo_Freelance.Infrastructure.Services;
using Xunit;
using PersonalToDo_Freelance.Application.ViewModels;

namespace PersonalToDo_Freelance.Tests
{
    public class TaskStatusTests
    {
        private ApplicationDbContext CreateDbContext(string name)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(name).Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task MarkCompleted_SetsCompletedAt()
        {
            var db = CreateDbContext("status1");
            var user = new FakeCurrentUser { UserId = "u1" };
            var task = new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "u1", Title = "t" };
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            var service = new TaskService(db, user);
            var res = await service.ChangeStatusAsync(task.Id, PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus.Completed);
            Assert.True(res.Succeeded);
            var t = await db.Tasks.FindAsync(task.Id);
            Assert.Equal(PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus.Completed, t!.Status);
            Assert.NotNull(t.CompletedAt);
        }

        [Fact]
        public async Task Reopen_ClearsCompletedAt()
        {
            var db = CreateDbContext("status2");
            var user = new FakeCurrentUser { UserId = "u2" };
            var task = new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "u2", Title = "t", Status = PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus.Completed, CompletedAt = System.DateTime.UtcNow };
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            var service = new TaskService(db, user);
            var res = await service.ChangeStatusAsync(task.Id, PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus.InProgress);
            Assert.True(res.Succeeded);
            var t = await db.Tasks.FindAsync(task.Id);
            Assert.Equal(PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus.InProgress, t!.Status);
            Assert.Null(t.CompletedAt);
        }

        [Fact]
        public async Task Overdue_NotStoredButComputed()
        {
            var db = CreateDbContext("status3");
            var user = new FakeCurrentUser { UserId = "u3" };
            var task = new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "u3", Title = "t", DueDate = System.DateTime.UtcNow.AddDays(-2), Status = PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus.NotStarted };
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            var service = new TaskService(db, user);
            var details = await service.GetDetailsAsync(task.Id);
            Assert.True(details!.IsOverdue);
            Assert.NotEqual(PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus.Cancelled, details.Status);
            Assert.NotEqual(PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus.Completed, details.Status);
        }
    }
}
