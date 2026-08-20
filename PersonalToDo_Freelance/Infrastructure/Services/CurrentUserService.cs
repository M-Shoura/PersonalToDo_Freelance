using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PersonalToDo_Freelance.Application.Interfaces;

namespace PersonalToDo_Freelance.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public CurrentUserService(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public string? UserId => _contextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public ClaimsPrincipal? User => _contextAccessor.HttpContext?.User;
    }
}
