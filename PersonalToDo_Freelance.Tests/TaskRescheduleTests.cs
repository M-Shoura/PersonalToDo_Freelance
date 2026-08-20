using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using PersonalToDo_Freelance.Data;
using System.Linq;
using PersonalToDo_Freelance.Infrastructure.Services;
using PersonalToDo_Freelance.Application.ViewModels;

namespace PersonalToDo_Freelance.Tests
{
    public class TaskRescheduleTests
    {
        private ApplicationDbContext CreateDbContext(string name)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(name).Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Reschedule_SingleTask_ValidatesOwnershipAndDate()
        {
            var db = CreateDbContext("res1");
            var task = new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "u1", Title = "T1", DueDate = System.DateTime.UtcNow.AddDays(-1) };
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            var service = new TaskService(db, new FakeCurrentUser { UserId = "u1" });
            var (succeeded, error) = await service.RescheduleAsync(task.Id, System.DateTime.UtcNow.AddDays(2));
            Assert.True(succeeded);
            var t = await db.Tasks.FindAsync(task.Id);
            Assert.Equal(System.DateTime.UtcNow.Date.AddDays(2), t!.DueDate.Value.Date);
        }

        [Fact]
        public async Task Reschedule_PreventsRescheduleOfCompleted()
        {
            var db = CreateDbContext("res2");
            var task = new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "u2", Title = "T1", DueDate = System.DateTime.UtcNow.AddDays(-1), Status = PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus.Completed };
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            var service = new TaskService(db, new FakeCurrentUser { UserId = "u2" });
            var (succeeded, error) = await service.RescheduleAsync(task.Id, System.DateTime.UtcNow.AddDays(2));
            Assert.False(succeeded);
        }

        [Fact]
        public async Task BulkReschedule_OnlyAffectsOverdueActiveTasks()
        {
            var db = CreateDbContext("res3");
            db.Tasks.Add(new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "u3", Title = "O1", DueDate = System.DateTime.UtcNow.AddDays(-3), Status = PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus.NotStarted });
            db.Tasks.Add(new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "u3", Title = "C1", DueDate = System.DateTime.UtcNow.AddDays(-3), Status = PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus.Completed });
            db.Tasks.Add(new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "u3", Title = "F1", DueDate = System.DateTime.UtcNow.AddDays(5), Status = PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus.NotStarted });
            await db.SaveChangesAsync();
            var service = new TaskService(db, new FakeCurrentUser { UserId = "u3" });
            var count = await service.BulkRescheduleOverdueAsync(System.DateTime.UtcNow.AddDays(2));
            Assert.Equal(1, count);
            var list = await db.Tasks.Where(t => t.UserId == "u3").ToListAsync();
            Assert.Contains(list, t => t.Title == "O1" && t.DueDate.Value.Date == System.DateTime.UtcNow.Date.AddDays(2));
            Assert.Contains(list, t => t.Title == "C1" && t.Status == PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus.Completed);
        }
    }
}
