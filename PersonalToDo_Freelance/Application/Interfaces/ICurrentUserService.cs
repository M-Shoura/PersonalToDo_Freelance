using System.Security.Claims;

namespace PersonalToDo_Freelance.Application.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        ClaimsPrincipal? User { get; }
    }
}
