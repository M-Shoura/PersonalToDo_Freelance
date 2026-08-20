using System;
using PersonalToDo_Freelance.Domain.Enums;
using System.Collections.Generic;

namespace PersonalToDo_Freelance.Application.ViewModels
{
    public enum DateRangeFilter
    {
        None = 0,
        Today = 1,
        Tomorrow = 2,
        ThisWeek = 3,
        ThisMonth = 4,
        Custom = 5,
        Overdue = 6
    }

    public enum TaskSortField
    {
        DueDate = 0,
        Priority = 1,
        CreatedAt = 2,
        UpdatedAt = 3,
        CompletedAt = 4,
        Title = 5
    }

    public enum SortDirection
    {
        Asc = 0,
        Desc = 1
    }

    public class TaskQueryParameters
    {
        public TodoTaskStatus? Status { get; set; }
        public long? CategoryId { get; set; }
        public TaskPriority? Priority { get; set; }
        public DateRangeFilter DateFilter { get; set; } = DateRangeFilter.None;
        public DateTime? CustomStart { get; set; }
        public DateTime? CustomEnd { get; set; }
        public string? SearchTerm { get; set; }
        public TaskSortField SortBy { get; set; } = TaskSortField.DueDate;
        public SortDirection SortDirection { get; set; } = SortDirection.Asc;
    }
}
