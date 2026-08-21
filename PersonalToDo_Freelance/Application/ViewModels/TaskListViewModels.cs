using System;
using PersonalToDo_Freelance.Domain.Enums;

namespace PersonalToDo_Freelance.Application.ViewModels
{
    public class TaskListItemViewModel
    {
        public long Id { get; set; }
        public string Title { get; set; } = null!;
        public string? CategoryName { get; set; }
        public TaskPriority Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public TodoTaskStatus Status { get; set; }
        public bool IsRecurring { get; set; }
    }
}
