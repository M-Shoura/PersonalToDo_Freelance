using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using PersonalToDo_Freelance.Data;
using PersonalToDo_Freelance.Infrastructure.Services;
using PersonalToDo_Freelance.Application.ViewModels;
using System.Linq;

namespace PersonalToDo_Freelance.Tests
{
    public class TaskFilteringAndSortingTests
    {
        private ApplicationDbContext CreateDbContext(string name)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(name).Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Filter_ByStatusCategoryPriority_Combination()
        {
            var db = CreateDbContext("filter1");
            var user = new FakeCurrentUser { UserId = "u10" };
            db.Categories.Add(new PersonalToDo_Freelance.Domain.Entities.Category { UserId = "u10", Name = "Study" });
            await db.SaveChangesAsync();
            var cat = await db.Categories.FirstAsync();
            db.Tasks.Add(new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "u10", Title = "A", Priority = PersonalToDo_Freelance.Domain.Enums.TaskPriority.High, Status = PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus.NotStarted, CategoryId = cat.Id });
            db.Tasks.Add(new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "u10", Title = "B", Priority = PersonalToDo_Freelance.Domain.Enums.TaskPriority.Low, Status = PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus.NotStarted, CategoryId = cat.Id });
            db.Tasks.Add(new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "u10", Title = "C", Priority = PersonalToDo_Freelance.Domain.Enums.TaskPriority.High, Status = PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus.Completed, CategoryId = cat.Id });
            await db.SaveChangesAsync();
            var service = new TaskService(db, user);
            var q = new TaskQueryParameters { Status = PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus.NotStarted, CategoryId = cat.Id, Priority = PersonalToDo_Freelance.Domain.Enums.TaskPriority.High };
            var list = await service.GetUserTasksAsync(q);
            Assert.Single(list);
            Assert.Equal("A", list[0].Title);
        }

        [Fact]
        public async Task DateFilters_ThisWeek_And_Overdue()
        {
            var db = CreateDbContext("filter2");
            var user = new FakeCurrentUser { UserId = "u11" };
            var today = System.DateTime.UtcNow.Date;
            db.Tasks.Add(new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "u11", Title = "Today", DueDate = today });
            db.Tasks.Add(new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "u11", Title = "NextWeek", DueDate = today.AddDays(8) });
            db.Tasks.Add(new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "u11", Title = "Overdue", DueDate = today.AddDays(-2) });
            await db.SaveChangesAsync();
            var service = new TaskService(db, user);
            var week = new TaskQueryParameters { DateFilter = DateRangeFilter.ThisWeek };
            var listWeek = await service.GetUserTasksAsync(week);
            Assert.Contains(listWeek, x => x.Title == "Today");
            Assert.DoesNotContain(listWeek, x => x.Title == "NextWeek");
            var overdue = new TaskQueryParameters { DateFilter = DateRangeFilter.Overdue };
            var listOver = await service.GetUserTasksAsync(overdue);
            Assert.Single(listOver);
            Assert.Equal("Overdue", listOver[0].Title);
        }

        [Fact]
        public async Task Sorting_ByDueDate_Desc()
        {
            var db = CreateDbContext("sort1");
            var user = new FakeCurrentUser { UserId = "u12" };
            db.Tasks.Add(new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "u12", Title = "A", DueDate = System.DateTime.UtcNow.AddDays(1) });
            db.Tasks.Add(new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "u12", Title = "B", DueDate = System.DateTime.UtcNow.AddDays(3) });
            db.Tasks.Add(new PersonalToDo_Freelance.Domain.Entities.TodoTask { UserId = "u12", Title = "C", DueDate = System.DateTime.UtcNow.AddDays(2) });
            await db.SaveChangesAsync();
            var service = new TaskService(db, user);
            var q = new TaskQueryParameters { SortBy = TaskSortField.DueDate, SortDirection = SortDirection.Desc };
            var list = await service.GetUserTasksAsync(q);
            Assert.Equal(new[] { "B", "C", "A" }, list.Select(x => x.Title).ToArray());
        }
    }
}
