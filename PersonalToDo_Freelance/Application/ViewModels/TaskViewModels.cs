using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PersonalToDo_Freelance.Domain.Enums;

namespace PersonalToDo_Freelance.Application.ViewModels
{
    public enum RecurrenceEndCondition
    {
        Never = 0,
        OnDate = 1,
        AfterOccurrences = 2
    }

    public class RecurrenceRuleViewModel : IValidatableObject
    {
        public bool IsRecurring { get; set; }

        public RecurrenceType Type { get; set; } = RecurrenceType.None;

        [Range(1, 999)]
        public int Interval { get; set; } = 1;

        public DaysOfWeekFlags DaysOfWeek { get; set; } = DaysOfWeekFlags.None;

        public RecurrenceEndCondition EndCondition { get; set; } = RecurrenceEndCondition.Never;

        public DateTime? EndDate { get; set; }

        [Range(1, 9999)]
        public int? OccurrenceCount { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!IsRecurring)
            {
                yield break;
            }

            if (Type == RecurrenceType.None)
            {
                yield return new ValidationResult("Select a recurrence type.", new[] { nameof(Type) });
            }

            if (Interval < 1)
            {
                yield return new ValidationResult("Recurrence interval must be at least 1.", new[] { nameof(Interval) });
            }

            if (Type == RecurrenceType.Weekly && DaysOfWeek == DaysOfWeekFlags.None)
            {
                yield return new ValidationResult("Select at least one weekday for weekly recurrence.", new[] { nameof(DaysOfWeek) });
            }

            if (EndCondition == RecurrenceEndCondition.OnDate && !EndDate.HasValue)
            {
                yield return new ValidationResult("Select an end date.", new[] { nameof(EndDate) });
            }

            if (EndCondition == RecurrenceEndCondition.AfterOccurrences && (!OccurrenceCount.HasValue || OccurrenceCount.Value < 1))
            {
                yield return new ValidationResult("Enter the number of occurrences.", new[] { nameof(OccurrenceCount) });
            }
        }
    }

    public class TaskCreateViewModel : IValidatableObject
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;

        [StringLength(4000)]
        public string? Description { get; set; }

        public long? CategoryId { get; set; }

        [Required]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public DateTime? StartDate { get; set; }

        public DateTime? DueDate { get; set; }

        public TodoTaskStatus Status { get; set; } = TodoTaskStatus.NotStarted;

        public RecurrenceRuleViewModel Recurrence { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (var result in Recurrence.Validate(validationContext))
            {
                yield return result;
            }
        }
    }
}
