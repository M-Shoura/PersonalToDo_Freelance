using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PersonalToDo_Freelance.Data;
using PersonalToDo_Freelance.Domain.Entities;
using PersonalToDo_Freelance.Domain.Enums;
using PersonalToDo_Freelance.Infrastructure.Services;
using Xunit;

namespace PersonalToDo_Freelance.Tests
{
    public class TaskOccurrenceServiceTests
    {
        private static ApplicationDbContext CreateDbContext(string name)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(name).Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task Generate_DailyRecurrence_CreatesEveryIntervalDay()
        {
            var db = CreateDbContext("occ_daily");
            var task = await AddRecurringTaskAsync(db, RecurrenceType.Daily, new DateTime(2026, 8, 1), interval: 2);
            var service = new TaskOccurrenceService(db, new FakeCurrentUser { UserId = "u1" });

            await service.GenerateForTaskAsync(task.Id, new DateTime(2026, 8, 7));

            AssertDates(db, new DateTime(2026, 8, 1), new DateTime(2026, 8, 3), new DateTime(2026, 8, 5), new DateTime(2026, 8, 7));
        }

        [Fact]
        public async Task Generate_WeeklyRecurrence_CreatesSelectedWeekdayOnly()
        {
            var db = CreateDbContext("occ_weekly");
            var task = await AddRecurringTaskAsync(db, RecurrenceType.Weekly, new DateTime(2026, 8, 3), days: DaysOfWeekFlags.Monday);
            var service = new TaskOccurrenceService(db, new FakeCurrentUser { UserId = "u1" });

            await service.GenerateForTaskAsync(task.Id, new DateTime(2026, 8, 24));

            AssertDates(db, new DateTime(2026, 8, 3), new DateTime(2026, 8, 10), new DateTime(2026, 8, 17), new DateTime(2026, 8, 24));
        }

        [Fact]
        public async Task Generate_WeeklyMultipleWeekdays_CreatesEachSelectedDay()
        {
            var db = CreateDbContext("occ_weekly_multi");
            var task = await AddRecurringTaskAsync(db, RecurrenceType.Weekly, new DateTime(2026, 8, 3), days: DaysOfWeekFlags.Monday | DaysOfWeekFlags.Wednesday);
            var service = new TaskOccurrenceService(db, new FakeCurrentUser { UserId = "u1" });

            await service.GenerateForTaskAsync(task.Id, new DateTime(2026, 8, 12));

            AssertDates(db, new DateTime(2026, 8, 3), new DateTime(2026, 8, 5), new DateTime(2026, 8, 10), new DateTime(2026, 8, 12));
        }

        [Fact]
        public async Task Generate_MonthlyRecurrence_CreatesEveryIntervalMonth()
        {
            var db = CreateDbContext("occ_monthly");
            var task = await AddRecurringTaskAsync(db, RecurrenceType.Monthly, new DateTime(2026, 1, 15), interval: 2);
            var service = new TaskOccurrenceService(db, new FakeCurrentUser { UserId = "u1" });

            await service.GenerateForTaskAsync(task.Id, new DateTime(2026, 7, 15));

            AssertDates(db, new DateTime(2026, 1, 15), new DateTime(2026, 3, 15), new DateTime(2026, 5, 15), new DateTime(2026, 7, 15));
        }

        [Fact]
        public async Task Generate_StopsAtEndDate()
        {
            var db = CreateDbContext("occ_end_date");
            var task = await AddRecurringTaskAsync(db, RecurrenceType.Daily, new DateTime(2026, 8, 1), endDate: new DateTime(2026, 8, 3));
            var service = new TaskOccurrenceService(db, new FakeCurrentUser { UserId = "u1" });

            await service.GenerateForTaskAsync(task.Id, new DateTime(2026, 8, 10));

            AssertDates(db, new DateTime(2026, 8, 1), new DateTime(2026, 8, 2), new DateTime(2026, 8, 3));
        }

        [Fact]
        public async Task Generate_StopsAtOccurrenceCount()
        {
            var db = CreateDbContext("occ_count");
            var task = await AddRecurringTaskAsync(db, RecurrenceType.Daily, new DateTime(2026, 8, 1), occurrenceCount: 3);
            var service = new TaskOccurrenceService(db, new FakeCurrentUser { UserId = "u1" });

            await service.GenerateForTaskAsync(task.Id, new DateTime(2026, 8, 10));

            AssertDates(db, new DateTime(2026, 8, 1), new DateTime(2026, 8, 2), new DateTime(2026, 8, 3));
        }

        [Fact]
        public async Task Generate_IsIdempotentAndDoesNotCreateDuplicates()
        {
            var db = CreateDbContext("occ_idempotent");
            var task = await AddRecurringTaskAsync(db, RecurrenceType.Daily, new DateTime(2026, 8, 1));
            var service = new TaskOccurrenceService(db, new FakeCurrentUser { UserId = "u1" });

            await service.GenerateForTaskAsync(task.Id, new DateTime(2026, 8, 3));
            await service.GenerateForTaskAsync(task.Id, new DateTime(2026, 8, 3));

            Assert.Equal(3, db.TaskOccurrences.Count());
            AssertDates(db, new DateTime(2026, 8, 1), new DateTime(2026, 8, 2), new DateTime(2026, 8, 3));
        }

        [Fact]
        public async Task Reschedule_ChangesOnlySelectedOccurrence()
        {
            var db = CreateDbContext("occ_reschedule");
            var task = await AddRecurringTaskAsync(db, RecurrenceType.Daily, new DateTime(2026, 8, 1));
            var service = new TaskOccurrenceService(db, new FakeCurrentUser { UserId = "u1" });
            await service.GenerateForTaskAsync(task.Id, new DateTime(2026, 8, 2));
            var occurrence = db.TaskOccurrences.OrderBy(o => o.OccurrenceDate).First();

            var result = await service.RescheduleAsync(occurrence.Id, new DateTime(2026, 8, 5));

            Assert.True(result.Succeeded);
            AssertDates(db, new DateTime(2026, 8, 2), new DateTime(2026, 8, 5));
        }

        [Fact]
        public async Task Reschedule_OneWeeklyOccurrence_DoesNotChangeRecurrenceRuleOrFutureMondays()
        {
            var db = CreateDbContext("occ_reschedule_weekly_future");
            var task = await AddRecurringTaskAsync(db, RecurrenceType.Weekly, new DateTime(2026, 8, 3), days: DaysOfWeekFlags.Monday);
            var service = new TaskOccurrenceService(db, new FakeCurrentUser { UserId = "u1" });
            await service.GenerateForTaskAsync(task.Id, new DateTime(2026, 8, 17));
            var occurrence = db.TaskOccurrences.Single(o => o.OccurrenceDate.Date == new DateTime(2026, 8, 17));
            var ruleId = task.RecurrenceRule!.Id;

            var result = await service.RescheduleAsync(occurrence.Id, new DateTime(2026, 8, 18));
            await service.GenerateForTaskAsync(task.Id, new DateTime(2026, 8, 31));

            Assert.True(result.Succeeded);
            var rule = db.RecurrenceRules.Find(ruleId)!;
            Assert.Equal(RecurrenceType.Weekly, rule.Type);
            Assert.Equal(DaysOfWeekFlags.Monday, rule.DaysOfWeek);
            Assert.Equal(1, rule.Interval);
            AssertDates(db, new DateTime(2026, 8, 3), new DateTime(2026, 8, 10), new DateTime(2026, 8, 18), new DateTime(2026, 8, 24), new DateTime(2026, 8, 31));
            Assert.DoesNotContain(db.TaskOccurrences, o => o.OccurrenceDate.Date == new DateTime(2026, 8, 17));
            Assert.Equal(new DateTime(2026, 8, 17), db.TaskOccurrences.Single(o => o.OccurrenceDate.Date == new DateTime(2026, 8, 18)).OriginalOccurrenceDate);
        }

        [Fact]
        public async Task CompleteAndReopen_UpdateStatusAndCompletionTimestamp()
        {
            var db = CreateDbContext("occ_complete_reopen");
            var task = await AddRecurringTaskAsync(db, RecurrenceType.Daily, new DateTime(2026, 8, 1));
            var service = new TaskOccurrenceService(db, new FakeCurrentUser { UserId = "u1" });
            await service.GenerateForTaskAsync(task.Id, new DateTime(2026, 8, 1));
            var occurrence = db.TaskOccurrences.Single();

            var complete = await service.ChangeStatusAsync(occurrence.Id, OccurrenceStatus.Completed);
            var completedOccurrence = db.TaskOccurrences.Find(occurrence.Id)!;
            Assert.True(complete.Succeeded);
            Assert.Equal(OccurrenceStatus.Completed, completedOccurrence.Status);
            Assert.NotNull(completedOccurrence.CompletedAt);

            var reopen = await service.ReopenAsync(occurrence.Id);
            var reopenedOccurrence = db.TaskOccurrences.Find(occurrence.Id)!;

            Assert.True(reopen.Succeeded);
            Assert.Equal(OccurrenceStatus.Pending, reopenedOccurrence.Status);
            Assert.Null(reopenedOccurrence.CompletedAt);
        }

        [Fact]
        public async Task Cancel_MarksOccurrenceCancelledWithoutCompletionTimestamp()
        {
            var db = CreateDbContext("occ_cancel");
            var task = await AddRecurringTaskAsync(db, RecurrenceType.Daily, new DateTime(2026, 8, 1));
            var service = new TaskOccurrenceService(db, new FakeCurrentUser { UserId = "u1" });
            await service.GenerateForTaskAsync(task.Id, new DateTime(2026, 8, 1));
            var occurrence = db.TaskOccurrences.Single();

            var result = await service.ChangeStatusAsync(occurrence.Id, OccurrenceStatus.Cancelled);

            Assert.True(result.Succeeded);
            Assert.Equal(OccurrenceStatus.Cancelled, db.TaskOccurrences.Find(occurrence.Id)!.Status);
            Assert.Null(db.TaskOccurrences.Find(occurrence.Id)!.CompletedAt);
        }

        [Fact]
        public async Task GetDetails_ReturnsOccurrenceDetails()
        {
            var db = CreateDbContext("occ_details");
            var task = await AddRecurringTaskAsync(db, RecurrenceType.Daily, new DateTime(2026, 8, 1));
            var service = new TaskOccurrenceService(db, new FakeCurrentUser { UserId = "u1" });
            await service.GenerateForTaskAsync(task.Id, new DateTime(2026, 8, 1));
            var occurrence = db.TaskOccurrences.Single();

            var details = await service.GetDetailsAsync(occurrence.Id);

            Assert.NotNull(details);
            Assert.Equal(occurrence.Id, details!.Id);
            Assert.Equal(new DateTime(2026, 8, 1), details.ScheduledDate);
            Assert.Equal(new DateTime(2026, 8, 1), details.OriginalScheduledDate);
        }

        [Fact]
        public async Task StopRecurrence_SetsRuleEndDateAndDoesNotModifyExistingOccurrences()
        {
            var db = CreateDbContext("occ_stop_preserves_history");
            var task = await AddRecurringTaskAsync(db, RecurrenceType.Daily, new DateTime(2026, 8, 1));
            var occurrenceService = new TaskOccurrenceService(db, new FakeCurrentUser { UserId = "u1" });
            var taskService = new TaskService(db, new FakeCurrentUser { UserId = "u1" });
            await occurrenceService.GenerateForTaskAsync(task.Id, new DateTime(2026, 8, 5));
            var completed = db.TaskOccurrences.Single(o => o.OccurrenceDate.Date == new DateTime(2026, 8, 1));
            var skipped = db.TaskOccurrences.Single(o => o.OccurrenceDate.Date == new DateTime(2026, 8, 2));
            await occurrenceService.ChangeStatusAsync(completed.Id, OccurrenceStatus.Completed);
            await occurrenceService.SkipAsync(skipped.Id);

            var result = await taskService.StopRecurrenceAsync(task.Id, new DateTime(2026, 8, 3));

            Assert.True(result.Succeeded);
            var rule = db.RecurrenceRules.Find(task.RecurrenceRule!.Id)!;
            Assert.Equal(new DateTime(2026, 8, 3), rule.EndDate);
            Assert.False(rule.IsDeleted);
            Assert.Equal(RecurrenceType.Daily, rule.Type);
            Assert.Equal(OccurrenceStatus.Completed, db.TaskOccurrences.Find(completed.Id)!.Status);
            Assert.NotNull(db.TaskOccurrences.Find(completed.Id)!.CompletedAt);
            Assert.Equal(OccurrenceStatus.Skipped, db.TaskOccurrences.Find(skipped.Id)!.Status);
            Assert.Equal(5, db.TaskOccurrences.Count());
        }

        [Fact]
        public async Task StopRecurrence_PreventsGenerationAfterEndDate()
        {
            var db = CreateDbContext("occ_stop_generation");
            var task = await AddRecurringTaskAsync(db, RecurrenceType.Daily, new DateTime(2026, 8, 1));
            var occurrenceService = new TaskOccurrenceService(db, new FakeCurrentUser { UserId = "u1" });
            var taskService = new TaskService(db, new FakeCurrentUser { UserId = "u1" });
            await occurrenceService.GenerateForTaskAsync(task.Id, new DateTime(2026, 8, 2));

            await taskService.StopRecurrenceAsync(task.Id, new DateTime(2026, 8, 2));
            await occurrenceService.GenerateForTaskAsync(task.Id, new DateTime(2026, 8, 10));

            AssertDates(db, new DateTime(2026, 8, 1), new DateTime(2026, 8, 2));
        }

        [Fact]
        public async Task StopRecurrence_ReturnsErrorForNonRecurringTask()
        {
            var db = CreateDbContext("occ_stop_nonrecurring");
            var task = new TodoTask { UserId = "u1", Title = "One off" };
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            var taskService = new TaskService(db, new FakeCurrentUser { UserId = "u1" });

            var result = await taskService.StopRecurrenceAsync(task.Id, new DateTime(2026, 8, 1));

            Assert.False(result.Succeeded);
            Assert.Equal("Task is not recurring.", result.Error);
        }

        [Fact]
        public async Task Skip_MarksOnlySelectedOccurrenceSkipped()
        {
            var db = CreateDbContext("occ_skip");
            var task = await AddRecurringTaskAsync(db, RecurrenceType.Daily, new DateTime(2026, 8, 1));
            var service = new TaskOccurrenceService(db, new FakeCurrentUser { UserId = "u1" });
            await service.GenerateForTaskAsync(task.Id, new DateTime(2026, 8, 2));
            var occurrence = db.TaskOccurrences.OrderBy(o => o.OccurrenceDate).First();

            var result = await service.SkipAsync(occurrence.Id);

            Assert.True(result.Succeeded);
            Assert.Equal(OccurrenceStatus.Skipped, db.TaskOccurrences.Find(occurrence.Id)!.Status);
            Assert.Equal(OccurrenceStatus.Pending, db.TaskOccurrences.OrderBy(o => o.OccurrenceDate).Last().Status);
        }

        private static async Task<TodoTask> AddRecurringTaskAsync(
            ApplicationDbContext db,
            RecurrenceType type,
            DateTime startDate,
            int interval = 1,
            DaysOfWeekFlags days = DaysOfWeekFlags.None,
            DateTime? endDate = null,
            int? occurrenceCount = null)
        {
            var task = new TodoTask
            {
                UserId = "u1",
                Title = "Recurring",
                StartDate = startDate.Date,
                RecurrenceRule = new RecurrenceRule
                {
                    UserId = "u1",
                    Type = type,
                    Interval = interval,
                    DaysOfWeek = days,
                    StartDate = startDate.Date,
                    EndDate = endDate?.Date,
                    OccurrenceCount = occurrenceCount
                }
            };

            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            return task;
        }

        private static void AssertDates(ApplicationDbContext db, params DateTime[] expected)
        {
            var actual = db.TaskOccurrences
                .OrderBy(o => o.OccurrenceDate)
                .Select(o => o.OccurrenceDate.Date)
                .ToArray();

            Assert.Equal(expected.Select(d => d.Date).ToArray(), actual);
        }
    }
}
