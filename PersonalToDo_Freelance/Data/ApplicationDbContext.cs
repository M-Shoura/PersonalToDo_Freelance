using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PersonalToDo_Freelance.Models;
using PersonalToDo_Freelance.Domain.Entities;

namespace PersonalToDo_Freelance.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<TodoTask> Tasks { get; set; } = null!;
        public DbSet<RecurrenceRule> RecurrenceRules { get; set; } = null!;
        public DbSet<TaskOccurrence> TaskOccurrences { get; set; } = null!;
    }
}
