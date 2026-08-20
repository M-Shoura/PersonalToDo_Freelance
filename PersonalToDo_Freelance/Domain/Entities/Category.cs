using System.Collections.Generic;
using PersonalToDo_Freelance.Models;

namespace PersonalToDo_Freelance.Domain.Entities
{
    public class Category : BaseEntity
    {
        public string UserId { get; set; } = null!;

        public ApplicationUser? User { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public bool IsDeleted { get; set; }

        public ICollection<TodoTask> Tasks { get; set; } = new List<TodoTask>();
    }
}
