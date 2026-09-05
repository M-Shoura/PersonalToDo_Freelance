using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PersonalToDo_Freelance.Data;
using PersonalToDo_Freelance.Domain.Entities;
using PersonalToDo_Freelance.Domain.Enums;

class Program
{
    static void Main()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=PersonalToDoDb;Trusted_Connection=True;MultipleActiveResultSets=true";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;
        
        using var db = new ApplicationDbContext(options);
        
        var userId = "1faef5f4-8af9-48c9-b0ff-1cc0a0199754";
        
        // Insert a task due in 30 minutes to trigger the email reminder
        var task = new TodoTask
        {
            UserId = userId,
            Title = "Test Email Reminder Task",
            Description = "This task was created to test the email reminder system.",
            Priority = TaskPriority.High,
            Status = TodoTaskStatus.NotStarted,
            CreatedAt = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddMinutes(30),
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false,
            ReminderSentAt = null
        };
        
        db.Tasks.Add(task);
        db.SaveChanges();
        Console.WriteLine("Mock task for email reminder inserted successfully!");
        Console.WriteLine("Run the web application, and the ReminderWorker should pick it up immediately and send you an email.");
    }
}
