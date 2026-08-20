using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PersonalToDo_Freelance.Data;
using PersonalToDo_Freelance.Infrastructure.Services;
using Xunit;
using PersonalToDo_Freelance.Application.Interfaces;
using PersonalToDo_Freelance.Domain.Entities;
using PersonalToDo_Freelance.Application.ViewModels;
using System.Linq;

namespace PersonalToDo_Freelance.Tests
{
    public class FakeCurrentUser : Application.Interfaces.ICurrentUserService
    {
        public string? UserId { get; set; }
        public System.Security.Claims.ClaimsPrincipal? User => null;
    }

    public class CategoryServiceTests
    {
        private ApplicationDbContext CreateDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName).Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Create_PreventsDuplicateNamesPerUser()
        {
            var db = CreateDbContext("dup_test");
            var user = new FakeCurrentUser { UserId = "user1" };
            var service = new CategoryService(db, user);
            var res1 = await service.CreateAsync(new CategoryCreateViewModel { Name = "Work" });
            Assert.True(res1.Succeeded);
            var res2 = await service.CreateAsync(new CategoryCreateViewModel { Name = "Work" });
            Assert.False(res2.Succeeded);
        }

        [Fact]
        public async Task Delete_SetsIsDeletedAndNullsTaskCategory()
        {
            var db = CreateDbContext("del_test");
            var user = new FakeCurrentUser { UserId = "user2" };
            var cat = new Category { UserId = "user2", Name = "Home" };
            db.Categories.Add(cat);
            db.Tasks.Add(new TodoTask { UserId = "user2", Title = "T1", Category = cat });
            await db.SaveChangesAsync();
            var service = new CategoryService(db, user);
            var res = await service.DeleteAsync(cat.Id);
            Assert.True(res.Succeeded);
            var c = await db.Categories.FindAsync(cat.Id);
            Assert.True(c!.IsDeleted);
            var task = db.Tasks.First();
            Assert.Null(task.CategoryId);
        }
    }
}
