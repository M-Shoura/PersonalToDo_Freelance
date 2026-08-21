using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PersonalToDo_Freelance.Data;
using PersonalToDo_Freelance.Infrastructure.Services;
using Xunit;
using PersonalToDo_Freelance.Application.Interfaces;
using PersonalToDo_Freelance.Application.ViewModels;
using PersonalToDo_Freelance.Domain.Enums;

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

        [Fact]
        public async Task Create_WithRecurrence_PersistsRule()
        {
            var db = CreateDbContext("task_recurrence_create");
            var user = new FakeCurrentUser { UserId = "user3" };
            var service = new TaskService(db, user);
            var model = new TaskCreateViewModel
            {
                Title = "T3",
                Recurrence = new RecurrenceRuleViewModel
                {
                    IsRecurring = true,
                    Type = RecurrenceType.Weekly,
                    Interval = 2,
                    DaysOfWeek = DaysOfWeekFlags.Monday | DaysOfWeekFlags.Friday,
                    EndCondition = RecurrenceEndCondition.AfterOccurrences,
                    OccurrenceCount = 10
                }
            };

            var res = await service.CreateAsync(model);

            Assert.True(res.Succeeded);
            var rule = await db.RecurrenceRules.FirstOrDefaultAsync();
            Assert.NotNull(rule);
            Assert.Equal("user3", rule!.UserId);
            Assert.Equal(RecurrenceType.Weekly, rule.Type);
            Assert.Equal(2, rule.Interval);
            Assert.Equal(DaysOfWeekFlags.Monday | DaysOfWeekFlags.Friday, rule.DaysOfWeek);
            Assert.Equal(10, rule.OccurrenceCount);
        }

        [Fact]
        public async Task Create_WithInvalidRecurrence_ReturnsValidationError()
        {
            var db = CreateDbContext("task_recurrence_invalid_create");
            var user = new FakeCurrentUser { UserId = "user4" };
            var service = new TaskService(db, user);
            var model = new TaskCreateViewModel
            {
                Title = "T4",
                Recurrence = new RecurrenceRuleViewModel
                {
                    IsRecurring = true,
                    Type = RecurrenceType.Weekly,
                    Interval = 1,
                    DaysOfWeek = DaysOfWeekFlags.None
                }
            };

            var res = await service.CreateAsync(model);

            Assert.False(res.Succeeded);
            Assert.Equal("Select at least one weekday for weekly recurrence.", res.Error);
            Assert.Empty(db.RecurrenceRules);
        }
    }

}
