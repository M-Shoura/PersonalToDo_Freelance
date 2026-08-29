using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PersonalToDo_Freelance.Domain.Entities;

namespace PersonalToDo_Freelance.Application.Interfaces
{
    public interface IAttachmentService
    {
        Task<(bool Succeeded, string? Error, TaskAttachment? Attachment)> UploadAttachmentAsync(long taskId, IFormFile file);
        Task<IReadOnlyList<TaskAttachment>> GetTaskAttachmentsAsync(long taskId);
        Task<(bool Succeeded, string? Error)> DeleteAttachmentAsync(long attachmentId);
        Task<TaskAttachment?> GetAttachmentAsync(long attachmentId);
    }
}
