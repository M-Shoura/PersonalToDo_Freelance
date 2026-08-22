using System;
using System.Collections.Generic;
using PersonalToDo_Freelance.Domain.Enums;

namespace PersonalToDo_Freelance.Application.ViewModels
{
    public class CategoryCountViewModel
    {
        public string Category { get; set; } = null!;
        public int Count { get; set; }
    }

    public class PriorityCountViewModel
    {
        public TaskPriority Priority { get; set; }
        public int Count { get; set; }
    }

    public class DailyCountViewModel
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class StatisticsViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int TasksCreated { get; set; }
        public int TasksCompleted { get; set; }
        public int PendingTasks { get; set; }
        public int OverdueTasks { get; set; }
        public double CompletionRate { get; set; }

        public IReadOnlyList<CategoryCountViewModel> TasksByCategory { get; set; } = Array.Empty<CategoryCountViewModel>();
        public IReadOnlyList<PriorityCountViewModel> TasksByPriority { get; set; } = Array.Empty<PriorityCountViewModel>();
        public IReadOnlyList<DailyCountViewModel> CompletedPerDay { get; set; } = Array.Empty<DailyCountViewModel>();
    }
}
