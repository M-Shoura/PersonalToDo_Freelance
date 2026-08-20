using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PersonalToDo_Freelance.Data;
using PersonalToDo_Freelance.Infrastructure.Services;
using Xunit;
using PersonalToDo_Freelance.Application.Interfaces;
using PersonalToDo_Freelance.Application.ViewModels;

namespace PersonalToDo_Freelance.Tests
{
    public class TaskServiceTests
    {
        private ApplicationDbContext CreateDbContext(string name)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(name).Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Create_RejectsCategoryNotOwnedByUser()
        {
            var db = CreateDbContext("task_test1");
            var cat = new PersonalToDo_Freelance.Domain.Entities.Category { UserId = "other", Name = "Other" };
            db.Categories.Add(cat);
            await db.SaveChangesAsync();
            var user = new FakeCurrentUser { UserId = "user1" };
            var service = new TaskService(db, user);
            var model = new TaskCreateViewModel { Title = "T1", CategoryId = cat.Id };
            var res = await service.CreateAsync(model);
            Assert.False(res.Succeeded);
        }

        [Fact]
        public async Task Create_SucceedsAndSetsUserId()
        {
            var db = CreateDbContext("task_test2");
            var user = new FakeCurrentUser { UserId = "user2" };
            var service = new TaskService(db, user);
            var model = new TaskCreateViewModel { Title = "T2" };
            var res = await service.CreateAsync(model);
            Assert.True(res.Succeeded);
            var task = await db.Tasks.FirstOrDefaultAsync();
            Assert.Equal("user2", task!.UserId);
        }
    }
}
