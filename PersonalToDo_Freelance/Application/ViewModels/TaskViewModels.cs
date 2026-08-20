using System;
using System.ComponentModel.DataAnnotations;
using PersonalToDo_Freelance.Domain.Enums;

namespace PersonalToDo_Freelance.Application.ViewModels
{
    public class TaskCreateViewModel
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
    }
}
