using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PersonalToDo_Freelance.Application.Interfaces;
using PersonalToDo_Freelance.Data;
using PersonalToDo_Freelance.Domain.Entities;

namespace PersonalToDo_Freelance.Infrastructure.Services
{
    public class AttachmentService : IAttachmentService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _user;
        private readonly IWebHostEnvironment _env;

        public AttachmentService(ApplicationDbContext db, ICurrentUserService user, IWebHostEnvironment env)
        {
            _db = db;
            _user = user;
            _env = env;
        }

        public async Task<(bool Succeeded, string? Error, TaskAttachment? Attachment)> UploadAttachmentAsync(long taskId, IFormFile file)
        {
            if (file == null || file.Length == 0) return (false, "File is empty.", null);

            var userId = _user.UserId ?? string.Empty;
            var task = await _db.Tasks.Where(t => t.Id == taskId && t.UserId == userId && !t.IsDeleted).FirstOrDefaultAsync();
            if (task == null) return (false, "Task not found.", null);

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var attachment = new TaskAttachment
            {
                TodoTaskId = taskId,
                FileName = file.FileName,
                FilePath = "/uploads/" + uniqueFileName,
                ContentType = file.ContentType,
                SizeBytes = file.Length,
                CreatedAt = DateTime.UtcNow,
                UploadedAt = DateTime.UtcNow
            };

            _db.TaskAttachments.Add(attachment);
            await _db.SaveChangesAsync();

            return (true, null, attachment);
        }

        public async Task<IReadOnlyList<TaskAttachment>> GetTaskAttachmentsAsync(long taskId)
        {
            var userId = _user.UserId ?? string.Empty;
            return await _db.TaskAttachments
                .Include(a => a.TodoTask)
                .Where(a => a.TodoTaskId == taskId && a.TodoTask!.UserId == userId && !a.IsDeleted && !a.TodoTask.IsDeleted)
                .OrderByDescending(a => a.UploadedAt)
                .ToListAsync();
        }

        public async Task<(bool Succeeded, string? Error)> DeleteAttachmentAsync(long attachmentId)
        {
            var userId = _user.UserId ?? string.Empty;
            var attachment = await _db.TaskAttachments
                .Include(a => a.TodoTask)
                .Where(a => a.Id == attachmentId && a.TodoTask!.UserId == userId && !a.IsDeleted)
                .FirstOrDefaultAsync();

            if (attachment == null) return (false, "Attachment not found.");

            attachment.IsDeleted = true;
            attachment.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<TaskAttachment?> GetAttachmentAsync(long attachmentId)
        {
            var userId = _user.UserId ?? string.Empty;
            return await _db.TaskAttachments
                .Include(a => a.TodoTask)
                .Where(a => a.Id == attachmentId && a.TodoTask!.UserId == userId && !a.IsDeleted)
                .FirstOrDefaultAsync();
        }
    }
}
