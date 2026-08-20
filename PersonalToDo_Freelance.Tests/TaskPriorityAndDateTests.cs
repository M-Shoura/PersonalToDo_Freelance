using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PersonalToDo_Freelance.Data;
using PersonalToDo_Freelance.Infrastructure.Services;
using Xunit;
using PersonalToDo_Freelance.Application.ViewModels;

namespace PersonalToDo_Freelance.Tests
{
    public class TaskPriorityAndDateTests
    {
        private ApplicationDbContext CreateDbContext(string name)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(name).Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Create_RejectsStartAfterDue()
        {
            var db = CreateDbContext("date_test1");
            var user = new FakeCurrentUser { UserId = "u5" };
            var service = new TaskService(db, user);
            var model = new TaskCreateViewModel { Title = "T", StartDate = System.DateTime.UtcNow.AddDays(2), DueDate = System.DateTime.UtcNow.AddDays(1) };
            var res = await service.CreateAsync(model);
            Assert.False(res.Succeeded);
        }

        [Fact]
        public async Task Priority_Persists()
        {
            var db = CreateDbContext("prio_test1");
            var user = new FakeCurrentUser { UserId = "u6" };
            var service = new TaskService(db, user);
            var model = new TaskCreateViewModel { Title = "T", Priority = PersonalToDo_Freelance.Domain.Enums.TaskPriority.Critical };
            var res = await service.CreateAsync(model);
            Assert.True(res.Succeeded);
            var t = await db.Tasks.FirstOrDefaultAsync();
            Assert.Equal(PersonalToDo_Freelance.Domain.Enums.TaskPriority.Critical, t!.Priority);
        }

        [Fact]
        public async Task Overdue_ComputedCorrectly()
        {
            var db = CreateDbContext("date_test2");
            var user = new FakeCurrentUser { UserId = "u7" };
            var task = new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "u7", Title = "t", DueDate = System.DateTime.UtcNow.AddDays(-3), Status = PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus.NotStarted };
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            var service = new TaskService(db, user);
            var list = await service.GetUserTasksAsync();
            Assert.Single(list);
            Assert.True(list[0].IsOverdue);
        }
    }
}
