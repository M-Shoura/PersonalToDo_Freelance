using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PersonalToDo_Freelance.Data;
using PersonalToDo_Freelance.Domain.Entities;
using PersonalToDo_Freelance.Domain.Enums;
using PersonalToDo_Freelance.Infrastructure.Services;

class Program
{
    static void Main()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase("dbg_stats").Options;
        using var db = new ApplicationDbContext(options);
        var start = new DateTime(2026,8,1);
        var end = new DateTime(2026,8,7);
        db.Tasks.Add(new TodoTask { UserId = "u1", Title = "C1", CreatedAt = start.AddDays(1), DueDate = start.AddDays(2), Priority = TaskPriority.Medium, Status = TodoTaskStatus.NotStarted });
        db.Tasks.Add(new TodoTask { UserId = "u1", Title = "C2", CreatedAt = start.AddDays(2), DueDate = start.AddDays(3), Priority = TaskPriority.High, Status = TodoTaskStatus.Completed, CompletedAt = start.AddDays(3) });
        db.Tasks.Add(new TodoTask { UserId = "u1", Title = "Old", CreatedAt = start.AddDays(-5), DueDate = start.AddDays(2), Priority = TaskPriority.Low, Status = TodoTaskStatus.NotStarted });
        db.Tasks.Add(new TodoTask { UserId = "u1", Title = "OD", CreatedAt = start.AddDays(1), DueDate = start.AddDays(-1), Status = TodoTaskStatus.NotStarted });
        db.Tasks.Add(new TodoTask { UserId = "other", Title = "Other", CreatedAt = start.AddDays(2), DueDate = start.AddDays(2), Status = TodoTaskStatus.NotStarted });
        db.SaveChanges();

        var svc = new TaskService(db, new SimpleCurrentUser { UserId = "u1" });
        var stats = svc.GetStatisticsAsync(start, end).GetAwaiter().GetResult();
        Console.WriteLine($"TasksCreated: {stats.TasksCreated}");
        Console.WriteLine($"TasksCompleted: {stats.TasksCompleted}");
        Console.WriteLine($"PendingTasks: {stats.PendingTasks}");
        Console.WriteLine($"OverdueTasks: {stats.OverdueTasks}");
        Console.WriteLine($"CompletedPerDay count: {stats.CompletedPerDay.Count}");
    }
}

class SimpleCurrentUser : PersonalToDo_Freelance.Application.Interfaces.ICurrentUserService
{
    public string? UserId { get; set; }
    public System.Security.Claims.ClaimsPrincipal? User => null;
}
