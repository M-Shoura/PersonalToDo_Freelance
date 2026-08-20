using System;
using System.ComponentModel.DataAnnotations;
using PersonalToDo_Freelance.Domain.Enums;

namespace PersonalToDo_Freelance.Application.ViewModels
{
    public class TaskDetailsViewModel
    {
        public long Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public long? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public TaskPriority Priority { get; set; }
        public TodoTaskStatus Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsOverdue { get; set; }
    }

    public class TaskEditViewModel
    {
        public long Id { get; set; }

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
    }
}
