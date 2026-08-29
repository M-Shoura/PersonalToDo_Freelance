using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersonalToDo_Freelance.Application.Interfaces;
using PersonalToDo_Freelance.Infrastructure.Services;
using PersonalToDo_Freelance.Infrastructure.Workers;

namespace PersonalToDo_Freelance.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpContextAccessor();

            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<ITaskOccurrenceService, TaskOccurrenceService>();
            services.AddTransient<IEmailService, SmtpEmailService>();

            services.AddHostedService<ReminderWorker>();

            return services;
        }
    }
}
