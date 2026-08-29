using System;

namespace PersonalToDo_Freelance.Domain.Entities
{
    public class TaskAttachment : BaseEntity
    {
        public long TodoTaskId { get; set; }
        public TodoTask? TodoTask { get; set; }

        public string FileName { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long SizeBytes { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; }
    }
}
