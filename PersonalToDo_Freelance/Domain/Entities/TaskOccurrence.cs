using System;
using PersonalToDo_Freelance.Domain.Enums;

namespace PersonalToDo_Freelance.Domain.Entities
{
    public class TaskOccurrence : BaseEntity
    {
        public long TodoTaskId { get; set; }
        public TodoTask? TodoTask { get; set; }

        public long? RecurrenceRuleId { get; set; }
        public RecurrenceRule? RecurrenceRule { get; set; }

        public DateTime OccurrenceDate { get; set; }

        public DateTime? OriginalOccurrenceDate { get; set; }

        public OccurrenceStatus Status { get; set; } = OccurrenceStatus.Pending;

        public DateTime? CompletedAt { get; set; }

        public bool IsDeleted { get; set; }
    }
}
