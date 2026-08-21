using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using PersonalToDo_Freelance.Application.ViewModels;
using PersonalToDo_Freelance.Domain.Enums;
using Xunit;

namespace PersonalToDo_Freelance.Tests
{
    public class RecurrenceRuleValidationTests
    {
        private static List<ValidationResult> Validate(RecurrenceRuleViewModel model)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
            return results;
        }

        [Fact]
        public void NonRecurringRule_IsValidWithoutConfiguration()
        {
            var results = Validate(new RecurrenceRuleViewModel());

            Assert.Empty(results);
        }

        [Fact]
        public void RecurringRule_RequiresRecurrenceType()
        {
            var results = Validate(new RecurrenceRuleViewModel { IsRecurring = true, Type = RecurrenceType.None });

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(RecurrenceRuleViewModel.Type)));
        }

        [Fact]
        public void WeeklyRule_RequiresAtLeastOneWeekday()
        {
            var results = Validate(new RecurrenceRuleViewModel
            {
                IsRecurring = true,
                Type = RecurrenceType.Weekly,
                Interval = 1,
                DaysOfWeek = DaysOfWeekFlags.None
            });

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(RecurrenceRuleViewModel.DaysOfWeek)));
        }

        [Fact]
        public void EndOnDate_RequiresEndDate()
        {
            var results = Validate(new RecurrenceRuleViewModel
            {
                IsRecurring = true,
                Type = RecurrenceType.Daily,
                Interval = 1,
                EndCondition = RecurrenceEndCondition.OnDate
            });

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(RecurrenceRuleViewModel.EndDate)));
        }

        [Fact]
        public void EndAfterOccurrences_RequiresOccurrenceCount()
        {
            var results = Validate(new RecurrenceRuleViewModel
            {
                IsRecurring = true,
                Type = RecurrenceType.Monthly,
                Interval = 1,
                EndCondition = RecurrenceEndCondition.AfterOccurrences
            });

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(RecurrenceRuleViewModel.OccurrenceCount)));
        }

        [Fact]
        public void WeeklyRule_WithIntervalWeekdayAndNoEnd_IsValid()
        {
            var results = Validate(new RecurrenceRuleViewModel
            {
                IsRecurring = true,
                Type = RecurrenceType.Weekly,
                Interval = 2,
                DaysOfWeek = DaysOfWeekFlags.Monday | DaysOfWeekFlags.Wednesday,
                EndCondition = RecurrenceEndCondition.Never
            });

            Assert.Empty(results);
        }
    }
}
