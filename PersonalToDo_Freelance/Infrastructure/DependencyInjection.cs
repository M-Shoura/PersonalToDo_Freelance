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
            services.AddHttpContextAccessor();

            services.AddScoped<ICurrentUserService, CurrentUserService>();

            return services;
        }
    }
}
