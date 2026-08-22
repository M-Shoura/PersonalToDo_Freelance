using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PersonalToDo_Freelance.Data;
using Microsoft.AspNetCore.Builder;
using System.Linq;
using Microsoft.AspNetCore.TestHost;
using System.Collections.Generic;

namespace PersonalToDo_Freelance.Tests.IntegrationTests
{
    public class IntegrationTestFactory : WebApplicationFactory<PersonalToDo_Freelance.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Inject a unique LocalDB connection string so the app uses SQL Server against an isolated DB for tests
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var dict = new Dictionary<string, string>();
                var dbName = "IntegrationTestDb_" + System.Guid.NewGuid().ToString("N");
                dict["ConnectionStrings:DefaultConnection"] = $"Server=(localdb)\\mssqllocaldb;Database={dbName};Trusted_Connection=True;MultipleActiveResultSets=true";
                config.AddInMemoryCollection(dict);
            });

            // After the host is built, ensure the database is created
            builder.ConfigureTestServices(services =>
            {
                // Add a test authentication scheme to allow bypassing UI login in tests
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                }).AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });

                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    db.Database.EnsureCreated();
                }
            });
        }
    }
}
