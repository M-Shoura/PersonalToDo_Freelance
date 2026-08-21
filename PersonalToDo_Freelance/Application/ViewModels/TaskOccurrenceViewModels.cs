using PersonalToDo_Freelance.Domain.Enums;

namespace PersonalToDo_Freelance.Application.ViewModels
{
    public class TaskOccurrenceViewModel
    {
        public long Id { get; set; }
        public long TodoTaskId { get; set; }
        public long? RecurrenceRuleId { get; set; }
        public string TaskTitle { get; set; } = null!;
        public DateTime ScheduledDate { get; set; }
        public OccurrenceStatus Status { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
