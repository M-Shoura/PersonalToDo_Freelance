using System.Threading.Tasks;

namespace PersonalToDo_Freelance.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }
}
