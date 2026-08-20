using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PersonalToDo_Freelance.Data;
using PersonalToDo_Freelance.Infrastructure.Services;
using Xunit;
using PersonalToDo_Freelance.Application.Interfaces;
using PersonalToDo_Freelance.Application.ViewModels;

namespace PersonalToDo_Freelance.Tests
{
    public class TaskServiceOwnershipTests
    {
        private ApplicationDbContext CreateDbContext(string name)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(name).Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task GetForEdit_IsScopedToUser()
        {
            var db = CreateDbContext("own_test1");
            var task = new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "owner", Title = "Owned" };
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            var user = new FakeCurrentUser { UserId = "other" };
            var service = new TaskService(db, user);
            var vm = await service.GetForEditAsync(task.Id);
            Assert.Null(vm);
        }

        [Fact]
        public async Task Delete_PreventsOtherUserFromAccessing()
        {
            var db = CreateDbContext("own_test2");
            var task = new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "owner2", Title = "ToDelete" };
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            var user = new FakeCurrentUser { UserId = "owner2" };
            var service = new TaskService(db, user);
            var res = await service.DeleteAsync(task.Id);
            Assert.True(res.Succeeded);
            var other = new FakeCurrentUser { UserId = "intruder" };
            var otherService = new TaskService(db, other);
            var vm = await otherService.GetForEditAsync(task.Id);
            Assert.Null(vm);
        }
    }
}
