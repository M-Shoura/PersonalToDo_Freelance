using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using PersonalToDo_Freelance.Data;
using PersonalToDo_Freelance.Infrastructure.Services;
using PersonalToDo_Freelance.Application.ViewModels;
using System.Linq;

namespace PersonalToDo_Freelance.Tests
{
    public class TaskSearchTests
    {
        private ApplicationDbContext CreateDbContext(string name)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(name).Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Search_ByTitle_Works()
        {
            var db = CreateDbContext("search1");
            var user = new FakeCurrentUser { UserId = "s1" };
            db.Tasks.Add(new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "s1", Title = "Buy milk" });
            db.Tasks.Add(new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "s1", Title = "Read book" });
            await db.SaveChangesAsync();
            var service = new TaskService(db, user);
            var q = new TaskQueryParameters { SearchTerm = "buy" };
            var list = await service.GetUserTasksAsync(q);
            Assert.Single(list);
            Assert.Equal("Buy milk", list[0].Title);
        }

        [Fact]
        public async Task Search_ByDescription_Works()
        {
            var db = CreateDbContext("search2");
            var user = new FakeCurrentUser { UserId = "s2" };
            db.Tasks.Add(new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "s2", Title = "T1", Description = "Call Alice about project" });
            db.Tasks.Add(new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "s2", Title = "T2", Description = "Walk the dog" });
            await db.SaveChangesAsync();
            var service = new TaskService(db, user);
            var q = new TaskQueryParameters { SearchTerm = "alice" };
            var list = await service.GetUserTasksAsync(q);
            Assert.Single(list);
            Assert.Equal("T1", list[0].Title);
        }

        [Fact]
        public async Task Search_WithFilters_CombinesCorrectly()
        {
            var db = CreateDbContext("search3");
            var user = new FakeCurrentUser { UserId = "s3" };
            db.Categories.Add(new PersonalToDo_Freelance.Domain.Entities.Category { UserId = "s3", Name = "Work" });
            await db.SaveChangesAsync();
            var cat = await db.Categories.FirstAsync();
            db.Tasks.Add(new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "s3", Title = "Email boss", Description = "urgent", Priority = PersonalToDo_Freelance.Domain.Enums.TaskPriority.High, CategoryId = cat.Id });
            db.Tasks.Add(new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "s3", Title = "Email friend", Description = "casual", Priority = PersonalToDo_Freelance.Domain.Enums.TaskPriority.Low, CategoryId = cat.Id });
            await db.SaveChangesAsync();
            var service = new TaskService(db, user);
            var q = new TaskQueryParameters { SearchTerm = "email", Priority = PersonalToDo_Freelance.Domain.Enums.TaskPriority.High, CategoryId = cat.Id };
            var list = await service.GetUserTasksAsync(q);
            Assert.Single(list);
            Assert.Equal("Email boss", list[0].Title);
        }

        [Fact]
        public async Task Search_IsScopedToUser()
        {
            var db = CreateDbContext("search4");
            db.Tasks.Add(new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "uA", Title = "Private A" });
            db.Tasks.Add(new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "uB", Title = "Private B" });
            await db.SaveChangesAsync();
            var serviceA = new TaskService(db, new FakeCurrentUser { UserId = "uA" });
            var resultA = await serviceA.GetUserTasksAsync(new TaskQueryParameters { SearchTerm = "Private" });
            Assert.Single(resultA);
            Assert.Equal("Private A", resultA[0].Title);
            var serviceB = new TaskService(db, new FakeCurrentUser { UserId = "uB" });
            var resultB = await serviceB.GetUserTasksAsync(new TaskQueryParameters { SearchTerm = "Private" });
            Assert.Single(resultB);
            Assert.Equal("Private B", resultB[0].Title);
        }
    }
}
