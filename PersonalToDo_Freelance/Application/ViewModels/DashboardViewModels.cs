using System;
using System.Collections.Generic;

namespace PersonalToDo_Freelance.Application.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalToday { get; set; }
        public int CompletedToday { get; set; }
        public int PendingToday { get; set; }
        public int Overdue { get; set; }
        public double CompletionRate { get; set; }
        public IReadOnlyList<TaskListItemViewModel> TodayTasks { get; set; } = Array.Empty<TaskListItemViewModel>();
    }
}
