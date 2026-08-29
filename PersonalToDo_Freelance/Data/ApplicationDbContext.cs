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
        public DbSet<TaskAttachment> TaskAttachments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>(b =>
            {
                b.ToTable("Categories");
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired().HasMaxLength(200);
                b.Property(x => x.Description).HasMaxLength(4000);
                b.Property(x => x.UserId).IsRequired().HasMaxLength(450);
                b.HasIndex(x => x.UserId);
                b.HasMany(x => x.Tasks).WithOne(t => t.Category).HasForeignKey(t => t.CategoryId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TodoTask>(b =>
            {
                b.ToTable("Tasks");
                b.HasKey(x => x.Id);
                b.Property(x => x.Title).IsRequired().HasMaxLength(200);
                b.Property(x => x.Description).HasMaxLength(4000);
                b.Property(x => x.UserId).IsRequired().HasMaxLength(450);
                b.Property(x => x.Priority).IsRequired();
                b.Property(x => x.Status).IsRequired();
                b.Property(x => x.IsDeleted).IsRequired();
                b.HasIndex(x => x.UserId);
                b.HasIndex(x => new { x.UserId, x.DueDate });
                b.HasIndex(x => new { x.UserId, x.Status });
                b.HasIndex(x => new { x.UserId, x.CategoryId });
                b.HasOne(x => x.RecurrenceRule).WithOne(r => r.TodoTask).HasForeignKey<RecurrenceRule>(r => r.TodoTaskId).OnDelete(DeleteBehavior.Cascade);
                b.HasMany(x => x.Occurrences).WithOne(o => o.TodoTask).HasForeignKey(o => o.TodoTaskId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RecurrenceRule>(b =>
            {
                b.ToTable("RecurrenceRules");
                b.HasKey(x => x.Id);
                b.Property(x => x.UserId).IsRequired().HasMaxLength(450);
                b.Property(x => x.Type).IsRequired();
                b.Property(x => x.Interval).IsRequired();
                b.HasIndex(x => x.UserId);
                b.HasIndex(x => x.TodoTaskId).IsUnique();
            });

            modelBuilder.Entity<TaskOccurrence>(b =>
            {
                b.ToTable("TaskOccurrences");
                b.HasKey(x => x.Id);
                b.Property(x => x.OccurrenceDate).IsRequired();
                b.Property(x => x.OriginalOccurrenceDate);
                b.Property(x => x.Status).IsRequired();
                b.Property(x => x.IsDeleted).IsRequired();
                b.HasIndex(x => new { x.TodoTaskId, x.OccurrenceDate });
                b.HasIndex(x => new { x.TodoTaskId, x.RecurrenceRuleId, x.OriginalOccurrenceDate }).IsUnique();
                b.HasOne(x => x.RecurrenceRule).WithMany().HasForeignKey(x => x.RecurrenceRuleId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<TaskAttachment>(b =>
            {
                b.ToTable("TaskAttachments");
                b.HasKey(x => x.Id);
                b.Property(x => x.FileName).IsRequired().HasMaxLength(255);
                b.Property(x => x.FilePath).IsRequired().HasMaxLength(1000);
                b.Property(x => x.ContentType).IsRequired().HasMaxLength(100);
                b.HasOne(x => x.TodoTask).WithMany(t => t.Attachments).HasForeignKey(x => x.TodoTaskId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
