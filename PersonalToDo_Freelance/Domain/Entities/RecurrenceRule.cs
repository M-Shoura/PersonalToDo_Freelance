using System;
using PersonalToDo_Freelance.Domain.Enums;

namespace PersonalToDo_Freelance.Domain.Entities
{
    public class RecurrenceRule : BaseEntity
    {
        public string UserId { get; set; } = null!;

        public long? TodoTaskId { get; set; }
        public TodoTask? TodoTask { get; set; }

        public RecurrenceType Type { get; set; } = RecurrenceType.None;

        public int Interval { get; set; } = 1;

        public DaysOfWeekFlags DaysOfWeek { get; set; } = DaysOfWeekFlags.None;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public int? OccurrenceCount { get; set; }

        public bool IsDeleted { get; set; }
    }
}
