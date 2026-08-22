using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PersonalToDo_Freelance.Data;
using PersonalToDo_Freelance.Infrastructure.Services;
using Xunit;
using PersonalToDo_Freelance.Application.ViewModels;
using PersonalToDo_Freelance.Domain.Entities;

namespace PersonalToDo_Freelance.Tests
{
    public class AdditionalBusinessLogicTests
    {
        private ApplicationDbContext CreateDbContext(string name)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(name).Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Create_Rejects_StartDateAfterDueDate()
        {
            var db = CreateDbContext("val_create1");
            var user = new FakeCurrentUser { UserId = "u1" };
            var svc = new TaskService(db, user);
            var model = new TaskCreateViewModel
            {
                Title = "T",
                StartDate = new DateTime(2026, 8, 10),
                DueDate = new DateTime(2026, 8, 5)
            };

            var res = await svc.CreateAsync(model);
            Assert.False(res.Succeeded);
            Assert.Equal("Start date cannot be after due date.", res.Error);
        }

        [Fact]
        public async Task Update_Rejects_StartDateAfterDueDate()
        {
            var db = CreateDbContext("val_update1");
            var user = new FakeCurrentUser { UserId = "u2" };
            var task = new TodoTask { UserId = "u2", Title = "T1" };
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            var svc = new TaskService(db, user);
            var edit = new TaskEditViewModel
            {
                Id = task.Id,
                Title = "T1",
                StartDate = new DateTime(2026, 9, 10),
                DueDate = new DateTime(2026, 9, 1)
            };

            var res = await svc.UpdateAsync(edit);
            Assert.False(res.Succeeded);
            Assert.Equal("Start date cannot be after due date.", res.Error);
        }

        [Fact]
        public async Task ChangeStatus_SameStatus_DoesNotModifyCompletedAt()
        {
            var db = CreateDbContext("status_same");
            var user = new FakeCurrentUser { UserId = "u3" };
            var task = new TodoTask { UserId = "u3", Title = "T", Status = PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus.InProgress };
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            var svc = new TaskService(db, user);

            var before = task.CompletedAt;
            var res = await svc.ChangeStatusAsync(task.Id, PersonalToDo_Freelance.Domain.Enums.TodoTaskStatus.InProgress);
            Assert.True(res.Succeeded);
            var t = await db.Tasks.FindAsync(task.Id);
            Assert.Equal(before, t!.CompletedAt);
        }

        [Fact]
        public async Task Delete_MarksIsDeleted_ExcludedFromUserQueries()
        {
            var db = CreateDbContext("del_exclude");
            var user = new FakeCurrentUser { UserId = "u4" };
            var task = new TodoTask { UserId = "u4", Title = "ToDel" };
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            var svc = new TaskService(db, user);

            var del = await svc.DeleteAsync(task.Id);
            Assert.True(del.Succeeded);
            var list = await svc.GetUserTasksAsync();
            Assert.DoesNotContain(list, t => t.Id == task.Id);
        }
    }
}
