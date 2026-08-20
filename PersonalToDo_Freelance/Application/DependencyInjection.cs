using Microsoft.Extensions.DependencyInjection;

namespace PersonalToDo_Freelance.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register application services, validators, and mappers here.
            // Example: services.AddScoped<ITaskService, TaskService>();

            return services;
        }
    }
}
