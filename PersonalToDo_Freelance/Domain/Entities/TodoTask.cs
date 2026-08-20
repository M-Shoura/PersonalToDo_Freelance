using System;
using System.Collections.Generic;
using PersonalToDo_Freelance.Models;
using PersonalToDo_Freelance.Domain.Enums;

namespace PersonalToDo_Freelance.Domain.Entities
{
    public class TodoTask : BaseEntity
    {
        public string UserId { get; set; } = null!;

        public ApplicationUser? User { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public long? CategoryId { get; set; }

        public Category? Category { get; set; }

        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public TodoTaskStatus Status { get; set; } = TodoTaskStatus.NotStarted;

        public DateTime? StartDate { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime? CompletedAt { get; set; }

        public bool IsDeleted { get; set; }

        public RecurrenceRule? RecurrenceRule { get; set; }

        public ICollection<TaskOccurrence> Occurrences { get; set; } = new List<TaskOccurrence>();
    }
}
