using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersonalToDo_Freelance.Application.Interfaces;
using PersonalToDo_Freelance.Infrastructure.Services;

namespace PersonalToDo_Freelance.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Common infrastructure services
            services.AddHttpContextAccessor();

            // Current user accessor
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            // Add other infrastructure services here (email, file storage, etc.)

            return services;
        }
    }
}
